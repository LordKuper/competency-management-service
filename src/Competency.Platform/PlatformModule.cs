using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Competency.Platform;

/// <summary>
/// Registers and maps the cross-cutting platform services shared by all modules.
/// </summary>
public static class PlatformModule
{
    /// <summary>
    /// Name of the connection string the database context reads from configuration.
    /// </summary>
    public const string ConnectionStringName = "Default";

    private const string KeysPathSetting = "DataProtection:KeysPath";
    private const string DataProtectionApplicationName = "competency-management-service";
    private const string ReadyTag = "ready";
    private const int PostgresMajorVersion = 18;

    /// <summary>
    /// Registers the database context and the readiness check.
    /// Every <see cref="IInterceptor"/> registered in the container, by the platform or by a module, is attached to the context.
    /// Nothing here connects to the database or reads the connection string until the context is first resolved.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration holding the connection string.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IInterceptor, EntityStampingInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(
                GetConnectionString(configuration),
                npgsql => npgsql.SetPostgresVersion(PostgresMajorVersion, 0))
            .AddInterceptors(provider.GetServices<IInterceptor>()));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        return services;
    }

    /// <summary>
    /// Persists data-protection keys to the configured directory; creates keys at host start,
    /// so it must not run in tooling hosts that only inspect the application.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration holding the key directory; defaults to the user profile.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddDataProtectionKeyStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(GetKeysPath(configuration)));

        return services;
    }

    /// <summary>
    /// Maps the anonymous liveness and readiness probes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        endpoints.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous();
        return endpoints;
    }

    /// <summary>
    /// Applies pending schema migrations; the application owns its schema.
    /// </summary>
    /// <param name="services">The root service provider.</param>
    /// <param name="cancellationToken">Cancels the migration run.</param>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

    private static string GetKeysPath(IConfiguration configuration) =>
        configuration[KeysPathSetting]
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            DataProtectionApplicationName,
            "keys");
}
