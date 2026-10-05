using Competency.Platform;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.Audit;

/// <summary>
/// Entry points through which the host composes the Audit module.
/// </summary>
public static class AuditModule
{
    /// <summary>
    /// Registers the Audit module services.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationContributor, AuditEntityConfiguration>();
        services.AddSingleton<AuditEventFactory>();
        services.AddSingleton<IInterceptor, AuditSaveChangesInterceptor>();
        services.AddSingleton<IAuditWriter, AuditWriter>();
        return services;
    }

    /// <summary>
    /// Maps the Audit module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        AuditEndpoints.Map(endpoints);
        return endpoints;
    }
}
