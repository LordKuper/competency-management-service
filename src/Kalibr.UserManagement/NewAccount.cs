using Kalibr.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kalibr.UserManagement;

/// <summary>
/// Creates an account that has no password and e-mails its owner the link to set one. Shared by the administrator's create call and the port
/// other modules invite employees through, so both create, link, commit and mail in the same order.
/// </summary>
internal static class NewAccount
{
    /// <summary>
    /// Creates the account, lets <paramref name="afterCreate"/> add to it, issues the invitation link, commits the caller's transaction and mails the link.
    /// The caller has already checked the employee and the address, under the lock <paramref name="transaction"/> holds.
    /// </summary>
    /// <param name="user">The new account with its address and employee set.</param>
    /// <param name="transaction">The caller's exclusive transaction; committed here, once the account and its link are saved.</param>
    /// <param name="afterCreate">Runs once the account exists, for example to give it roles; <see langword="null"/> adds nothing.</param>
    /// <param name="users">The identity manager that creates the account.</param>
    /// <param name="context">The database context the account and its link are saved through.</param>
    /// <param name="mail">Sends the invitation.</param>
    /// <param name="links">How long the link lives.</param>
    /// <param name="time">The clock.</param>
    /// <param name="cancellationToken">Cancels the changes.</param>
    /// <returns>The result of the failed step, or success with whether the mail server accepted the invitation.</returns>
    public static async Task<(IdentityResult Result, bool MailSent)> InviteAsync(
        AppUser user,
        IDbContextTransaction transaction,
        Func<Task<IdentityResult>>? afterCreate,
        UserManager<AppUser> users,
        AppDbContext context,
        AccountMail mail,
        AccountLinkOptions links,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        var result = await users.CreateAsync(user);
        if (result.Succeeded && afterCreate is not null)
        {
            result = await afterCreate();
        }

        if (!result.Succeeded)
        {
            return (result, false);
        }

        var token = user.IssueLink(time.GetUtcNow(), links.InvitationLifetime);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (result, await mail.SendInvitationAsync(user, token, isRepeat: false, cancellationToken));
    }
}
