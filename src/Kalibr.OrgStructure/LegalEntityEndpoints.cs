using Kalibr.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// The legal entity directory API. Any signed-in user searches the directory; global administrators and HR add to it by name,
/// under the tree lock, so two requests for the same new name never create it twice. Legal entities are never renamed or deleted.
/// </summary>
internal static class LegalEntityEndpoints
{
    private const string Route = "/api/v1/legal-entities";
    private const string Tag = "LegalEntities";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, ListAsync).WithName("ListLegalEntities");

        var keepers = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        keepers.MapPost("/quick-add", QuickAddAsync).WithName("QuickAddLegalEntity");
    }

    private static async Task<Results<Ok<PageResponse<LegalEntityResponse>>, ValidationProblem>> ListAsync(
        [AsParameters] LegalEntityListQuery query,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var text = TextSearch.Normalize(query.Q);
        var pattern = text is null ? null : ListRequest.ContainsPattern(text);
        var page = await context.Set<LegalEntity>()
            .AsNoTracking()
            .WhereIf(text is not null, entity => EF.Functions.ILike(entity.Name, pattern!, ListRequest.LikeEscape))
            .OrderBy(entity => entity.Name)
            .ThenBy(entity => entity.Id)
            .Select(LegalEntityResponse.Projection)
            .ToPageAsync(query.Page, query.PageSize, cancellationToken);

        return TypedResults.Ok(page);
    }

    /// <summary>
    /// Finds the legal entity whose name equals the given one regardless of letter case, comparing with the database's
    /// <c>lower()</c> on both sides, as the unique index does, or creates it, so the employee form can pick it at once.
    /// </summary>
    private static async Task<Results<Ok<LegalEntityResponse>, ValidationProblem>> QuickAddAsync(
        LegalEntityRequest request,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var name = request.TrimmedName();
        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var entity = await context.Set<LegalEntity>()
            .FromSql($"SELECT * FROM legal_entities WHERE lower(name) = lower({name})")
            .FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new LegalEntity { Name = name };
            context.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(LegalEntityResponse.From(entity));
    }
}
