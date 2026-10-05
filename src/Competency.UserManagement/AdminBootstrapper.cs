using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Competency.UserManagement;

/// <summary>
/// Creates the first global administrator from the environment when none is active, so a fresh system can be signed in to.
/// An existing administrator is never touched, whatever the environment says; the password is never logged.
/// </summary>
internal sealed class AdminBootstrapper(
    AppDbContext context,
    UserManager<AppUser> users,
    IConfiguration configuration,
    ILogger<AdminBootstrapper> logger)
{
    private const string UserNameSetting = "Bootstrap:AdminUserName";
    private const string PasswordSetting = "Bootstrap:AdminPassword";

    /// <summary>
    /// Creates the administrator unless one is already active.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check and the creation.</param>
    /// <exception cref="InvalidOperationException">No administrator is active and the environment does not provide a usable one.</exception>
    public async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (await context.Set<AppUser>().AnyAsync(user => user.Role == UserRole.GlobalAdmin && !user.IsBlocked, cancellationToken))
        {
            return;
        }

        var userName = configuration[UserNameSetting];
        var password = configuration[PasswordSetting];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                $"No active global administrator exists. Set '{UserNameSetting}' and '{PasswordSetting}' in the environment to create the first one.");
        }

        var administrator = new AppUser { UserName = userName.Trim(), Role = UserRole.GlobalAdmin };
        var result = await users.CreateAsync(administrator, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"The bootstrap administrator was not created: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }

        logger.LogInformation("Bootstrap administrator {UserId} created", administrator.Id);
    }
}
