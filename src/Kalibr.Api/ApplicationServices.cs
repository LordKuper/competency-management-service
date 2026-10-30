using Kalibr.Audit;
using Kalibr.CareerFramework;
using Kalibr.Desk;
using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.Promotion;
using Kalibr.UserManagement;

namespace Kalibr.Api;

/// <summary>
/// Composes the platform and every module, shared by the running host and EF design-time tooling.
/// </summary>
internal static class ApplicationServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) => services
        .AddPlatform(configuration)
        .AddAuditModule()
        .AddOrgStructureModule()
        .AddUserManagementModule(configuration)
        .AddCareerFrameworkModule()
        .AddPromotionModule()
        .AddDeskModule();
}
