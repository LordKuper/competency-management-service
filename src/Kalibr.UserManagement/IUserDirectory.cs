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

    /// <summary>
    /// Whether any account is <see cref="UserStatus.CanBeHr"/>, so that some employee can be assigned as a responsible HR; one query.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns><see langword="true"/> when at least one such account exists.</returns>
    Task<bool> HasHrCandidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds every global administrator account that can act: neither blocked nor still invited.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The account identifiers.</returns>
    Task<IReadOnlyList<Guid>> FindActiveAdministratorIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Says why an address cannot be given to a new account: it is not a plain e-mail address or another account already uses it, letter case aside.
    /// </summary>
    /// <param name="email">The address as entered.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The reason, addressed to the user; <see langword="null"/> when the address is free.</returns>
    Task<string?> EmailProblemAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an account without roles bound to a working employee who has none, with the given address, and e-mails it the invitation to set a
    /// password. Takes the employee tree lock itself, so call it outside a transaction.
    /// </summary>
    /// <param name="employeeId">The employee.</param>
    /// <param name="email">The address of the new account.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>What came of it.</returns>
    Task<EmployeeInvitation> InviteEmployeeAsync(Guid employeeId, string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// What came of inviting an employee through <see cref="IUserDirectory.InviteEmployeeAsync"/>.
/// </summary>
public enum EmployeeInvitation
{
    /// <summary>
    /// The account was created and the mail server accepted the invitation.
    /// </summary>
    Sent,

    /// <summary>
    /// The account was created but the mail server did not accept the invitation; an administrator can send it again.
    /// </summary>
    NotSent,

    /// <summary>
    /// No account was created: the employee does not work or has an account already, or the address is unusable.
    /// </summary>
    Refused,
}
