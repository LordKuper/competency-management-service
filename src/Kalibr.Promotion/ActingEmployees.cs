using Kalibr.OrgStructure;
using Kalibr.UserManagement;

namespace Kalibr.Promotion;

/// <summary>
/// Tells who can take part in the process: an employee who works and whose account can act. Scoped to a request, because it shares the
/// request's database context; a caller that acts on the answer holds the promotion locks first.
/// </summary>
internal sealed class ActingEmployees(IEmployeeDirectory employees, IUserDirectory users)
{
    /// <summary>
    /// Whether an employee works and has an account that can act.
    /// </summary>
    /// <param name="person">The employee.</param>
    /// <param name="accounts">The accounts of the employees, by employee, as <see cref="IUserDirectory.FindByEmployeeIdsAsync"/> finds them.</param>
    /// <returns><see langword="true"/> when the employee takes part.</returns>
    public static bool Acts(EmployeeStatus person, IReadOnlyDictionary<Guid, UserStatus> accounts) =>
        person.IsActive && accounts.TryGetValue(person.Id, out var account) && account.IsActive;

    /// <summary>
    /// Says why employees cannot take part: they do not exist, do not work, or have no account that can act.
    /// </summary>
    /// <param name="employeeIds">The employees.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One line per employee who cannot, addressed to the user; empty when all can.</returns>
    public async Task<List<string>> ProblemsAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var people = await employees.FindManyAsync(employeeIds, cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync(employeeIds, cancellationToken);
        return [.. employeeIds
            .Select(id => people.TryGetValue(id, out var person)
                ? Acts(person, accounts) ? null : $"Сотрудник «{person.FullName}» не работает или у него нет действующей учётной записи"
                : "Сотрудник не найден")
            .OfType<string>()];
    }
}
