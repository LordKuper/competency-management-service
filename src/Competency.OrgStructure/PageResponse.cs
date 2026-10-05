namespace Competency.OrgStructure;

/// <summary>
/// One page of a list, with the number of rows matching the filters across all pages.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
internal sealed record PageResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
