using Competency.Platform;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Competency.Promotion;

/// <summary>
/// Entry points through which the host composes the Promotion module.
/// </summary>
public static class PromotionModule
{
    /// <summary>
    /// Registers the Promotion module services. Its tables refer to units, employees, accounts and matrices, and it reads the career model, so the host adds the module
    /// after OrgStructure, UserManagement and CareerFramework. It also implements <see cref="IUnitReferences"/> for the unit module and
    /// requires <see cref="AppOptions.TimeZone"/> to name a time zone the host knows. It also runs the scheduler that moves waves from stage to stage
    /// and the queue that sends their letters, and contributes the waves' findings to the desk.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddPromotionModule(this IServiceCollection services)
    {
        services.AddOptions<AppOptions>()
            .Validate(
                app => TimeZoneInfo.TryFindSystemTimeZoneById(app.TimeZone, out _),
                "'App:TimeZone' is not a time zone identifier of this host.");
        services.AddSingleton<IEntityConfigurationContributor, PromotionEntityConfiguration>();
        services.AddSingleton<PromotionCalendar>();
        services.AddScoped<PromotionLocks>();
        services.AddScoped<ActingEmployees>();
        services.AddScoped<CommissionReader>();
        services.AddScoped<WaveUnits>();
        services.AddScoped<WaveReader>();
        services.AddScoped<OpenWaves>();
        services.AddScoped<ApplicationAccess>();
        services.AddScoped<ApplicationRules>();
        services.AddScoped<ApplicationReader>();
        services.AddScoped<ReviewReader>();
        services.AddScoped<StageActions>();
        services.AddScoped<PromotionMail>();
        services.AddScoped<WaveStep>();
        services.AddScoped<WaveRollback>();
        services.AddSingleton<PromotionMailQueue>();
        services.AddHostedService(provider => provider.GetRequiredService<PromotionMailQueue>());
        services.AddSingleton<PromotionTick>();
        services.AddHostedService<PromotionScheduler>();
        services.AddScoped<IUnitReferences, PromotionUnitReferences>();
        services.AddScoped<IDeskSource, PromotionDeskSource>();
        return services;
    }

    /// <summary>
    /// Maps the Promotion module HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        SettingsEndpoints.Map(endpoints);
        CommissionEndpoints.Map(endpoints);
        WaveEndpoints.Map(endpoints);
        WaveRollbackEndpoints.Map(endpoints);
        WaveApplicationEndpoints.Map(endpoints);
        ApplicationEndpoints.Map(endpoints);
        ApplicationStageEndpoints.Map(endpoints);
        RatingEndpoints.Map(endpoints);
        ReviewEndpoints.Map(endpoints);
        return endpoints;
    }
}
