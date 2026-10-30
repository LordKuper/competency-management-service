using Microsoft.EntityFrameworkCore;

namespace Competency.Platform;

/// <summary>
/// Lets a module contribute its entity configurations to the shared <see cref="AppDbContext"/> model.
/// </summary>
public interface IEntityConfigurationContributor
{
    /// <summary>
    /// Applies the module's entity configurations to the model being built.
    /// </summary>
    /// <param name="modelBuilder">The builder of the shared model.</param>
    void Configure(ModelBuilder modelBuilder);
}
