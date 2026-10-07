using Competency.Platform;
using Microsoft.EntityFrameworkCore.Design;

namespace Competency.Api;

/// <summary>
/// Builds <see cref="AppDbContext"/> for EF tools with the full module model and a connection string that is never opened.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string UnusedConnectionString = "Host=unused;Database=unused";

    public AppDbContext CreateDbContext(string[] args)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{PlatformModule.ConnectionStringName}"] = UnusedConnectionString,
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var provider = new ServiceCollection().AddApplication(configuration).BuildServiceProvider();
        return provider.GetRequiredService<AppDbContext>();
    }
}
