using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// The checks that say why a referenced unit, employee or position cannot be used, so a change can be rejected with that reason.
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

    /// <summary>
    /// Says why an employee of the unit cannot be given the position: it is inactive, or neither the unit nor any unit above it allows it.
    /// An unknown position is reported as not allowed.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="positionId">The referenced position.</param>
    /// <param name="unitId">The employee's unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The reason, or <see langword="null"/> when the position may be given.</returns>
    public static async Task<string?> PositionProblemAsync(this AppDbContext context, Guid positionId, Guid unitId, CancellationToken cancellationToken)
    {
        var isActive = await context.Set<Position>()
            .Where(position => position.Id == positionId)
            .Select(position => (bool?)position.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        if (isActive is false)
        {
            return "Должность неактивна";
        }

        var unitIds = context.AncestorIds(unitId);
        var isAllowed = isActive is true && await context.Set<OrgUnitPosition>()
            .AnyAsync(link => link.PositionId == positionId && unitIds.Contains(link.OrgUnitId), cancellationToken);
        return isAllowed ? null : "Должность недоступна в выбранном подразделении";
    }
}
