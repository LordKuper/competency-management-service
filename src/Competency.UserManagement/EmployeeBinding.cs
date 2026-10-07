using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.UserManagement;

/// <summary>
/// The rules that tie an account to an employee: an account of any role may have one working employee or none,
/// and an employee has at most one account.
/// </summary>
internal static class EmployeeBinding
{
    public const string AlreadyBound = "Сотрудник уже привязан к другой учётной записи.";

    /// <summary>
    /// Checks the role in a request, which needs no lookup.
    /// </summary>
    /// <param name="errors">Collects the problems by field name.</param>
    /// <param name="role">The requested role.</param>
    public static void CheckRole(Dictionary<string, string[]> errors, UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            errors["role"] = ["Роль: укажите допустимое значение."];
        }
    }

    /// <summary>
    /// Says why an account cannot be bound to an employee: they do not exist or do not work in the organization.
    /// </summary>
    /// <param name="employees">The employee directory.</param>
    /// <param name="employeeId">The employee to bind.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The reason, or <see langword="null"/> when the employee exists and works.</returns>
    public static async Task<string?> ProblemAsync(this IEmployeeDirectory employees, Guid employeeId, CancellationToken cancellationToken) =>
        await employees.FindAsync(employeeId, cancellationToken) switch
        {
            null => "Сотрудник не найден.",
            { IsActive: false } => "Сотрудник не работает.",
            _ => null,
        };

    /// <summary>
    /// Whether an account other than the given one is already bound to the employee.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employeeId">The employee to bind.</param>
    /// <param name="userId">The account being bound, or <see cref="Guid.Empty"/> for one that does not exist yet.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns><see langword="true"/> when the employee already has another account.</returns>
    public static Task<bool> IsBoundElsewhereAsync(this AppDbContext context, Guid employeeId, Guid userId, CancellationToken cancellationToken) =>
        context.Set<AppUser>().AnyAsync(user => user.EmployeeId == employeeId && user.Id != userId, cancellationToken);

    /// <summary>
    /// Finds the full name of an account's employee for display.
    /// </summary>
    /// <param name="employees">The employee directory.</param>
    /// <param name="employeeId">The account's employee, if bound.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The full name, or <see langword="null"/> when the account has no employee.</returns>
    public static async Task<string?> NameAsync(this IEmployeeDirectory employees, Guid? employeeId, CancellationToken cancellationToken) =>
        employeeId is { } id ? (await employees.FindAsync(id, cancellationToken))?.FullName : null;
}
