using Competency.Platform;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of professional directions. Direction names are unique across the catalogue. A direction no track, competency, version in a
/// competency's history or matrix names can be deleted.
/// </summary>
internal static class CareerDirectionEndpoints
{
    private const string InUseMessage = "Направление используют треки, компетенции или матрицы — удалить нельзя";

    private static readonly CatalogKind<CareerDirection, CatalogEntryResponse> Kind = new("CareerDirection", (entry, _) => CatalogEntryResponse.From(entry));

    public static void Map(IEndpointRouteBuilder endpoints) => endpoints.MapNamedCatalog(
            "/api/v1/career/directions",
            "ListCareerDirections",
            Kind,
            (request, status, authorId) => new CareerDirection
            {
                Name = request.TrimmedName(),
                Status = status,
                AuthorId = authorId,
            })
        .MapDeleteEntry(Kind, InUseAsync);

    private static async Task<string?> InUseAsync(CareerDirection direction, AppDbContext context, CatalogStore store, CancellationToken cancellationToken)
    {
        var id = direction.Id;
        return await context.Set<CareerTrack>().AnyAsync(track => track.DirectionId == id, cancellationToken)
            || await context.Set<CareerCompetency>().AnyAsync(competency => competency.DirectionId == id, cancellationToken)
            || await context.Set<CompetencyVersion>().AnyAsync(version => version.DirectionId == id, cancellationToken)
            || await context.Set<CompetencyMatrix>().AnyAsync(matrix => matrix.DirectionId == id, cancellationToken)
                ? InUseMessage
                : null;
    }
}
