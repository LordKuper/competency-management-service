using Kalibr.OrgStructure;
using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// Tells who may not be asked to rate: the employees on the global administrator's list and the holders of the positions on it. Scoped to a request,
/// because it shares the request's database context.
/// </summary>
internal sealed class RaterExclusions(AppDbContext context)
{
    /// <summary>
    /// Finds which of some employees are excluded, directly or by their current position.
    /// </summary>
    /// <param name="people">The employees.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The identifiers of the excluded ones; empty when none is.</returns>
    public async Task<HashSet<Guid>> AmongAsync(IEnumerable<EmployeeStatus> people, CancellationToken cancellationToken)
    {
        var rows = await context.Set<PromotionRaterExclusion>().AsNoTracking().ToListAsync(cancellationToken);
        var employeeIds = rows.Where(row => row.Kind == RaterExclusionKind.Employee).Select(row => row.SubjectId).ToHashSet();
        var positionIds = rows.Where(row => row.Kind == RaterExclusionKind.Position).Select(row => row.SubjectId).ToHashSet();
        return [.. people.Where(person => employeeIds.Contains(person.Id) || positionIds.Contains(person.PositionId)).Select(person => person.Id)];
    }
}
