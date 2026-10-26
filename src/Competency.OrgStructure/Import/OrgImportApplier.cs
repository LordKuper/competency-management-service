using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure.Import;

/// <summary>
/// Tracks every change of an import plan on the context, in the order the rules need: positions, units from the top down, units' position lists,
/// employees, dismissals with their cascade, deactivations. The caller saves once, in its transaction under the tree lock, so everything is applied or nothing.
/// The plan guarantees what the manual rules check one by one: every unit and position it uses is active, and every position of a row is on the list of the row's unit.
/// </summary>
internal sealed class OrgImportApplier(AppDbContext context, IEmployeeAccounts accounts, IHrAssignments hrAssignments)
{
    /// <summary>
    /// Tracks the plan and the decisions.
    /// </summary>
    /// <param name="plan">The plan, computed under the same lock.</param>
    /// <param name="decisions">The decisions, already checked against the rules.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>A task that completes when every change is tracked.</returns>
    public async Task ApplyAsync(ImportPlan plan, EffectiveDecisions decisions, CancellationToken cancellationToken)
    {
        await AddPositionsAsync(plan, cancellationToken);
        await AddUnitsAsync(plan, cancellationToken);
        AddLegalEntities(plan);
        ChangeUnitLists(plan);
        await ChangeEmployeesAsync(plan, cancellationToken);
        await DismissAsync(decisions.EmployeesToDismiss, cancellationToken);
        await DeactivateAsync(decisions.UnitsToDeactivate, cancellationToken);
    }

    private async Task AddPositionsAsync(ImportPlan plan, CancellationToken cancellationToken)
    {
        var created = plan.Positions.Where(position => position.IsNew).Select(position => (Ref: position, Entity: new Position { Name = position.Name })).ToList();
        context.AddRange(created.Select(pair => pair.Entity));
        foreach (var (position, entity) in created)
        {
            position.Id = entity.Id;
        }

        var reactivatedIds = plan.Positions.Where(position => position.WasInactive).Select(position => position.Id!.Value).ToArray();
        foreach (var position in await context.Set<Position>().Where(candidate => reactivatedIds.Contains(candidate.Id)).ToListAsync(cancellationToken))
        {
            position.IsActive = true;
        }
    }

    private async Task AddUnitsAsync(ImportPlan plan, CancellationToken cancellationToken)
    {
        foreach (var node in plan.Units.Where(node => node.IsNew))
        {
            var unit = new OrgUnit { Name = node.Name, ParentId = node.Parent?.Id };
            context.Add(unit);
            node.Id = unit.Id;
        }

        var activatedIds = plan.Units.Where(node => node.WasInactive).Select(node => node.Id!.Value).ToArray();
        foreach (var unit in await context.Set<OrgUnit>().Where(candidate => activatedIds.Contains(candidate.Id)).ToListAsync(cancellationToken))
        {
            unit.IsActive = true;
        }
    }

    private void AddLegalEntities(ImportPlan plan)
    {
        var created = plan.LegalEntities.Where(entity => entity.IsNew).Select(entity => (Ref: entity, Entity: new LegalEntity { Name = entity.Name })).ToList();
        context.AddRange(created.Select(pair => pair.Entity));
        foreach (var (entity, row) in created)
        {
            entity.Id = row.Id;
        }
    }

    private void ChangeUnitLists(ImportPlan plan)
    {
        foreach (var list in plan.UnitLists)
        {
            var unitId = list.Unit.Id!.Value;
            context.AddRange(list.Added.Select(position => new OrgUnitPosition { OrgUnitId = unitId, PositionId = position.Id!.Value }));
            context.RemoveRange(list.RemovedPositionIds.Select(positionId => new OrgUnitPosition { OrgUnitId = unitId, PositionId = positionId }));
        }
    }

    private async Task ChangeEmployeesAsync(ImportPlan plan, CancellationToken cancellationToken)
    {
        foreach (var change in plan.Employees.Where(change => change.Existing is null))
        {
            AddEmployee(change);
        }

        var existing = plan.Employees.Where(change => change.Existing is not null && change.HasChanges).ToList();
        if (existing.Count == 0)
        {
            return;
        }

        var ids = existing.Select(change => change.Existing!.Id).ToArray();
        var employees = await context.Set<Employee>().Where(employee => ids.Contains(employee.Id)).ToDictionaryAsync(employee => employee.Id, cancellationToken);
        var assignments = (await context.Set<EmployeeAssignment>().Include(assignment => assignment.LegalEntities)
            .Where(assignment => ids.Contains(assignment.EmployeeId)).ToListAsync(cancellationToken)).ToLookup(assignment => assignment.EmployeeId);
        var mainLinks = (await context.Set<EmployeeLegalEntity>().Where(link => ids.Contains(link.EmployeeId)).ToListAsync(cancellationToken)).ToLookup(link => link.EmployeeId);
        var headedUnits = (await context.Set<OrgUnit>().Where(unit => unit.HeadEmployeeId != null && ids.Contains(unit.HeadEmployeeId.Value)).ToListAsync(cancellationToken))
            .ToLookup(unit => unit.HeadEmployeeId!.Value);

        foreach (var change in existing)
        {
            var employee = employees[change.Existing!.Id];
            ChangeEmployee(change, employee, assignments[employee.Id].ToList(), mainLinks[employee.Id].ToList(), headedUnits[employee.Id]);
            context.Entry(employee).Property(nameof(IVersioned.Version)).IsModified = true;
        }
    }

    private void AddEmployee(EmployeeChange change)
    {
        var employee = new Employee
        {
            LastName = change.Name.LastName,
            FirstName = change.Name.FirstName,
            MiddleName = change.Name.MiddleName,
            OrgUnitId = change.Main.Unit.Id!.Value,
            PositionId = change.Main.Position.Id!.Value,
        };
        context.Add(employee);
        context.AddRange(DistinctIds(change.LegalEntities[change.Main]).Select(id => new EmployeeLegalEntity { EmployeeId = employee.Id, LegalEntityId = id }));
        foreach (var placement in change.Secondary)
        {
            context.Add(NewAssignment(employee.Id, placement, change));
        }
    }

    private void ChangeEmployee(
        EmployeeChange change,
        Employee employee,
        List<EmployeeAssignment> assignments,
        List<EmployeeLegalEntity> mainLinks,
        IEnumerable<OrgUnit> headedUnits)
    {
        if (change.IsRehire)
        {
            employee.IsActive = true;
        }

        if (change.IsTransfer)
        {
            foreach (var unit in headedUnits.Where(unit => unit.Id == employee.OrgUnitId))
            {
                unit.HeadEmployeeId = null;
            }

            employee.OrgUnitId = change.Main.Unit.Id!.Value;
            employee.PositionId = change.Main.Position.Id!.Value;
        }

        var removedIds = change.RemovedAssignments.Select(assignment => assignment.Id).ToHashSet();
        context.RemoveRange(assignments.Where(assignment => removedIds.Contains(assignment.Id)));
        foreach (var placement in change.AddedAssignments)
        {
            context.Add(NewAssignment(employee.Id, placement, change));
        }

        foreach (var placement in change.ChangedAssignmentLegalEntities)
        {
            var assignment = assignments.Single(candidate => placement.Is(candidate.OrgUnitId, candidate.PositionId));
            var wanted = DistinctIds(change.LegalEntities[placement]);
            context.RemoveRange(assignment.LegalEntities.Where(link => !wanted.Contains(link.LegalEntityId)));
            context.AddRange(wanted
                .Where(id => assignment.LegalEntities.All(link => link.LegalEntityId != id))
                .Select(id => new AssignmentLegalEntity { AssignmentId = assignment.Id, LegalEntityId = id }));
        }

        if (change.MainLegalEntitiesChanged)
        {
            var wanted = DistinctIds(change.LegalEntities[change.Main]);
            context.RemoveRange(mainLinks.Where(link => !wanted.Contains(link.LegalEntityId)));
            context.AddRange(wanted
                .Where(id => mainLinks.All(link => link.LegalEntityId != id))
                .Select(id => new EmployeeLegalEntity { EmployeeId = employee.Id, LegalEntityId = id }));
        }
    }

    private static EmployeeAssignment NewAssignment(Guid employeeId, Placement placement, EmployeeChange change)
    {
        var assignment = new EmployeeAssignment { EmployeeId = employeeId, OrgUnitId = placement.Unit.Id!.Value, PositionId = placement.Position.Id!.Value };
        assignment.LegalEntities.AddRange(DistinctIds(change.LegalEntities[placement]).Select(id => new AssignmentLegalEntity { LegalEntityId = id }));
        return assignment;
    }

    private static HashSet<Guid> DistinctIds(IEnumerable<LegalEntityRef> entities) => [.. entities.Select(entity => entity.Id!.Value)];

    /// <summary>
    /// Dismisses the employees with the cascade of a manual dismissal, computing each impact on its own: about 2.5 ms an employee, so 9 000 dismissals take about 25 s;
    /// a set-based impact is the upgrade path when imports dismiss far more than that.
    /// </summary>
    private async Task DismissAsync(IReadOnlyList<MissingEmployeeResponse> dismissed, CancellationToken cancellationToken)
    {
        var ids = dismissed.Select(employee => employee.Id).ToArray();
        foreach (var employee in await context.Set<Employee>().Where(candidate => ids.Contains(candidate.Id)).ToListAsync(cancellationToken))
        {
            var impact = await EmployeeImpact.OfAsync(context, accounts, hrAssignments, employee.Id, cancellationToken);
            await impact.ApplyAsync(employee.Id, accounts, hrAssignments, unbindAccount: false, cancellationToken);
            employee.IsActive = false;
        }
    }

    private async Task DeactivateAsync(IReadOnlyList<MissingUnitResponse> deactivated, CancellationToken cancellationToken)
    {
        var ids = deactivated.Select(unit => unit.Id).ToArray();
        foreach (var unit in await context.Set<OrgUnit>().Where(candidate => ids.Contains(candidate.Id)).ToListAsync(cancellationToken))
        {
            unit.IsActive = false;
        }
    }
}
