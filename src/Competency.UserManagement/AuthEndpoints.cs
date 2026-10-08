using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Competency.UserManagement;

/// <summary>
/// The session API. Permission matrix: signing in, asking for a password reset link, and setting a password from an invitation or reset link
/// are anonymous and rate limited per client address; reading the password policy is anonymous and not rate limited; signing out, reading the own account and changing the own password need any signed-in user
/// and act on the account in the session, never on one named in the request.
/// Every failed sign-in answers with the same response, whether the account is unknown, the password wrong, the account locked,
/// blocked, invited and not yet registered, or its employee gone, so the response reveals nothing about accounts; so does every request for a reset link.
/// Any unusable link of one kind gets one and the same response, which never says why. Events are audited without passwords or links.
/// </summary>
internal static class AuthEndpoints
{
    private const string Route = "/api/v1/auth";
    private const string Tag = "Auth";
    private const string LoginFailedTitle = "Не удалось войти";
    private const string LoginFailedDetail = "Проверьте e-mail и пароль. Если вход не удаётся, обратитесь к администратору.";
    private const string UnknownActor = "unknown";
    private const string AnonymousRole = "anonymous";
    private const string LoginSucceededAction = "Auth.LoginSucceeded";
    private const string LoginFailedAction = "Auth.LoginFailed";
    private const string LockedOutAction = "Auth.LockedOut";
    private const string LogoutAction = "Auth.Logout";
    private const string PasswordChangedAction = "Auth.PasswordChanged";
    private const string RegistrationCompletedAction = "Auth.RegistrationCompleted";
    private const string PasswordResetCompletedAction = "Auth.PasswordResetCompleted";
    private const string InvalidLinkTitle = "Ссылка недействительна";
    private const string InvalidInvitationDetail = "Ссылка устарела или уже использована. Попросите администратора отправить приглашение повторно.";
    private const string InvalidResetDetail = "Ссылка устарела или уже использована. Запросите новую ссылку на экране входа: «Не помню пароль».";

    private static readonly LinkUse Invitation = new(ForInvitedAccount: true, RegistrationCompletedAction, InvalidInvitationDetail);
    private static readonly LinkUse PasswordReset = new(ForInvitedAccount: false, PasswordResetCompletedAction, InvalidResetDetail);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/login", LoginAsync).WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(PlatformModule.LoginRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        var links = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .AllowAnonymous()
            .RequireRateLimiting(PlatformModule.PasswordResetRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        links.MapPost("/forgot-password", ForgotPassword).WithName("ForgotPassword");
        links.MapPost("/reset-password", ResetPasswordAsync).WithName("ResetPassword")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        links.MapPost("/accept-invitation", AcceptInvitationAsync).WithName("AcceptInvitation")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapGet(Route + "/password-policy", GetPasswordPolicy).WithName("GetPasswordPolicy")
            .WithTags(Tag)
            .AllowAnonymous();

        var signedIn = auth.MapGroup(string.Empty).RequireAuthorization(AuthorizationPolicies.Authenticated);
        signedIn.MapPost("/logout", LogoutAsync).WithName("Logout");
        signedIn.MapGet("/me", GetCurrentAsync).WithName("GetCurrentUser");
        signedIn.MapPost("/change-password", ChangePasswordAsync).WithName("ChangePassword");
    }

    /// <summary>
    /// Why a sign-in attempt was refused; recorded in the journal, never sent to the client.
    /// </summary>
    private enum LoginDenial
    {
        UnknownUser,
        Invited,
        LockedOut,
        WrongPassword,
        LockoutStarted,
        Blocked,
        EmployeeInactive,
    }

    /// <summary>
    /// What an e-mailed link sets a password for: the account state it needs, the event it journals and the refusal it answers with.
    /// </summary>
    private sealed record LinkUse(bool ForInvitedAccount, string Action, string InvalidLinkDetail)
    {
        public bool Accepts(AppUser user, byte[] tokenHash, TimeProvider time) =>
            user.IsInvited == ForInvitedAccount && user.HasValidLink(tokenHash, time.GetUtcNow());
    }

    private static async Task<Results<Ok<CurrentUserResponse>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IEmployeeDirectory employees,
        IAuditWriter audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            SpendHashingTime(users, request.Password);
            return await DenyAsync(audit, null, LoginDenial.UnknownUser, cancellationToken);
        }

        if (user.IsInvited)
        {
            SpendHashingTime(users, request.Password);
            return await DenyAsync(audit, user, LoginDenial.Invited, cancellationToken);
        }

        var denial = await VerifyPasswordAsync(users, context, user, request.Password, cancellationToken);
        var employee = denial is null && user.EmployeeId is { } employeeId
            ? await employees.FindAsync(employeeId, cancellationToken)
            : null;
        denial ??= CheckAccount(user, employee);
        if (denial is { } reason)
        {
            return await DenyAsync(audit, user, reason, cancellationToken);
        }

        await httpContext.SignInAsync(SessionAuthentication.Scheme, SessionAuthentication.CreatePrincipal(user));
        await audit.WriteAsync(
            new AuditEntry(LoginSucceededAction, nameof(AppUser), user.Id.ToString(), Actor: user.Id.ToString(), Role: user.Role.ToString()),
            cancellationToken);
        return TypedResults.Ok(CurrentUserResponse.From(user, employee));
    }

    /// <summary>
    /// Tells the shortest accepted password length, read from the identity options so that the interface never repeats the setting.
    /// </summary>
    private static Ok<PasswordPolicyResponse> GetPasswordPolicy(IOptions<IdentityOptions> options) =>
        TypedResults.Ok(new PasswordPolicyResponse(options.Value.Password.RequiredLength));

    private static async Task<NoContent> LogoutAsync(HttpContext httpContext, ICurrentActor actor, IAuditWriter audit, CancellationToken cancellationToken)
    {
        await httpContext.SignOutAsync(SessionAuthentication.Scheme);
        await audit.WriteAsync(new AuditEntry(LogoutAction, nameof(AppUser), actor.UserId?.ToString()), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CurrentUserResponse>, UnauthorizedHttpResult>> GetCurrentAsync(
        HttpContext httpContext,
        UserManager<AppUser> users,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var employee = user.EmployeeId is { } employeeId ? await employees.FindAsync(employeeId, cancellationToken) : null;
        return TypedResults.Ok(CurrentUserResponse.From(user, employee));
    }

    /// <summary>
    /// Replaces the signed-in user's password and voids any password reset link. Every other session of the user ends; the current one continues under the new security stamp.
    /// The current password is checked with the sign-in lockout accounting, so that a stolen session cannot guess it without limit:
    /// a wrong one counts as a failed attempt, a lockout it starts is audited as at sign-in, and a locked account is refused without checking. The new password and its audit event are saved together.
    /// </summary>
    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> ChangePasswordAsync(
        ChangePasswordRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IAuditWriter audit,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var user = await users.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        if (await VerifyPasswordAsync(users, context, user, request.CurrentPassword, cancellationToken) is { } denial)
        {
            if (denial == LoginDenial.LockoutStarted)
            {
                await AuditLockoutAsync(audit, user, cancellationToken);
            }

            return Rejections.From(IdentityResult.Failed(users.ErrorDescriber.PasswordMismatch()));
        }

        var policy = await users.ValidatePasswordAsync(user, request.NewPassword);
        if (!policy.Succeeded)
        {
            return Rejections.From(policy);
        }

        user.PasswordHash = users.PasswordHasher.HashPassword(user, request.NewPassword);
        user.ClearLockout();
        user.ClearLink();
        audit.Stage(new AuditEntry(PasswordChangedAction, nameof(AppUser), user.Id.ToString()), context);
        var result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            return Rejections.From(result);
        }

        await httpContext.SignInAsync(SessionAuthentication.Scheme, SessionAuthentication.CreatePrincipal(user));
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Asks for a password reset link. The answer is the same 202 at once for any well-formed address: the link is issued and e-mailed later,
    /// only to an account that may reset its password, so neither the answer nor its timing tells whether such an account exists.
    /// </summary>
    private static Results<Accepted, ValidationProblem> ForgotPassword(
        ForgotPasswordRequest request,
        PasswordResetQueue queue,
        ICurrentActor actor)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        queue.Enqueue(request.Email.Trim(), actor.RequestId);
        return TypedResults.Accepted((string?)null);
    }

    /// <summary>
    /// Sets the first password of an invited account from its invitation link and spends the link; the user then signs in as usual.
    /// </summary>
    private static Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> AcceptInvitationAsync(
        LinkPasswordRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IAuditWriter audit,
        TimeProvider time,
        CancellationToken cancellationToken) =>
        SetPasswordByLinkAsync(Invitation, request, users, context, audit, time, cancellationToken);

    /// <summary>
    /// Replaces the password of a registered account from its password reset link and spends the link. It ends a lockout from wrong passwords,
    /// but neither unblocks the account nor lets in a user whose employee no longer works.
    /// </summary>
    private static Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ResetPasswordAsync(
        LinkPasswordRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IAuditWriter audit,
        TimeProvider time,
        CancellationToken cancellationToken) =>
        SetPasswordByLinkAsync(PasswordReset, request, users, context, audit, time, cancellationToken);

    /// <summary>
    /// Sets the password from an e-mailed link and ends every session of the account.
    /// The password is hashed first; then the account row is locked and re-read, the link checked again against the committed state,
    /// and the password, the cleared lockout and link, a new security stamp and the audit event are saved together, so a link is spent once
    /// and a concurrent new link, block or password change either voids it first or comes after. Nothing else is requested while the row is locked.
    /// </summary>
    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> SetPasswordByLinkAsync(
        LinkUse use,
        LinkPasswordRequest request,
        UserManager<AppUser> users,
        AppDbContext context,
        IAuditWriter audit,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return InvalidLink(use);
        }

        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var tokenHash = AppUser.HashLinkToken(request.Token);
        var user = await context.Set<AppUser>().FirstOrDefaultAsync(candidate => candidate.LinkTokenHash == tokenHash, cancellationToken);
        if (user is null || !use.Accepts(user, tokenHash, time))
        {
            return InvalidLink(use);
        }

        var policy = await users.ValidatePasswordAsync(user, request.Password);
        if (!policy.Succeeded)
        {
            return Rejections.From(policy, "password");
        }

        var passwordHash = users.PasswordHasher.HashPassword(user, request.Password);
        await using var transaction = await context.BeginAccountLockAsync(user.Id, cancellationToken);
        await context.Entry(user).ReloadAsync(cancellationToken);
        if (!use.Accepts(user, tokenHash, time))
        {
            return InvalidLink(use);
        }

        user.PasswordHash = passwordHash;
        user.ClearLockout();
        user.ClearLink();
        user.EndSessions();
        audit.Stage(
            new AuditEntry(use.Action, nameof(AppUser), user.Id.ToString(), Actor: user.Id.ToString(), Role: user.Role.ToString()),
            context);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Refuses an unusable link with 400 rather than 401, which the application would take for an ended session.
    /// </summary>
    private static ProblemHttpResult InvalidLink(LinkUse use) =>
        TypedResults.Problem(detail: use.InvalidLinkDetail, statusCode: StatusCodes.Status400BadRequest, title: InvalidLinkTitle);

    /// <summary>
    /// Verifies the password with lockout accounting, one attempt at a time per account: the account row is locked and re-read first,
    /// so concurrent attempts are counted one after another against the committed state and only the attempt that reaches the limit
    /// starts the lockout. A locked account is refused without checking, a wrong password counts as a failed attempt and a correct one
    /// clears the count. The row lock is the only lock this method takes: nothing is held while waiting for it and nothing else is requested
    /// while holding it, so it cannot deadlock with the employee tree and administrator locks; it ends before the method returns.
    /// </summary>
    private static async Task<LoginDenial?> VerifyPasswordAsync(
        UserManager<AppUser> users,
        AppDbContext context,
        AppUser user,
        string password,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginAccountLockAsync(user.Id, cancellationToken);
        await context.Entry(user).ReloadAsync(cancellationToken);
        var denial = await CountAttemptAsync(users, user, password);
        await transaction.CommitAsync(cancellationToken);
        return denial;
    }

    /// <summary>
    /// Counts one attempt on the account as it is under the row lock of <see cref="VerifyPasswordAsync"/>.
    /// A count that cannot be saved fails the request instead of being taken for counted.
    /// </summary>
    private static async Task<LoginDenial?> CountAttemptAsync(UserManager<AppUser> users, AppUser user, string password)
    {
        if (await users.IsLockedOutAsync(user))
        {
            SpendHashingTime(users, password);
            return LoginDenial.LockedOut;
        }

        if (await users.CheckPasswordAsync(user, password))
        {
            EnsureSaved(await users.ResetAccessFailedCountAsync(user), user);
            return null;
        }

        EnsureSaved(await users.AccessFailedAsync(user), user);
        return await users.IsLockedOutAsync(user) ? LoginDenial.LockoutStarted : LoginDenial.WrongPassword;
    }

    private static void EnsureSaved(IdentityResult result, AppUser user)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Saving the sign-in attempt count of account {user.Id} failed: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
    }

    private static LoginDenial? CheckAccount(AppUser user, EmployeeStatus? employee)
    {
        if (user.IsBlocked)
        {
            return LoginDenial.Blocked;
        }

        return user.EmployeeId is not null && employee is not { IsActive: true } ? LoginDenial.EmployeeInactive : null;
    }

    /// <summary>
    /// Hashes the password once without using the result, so that refusals that skip the password check take as long as those that do,
    /// and response time does not tell an unknown or locked account from a known one.
    /// </summary>
    private static void SpendHashingTime(UserManager<AppUser> users, string password) =>
        users.PasswordHasher.HashPassword(new AppUser(), password);

    /// <summary>
    /// Journals a refused attempt under the account's own identity, or under <c>unknown</c> when no such account exists,
    /// so that nothing the client typed, which may be a password in the wrong field, enters the journal.
    /// </summary>
    private static async Task<ProblemHttpResult> DenyAsync(IAuditWriter audit, AppUser? user, LoginDenial denial, CancellationToken cancellationToken)
    {
        var actor = user?.Id.ToString() ?? UnknownActor;
        var role = user?.Role.ToString() ?? AnonymousRole;
        var reason = denial == LoginDenial.LockoutStarted ? LoginDenial.WrongPassword : denial;
        await audit.WriteAsync(
            new AuditEntry(LoginFailedAction, nameof(AppUser), user?.Id.ToString(), Reason: reason.ToString(), Actor: actor, Role: role),
            cancellationToken);
        if (denial == LoginDenial.LockoutStarted && user is not null)
        {
            await AuditLockoutAsync(audit, user, cancellationToken);
        }

        return TypedResults.Problem(detail: LoginFailedDetail, statusCode: StatusCodes.Status401Unauthorized, title: LoginFailedTitle);
    }

    /// <summary>
    /// Journals the lockout that the account's failed attempt just started, from a sign-in or from a password change.
    /// </summary>
    private static Task AuditLockoutAsync(IAuditWriter audit, AppUser user, CancellationToken cancellationToken) =>
        audit.WriteAsync(
            new AuditEntry(LockedOutAction, nameof(AppUser), user.Id.ToString(), Actor: user.Id.ToString(), Role: user.Role.ToString()),
            cancellationToken);
}
