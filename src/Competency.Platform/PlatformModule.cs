using System.Data.Common;
using System.Diagnostics;
using System.Net;
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
using Microsoft.Extensions.Options;
using MimeKit;

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

    /// <summary>
    /// Name of the rate-limiting policy for the anonymous endpoints that take an e-mailed link or ask for one, for <c>RequireRateLimiting</c>.
    /// Every endpoint under it draws on one budget per client address.
    /// </summary>
    public const string PasswordResetRateLimitPolicy = "password-reset";

    private const string LoginRateLimitSection = "RateLimiting:Login";
    private const string PasswordResetRateLimitSection = "RateLimiting:PasswordReset";
    private const string SmtpSection = "Smtp";
    private const string AppSection = "App";
    private const string UnknownClient = "unknown";
    private const string KeysPathSetting = "DataProtection:KeysPath";
    private const string DataProtectionApplicationName = "competency-management-service";
    private const string ReadyTag = "ready";
    private const int PostgresMajorVersion = 18;
    private static readonly TimeSpan DatabaseWaitTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DatabaseWaitDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxSmtpTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <summary>
    /// Registers the cross-cutting services every module relies on.
    /// JSON numbers are numbers only, never strings, so the API description types integers as numbers.
    /// Every <see cref="IInterceptor"/> in the container, from the platform or a module, is attached to the context.
    /// Every endpoint requires an authenticated user unless it carries anonymous metadata, which a module exposing an anonymous endpoint must add itself.
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
        AddRateLimiter(services, configuration);
        AddMail(services, configuration);

        return services;
    }

    /// <summary>
    /// Fails, naming the settings at fault, when the <c>Smtp</c> or <c>App</c> settings are missing or invalid, so a misconfigured host does not start.
    /// Call it explicitly at start, never from a tooling host, which is not configured for mail.
    /// </summary>
    /// <param name="services">The root service provider.</param>
    /// <exception cref="OptionsValidationException">A setting is missing or invalid.</exception>
    public static void ValidateMailSettings(this IServiceProvider services)
    {
        _ = services.GetRequiredService<IOptions<SmtpOptions>>().Value;
        _ = services.GetRequiredService<IOptions<AppOptions>>().Value;
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

    /// <summary>
    /// Adds the fixed-window policies partitioned by client address. The middleware keys a partition by policy name and address,
    /// so the endpoints sharing a policy share its counter.
    /// </summary>
    private static void AddRateLimiter(IServiceCollection services, IConfiguration configuration) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddPerAddressPolicy(options, LoginRateLimitPolicy, configuration.GetRequiredSection(LoginRateLimitSection));
            AddPerAddressPolicy(options, PasswordResetRateLimitPolicy, configuration.GetRequiredSection(PasswordResetRateLimitSection));
        });

    private static void AddPerAddressPolicy(RateLimiterOptions options, string policy, IConfigurationSection section)
    {
        var budget = section.Get<RateLimitOptions>()!;
        options.AddPolicy(policy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = budget.PermitLimit,
                Window = TimeSpan.FromSeconds(budget.WindowSeconds),
            }));
    }

    private static void AddMail(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpSection))
            .Validate(smtp => Uri.CheckHostName(smtp.Host) != UriHostNameType.Unknown, "'Smtp:Host' is not a host name or address.")
            .Validate(smtp => smtp.Port is > IPEndPoint.MinPort and <= IPEndPoint.MaxPort, "'Smtp:Port' is not a port number.")
            .Validate(smtp => Enum.IsDefined(smtp.SecureSocketOptions), "'Smtp:SecureSocketOptions' is not a defined option.")
            .Validate(
                smtp => smtp.Timeout > TimeSpan.Zero && smtp.Timeout <= MaxSmtpTimeout,
                $"'Smtp:Timeout' is not a positive time span of at most {MaxSmtpTimeout}.")
            .Validate(
                smtp => string.IsNullOrEmpty(smtp.UserName) == string.IsNullOrEmpty(smtp.Password),
                "'Smtp:UserName' and 'Smtp:Password' are set only together.")
            .Validate(
                smtp => MailboxAddress.TryParse(smtp.From, out var sender) && sender.Domain.Length > 0,
                "'Smtp:From' is not a mailbox address.");
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppSection))
            .Validate(
                app => app.PublicBaseUrl is { IsAbsoluteUri: true, Scheme: "http" or "https" },
                "'App:PublicBaseUrl' is not an absolute http or https URL.");
        services.AddSingleton<MailSender>();
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
