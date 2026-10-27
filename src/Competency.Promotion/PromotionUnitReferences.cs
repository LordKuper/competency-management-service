using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Answers <see cref="IUnitReferences"/> from the waves and the commission of a unit, on the request's database context: a unit that is or was in a wave
/// stays, and the commission of any other goes with the unit.
/// </summary>
internal sealed class PromotionUnitReferences(AppDbContext context) : IUnitReferences
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> BlockersAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var waves = await context.Set<PromotionWaveUnit>().CountAsync(member => member.OrgUnitId == unitId, cancellationToken);
        return waves > 0 ? [$"Волны повышения подразделения, в том числе завершённые: {waves}"] : [];
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(Guid unitId, CancellationToken cancellationToken)
    {
        context.RemoveRange(await context.Set<PromotionCommissionMember>().Where(member => member.OrgUnitId == unitId).ToListAsync(cancellationToken));
        context.RemoveRange(await context.Set<PromotionCommission>().Where(commission => commission.OrgUnitId == unitId).ToListAsync(cancellationToken));
    }
}
