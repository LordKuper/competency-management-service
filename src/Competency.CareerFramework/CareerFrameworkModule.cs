using Competency.Platform;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Competency.CareerFramework;

/// <summary>
/// Entry points through which the host composes the CareerFramework module.
/// </summary>
public static class CareerFrameworkModule
{
    /// <summary>
    /// Registers the CareerFramework module services. Its table refers to units and accounts, so the host adds the module after OrgStructure and UserManagement.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddCareerFrameworkModule(this IServiceCollection services)
    {
        services.AddSingleton<IEntityConfigurationContributor, CareerFrameworkEntityConfiguration>();
        services.AddScoped<ProcessRoles>();
        services.AddScoped<CatalogStore>();
        services.AddScoped<MatrixRoute>();
        services.AddScoped<CatalogPublication>();
        services.AddScoped<MatrixCatalog>();
        services.AddScoped<MatrixWorkflow>();
        services.AddScoped<CareerMail>();
        services.AddScoped<IDeskSource, CareerDeskSource>();
        services.AddSingleton<CareerMailQueue>();
        services.AddHostedService(provider => provider.GetRequiredService<CareerMailQueue>());
        return services;
    }

    /// <summary>
    /// Maps the CareerFramework module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapCareerFrameworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        OrgUnitHrEndpoints.Map(endpoints);
        OrgUnitSetupEndpoints.Map(endpoints);
        ProcessRoleEndpoints.Map(endpoints);
        MyPlaceEndpoints.Map(endpoints);
        CareerDirectionEndpoints.Map(endpoints);
        CareerTrackEndpoints.Map(endpoints);
        CareerLevelEndpoints.Map(endpoints);
        CareerLevelPositionEndpoints.Map(endpoints);
        CompetencyCategoryEndpoints.Map(endpoints);
        CompetencyEndpoints.Map(endpoints);
        CompetencyScaleEndpoints.Map(endpoints);
        CatalogDraftEndpoints.Map(endpoints);
        UnusedEntryEndpoints.Map(endpoints);
        MatrixEndpoints.Map(endpoints);
        MatrixVersionEndpoints.Map(endpoints);
        MatrixApprovalEndpoints.Map(endpoints);
        return endpoints;
    }
}
