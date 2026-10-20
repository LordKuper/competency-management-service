using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Competency.CareerFramework;

/// <summary>
/// What is left of the API of competency categories for clients of the contract that had them: competencies have no category any more,
/// so the list is always empty and creating one is refused as gone. The routes of single categories are not mapped and answer 404, as
/// for a category that does not exist.
/// </summary>
internal static class CompetencyCategoryEndpoints
{
    private const string Route = "/api/v1/career/categories";
    private const string GoneMessage = "Категорий компетенций больше нет";
    private const string DeprecatedDescription = "Deprecated, removed in the next major version: competencies have no category.";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapCatalogGroup(Route);
        group.MapGet(string.Empty, ListAsync)
            .WithName("ListCompetencyCategories")
            .WithDescription(DeprecatedDescription);
        group.MapPost(string.Empty, () => TypedResults.Problem(detail: GoneMessage, statusCode: StatusCodes.Status410Gone))
            .WithName("CreateCompetencyCategory")
            .WithDescription(DeprecatedDescription)
            .ProducesProblem(StatusCodes.Status410Gone);
    }

    /// <summary>
    /// An empty page, its paging checked as the list always checked it.
    /// </summary>
    private static Results<Ok<PageResponse<CatalogEntryResponse>>, ValidationProblem> ListAsync([AsParameters] CatalogListQuery query)
    {
        var errors = query.Validate();
        return errors.Count > 0
            ? TypedResults.ValidationProblem(errors)
            : TypedResults.Ok(new PageResponse<CatalogEntryResponse>([], 0, query.Page, query.PageSize));
    }
}
