using System.Security.Claims;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.UserManagement;

/// <summary>
/// The cookie session: how it is configured, which claims it carries, and the check every request makes
/// so that blocking an account, changing its credentials or role, or a leaving employee ends the session at once.
/// </summary>
internal static class SessionAuthentication
{
    public const string Scheme = "Session";

    private const string CookieSection = "Authentication:Cookie";
    private const string SecurityStampClaim = "security_stamp";

    public static void Configure(CookieAuthenticationOptions options, IConfiguration configuration)
    {
        var settings = configuration.GetRequiredSection(CookieSection).Get<CookieSettings>()!;
        options.Cookie.Name = settings.Name;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = settings.SecurePolicy;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = settings.ExpireTimeSpan;
        options.Events.OnValidatePrincipal = ValidateAsync;
        options.Events.OnRedirectToLogin = context => RespondWithStatus(context.Response, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => RespondWithStatus(context.Response, StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Builds the principal a session carries: the account, its role, its employee when bound, and the security stamp the session was issued under.
    /// </summary>
    /// <param name="user">The account that signed in.</param>
    /// <returns>The authenticated principal to sign in with.</returns>
    public static ClaimsPrincipal CreatePrincipal(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(PlatformClaims.Subject, user.Id.ToString()),
            new(PlatformClaims.Role, user.Role.ToString()),
            new(SecurityStampClaim, user.SecurityStamp!),
        };
        if (user.EmployeeId is { } employeeId)
        {
            claims.Add(new Claim(PlatformClaims.EmployeeId, employeeId.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme, nameType: null, roleType: PlatformClaims.Role));
    }

    private static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        if (!await IsSessionValidAsync(context))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(Scheme);
        }
    }

    private static async Task<bool> IsSessionValidAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirstValue(PlatformClaims.Subject), out var userId))
        {
            return false;
        }

        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        var account = await services.GetRequiredService<AppDbContext>().Set<AppUser>()
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new Account(user.SecurityStamp, user.IsBlocked, user.EmployeeId))
            .FirstOrDefaultAsync(cancellationToken);
        if (account is null || account.IsBlocked || account.SecurityStamp != principal!.FindFirstValue(SecurityStampClaim))
        {
            return false;
        }

        return account.EmployeeId is not { } employeeId
            || await services.GetRequiredService<IEmployeeDirectory>().FindAsync(employeeId, cancellationToken) is { IsActive: true };
    }

    private static Task RespondWithStatus(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
    }

    private sealed record Account(string? SecurityStamp, bool IsBlocked, Guid? EmployeeId);

    private sealed class CookieSettings
    {
        public string Name { get; set; } = null!;

        public CookieSecurePolicy SecurePolicy { get; set; }

        public TimeSpan ExpireTimeSpan { get; set; }
    }
}
