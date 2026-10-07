using Competency.Audit;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;

namespace Competency.Api;

/// <summary>
/// Composes the platform and every module, shared by the running host and EF design-time tooling.
/// </summary>
internal static class ApplicationServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) => services
        .AddPlatform(configuration)
        .AddAuditModule()
        .AddOrgStructureModule()
        .AddUserManagementModule(configuration);
}
