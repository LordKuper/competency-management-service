using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.UserManagement;

/// <summary>
/// Entry points through which the host composes the UserManagement module.
/// </summary>
public static class UserManagementModule
{
    /// <summary>
    /// Registers the UserManagement module services.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddUserManagementModule(this IServiceCollection services) => services;

    /// <summary>
    /// Maps the UserManagement module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
