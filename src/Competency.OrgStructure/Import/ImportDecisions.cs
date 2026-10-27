namespace Competency.OrgStructure.Import;

/// <summary>
/// The units to deactivate and the employees to dismiss once the defaults are applied to the entities the user did not decide on.
/// </summary>
/// <param name="UnitsToDeactivate">The missing units to deactivate.</param>
/// <param name="EmployeesToDismiss">The missing employees to dismiss.</param>
internal sealed record EffectiveDecisions(IReadOnlyList<MissingUnitResponse> UnitsToDeactivate, IReadOnlyList<MissingEmployeeResponse> EmployeesToDismiss);

/// <summary>
/// Checks the user's decisions about the units and employees the file does not name, against the plan and against the rules of the organization.
/// </summary>
internal static class ImportDecisions
{
    /// <summary>
    /// Turns the requested decisions into the effective ones: a missing unit is deactivated and a missing employee dismissed unless the user said to keep them.
    /// </summary>
    /// <param name="plan">The plan, whose lists of missing units and employees are the only entities a decision may name.</param>
    /// <param name="requested">The decisions as the request carries them.</param>
    /// <param name="effective">The effective decisions; empty when the request is invalid.</param>
    /// <returns>The reason the request is invalid, addressed to the user; <see langword="null"/> when it is valid.</returns>
    public static string? Resolve(OrgImportPlanResponse plan, IReadOnlyList<ImportDecisionRequest> requested, out EffectiveDecisions effective)
    {
        effective = new EffectiveDecisions([], []);
        var unitIds = plan.MissingUnits.Select(unit => unit.Id).ToHashSet();
        var employeeIds = plan.MissingEmployees.Select(employee => employee.Id).ToHashSet();
        var chosen = new Dictionary<Guid, ImportDecision>();
        foreach (var request in requested)
        {
            var isUnit = unitIds.Contains(request.Id);
            if (!isUnit && !employeeIds.Contains(request.Id))
            {
                return "Решение относится к подразделению или сотруднику, которого нет в списке отсутствующих.";
            }

            if (request.Decision == (isUnit ? ImportDecision.Dismiss : ImportDecision.Deactivate))
            {
                return isUnit ? "Подразделение можно оставить или деактивировать." : "Сотрудника можно оставить или уволить.";
            }

            if (!chosen.TryAdd(request.Id, request.Decision))
            {
                return "Решение по одной записи указано дважды.";
            }
        }

        effective = new EffectiveDecisions(
            [.. plan.MissingUnits.Where(unit => chosen.GetValueOrDefault(unit.Id, ImportDecision.Deactivate) == ImportDecision.Deactivate)],
            [.. plan.MissingEmployees.Where(employee => chosen.GetValueOrDefault(employee.Id, ImportDecision.Dismiss) == ImportDecision.Dismiss)]);
        return null;
    }

    /// <summary>
    /// Finds a rule of the organization the decisions would break: dismissing every active administrator, or deactivating a unit that keeps
    /// working employees or active child units. Call it under the tree lock; it takes the administrator lock after it, the order a manual dismissal uses.
    /// </summary>
    /// <param name="plan">The plan the decisions are about.</param>
    /// <param name="decisions">The effective decisions.</param>
    /// <param name="data">The organization as the plan was computed from it.</param>
    /// <param name="accounts">Locks and reads the active administrators, which another module owns.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The reason the decisions are refused, addressed to the user; <see langword="null"/> when none is broken.</returns>
    public static async Task<string?> BrokenRuleAsync(
        OrgImportPlanResponse plan,
        EffectiveDecisions decisions,
        ImportSnapshot data,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        var dismissedIds = decisions.EmployeesToDismiss.Select(employee => employee.Id).ToHashSet();
        if (dismissedIds.Any(data.EmployeesWithAccount.Contains))
        {
            await accounts.LockAdministratorsAsync(cancellationToken);
            if (await accounts.LeavesNoActiveAdministratorAsync([.. dismissedIds], cancellationToken))
            {
                return EmployeeImpact.LastAdministratorMessage;
            }
        }

        var deactivatedIds = decisions.UnitsToDeactivate.Select(unit => unit.Id).ToHashSet();
        foreach (var unit in decisions.UnitsToDeactivate)
        {
            var staying = plan.MissingEmployees.Count(employee => !dismissedIds.Contains(employee.Id) && employee.OrgUnitIds.Contains(unit.Id));
            if (staying > 0)
            {
                return $"Нельзя деактивировать подразделение «{unit.Path}»: в нём остаются работающие сотрудники ({staying}). Оставьте подразделение или уволите сотрудников.";
            }

            var activeChildren = plan.MissingUnits.Count(child => child.ParentId == unit.Id && !deactivatedIds.Contains(child.Id));
            if (activeChildren > 0)
            {
                return $"Нельзя деактивировать подразделение «{unit.Path}»: в нём остаются активные дочерние подразделения ({activeChildren}). Оставьте подразделение или деактивируйте дочерние.";
            }
        }

        return null;
    }
}
