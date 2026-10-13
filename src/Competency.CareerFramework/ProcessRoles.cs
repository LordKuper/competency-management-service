using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Works out the career process roles of an account from the org structure and the HR assignments at the moment of asking, so a change
/// of head or assignment takes effect at once and nothing about the roles is stored. Only an account that can act takes part in the
/// process, and a deactivated unit takes part in none. A caller that acts on the answer holds the tree lock first, shared or exclusive,
/// so the answer stays true until it commits. Each rule is also a static function over data read beforehand, so a caller checking many
/// units at once, such as the desk, applies the same rule to an organization snapshot.
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
        var account = await users.FindAsync(userId, cancellationToken);
        var employee = account is { IsActive: true, EmployeeId: { } employeeId } ? await employees.FindAsync(employeeId, cancellationToken) : null;
        return employee is { IsActive: true }
            ? [.. HeadedUnits(await employees.FindHeadedUnitsAsync(employee.Id, cancellationToken), account, employee)]
            : [];
    }

    /// <summary>
    /// The units an account heads among the given ones: active units headed by the working employee the account is bound to, while the account can act.
    /// </summary>
    /// <param name="units">The candidate units.</param>
    /// <param name="account">The account; null when it does not exist.</param>
    /// <param name="employee">The employee the account is bound to; null when it is bound to none.</param>
    /// <returns>The headed units in their given order.</returns>
    public static IEnumerable<OrgUnitStatus> HeadedUnits(IEnumerable<OrgUnitStatus> units, UserStatus? account, EmployeeStatus? employee) =>
        account is { IsActive: true } && employee is { IsActive: true } && account.EmployeeId == employee.Id
            ? units.Where(unit => unit.IsActive && unit.HeadEmployeeId == employee.Id)
            : [];

    /// <summary>
    /// The units the account is the responsible HR of: active units whose nearest assignment, on the unit itself or else on the nearest
    /// unit above it, names this account.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The units in name order; empty when the account cannot act or has no assignment.</returns>
    public async Task<IReadOnlyList<OrgUnitStatus>> HrUnitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await users.FindAsync(userId, cancellationToken);
        var assigneeOf = account is { IsActive: true } ? await AllAssigneesAsync(cancellationToken) : null;
        if (assigneeOf is null || !assigneeOf.Values.Contains(userId))
        {
            return [];
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        return [.. HrUnits(snapshot, assigneeOf, account).OrderBy(unit => unit.Name).ThenBy(unit => unit.Id)];
    }

    /// <summary>
    /// The units an account is the responsible HR of, over an organization snapshot: active units whose nearest assignment names the account, while it can act.
    /// </summary>
    /// <param name="snapshot">The organization snapshot.</param>
    /// <param name="assigneeOf">Every HR assignment, as the assigned account by unit.</param>
    /// <param name="account">The account; null when it does not exist.</param>
    /// <returns>The units in no particular order.</returns>
    public static IEnumerable<OrgUnitStatus> HrUnits(OrgSnapshot snapshot, IReadOnlyDictionary<Guid, Guid> assigneeOf, UserStatus? account) =>
        account is { IsActive: true }
            ? snapshot.Units.Values.Where(unit => unit.IsActive
                && NearestAssigned(snapshot.Ancestry(unit.Id), assigneeOf) is { } nearest
                && assigneeOf[nearest.Id] == account.Id)
            : [];

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

        return ResponsibleHr(ancestry, assigneeOf, await users.FindManyAsync([assigneeOf[nearest.Id]], cancellationToken));
    }

    /// <summary>
    /// The responsible HR of the first unit of a chain, by the rule of <see cref="ResponsibleHrAsync"/>, over data read beforehand.
    /// </summary>
    /// <param name="ancestry">A unit and the units above it, nearest first.</param>
    /// <param name="assigneeOf">The assigned account by unit, covering at least the units of the chain.</param>
    /// <param name="accounts">The accounts by identifier, covering at least the one assigned to the nearest assigned unit.</param>
    /// <returns>The account, or <see langword="null"/> when no assignment applies or its account cannot act.</returns>
    public static Guid? ResponsibleHr(
        IReadOnlyList<OrgUnitStatus> ancestry,
        IReadOnlyDictionary<Guid, Guid> assigneeOf,
        IReadOnlyDictionary<Guid, UserStatus> accounts) =>
        NearestAssigned(ancestry, assigneeOf) is { } nearest && accounts.GetValueOrDefault(assigneeOf[nearest.Id]) is { IsActive: true } account
            ? account.Id
            : null;

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
    /// Every HR assignment in one query; there are as many as units at most, and usually few.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The assigned account by unit.</returns>
    public async Task<IReadOnlyDictionary<Guid, Guid>> AllAssigneesAsync(CancellationToken cancellationToken) =>
        await context.Set<OrgUnitHr>().ToDictionaryAsync(assignment => assignment.OrgUnitId, assignment => assignment.UserId, cancellationToken);

    /// <summary>
    /// The account of the unit's head when the head is a working employee bound to an account that can act.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The account, or <see langword="null"/> when the unit is deactivated or has no such head.</returns>
    public async Task<Guid?> HeadAccountAsync(OrgUnitStatus unit, CancellationToken cancellationToken) =>
        (await HeadAccountsAsync([unit], cancellationToken)).TryGetValue(unit.Id, out var accountId) ? accountId : null;

    /// <summary>
    /// The accounts of the heads of many units in two queries, by the rule of <see cref="HeadAccountAsync"/>.
    /// </summary>
    /// <param name="units">The units.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The head's account by unit; units without a head whose account can act are absent.</returns>
    public async Task<IReadOnlyDictionary<Guid, Guid>> HeadAccountsAsync(IReadOnlyCollection<OrgUnitStatus> units, CancellationToken cancellationToken)
    {
        var headIds = units.Where(unit => unit.IsActive).Select(unit => unit.HeadEmployeeId).OfType<Guid>().Distinct().ToList();
        if (headIds.Count == 0)
        {
            return new Dictionary<Guid, Guid>();
        }

        var heads = await employees.FindManyAsync(headIds, cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync(headIds, cancellationToken);
        return units
            .DistinctBy(unit => unit.Id)
            .Select(unit => (unit.Id, AccountId: ActingHeadAccount(unit, heads, accounts)))
            .Where(pair => pair.AccountId is not null)
            .ToDictionary(pair => pair.Id, pair => pair.AccountId!.Value);
    }

    /// <summary>
    /// Whether the head of a unit can act, over data read beforehand: the unit is active, its head works, and the head's account can act.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <param name="employeesById">The employees by identifier, covering at least the unit's head.</param>
    /// <param name="accountsByEmployee">The accounts by the employee they are bound to, covering at least the unit's head.</param>
    /// <returns>The head's account, or <see langword="null"/> when the head cannot act.</returns>
    public static Guid? ActingHeadAccount(
        OrgUnitStatus unit,
        IReadOnlyDictionary<Guid, EmployeeStatus> employeesById,
        IReadOnlyDictionary<Guid, UserStatus> accountsByEmployee) =>
        unit is { IsActive: true, HeadEmployeeId: { } headId }
        && employeesById.GetValueOrDefault(headId) is { IsActive: true }
        && accountsByEmployee.GetValueOrDefault(headId) is { IsActive: true } account
            ? account.Id
            : null;
}
