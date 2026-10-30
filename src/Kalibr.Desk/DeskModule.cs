using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.Desk;

/// <summary>
/// Entry points through which the host composes the Desk module.
/// </summary>
public static class DeskModule
{
    /// <summary>
    /// Registers the Desk module services. The desk reads every <see cref="Competency.Platform.IDeskSource"/> the other modules register.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddDeskModule(this IServiceCollection services) => services.AddScoped<DeskBoard>();

    /// <summary>
    /// Maps the Desk module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapDeskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        DeskEndpoints.Map(endpoints);
        return endpoints;
    }
}
