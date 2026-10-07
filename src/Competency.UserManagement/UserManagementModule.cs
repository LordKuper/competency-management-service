using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.UserManagement;

/// <summary>
/// Entry points through which the host composes the UserManagement module.
/// </summary>
public static class UserManagementModule
{
    private const string IdentitySection = "Identity";

    /// <summary>
    /// Registers the UserManagement module services: the account table, Identity with the password and lockout policy read from
    /// the <c>Identity</c> configuration section, no restriction on user-name characters (the user name is the e-mail, checked by the requests) and the account id read from the platform subject claim, the cookie session read from
    /// <c>Authentication:Cookie</c>, and the bootstrap of the first administrator. The host adds <c>UseAuthentication</c> to its pipeline.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration holding the Identity and cookie settings.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEntityConfigurationContributor, UserManagementEntityConfiguration>();

        services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserIdClaimType = PlatformClaims.Subject;
            options.User.AllowedUserNameCharacters = string.Empty;
        });
        services.Configure<IdentityOptions>(configuration.GetSection(IdentitySection));
        services.AddIdentityCore<AppUser>()
            .AddUserStore<AppUserStore>()
            .AddErrorDescriber<RussianIdentityErrorDescriber>();

        services.AddAuthentication(SessionAuthentication.Scheme)
            .AddCookie(SessionAuthentication.Scheme, options => SessionAuthentication.Configure(options, configuration));

        services.AddScoped<AdminBootstrapper>();
        services.AddScoped<IEmployeeAccounts, EmployeeAccounts>();
        return services;
    }

    /// <summary>
    /// Maps the UserManagement module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AuthEndpoints.Map(endpoints);
        UserEndpoints.Map(endpoints);
        return endpoints;
    }

    /// <summary>
    /// Creates the first global administrator from <c>Bootstrap:AdminEmail</c> and <c>Bootstrap:AdminPassword</c> when no active one exists.
    /// Call it explicitly after the schema is migrated, never from a tooling host; it fails with a clear message when an administrator is needed but not configured.
    /// </summary>
    /// <param name="services">The root service provider.</param>
    /// <param name="cancellationToken">Cancels the bootstrap.</param>
    public static async Task EnsureBootstrapAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminBootstrapper>().EnsureAdminAsync(cancellationToken);
    }
}
