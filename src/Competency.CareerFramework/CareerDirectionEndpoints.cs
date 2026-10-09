using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of professional directions. Direction names are unique across the catalogue.
/// </summary>
internal static class CareerDirectionEndpoints
{
    private static readonly CatalogKind<CareerDirection, CatalogEntryResponse> Kind = new("CareerDirection", CatalogEntryResponse.From);

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapNamedCatalog(
        "/api/v1/career/directions",
        "ListCareerDirections",
        Kind,
        (request, status, authorId) => new CareerDirection
        {
            Name = request.TrimmedName(),
            OwnerOrgUnitId = request.OwnerOrgUnitId,
            Status = status,
            AuthorId = authorId,
        });
}
