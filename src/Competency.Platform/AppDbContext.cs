using Microsoft.EntityFrameworkCore;

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
    }
}
