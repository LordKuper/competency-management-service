using Kalibr.Platform;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// The replacement of an employee's whole set of assignments, the main one and the secondary ones, by what a request names.
/// It checks the request first and only then tracks the changes, so a rejected request changes nothing; both steps run under the tree lock.
/// A unit or a pair of unit and position the employee already has is not checked again, so an employee keeps an assignment whose unit or position
/// has become unusable since, and making a secondary assignment the main one needs no recheck.
/// </summary>
internal sealed class AssignmentChange
{
    private const string LegalEntitiesField = "legalEntityIds";

    private readonly AppDbContext context;
    private readonly Employee employee;
    private readonly EmployeeRequest input;
    private readonly bool isNew;
    private readonly List<EmployeeAssignment> assignments;
    private readonly List<EmployeeLegalEntity> mainEntities;
    private readonly HashSet<Guid> knownUnitIds;
    private readonly HashSet<(Guid OrgUnitId, Guid PositionId)> knownPairs;

    private AssignmentChange(
        AppDbContext context,
        Employee employee,
        EmployeeRequest input,
        bool isNew,
        List<EmployeeAssignment> assignments,
        List<EmployeeLegalEntity> mainEntities)
    {
        this.context = context;
        this.employee = employee;
        this.input = input;
        this.isNew = isNew;
        this.assignments = assignments;
        this.mainEntities = mainEntities;
        knownUnitIds = [.. assignments.Select(assignment => assignment.OrgUnitId)];
        knownPairs = [.. assignments.Select(assignment => (assignment.OrgUnitId, assignment.PositionId))];
        if (!isNew)
        {
            knownUnitIds.Add(employee.OrgUnitId);
            knownPairs.Add((employee.OrgUnitId, employee.PositionId));
        }
    }

    /// <summary>
    /// Reads the assignments the employee has now, with their legal entities, tracked so that applying the change updates them.
    /// Call it before the employee's own unit and position are changed, because they are the main assignment it remembers.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employee">The employee, tracked; a new employee has nothing to read.</param>
    /// <param name="input">The trimmed request.</param>
    /// <param name="isNew">Whether the employee is being created.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The change, not yet checked.</returns>
    public static async Task<AssignmentChange> LoadAsync(
        AppDbContext context,
        Employee employee,
        EmployeeRequest input,
        bool isNew,
        CancellationToken cancellationToken)
    {
        if (isNew)
        {
            return new AssignmentChange(context, employee, input, isNew, [], []);
        }

        var assignments = await context.Set<EmployeeAssignment>()
            .Include(assignment => assignment.LegalEntities)
            .Where(assignment => assignment.EmployeeId == employee.Id)
            .ToListAsync(cancellationToken);
        var mainEntities = await context.Set<EmployeeLegalEntity>()
            .Where(link => link.EmployeeId == employee.Id)
            .ToListAsync(cancellationToken);
        return new AssignmentChange(context, employee, input, isNew, assignments, mainEntities);
    }

    /// <summary>
    /// Checks the request against the rules of an assignment: the unit is active and the position is active and allowed in it,
    /// each legal entity exists, and no pair of unit and position repeats, the main one included.
    /// </summary>
    /// <param name="mainUnit">The unit of the main assignment, which exists.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The first problem found, naming the field and, for a secondary assignment, its index; <see langword="null"/> when the request is valid.</returns>
    public async Task<ValidationProblem?> CheckAsync(OrgUnit mainUnit, CancellationToken cancellationToken)
    {
        if (!knownUnitIds.Contains(mainUnit.Id) && !mainUnit.IsActive)
        {
            return Rejection.Invalid("orgUnitId", "Подразделение неактивно.");
        }

        if (await PositionProblemAsync(input.PositionId, mainUnit.Id, cancellationToken) is { } mainProblem)
        {
            return Rejection.Invalid("positionId", mainProblem);
        }

        var entityIds = input.LegalEntityIds.Concat(input.Assignments.SelectMany(assignment => assignment.LegalEntityIds)).ToHashSet();
        var existingEntityIds = await context.Set<LegalEntity>()
            .Where(entity => entityIds.Contains(entity.Id))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        if (input.LegalEntityIds.Any(id => !existingEntityIds.Contains(id)))
        {
            return Rejection.Invalid(LegalEntitiesField, "Юридическое лицо не найдено.");
        }

        var seen = new HashSet<(Guid, Guid)> { (mainUnit.Id, input.PositionId) };
        for (var index = 0; index < input.Assignments.Count; index++)
        {
            var assignment = input.Assignments[index];
            var field = $"assignments[{index}]";
            if (!seen.Add((assignment.OrgUnitId, assignment.PositionId)))
            {
                return Rejection.Invalid($"{field}.positionId", "Такое назначение уже есть: подразделение и должность не должны повторяться.");
            }

            if (!knownUnitIds.Contains(assignment.OrgUnitId)
                && await context.UnitProblemAsync(assignment.OrgUnitId, "Подразделение", cancellationToken) is { } unitProblem)
            {
                return Rejection.Invalid($"{field}.orgUnitId", unitProblem);
            }

            if (await PositionProblemAsync(assignment.PositionId, assignment.OrgUnitId, cancellationToken) is { } positionProblem)
            {
                return Rejection.Invalid($"{field}.positionId", positionProblem);
            }

            if (assignment.LegalEntityIds.Any(id => !existingEntityIds.Contains(id)))
            {
                return Rejection.Invalid($"{field}.{LegalEntitiesField}", "Юридическое лицо не найдено.");
            }
        }

        return null;
    }

    /// <summary>
    /// Tracks the replacement: removes the secondary assignments the request no longer names, adds the new ones, updates the legal entities of those that stay
    /// and of the main assignment, and marks an existing employee as changed so that its version moves even when no field of the employee did.
    /// Call it after the request was checked and after the version guard.
    /// </summary>
    public void Apply()
    {
        var wanted = input.Assignments.ToDictionary(assignment => (assignment.OrgUnitId, assignment.PositionId));
        var changed = false;
        foreach (var removed in assignments.Where(assignment => !wanted.ContainsKey((assignment.OrgUnitId, assignment.PositionId))))
        {
            context.Remove(removed);
            changed = true;
        }

        foreach (var (pair, request) in wanted)
        {
            var existing = assignments.FirstOrDefault(assignment => (assignment.OrgUnitId, assignment.PositionId) == pair);
            if (existing is null)
            {
                var added = new EmployeeAssignment { EmployeeId = employee.Id, OrgUnitId = pair.OrgUnitId, PositionId = pair.PositionId };
                added.LegalEntities.AddRange(request.LegalEntityIds.Distinct().Select(id => new AssignmentLegalEntity { LegalEntityId = id }));
                context.Add(added);
                changed = true;
            }
            else
            {
                changed |= ReplaceAssignmentEntities(existing, request.LegalEntityIds.ToHashSet());
            }
        }

        changed |= ReplaceMainEntities(input.LegalEntityIds.ToHashSet());
        if (changed && !isNew)
        {
            context.Entry(employee).Property(nameof(IVersioned.Version)).IsModified = true;
        }
    }

    private bool ReplaceAssignmentEntities(EmployeeAssignment assignment, HashSet<Guid> wantedIds)
    {
        var removed = assignment.LegalEntities.Where(link => !wantedIds.Contains(link.LegalEntityId)).ToList();
        var added = wantedIds.Where(id => assignment.LegalEntities.All(link => link.LegalEntityId != id)).ToList();
        context.RemoveRange(removed);
        foreach (var id in added)
        {
            context.Add(new AssignmentLegalEntity { AssignmentId = assignment.Id, LegalEntityId = id });
        }

        return removed.Count + added.Count > 0;
    }

    private bool ReplaceMainEntities(HashSet<Guid> wantedIds)
    {
        var removed = mainEntities.Where(link => !wantedIds.Contains(link.LegalEntityId)).ToList();
        var added = wantedIds.Where(id => mainEntities.All(link => link.LegalEntityId != id)).ToList();
        context.RemoveRange(removed);
        foreach (var id in added)
        {
            context.Add(new EmployeeLegalEntity { EmployeeId = employee.Id, LegalEntityId = id });
        }

        return removed.Count + added.Count > 0;
    }

    private Task<string?> PositionProblemAsync(Guid positionId, Guid unitId, CancellationToken cancellationToken) =>
        knownPairs.Contains((unitId, positionId))
            ? Task.FromResult<string?>(null)
            : context.PositionProblemAsync(positionId, unitId, cancellationToken);
}
