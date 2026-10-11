using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency categories. Category names are unique across the catalogue. A category no competency uses, nor any version in
/// a competency's history, can be deleted; deletion is journaled.
/// </summary>
internal static class CompetencyCategoryEndpoints
{
    private const string InUseMessage = "Категорию используют компетенции — удалить нельзя";

    private static readonly CatalogKind<CompetencyCategory, CatalogEntryResponse> Kind = new("CompetencyCategory", (entry, _) => CatalogEntryResponse.From(entry));

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapNamedCatalog(
            "/api/v1/career/categories",
            "ListCompetencyCategories",
            Kind,
            (request, status, authorId) => new CompetencyCategory
            {
                Name = request.TrimmedName(),
                Status = status,
                AuthorId = authorId,
            })
        .MapDelete("/{id:guid}", DeleteAsync)
        .WithName("DeleteCompetencyCategory")
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

    /// <summary>
    /// Deletes a category its keepers no longer need. The check runs under the catalogue lock, which every competency change also takes,
    /// so no competency can start using the category before the deletion commits.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IfMatch ifMatch,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginWriteAsync(cancellationToken);
        var viewer = await store.ViewerAsync(cancellationToken);
        if (await store.FindVisibleAsync<CompetencyCategory>(id, viewer, cancellationToken) is not { } category)
        {
            return TypedResults.NotFound();
        }

        if (!viewer.CanManage(category))
        {
            return Rejection.Forbidden();
        }

        if (await context.Set<CareerCompetency>().AnyAsync(competency => competency.CategoryId == id, cancellationToken)
            || await context.Set<CompetencyVersion>().AnyAsync(version => version.CategoryId == id, cancellationToken))
        {
            return Rejection.Conflict(InUseMessage);
        }

        ifMatch.ApplyTo(context, category);
        context.Remove(category);
        await store.SaveAsync(category, transaction, cancellationToken);
        return TypedResults.NoContent();
    }
}
