using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.UserManagement;

/// <summary>
/// The rule that at least one active global administrator always exists, enforced by locking the set of active administrators
/// for the length of every change that could shrink it, so two concurrent changes cannot each leave "the other one" behind.
/// </summary>
internal static class ActiveAdministrators
{
    public const string LastOneMessage = "Нельзя заблокировать, понизить или иначе лишить роли последнего активного глобального администратора.";

    /// <summary>
    /// Starts a transaction, takes the employee tree lock and then locks every active administrator row, in key order, until the transaction ends.
    /// The tree lock always comes first, the order in which dismissing an employee takes the same two, so concurrent callers cannot deadlock;
    /// it also keeps a change of the account's employee from racing a dismissal of that employee. The locks need the default read committed
    /// isolation, under which later queries see what the previous holder committed.
    /// </summary>
    /// <param name="context">The context the changes will be saved through.</param>
    /// <param name="employees">The employee directory, which owns the tree lock.</param>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public static async Task<IDbContextTransaction> BeginExclusiveAsync(
        this AppDbContext context,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        await context.LockAsync(cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Locks every active administrator row, in key order, until the transaction the caller already holds ends; for a caller that
    /// took another lock first and so cannot start its transaction here, such as an employee change that may block an account.
    /// </summary>
    /// <param name="context">The context whose open transaction holds the lock.</param>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>A task that completes once the lock is held.</returns>
    public static async Task LockAsync(this AppDbContext context, CancellationToken cancellationToken) =>
        await context.Database.ExecuteSqlAsync(
            $"SELECT id FROM users WHERE role = {nameof(UserRole.GlobalAdmin)} AND NOT is_blocked ORDER BY id FOR UPDATE",
            cancellationToken);

    public static bool IsActiveAdministrator(this AppUser user) => user.Role == UserRole.GlobalAdmin && !user.IsBlocked;

    /// <summary>
    /// Whether another active administrator exists, so the given one may be blocked or demoted; call it under the lock of <see cref="BeginExclusiveAsync"/> or <see cref="LockAsync"/>.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="userId">The administrator about to leave the active set.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns><see langword="true"/> when at least one other active administrator exists.</returns>
    public static Task<bool> HasOtherAsync(this AppDbContext context, Guid userId, CancellationToken cancellationToken) =>
        context.Set<AppUser>().AnyAsync(user => user.Role == UserRole.GlobalAdmin && !user.IsBlocked && user.Id != userId, cancellationToken);
}
