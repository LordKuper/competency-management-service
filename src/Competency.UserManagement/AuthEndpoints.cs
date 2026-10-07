using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace Competency.UserManagement;

/// <summary>
/// The session API. Permission matrix: signing in is anonymous and rate limited; signing out, reading the own account and
/// changing the own password need any signed-in user and act on the account in the session, never on one named in the request.
/// Every failed sign-in answers with the same response, whether the account is unknown, the password wrong, the account locked,
/// blocked or its employee gone, so the response reveals nothing about accounts. Events are audited without passwords.
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

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/login", LoginAsync).WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(PlatformModule.LoginRateLimitPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

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
        LockedOut,
        WrongPassword,
        LockoutStarted,
        Blocked,
        EmployeeInactive,
    }

    private static async Task<Results<Ok<CurrentUserResponse>, ValidationProblem, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        UserManager<AppUser> users,
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

        var denial = await VerifyPasswordAsync(users, user, request.Password);
        var employee = denial is null && user.EmployeeId is { } employeeId
            ? await employees.FindAsync(employeeId, cancellationToken)
            : null;
        denial ??= CheckAccount(user, employee);
        if (denial is { } reason)
        {
            return await DenyAsync(audit, user, reason, cancellationToken);
        }

        await users.ResetAccessFailedCountAsync(user);
        await httpContext.SignInAsync(SessionAuthentication.Scheme, SessionAuthentication.CreatePrincipal(user));
        await audit.WriteAsync(
            new AuditEntry(LoginSucceededAction, nameof(AppUser), user.Id.ToString(), Actor: user.Id.ToString(), Role: user.Role.ToString()),
            cancellationToken);
        return TypedResults.Ok(CurrentUserResponse.From(user, employee));
    }

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
    /// Replaces the signed-in user's password. Every other session of the user ends; the current one continues under the new security stamp.
    /// The current password is checked with the sign-in lockout accounting, so that a stolen session cannot guess it without limit:
    /// a wrong one counts as a failed attempt and a locked account is refused without checking. The new password and its audit event are saved together.
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

        if (await VerifyPasswordAsync(users, user, request.CurrentPassword) is not null)
        {
            return Rejections.From(IdentityResult.Failed(users.ErrorDescriber.PasswordMismatch()));
        }

        var policy = await users.ValidatePasswordAsync(user, request.NewPassword);
        if (!policy.Succeeded)
        {
            return Rejections.From(policy);
        }

        user.PasswordHash = users.PasswordHasher.HashPassword(user, request.NewPassword);
        user.ClearLockout();
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
    /// Verifies the password with lockout accounting: a locked account is refused without checking, a wrong password counts
    /// as a failed attempt, and the failed attempt that reaches the limit starts the lockout.
    /// </summary>
    private static async Task<LoginDenial?> VerifyPasswordAsync(UserManager<AppUser> users, AppUser user, string password)
    {
        if (await users.IsLockedOutAsync(user))
        {
            SpendHashingTime(users, password);
            return LoginDenial.LockedOut;
        }

        if (await users.CheckPasswordAsync(user, password))
        {
            return null;
        }

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? LoginDenial.LockoutStarted : LoginDenial.WrongPassword;
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
        if (denial == LoginDenial.LockoutStarted)
        {
            await audit.WriteAsync(
                new AuditEntry(LockedOutAction, nameof(AppUser), user?.Id.ToString(), Actor: actor, Role: role),
                cancellationToken);
        }

        return TypedResults.Problem(detail: LoginFailedDetail, statusCode: StatusCodes.Status401Unauthorized, title: LoginFailedTitle);
    }
}
