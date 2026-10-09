using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of competency categories. Category names are unique across the catalogue.
/// </summary>
internal static class CompetencyCategoryEndpoints
{
    private static readonly CatalogKind<CompetencyCategory, CatalogEntryResponse> Kind = new("CompetencyCategory", CatalogEntryResponse.From);

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapNamedCatalog(
        "/api/v1/career/categories",
        "ListCompetencyCategories",
        Kind,
        (request, status, authorId) => new CompetencyCategory
        {
            Name = request.TrimmedName(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
            Status = status,
            AuthorId = authorId,
        });
}
