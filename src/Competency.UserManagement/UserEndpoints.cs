using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Competency.UserManagement;

/// <summary>
/// The account API. Permission matrix: every operation needs a global administrator, so a signed-in user without the role is refused
/// with 403 whichever account the request names, and an unknown account answers 404 only to an administrator.
/// Changes carry <c>If-Match</c>. Creating, changing, blocking and unblocking an account run under the employee tree lock, taken first,
/// so that no account is ever bound to or unblocked with an employee who is being dismissed; those that could remove an administrator
/// also lock the active administrators, after it. A password reset and its audit event are saved together.
/// A new account is invited: it has no password and is e-mailed a link to set one. A repeated invitation locks only the account row.
/// Invitations are e-mailed after the change is committed and its locks released, and the response tells whether the mail server accepted them.
/// Accounts are never deleted: blocking ends them. Blocking, a password reset, and a change of role or employee end the account's sessions.
/// </summary>
internal static class UserEndpoints
{
    private const string Route = "/api/v1/users";
    private const string Tag = "Users";
    private const string PasswordResetAction = "AppUser.PasswordReset";
    private const string ResendNeedsInvitedAccount =
        "Приглашение можно отправить повторно только учётной записи, которая ещё не задала пароль и не заблокирована.";
    private const string UnblockNeedsWorkingEmployee =
        "Нельзя разблокировать учётную запись, пока привязанный сотрудник не работает. Сначала верните сотрудника на работу или отвяжите от него учётную запись.";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var administrators = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        administrators.MapGet(string.Empty, ListAsync).WithName("ListUsers");
        administrators.MapGet("/{id:guid}", GetAsync).WithName("GetUser").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
        administrators.MapPost(string.Empty, CreateAsync).WithName("CreateUser").ProducesETag()
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateUser").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/block", BlockAsync).WithName("BlockUser").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/unblock", UnblockAsync).WithName("UnblockUser").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/resend-invitation", ResendInvitationAsync).WithName("ResendUserInvitation").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        administrators.MapPost("/{id:guid}/reset-password", ResetPasswordAsync).WithName("ResetUserPassword").ProducesETag()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// A page of accounts; each row carries the employee's name, looked up through the directory because the employee table belongs to another module.
    /// </summary>
    private static async Task<Results<Ok<PageResponse<UserResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] UserListQuery query,
        AppDbContext context,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var users = context.Set<AppUser>().AsNoTracking();
        if (query.Role is { } role)
        {
            users = users.Where(user => user.Role == role);
        }

        if (query.IsBlocked is { } isBlocked)
        {
            users = users.Where(user => user.IsBlocked == isBlocked);
        }

        if (query.IsInvited is { } isInvited)
        {
            users = isInvited ? users.Where(user => user.PasswordHash == null) : users.Where(user => user.PasswordHash != null);
        }

        if (query.SearchPattern() is { } pattern)
        {
            users = users.Where(user => EF.Functions.ILike(user.Email!, pattern, ListRequest.LikeEscape));
        }

        var total = await users.CountAsync(cancellationToken);
        var rows = await users
            .OrderBy(user => user.NormalizedEmail)
            .ThenBy(user => user.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(UserResponse.Projection)
            .ToListAsync(cancellationToken);

        var statuses = await employees.FindManyAsync([.. rows.Select(row => row.EmployeeId).OfType<Guid>().Distinct()], cancellationToken);
        var items = rows.ConvertAll(row => row with
        {
            EmployeeName = row.EmployeeId is { } id && statuses.TryGetValue(id, out var status) ? status.FullName : null,
        });
        return TypedResults.Ok(new PageResponse<UserResponse>(items, total, query.Page, query.PageSize));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var user = await context.Set<AppUser>()
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(UserResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        response.SetETag(user.Version);
        return TypedResults.Ok(user with { EmployeeName = await employees.NameAsync(user.EmployeeId, cancellationToken) });
    }

    private static async Task<Results<Created<UserResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
        CreateUserRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        AccountMail mail,
        IOptions<AccountLinkOptions> links,
        TimeProvider time,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        if (request.EmployeeId is { } employeeId)
        {
            if (await employees.ProblemAsync(employeeId, cancellationToken) is { } problem)
            {
                return Rejection.Invalid("employeeId", problem);
            }

            if (await context.IsBoundElsewhereAsync(employeeId, Guid.Empty, cancellationToken))
            {
                return Rejection.Conflict(EmployeeBinding.AlreadyBound);
            }
        }

        var user = new AppUser { Role = request.Role, EmployeeId = request.EmployeeId };
        user.SetEmail(request.Email.Trim());
        var result = await users.CreateAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        var token = user.IssueLink(time.GetUtcNow(), links.Value.InvitationLifetime);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var mailSent = await mail.SendInvitationAsync(user, token, isRepeat: false, cancellationToken);
        var created = await RespondAsync(user, employees, response, cancellationToken) with { MailSent = mailSent };
        return TypedResults.Created($"{Route}/{created.Id}", created);
    }

    /// <summary>
    /// Replaces the link of an account that has not set its password yet and e-mails it again, so the earlier invitation no longer works.
    /// </summary>
    private static async Task<Results<Ok<UserResponse>, NotFound, ProblemHttpResult>> ResendInvitationAsync(
        Guid id,
        IfMatch ifMatch,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        AccountMail mail,
        IOptions<AccountLinkOptions> links,
        TimeProvider time,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginAccountLockAsync(id, cancellationToken);
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (!user.IsInvited || user.IsBlocked)
        {
            return Rejection.Conflict(ResendNeedsInvitedAccount);
        }

        ifMatch.ApplyTo(context, user);
        var token = user.IssueLink(time.GetUtcNow(), links.Value.InvitationLifetime);
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        await transaction.CommitAsync(cancellationToken);
        var mailSent = await mail.SendInvitationAsync(user, token, isRepeat: true, cancellationToken);
        return TypedResults.Ok(await RespondAsync(user, employees, response, cancellationToken) with { MailSent = mailSent });
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        IfMatch ifMatch,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await context.BeginExclusiveAsync(employees, cancellationToken);
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (request.EmployeeId is { } employeeId && employeeId != user.EmployeeId)
        {
            if (await employees.ProblemAsync(employeeId, cancellationToken) is { } problem)
            {
                return Rejection.Invalid("employeeId", problem);
            }

            if (await context.IsBoundElsewhereAsync(employeeId, user.Id, cancellationToken))
            {
                return Rejection.Conflict(EmployeeBinding.AlreadyBound);
            }
        }

        if (user.IsActiveAdministrator() && request.Role != UserRole.GlobalAdmin
            && !await context.HasOtherAsync(user.Id, cancellationToken))
        {
            return Rejection.Conflict(ActiveAdministrators.LastOneMessage);
        }

        ifMatch.ApplyTo(context, user);
        var endsSessions = user.Role != request.Role || user.EmployeeId != request.EmployeeId;
        user.SetEmail(request.Email.Trim());
        user.Role = request.Role;
        user.EmployeeId = request.EmployeeId;

        var result = endsSessions ? await users.UpdateSecurityStampAsync(user) : await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(await RespondAsync(user, employees, response, cancellationToken));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ProblemHttpResult>> BlockAsync(
        Guid id,
        IfMatch ifMatch,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginExclusiveAsync(employees, cancellationToken);
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (user.IsActiveAdministrator() && !await context.HasOtherAsync(user.Id, cancellationToken))
        {
            return Rejection.Conflict(ActiveAdministrators.LastOneMessage);
        }

        ifMatch.ApplyTo(context, user);
        user.IsBlocked = true;
        var result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(await RespondAsync(user, employees, response, cancellationToken));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ProblemHttpResult>> UnblockAsync(
        Guid id,
        IfMatch ifMatch,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        if (user.EmployeeId is { } employeeId && await employees.ProblemAsync(employeeId, cancellationToken) is not null)
        {
            return Rejection.Conflict(UnblockNeedsWorkingEmployee);
        }

        ifMatch.ApplyTo(context, user);
        user.IsBlocked = false;
        user.ClearLockout();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(await RespondAsync(user, employees, response, cancellationToken));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ValidationProblem, ProblemHttpResult>> ResetPasswordAsync(
        Guid id,
        ResetPasswordRequest request,
        IfMatch ifMatch,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        IAuditWriter audit,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var policy = await users.ValidatePasswordAsync(user, request.NewPassword);
        if (!policy.Succeeded)
        {
            return Rejections.From(policy);
        }

        ifMatch.ApplyTo(context, user);
        user.PasswordHash = users.PasswordHasher.HashPassword(user, request.NewPassword);
        user.ClearLockout();
        audit.Stage(new AuditEntry(PasswordResetAction, nameof(AppUser), user.Id.ToString()), context);
        var result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        return TypedResults.Ok(await RespondAsync(user, employees, response, cancellationToken));
    }

    private static async Task<UserResponse> RespondAsync(
        AppUser user,
        IEmployeeDirectory employees,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        response.SetETag(user.Version);
        return UserResponse.From(user, await employees.NameAsync(user.EmployeeId, cancellationToken));
    }
}
