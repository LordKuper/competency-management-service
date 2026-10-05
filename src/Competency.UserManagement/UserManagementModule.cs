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
    /// the <c>Identity</c> configuration section and the account id read from the platform subject claim, and the cookie session read from
    /// <c>Authentication:Cookie</c>. The host adds <c>UseAuthentication</c> to its pipeline.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The application configuration holding the Identity and cookie settings.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEntityConfigurationContributor, UserManagementEntityConfiguration>();

        services.Configure<IdentityOptions>(options => options.ClaimsIdentity.UserIdClaimType = PlatformClaims.Subject);
        services.Configure<IdentityOptions>(configuration.GetSection(IdentitySection));
        services.AddIdentityCore<AppUser>()
            .AddUserStore<AppUserStore>()
            .AddErrorDescriber<RussianIdentityErrorDescriber>();

        services.AddAuthentication(SessionAuthentication.Scheme)
            .AddCookie(SessionAuthentication.Scheme, options => SessionAuthentication.Configure(options, configuration));

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
}
