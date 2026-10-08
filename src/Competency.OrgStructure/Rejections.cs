using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// The checks that say why a referenced unit or employee cannot be used, so a change can be rejected with that reason.
/// </summary>
internal static class Rejections
{
    /// <summary>
    /// Says why a unit cannot be used as a parent: it does not exist or is inactive.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="unitId">The referenced unit.</param>
    /// <param name="subject">How the reason names the unit, a neuter noun phrase such as "Родительское подразделение".</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The reason, or <see langword="null"/> when the unit exists and is active.</returns>
    public static async Task<string?> UnitProblemAsync(this AppDbContext context, Guid unitId, string subject, CancellationToken cancellationToken)
    {
        var isActive = await context.Set<OrgUnit>()
            .Where(unit => unit.Id == unitId)
            .Select(unit => (bool?)unit.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        return isActive switch
        {
            null => $"{subject} не найдено.",
            false => $"{subject} неактивно.",
            true => null,
        };
    }

    /// <summary>
    /// Says why an employee cannot be used as a unit head: they do not exist or do not work in the organization.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employeeId">The referenced employee.</param>
    /// <param name="subject">How the reason names the employee, a masculine noun such as "Руководитель".</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The reason, or <see langword="null"/> when the employee exists and works.</returns>
    public static async Task<string?> EmployeeProblemAsync(this AppDbContext context, Guid employeeId, string subject, CancellationToken cancellationToken)
    {
        var isActive = await context.Set<Employee>()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => (bool?)employee.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        return isActive switch
        {
            null => $"{subject} не найден.",
            false => $"{subject} не работает.",
            true => null,
        };
    }
}
