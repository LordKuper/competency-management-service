using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.Promotion;

/// <summary>
/// The locks of the promotion module, taken in the global order: the tree lock in shared mode first, because the rights of the actors come from
/// the tree, then the module lock, then the row of the wave, then the row of the application. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class PromotionLocks(AppDbContext context, IEmployeeDirectory employees)
{
    private const long LockKey = 5_006_001;

    /// <summary>
    /// Starts a transaction for the creation of a wave or a change of its units, which no other change of a wave, application or commission may overlap,
    /// because the units of unfinished waves must not repeat.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public async Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken)
    {
        var transaction = await employees.BeginSharedAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Starts a transaction for any other change of a wave, an application or a commission, which may run alongside each other.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public async Task<IDbContextTransaction> BeginSharedAsync(CancellationToken cancellationToken)
    {
        var transaction = await employees.BeginSharedAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock_shared({LockKey})", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Reads a wave and locks its row until the transaction ends, tracked for a change. Call it under a lock of this class.
    /// </summary>
    /// <param name="waveId">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The wave, or <see langword="null"/> when there is none.</returns>
    public Task<PromotionWave?> FindWaveForUpdateAsync(Guid waveId, CancellationToken cancellationToken) =>
        context.Set<PromotionWave>()
            .FromSql($"SELECT * FROM promotion_waves WHERE id = {waveId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Reads an application and locks its row after the row of its wave, until the transaction ends, both tracked for a change. Call it under a
    /// lock of this class, and lock no wave before it.
    /// </summary>
    /// <param name="applicationId">The application.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The wave and the application, or <see langword="null"/> when there is no such application.</returns>
    public async Task<(PromotionWave Wave, PromotionApplication Application)?> FindApplicationForUpdateAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var waveId = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.Id == applicationId)
            .Select(application => (Guid?)application.WaveId)
            .FirstOrDefaultAsync(cancellationToken);
        if (waveId is null || await FindWaveForUpdateAsync(waveId.Value, cancellationToken) is not { } wave)
        {
            return null;
        }

        var found = await context.Set<PromotionApplication>()
            .FromSql($"SELECT * FROM promotion_applications WHERE id = {applicationId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
        return found is null ? null : (wave, found);
    }
}
