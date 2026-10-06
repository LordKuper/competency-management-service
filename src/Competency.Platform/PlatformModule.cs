using System.Data.Common;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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

    /// <summary>
    /// Name of the rate-limiting policy for the sign-in endpoint, for <c>RequireRateLimiting</c>.
    /// </summary>
    public const string LoginRateLimitPolicy = "login";

    private const string LoginRateLimitSection = "RateLimiting:Login";
    private const string UnknownClient = "unknown";
    private const string KeysPathSetting = "DataProtection:KeysPath";
    private const string DataProtectionApplicationName = "competency-management-service";
    private const string ReadyTag = "ready";
    private const int PostgresMajorVersion = 18;
    private static readonly TimeSpan DatabaseWaitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DatabaseWaitDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Registers the database context, the readiness check, error handling, the current actor, the authorization policies and the sign-in rate limiter.
    /// JSON numbers are read and written as numbers only, so the API description types integers as numbers rather than numbers or strings.
    /// Every <see cref="IInterceptor"/> registered in the container, by the platform or by a module, is attached to the context.
    /// Every endpoint requires an authenticated user unless it carries anonymous metadata; only the health probes,
    /// the SPA fallback and the sign-in endpoint do, and a module exposing another anonymous endpoint must say so explicitly.
    /// Nothing here connects to the database or reads the connection string until the context is first resolved.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration holding the connection string.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
        services.AddSingleton<IInterceptor, EntityStampingInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(
                GetConnectionString(configuration),
                npgsql => npgsql.SetPostgresVersion(PostgresMajorVersion, 0))
            .AddInterceptors(provider.GetServices<IInterceptor>()));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        services.AddProblemDetails();
        services.AddExceptionHandler<ProblemExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentActor, HttpContextCurrentActor>();
        AddAuthorizationPolicies(services);
        AddLoginRateLimiter(services, configuration);

        return services;
    }

    /// <summary>
    /// Adds the platform request pipeline: request id, ProblemDetails error responses for unhandled exceptions and bodyless error statuses,
    /// the cross-site check on state-changing requests and rate limiting.
    /// The host adds authentication, when it has a scheme, and then authorization after this call.
    /// </summary>
    /// <param name="app">The application pipeline to extend.</param>
    /// <returns>The same pipeline, for chaining.</returns>
    public static IApplicationBuilder UsePlatform(this IApplicationBuilder app) => app
        .UseMiddleware<RequestIdMiddleware>()
        .UseExceptionHandler()
        .UseStatusCodePages()
        .UseMiddleware<CrossSiteRequestMiddleware>()
        .UseRateLimiter();

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
    /// <param name="waitForDatabase">
    /// Retries the connection for about a minute before migrating, for a host started alongside its database;
    /// when the database stays unreachable the connection error surfaces as it would without the wait.
    /// </param>
    /// <param name="cancellationToken">Cancels the wait and the migration run.</param>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        bool waitForDatabase = false,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (waitForDatabase)
        {
            await WaitForDatabaseAsync(context, cancellationToken);
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task WaitForDatabaseAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var waited = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                await connection.CloseAsync();
                return;
            }
            catch (DbException) when (waited.Elapsed < DatabaseWaitTimeout)
            {
                await Task.Delay(DatabaseWaitDelay, cancellationToken);
            }
        }
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        var authenticated = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Authenticated, authenticated)
            .AddPolicy(
                AuthorizationPolicies.GlobalAdmin,
                policy => policy.RequireAuthenticatedUser().RequireClaim(PlatformClaims.Role, PlatformClaims.GlobalAdminRole))
            .SetFallbackPolicy(authenticated);
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, StatusAuthorizationResultHandler>();
    }

    private static void AddLoginRateLimiter(IServiceCollection services, IConfiguration configuration) =>
        services.AddRateLimiter(options =>
        {
            var login = configuration.GetRequiredSection(LoginRateLimitSection).Get<LoginRateLimitOptions>()!;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = login.PermitLimit,
                    Window = TimeSpan.FromSeconds(login.WindowSeconds),
                }));
        });

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
