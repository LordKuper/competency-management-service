using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.Promotion;

/// <summary>
/// The API of the promotion settings. Global administrators and HR read them, because HR needs the default stage lengths to announce a wave;
/// only global administrators change them, with <c>If-Match</c>.
/// </summary>
internal static class SettingsEndpoints
{
    private const string Route = "/api/v1/promotion/settings";
    private const string Tag = "Promotion";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        readers.MapGet(string.Empty, GetAsync).WithName("GetPromotionSettings").ProducesETag();

        readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .MapPut(string.Empty, UpdateAsync).WithName("UpdatePromotionSettings").ProducesETag();
    }

    private static async Task<Ok<SettingsResponse>> GetAsync(AppDbContext context, HttpResponse response, CancellationToken cancellationToken)
    {
        var settings = await context.Set<PromotionSettings>().AsNoTracking().SingleAsync(cancellationToken);
        response.SetETag(settings.Version);
        return TypedResults.Ok(SettingsResponse.From(settings));
    }

    private static async Task<Results<Ok<SettingsResponse>, ValidationProblem>> UpdateAsync(
        SettingsRequest request,
        IfMatch ifMatch,
        AppDbContext context,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var settings = await context.Set<PromotionSettings>().SingleAsync(cancellationToken);
        ifMatch.ApplyTo(context, settings);
        settings.MinIntervalMonths = request.MinIntervalMonths;
        settings.StageDurationsDays = [.. request.StageDurationsDays];

        await context.SaveChangesAsync(cancellationToken);
        response.SetETag(settings.Version);
        return TypedResults.Ok(SettingsResponse.From(settings));
    }
}
