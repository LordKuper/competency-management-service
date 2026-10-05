using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Competency.Platform;

/// <summary>
/// The single application database context; its model is the union of all module contributions.
/// </summary>
/// <param name="options">The context options.</param>
/// <param name="contributors">The module-supplied entity configurations applied in registration order.</param>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IEnumerable<IEntityConfigurationContributor> contributors) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var contributor in contributors)
        {
            contributor.Configure(modelBuilder);
        }

        ApplyEntityBaseConventions(modelBuilder);
    }

    private static void ApplyEntityBaseConventions(ModelBuilder modelBuilder)
    {
        var rootTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => entityType.BaseType is null)
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var clrType in rootTypes)
        {
            var entity = modelBuilder.Entity(clrType);
            if (typeof(IVersioned).IsAssignableFrom(clrType))
            {
                entity.Property(nameof(IVersioned.Version)).IsConcurrencyToken();
            }

            if (typeof(EntityBase).IsAssignableFrom(clrType))
            {
                entity.Property(nameof(EntityBase.Id)).HasValueGenerator<GuidValueGenerator>();
            }
        }
    }
}
