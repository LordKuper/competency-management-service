namespace Competency.Platform;

// One page of a list, with the number of rows matching the filters across all pages.
// Deliberately without XML documentation: the OpenAPI generator would copy it into the published schemas.
#pragma warning disable CS1591
public record PageResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
#pragma warning restore CS1591
