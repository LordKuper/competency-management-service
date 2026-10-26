using System.Linq.Expressions;
using Competency.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Gathers the records that keep an entry from being deleted, for the refusal to list. Every record counts, but only the first five the
/// user can read are named, so the refusal never shows the name of a record the user may not see.
/// </summary>
/// <param name="viewer">The user's rights.</param>
/// <param name="context">The request's database context.</param>
/// <param name="store">The catalogue store, for unit names.</param>
/// <param name="cancellationToken">Cancels the queries.</param>
internal sealed class DeletionBlockers(CatalogViewer viewer, AppDbContext context, CatalogStore store, CancellationToken cancellationToken)
{
    private const int ShownLimit = 5;

    private readonly List<DeletionBlocker> shown = [];
    private int total;

    /// <summary>
    /// Whether any record was added.
    /// </summary>
    public bool Any => total > 0;

    /// <summary>
    /// Adds catalogue entries of one kind; those the user can read are named in name order.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <param name="entries">The referring entries.</param>
    /// <param name="kind">What they are.</param>
    /// <returns>A task that completes when they are counted.</returns>
    public Task AddAsync<T>(IQueryable<T> entries, DeletionBlockerKind kind)
        where T : CatalogEntry =>
        AddAsync(entries, entry => new DeletionBlocker(kind, entry.Id, entry.Name, null));

    /// <summary>
    /// Adds matrices, oldest first, each named by its track or position and its unit; every signed-in user reads matrices.
    /// </summary>
    /// <param name="matrices">The referring matrices.</param>
    /// <returns>A task that completes when they are counted.</returns>
    public async Task AddMatricesAsync(IQueryable<CompetencyMatrix> matrices)
    {
        total += await matrices.CountAsync(cancellationToken);
        var room = ShownLimit - shown.Count;
        if (room <= 0)
        {
            return;
        }

        var tracks = context.Set<CareerTrack>();
        var rows = await matrices
            .OrderBy(matrix => matrix.CreatedAt)
            .ThenBy(matrix => matrix.Id)
            .Take(room)
            .Select(matrix => new
            {
                matrix.Id,
                matrix.OrgUnitId,
                matrix.PositionId,
                Track = tracks.Where(track => track.Id == matrix.TrackId).Select(track => track.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var unitNames = await store.UnitNamesAsync(rows.Select(row => (Guid?)row.OrgUnitId), cancellationToken);
        var positionNames = await store.PositionNamesAsync(rows.Select(row => row.PositionId), cancellationToken);
        shown.AddRange(rows.Select(row => new DeletionBlocker(
            DeletionBlockerKind.Matrix,
            row.Id,
            row.Track ?? (row.PositionId is { } positionId ? positionNames.GetValueOrDefault(positionId) : null) ?? string.Empty,
            unitNames.GetValueOrDefault(row.OrgUnitId))));
    }

    /// <summary>
    /// The refusal with the gathered records.
    /// </summary>
    /// <param name="detail">The reason, addressed to the user.</param>
    /// <returns>A 409 response.</returns>
    public ProblemHttpResult Refuse(string detail) => TypedResults.Problem(new DeletionRefusal
    {
        Status = StatusCodes.Status409Conflict,
        Title = Rejection.ConflictTitle,
        Detail = detail,
        Blockers = shown,
        MoreCount = total - shown.Count,
    });

    private async Task AddAsync<T>(IQueryable<T> entries, Expression<Func<T, DeletionBlocker>> describe)
        where T : CatalogEntry
    {
        total += await entries.CountAsync(cancellationToken);
        var room = ShownLimit - shown.Count;
        if (room <= 0)
        {
            return;
        }

        shown.AddRange(await entries
            .Where(viewer.Sees<T>())
            .OrderBy(entry => entry.Name)
            .ThenBy(entry => entry.Id)
            .Take(room)
            .Select(describe)
            .ToListAsync(cancellationToken));
    }
}
