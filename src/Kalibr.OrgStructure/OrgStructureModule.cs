using Competency.OrgStructure.Import;
using Competency.Platform;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.OrgStructure;

/// <summary>
/// Entry points through which the host composes the OrgStructure module.
/// </summary>
public static class OrgStructureModule
{
    /// <summary>
    /// Registers the OrgStructure module services, including the <see cref="IEmployeeDirectory"/> other modules read employees and units through
    /// and the module's <see cref="IDeskSource"/>.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddOrgStructureModule(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationContributor, OrgStructureEntityConfiguration>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IDeskSource, OrgStructureDeskSource>();
        return services;
    }

    /// <summary>
    /// Maps the OrgStructure module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapOrgStructureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        OrgUnitEndpoints.Map(endpoints);
        EmployeeEndpoints.Map(endpoints);
        PositionEndpoints.Map(endpoints);
        OrgUnitPositionEndpoints.Map(endpoints);
        LegalEntityEndpoints.Map(endpoints);
        OrgImportEndpoints.Map(endpoints);
        return endpoints;
    }
}
