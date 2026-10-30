using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.UserManagement;

/// <summary>
/// The lock on one account row, last in the global lock order, for work on a single account's credentials:
/// password attempts and the e-mailed link. Its holder takes no other lock, so it cannot deadlock with the employee tree or administrator locks.
/// </summary>
internal static class AccountRowLock
{
    /// <summary>
    /// Starts a transaction and locks the account row until it ends, so concurrent work on the account runs one at a time against committed state.
    /// Read the account after this call, or reload it if it was read before.
    /// </summary>
    /// <param name="context">The context the changes will be saved through.</param>
    /// <param name="userId">The account to lock; an unknown id locks nothing.</param>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    public static async Task<IDbContextTransaction> BeginAccountLockAsync(this AppDbContext context, Guid userId, CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT id FROM users WHERE id = {userId} FOR UPDATE", cancellationToken);
        return transaction;
    }
}
