using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Answers <see cref="IUnitReferences"/> from the career tracks, shared blocks, matrices and HR assignment of a unit, on the request's database context.
/// </summary>
internal sealed class UnitReferences(AppDbContext context) : IUnitReferences
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> BlockersAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var tracks = await context.Set<CareerTrack>().CountAsync(track => track.OwnerOrgUnitId == unitId, cancellationToken);
        var blocks = await context.Set<CompetencyBlock>().CountAsync(block => block.ScopeUnitId == unitId, cancellationToken);
        var matrices = await context.Set<CompetencyMatrix>().CountAsync(matrix => matrix.OrgUnitId == unitId, cancellationToken);

        List<string> lines = [];
        if (tracks > 0)
        {
            lines.Add($"Треки карьеры подразделения: {tracks}");
        }

        if (blocks > 0)
        {
            lines.Add($"Блоки компетенций с областью действия в этом подразделении: {blocks}");
        }

        if (matrices > 0)
        {
            lines.Add($"Матрицы компетенций подразделения: {matrices}");
        }

        return lines;
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(Guid unitId, CancellationToken cancellationToken) =>
        context.RemoveRange(await context.Set<OrgUnitHr>().Where(assignment => assignment.OrgUnitId == unitId).ToListAsync(cancellationToken));
}
