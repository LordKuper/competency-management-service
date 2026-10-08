using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Competency.UserManagement;

/// <summary>
/// Creates the first global administrator from the environment when none is active, so a fresh system can be signed in to;
/// an invited administrator who has not set a password yet does not count, since no one can sign in as them.
/// An existing administrator is never touched, whatever the environment says; the password is never logged.
/// </summary>
internal sealed class AdminBootstrapper(
    AppDbContext context,
    UserManager<AppUser> users,
    IConfiguration configuration,
    ILogger<AdminBootstrapper> logger)
{
    private const string EmailSetting = "Bootstrap:AdminEmail";
    private const string PasswordSetting = "Bootstrap:AdminPassword";

    /// <summary>
    /// Creates the administrator unless one is already active.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check and the creation.</param>
    /// <exception cref="InvalidOperationException">No administrator is active and the environment does not provide a usable one.</exception>
    public async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<AppUser>().AnyAsync(user => user.Role == UserRole.GlobalAdmin && !user.IsBlocked && user.PasswordHash != null, cancellationToken))
        {
            return;
        }

        var email = configuration[EmailSetting]?.Trim();
        var password = configuration[PasswordSetting];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(MissingSettingsMessage(email, password));
        }

        if (email.Length > AppUser.EmailMaxLength || !CredentialChecks.IsEmailAddress(email))
        {
            throw new InvalidOperationException($"'{EmailSetting}' is not a valid e-mail address of at most {AppUser.EmailMaxLength} characters.");
        }

        var administrator = new AppUser { Role = UserRole.GlobalAdmin };
        administrator.SetEmail(email);
        var result = await users.CreateAsync(administrator, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"The bootstrap administrator was not created: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }

        logger.LogInformation("Bootstrap administrator {UserId} created", administrator.Id);
    }

    private static string MissingSettingsMessage(string? email, string? password)
    {
        var missing = new List<string>();
        if (string.IsNullOrEmpty(email))
        {
            missing.Add(EmailSetting);
        }

        if (string.IsNullOrEmpty(password))
        {
            missing.Add(PasswordSetting);
        }

        return $"No active global administrator exists. Set {string.Join(" and ", missing.Select(setting => $"'{setting}'"))} in the environment to create the first one.";
    }
}
