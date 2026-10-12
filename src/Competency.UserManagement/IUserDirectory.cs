namespace Competency.UserManagement;

/// <summary>
/// Access to accounts for other modules, which never see the module's entities.
/// Implementations are scoped to a request, because they share its database context.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Finds an account.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The account's status, or <see langword="null"/> when no such account exists.</returns>
    Task<UserStatus?> FindAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds many accounts in one query.
    /// </summary>
    /// <param name="userIds">The account identifiers.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The status of each account that exists, by identifier; identifiers of accounts that do not exist are absent.</returns>
    Task<IReadOnlyDictionary<Guid, UserStatus>> FindManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the accounts bound to many employees in one query.
    /// </summary>
    /// <param name="employeeIds">The employee identifiers.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The status of each bound account, by employee identifier; employees without an account are absent.</returns>
    Task<IReadOnlyDictionary<Guid, UserStatus>> FindByEmployeeIdsAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken = default);
}
