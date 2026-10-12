using Competency.Platform;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency categories. Category names are unique across the catalogue. A category no competency uses, nor any version in
/// a competency's history, can be deleted; deletion is journaled.
/// </summary>
internal static class CompetencyCategoryEndpoints
{
    private const string InUseMessage = "Категорию используют компетенции — удалить нельзя";

    internal static readonly CatalogKind<CompetencyCategory, CatalogEntryResponse> Kind = new("CompetencyCategory", (entry, _, viewer) => CatalogEntryResponse.From(entry, viewer));

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
        .MapDeleteEntry(Kind, InUseAsync);

    /// <summary>
    /// The check runs under the catalogue lock, which every competency change also takes, so no competency can start using the category
    /// before the deletion commits.
    /// </summary>
    private static async Task<string?> InUseAsync(
        CompetencyCategory category,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        var id = category.Id;
        var competencies = context.Set<CareerCompetency>();
        await blockers.AddAsync(competencies.Where(competency => competency.CategoryId == id), DeletionBlockerKind.Competency);
        await blockers.AddAsync(
            competencies.Where(competency => competency.CategoryId != id
                && context.Set<CompetencyVersion>().Any(version => version.CompetencyId == competency.Id && version.CategoryId == id)),
            DeletionBlockerKind.CompetencyVersion);
        return blockers.Any ? InUseMessage : null;
    }
}
