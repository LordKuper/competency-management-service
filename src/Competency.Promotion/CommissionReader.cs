using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Works out the commissions of units from the stored members and the structure at the moment of asking: the responsible HR and the head of a unit
/// are members without being stored, and a member counts only while they work and their account can act. A caller that acts on the answer
/// holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class CommissionReader(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    ICurrentActor actor)
{
    /// <summary>
    /// Reads the commissions of units, in a fixed number of queries.
    /// </summary>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The commission by unit; units that do not exist are absent.</returns>
    public async Task<IReadOnlyDictionary<Guid, CommissionRoster>> ReadAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        var ids = unitIds.Distinct().ToArray();
        var units = await employees.FindUnitsAsync(ids, cancellationToken);
        var commissions = await context.Set<PromotionCommission>()
            .AsNoTracking()
            .Where(commission => ids.Contains(commission.OrgUnitId))
            .ToDictionaryAsync(commission => commission.OrgUnitId, cancellationToken);
        var stored = (await context.Set<PromotionCommissionMember>()
                .AsNoTracking()
                .Where(member => ids.Contains(member.OrgUnitId))
                .ToListAsync(cancellationToken))
            .ToLookup(member => member.OrgUnitId, member => member.EmployeeId);
        var responsibleHr = await career.ResponsibleHrAsync(ids, cancellationToken);

        var personIds = stored.SelectMany(group => group)
            .Concat(units.Values.Select(unit => unit.HeadEmployeeId).OfType<Guid>())
            .Concat(responsibleHr.Values.Select(hr => hr.EmployeeId))
            .Distinct()
            .ToList();
        var people = await employees.FindManyAsync(personIds, cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync(personIds, cancellationToken);

        return units.Values.ToDictionary(
            unit => unit.Id,
            unit => Compose(unit, commissions.GetValueOrDefault(unit.Id), stored[unit.Id], responsibleHr.GetValueOrDefault(unit.Id), people, accounts));
    }

    /// <summary>
    /// Finds what the signed-in user may do with a commission.
    /// </summary>
    /// <param name="roster">The commission.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>Edit for a global administrator and the responsible HR of the unit, read for another member, otherwise none.</returns>
    public async Task<CommissionRights> RightsAsync(CommissionRoster roster, CancellationToken cancellationToken)
    {
        if (actor.IsGlobalAdmin() || (actor.UserId is not null && roster.ResponsibleHrUserId == actor.UserId))
        {
            return CommissionRights.Edit;
        }

        var employeeId = actor.UserId is { } userId ? (await users.FindAsync(userId, cancellationToken))?.EmployeeId : null;
        return employeeId is not null && roster.Members.Any(member => member.EmployeeId == employeeId) ? CommissionRights.Read : CommissionRights.None;
    }

    private static CommissionRoster Compose(
        OrgUnitStatus unit,
        PromotionCommission? commission,
        IEnumerable<Guid> storedIds,
        ResponsibleHr? responsibleHr,
        IReadOnlyDictionary<Guid, EmployeeStatus> people,
        IReadOnlyDictionary<Guid, UserStatus> accounts)
    {
        var members = new[] { responsibleHr?.EmployeeId, unit.HeadEmployeeId }
            .OfType<Guid>()
            .Concat(storedIds)
            .Distinct()
            .Where(people.ContainsKey)
            .Select(id => new CommissionMember(
                id,
                people[id].FullName,
                id == responsibleHr?.EmployeeId,
                id == unit.HeadEmployeeId,
                ActingEmployees.Acts(people[id], accounts) && (id != unit.HeadEmployeeId || unit.IsActive)))
            .OrderByDescending(member => member.IsRequired)
            .ThenBy(member => member.FullName, TextCollation.Comparer)
            .ThenBy(member => member.EmployeeId)
            .ToList();
        var chairId = commission?.ChairEmployeeId;
        return new CommissionRoster(
            unit.Id,
            unit.Name,
            commission?.Version ?? 0,
            members.Any(member => member.EmployeeId == chairId && member.IsActing) ? chairId : null,
            responsibleHr?.UserId,
            members);
    }
}
