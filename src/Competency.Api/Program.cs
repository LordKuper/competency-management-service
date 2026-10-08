using System.Reflection;
using Competency.Api;
using Competency.Audit;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.AspNetCore.DataProtection;

const string SpaFallbackPattern = "{*path:nonfile:regex(^(?!api/).*$)}";

var isToolingRun = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddOpenApi(options => options.AddOperationTransformer<VersionHeadersOperationTransformer>());
builder.Services.AddApplication(builder.Configuration);

if (isToolingRun)
{
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}
else
{
    builder.Services.AddDataProtectionKeyStorage(builder.Configuration);
}

var app = builder.Build();

if (!isToolingRun)
{
    app.Services.ValidateMailSettings();
    await app.Services.MigrateDatabaseAsync(waitForDatabase: app.Environment.IsDevelopment());
    await app.Services.EnsureBootstrapAdminAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UsePlatform();
app.UseAuthentication();
app.UseAuthorization();

app.MapPlatformEndpoints();
app.MapAuditEndpoints();
app.MapOrgStructureEndpoints();
app.MapUserManagementEndpoints();
app.MapFallbackToFile(SpaFallbackPattern, "index.html").AllowAnonymous();

await app.RunAsync();
