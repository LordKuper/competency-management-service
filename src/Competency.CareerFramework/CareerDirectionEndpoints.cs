using Competency.Platform;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// The API of professional directions. Direction names are unique across the catalogue. A direction no track, competency, version in a
/// competency's history or matrix names can be deleted.
/// </summary>
internal static class CareerDirectionEndpoints
{
    private const string InUseMessage = "Направление используют треки, компетенции или матрицы — удалить нельзя";

    private static readonly CatalogKind<CareerDirection, CatalogEntryResponse> Kind = new("CareerDirection", (entry, _, viewer) => CatalogEntryResponse.From(entry, viewer));

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

    private static async Task<string?> InUseAsync(
        CareerDirection direction,
        DeletionBlockers blockers,
        AppDbContext context,
        CatalogStore store,
        CancellationToken cancellationToken)
    {
        var id = direction.Id;
        var competencies = context.Set<CareerCompetency>();
        await blockers.AddAsync(context.Set<CareerTrack>().Where(track => track.DirectionId == id), DeletionBlockerKind.Track);
        await blockers.AddAsync(competencies.Where(competency => competency.DirectionId == id), DeletionBlockerKind.Competency);
        await blockers.AddAsync(
            competencies.Where(competency => competency.DirectionId != id
                && context.Set<CompetencyVersion>().Any(version => version.CompetencyId == competency.Id && version.DirectionId == id)),
            DeletionBlockerKind.CompetencyVersion);
        await blockers.AddMatricesAsync(context.Set<CompetencyMatrix>().Where(matrix => matrix.DirectionId == id));
        return blockers.Any ? InUseMessage : null;
    }
}
