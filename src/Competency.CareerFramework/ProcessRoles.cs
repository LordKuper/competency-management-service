using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Works out the career process roles of an account from the org structure and the HR assignments at the moment of asking, so a change
/// of head or assignment takes effect at once and nothing about the roles is stored. Only an account that can act takes part in the
/// process, and a deactivated unit takes part in none. A caller that acts on the answer holds the tree lock first, shared or exclusive,
/// so the answer stays true until it commits.
/// </summary>
internal sealed class ProcessRoles(AppDbContext context, IEmployeeDirectory employees, IUserDirectory users)
{
    /// <summary>
    /// The units the account heads: active units whose head is the working employee the account is bound to.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The headed units in name order; empty when the account cannot act or is bound to no working employee.</returns>
    public async Task<IReadOnlyList<OrgUnitStatus>> HeadedUnitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await users.FindAsync(userId, cancellationToken) is not { IsActive: true, EmployeeId: { } employeeId }
            || await employees.FindAsync(employeeId, cancellationToken) is not { IsActive: true })
        {
            return [];
        }

        var units = await employees.FindHeadedUnitsAsync(employeeId, cancellationToken);
        return [.. units.Where(unit => unit.IsActive)];
    }

    /// <summary>
    /// The units the account is the responsible HR of: active units whose nearest assignment, on the unit itself or else on the nearest
    /// unit above it, names this account.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The units in name order; empty when the account cannot act or has no assignment.</returns>
    public async Task<IReadOnlyList<OrgUnitStatus>> HrUnitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await users.FindAsync(userId, cancellationToken) is not { IsActive: true })
        {
            return [];
        }

        var assignedIds = await context.Set<OrgUnitHr>()
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.OrgUnitId)
            .ToListAsync(cancellationToken);

        var subtrees = new Dictionary<Guid, OrgUnitStatus>();
        foreach (var assignedId in assignedIds)
        {
            foreach (var unit in await employees.FindSubtreeAsync(assignedId, cancellationToken))
            {
                subtrees.TryAdd(unit.Id, unit);
            }
        }

        var subtreeIds = subtrees.Keys.ToList();
        var assigneeOf = await context.Set<OrgUnitHr>()
            .Where(assignment => subtreeIds.Contains(assignment.OrgUnitId))
            .ToDictionaryAsync(assignment => assignment.OrgUnitId, assignment => assignment.UserId, cancellationToken);

        return [.. subtrees.Values
            .Where(unit => unit.IsActive && NearestAssignee(unit.Id, subtrees, assigneeOf) == userId)
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)];
    }

    /// <summary>
    /// The responsible HR of a unit: the account assigned to the unit itself or else to the nearest unit above it with an assignment.
    /// An account that cannot act leaves the unit without a responsible HR rather than passing the role further up.
    /// </summary>
    /// <param name="unitId">The unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The account, or <see langword="null"/> when no assignment applies or its account cannot act.</returns>
    public async Task<Guid?> ResponsibleHrAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var ancestry = await employees.FindAncestryAsync(unitId, cancellationToken);
        var assigneeOf = await AssigneesAsync([.. ancestry.Select(unit => unit.Id)], cancellationToken);
        if (NearestAssigned(ancestry, assigneeOf) is not { } nearest)
        {
            return null;
        }

        var userId = assigneeOf[nearest.Id];
        return await users.FindAsync(userId, cancellationToken) is { IsActive: true } ? userId : null;
    }

    /// <summary>
    /// The unit whose HR assignment applies to the first unit of a chain: that unit itself or else the nearest unit above it with an
    /// assignment, whether or not the assigned account can act. The one rule for a chain read from the tree or from an organization snapshot.
    /// </summary>
    /// <param name="ancestry">A unit and the units above it, nearest first.</param>
    /// <param name="assigneeOf">The assigned account by unit, covering at least the units of the chain.</param>
    /// <returns>The assigned unit, or <see langword="null"/> when no unit of the chain has an assignment.</returns>
    public static OrgUnitStatus? NearestAssigned(IReadOnlyList<OrgUnitStatus> ancestry, IReadOnlyDictionary<Guid, Guid> assigneeOf) =>
        ancestry.FirstOrDefault(unit => assigneeOf.ContainsKey(unit.Id));

    /// <summary>
    /// The HR assignments of the given units in one query.
    /// </summary>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The assigned account by unit; units without an assignment are absent.</returns>
    public async Task<IReadOnlyDictionary<Guid, Guid>> AssigneesAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        var ids = unitIds.ToArray();
        return await context.Set<OrgUnitHr>()
            .Where(assignment => ids.Contains(assignment.OrgUnitId))
            .ToDictionaryAsync(assignment => assignment.OrgUnitId, assignment => assignment.UserId, cancellationToken);
    }

    /// <summary>
    /// The account of the unit's head when the head is a working employee bound to an account that can act.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The account, or <see langword="null"/> when the unit is deactivated or has no such head.</returns>
    public async Task<Guid?> HeadAccountAsync(OrgUnitStatus unit, CancellationToken cancellationToken)
    {
        if (unit is not { IsActive: true, HeadEmployeeId: { } headId }
            || await employees.FindAsync(headId, cancellationToken) is not { IsActive: true })
        {
            return null;
        }

        var accounts = await users.FindByEmployeeIdsAsync([headId], cancellationToken);
        return accounts.TryGetValue(headId, out var account) && account.IsActive ? account.Id : null;
    }

    /// <summary>
    /// Walks up from a unit to the first unit with an assignment. Every unit of the subtrees lies below an assigned unit,
    /// so the walk ends inside them.
    /// </summary>
    private static Guid? NearestAssignee(
        Guid unitId,
        IReadOnlyDictionary<Guid, OrgUnitStatus> units,
        IReadOnlyDictionary<Guid, Guid> assigneeOf)
    {
        Guid? current = unitId;
        while (current is { } id)
        {
            if (assigneeOf.TryGetValue(id, out var assignee))
            {
                return assignee;
            }

            current = units.GetValueOrDefault(id)?.ParentId;
        }

        return null;
    }
}
