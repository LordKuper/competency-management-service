using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Competency.Platform;

/// <summary>
/// Stamps tracked entities on save: increments the version of updated <see cref="IVersioned"/> entities
/// and sets the creation time of new <see cref="EntityBase"/> entities.
/// </summary>
internal sealed class EntityStampingInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry)
            {
                case { State: EntityState.Added, Entity: EntityBase }:
                    entry.Property(nameof(EntityBase.CreatedAt)).CurrentValue = now;
                    break;
                case { State: EntityState.Modified, Entity: IVersioned }:
                    var version = entry.Property(nameof(IVersioned.Version));
                    version.CurrentValue = (int)version.OriginalValue! + 1;
                    break;
            }
        }
    }
}
