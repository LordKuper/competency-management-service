using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// What the organization structure expects of a global administrator or HR, who alone may change it: units without a head or without positions,
/// employees whose position in any of their assignments their unit does not offer, positions out of use and empty units. Units and positions count
/// the employees of every assignment. Every user gets its prerequisites, other users nothing else.
/// </summary>
internal sealed class OrgStructureDeskSource(ICurrentActor actor, IEmployeeDirectory directory, AppDbContext context) : IDeskSource
{
    /// <inheritdoc />
    public async Task<DeskReport> FindAsync(CancellationToken cancellationToken)
    {
        var snapshot = await directory.FindOrgSnapshotAsync(cancellationToken);
        var prerequisites = Prerequisites(snapshot);
        if (!actor.SeesAll())
        {
            return new DeskReport([], prerequisites);
        }

        return new DeskReport(
            [
                .. UnitFindings(snapshot),
                .. PositionFindings(snapshot),
                .. await EmployeeFindingsAsync(snapshot, cancellationToken),
            ],
            prerequisites);
    }

    /// <summary>
    /// Whether there is an active unit, an active position and a working employee.
    /// Every signed-in user reads the tree, the catalogue and the working employees, so the facts reveal nothing.
    /// </summary>
    private static List<DeskPrerequisite> Prerequisites(OrgSnapshot snapshot)
    {
        var prerequisites = new List<DeskPrerequisite>();
        if (snapshot.Units.Values.Any(unit => unit.IsActive))
        {
            prerequisites.Add(DeskPrerequisite.ActiveUnit);
        }

        if (snapshot.Positions.Values.Any(position => position.IsActive))
        {
            prerequisites.Add(DeskPrerequisite.ActivePosition);
        }

        if (snapshot.WorkingEmployeeCounts.Count > 0)
        {
            prerequisites.Add(DeskPrerequisite.WorkingEmployee);
        }

        return prerequisites;
    }

    /// <summary>
    /// Units without a head; units whose employees can hold no position, because neither the unit nor any unit above it lists one;
    /// and units with neither working employees nor active units under them, which can be deactivated.
    /// </summary>
    private static IEnumerable<DeskFinding> UnitFindings(OrgSnapshot snapshot)
    {
        var employeeCounts = snapshot.WorkingAssigneeCounts
            .GroupBy(pair => pair.Key.OrgUnitId)
            .ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value));
        var parentsOfActive = snapshot.Units.Values.Where(unit => unit.IsActive).Select(unit => unit.ParentId).ToHashSet();
        foreach (var unit in snapshot.Units.Values.Where(unit => unit.IsActive))
        {
            var unitObject = snapshot.UnitDeskObject(unit);
            var employeeCount = employeeCounts.GetValueOrDefault(unit.Id);
            if (unit.HeadEmployeeId is null)
            {
                yield return new DeskFinding(DeskKind.UnitWithoutHead, unitObject, IsAction: true);
            }

            if (employeeCount > 0 && snapshot.AvailablePositionIds(unit.Id).Count == 0)
            {
                yield return new DeskFinding(DeskKind.UnitWithoutPositions, unitObject, IsAction: true, Details: new DeskDetails(EmployeeCount: employeeCount));
            }

            if (employeeCount == 0 && !parentsOfActive.Contains(unit.Id))
            {
                yield return new DeskFinding(DeskKind.EmptyUnit, unitObject, IsAction: false);
            }
        }
    }

    /// <summary>
    /// Inactive positions working employees still hold, and active positions on no unit's list that no working employee holds, in any assignment.
    /// </summary>
    private static IEnumerable<DeskFinding> PositionFindings(OrgSnapshot snapshot)
    {
        var employeeCounts = snapshot.WorkingAssigneeCounts
            .GroupBy(pair => pair.Key.PositionId)
            .ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value));
        var listed = snapshot.UnitPositionIds.SelectMany(list => list).ToHashSet();
        foreach (var position in snapshot.Positions.Values)
        {
            var positionObject = new DeskObject(DeskObjectType.Position, position.Id, position.Name);
            var employeeCount = employeeCounts.GetValueOrDefault(position.Id);
            if (!position.IsActive && employeeCount > 0)
            {
                yield return new DeskFinding(DeskKind.InactivePositionInUse, positionObject, IsAction: false, Details: new DeskDetails(EmployeeCount: employeeCount));
            }

            if (position.IsActive && employeeCount == 0 && !listed.Contains(position.Id))
            {
                yield return new DeskFinding(DeskKind.UnusedPosition, positionObject, IsAction: false);
            }
        }
    }

    /// <summary>
    /// Working employees with an assignment whose position is not among those its unit offers, one finding per such assignment.
    /// The snapshot tells which unit and position pairs are wrong, so only the assignments in those units are read.
    /// </summary>
    private async Task<IEnumerable<DeskFinding>> EmployeeFindingsAsync(OrgSnapshot snapshot, CancellationToken cancellationToken)
    {
        var wrongPairs = snapshot.WorkingAssigneeCounts.Keys
            .Where(pair => !snapshot.AvailablePositionIds(pair.OrgUnitId).Contains(pair.PositionId))
            .ToHashSet();
        if (wrongPairs.Count == 0)
        {
            return [];
        }

        var unitIds = wrongPairs.Select(pair => pair.OrgUnitId).Distinct().ToArray();
        var employees = await context
            .WorkingPlacements()
            .Where(placement => unitIds.Contains(placement.OrgUnitId))
            .Join(context.Set<Employee>(), placement => placement.EmployeeId, employee => employee.Id, (placement, employee) => new { employee.Id, employee.FullName, placement.OrgUnitId, placement.PositionId })
            .ToListAsync(cancellationToken);
        return employees
            .Where(employee => wrongPairs.Contains((employee.OrgUnitId, employee.PositionId)))
            .Select(employee =>
            {
                var unit = snapshot.Units[employee.OrgUnitId];
                var position = snapshot.Positions[employee.PositionId];
                return new DeskFinding(
                    DeskKind.PositionOutsideList,
                    new DeskObject(DeskObjectType.Employee, employee.Id, employee.FullName, [unit.Name]),
                    IsAction: true,
                    Details: new DeskDetails(Entries:
                    [
                        new DeskObject(DeskObjectType.Position, position.Id, position.Name),
                        new DeskObject(DeskObjectType.OrgUnit, unit.Id, unit.Name),
                    ]));
            });
    }
}
