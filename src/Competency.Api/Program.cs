using System.Reflection;
using Competency.Api;
using Competency.Audit;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;

const string SpaFallbackPattern = "{*path:nonfile:regex(^(?!api/).*$)}";

var isToolingRun = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddOpenApi(options => options.AddOperationTransformer<VersionHeadersOperationTransformer>());
builder.Services.AddApplication(builder.Configuration);

if (!isToolingRun)
{
    builder.Services.AddDataProtectionKeyStorage(builder.Configuration);
}

var app = builder.Build();

if (!isToolingRun)
{
    await app.Services.MigrateDatabaseAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPlatformEndpoints();
app.MapAuditEndpoints();
app.MapOrgStructureEndpoints();
app.MapUserManagementEndpoints();
app.MapFallbackToFile(SpaFallbackPattern, "index.html");

await app.RunAsync();
