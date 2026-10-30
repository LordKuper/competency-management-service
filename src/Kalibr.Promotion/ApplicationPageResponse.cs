namespace Kalibr.Promotion;

/// <summary>
/// One page of the applications of a wave with the counts for the filter buttons.
/// </summary>
/// <param name="Items">The rows of the page.</param>
/// <param name="Total">The rows matching the filters across all pages.</param>
/// <param name="Page">The page, from 1.</param>
/// <param name="PageSize">The rows per page.</param>
/// <param name="Counts">The counts for the filters.</param>
internal sealed record ApplicationPageResponse(
    IReadOnlyList<ApplicationSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize,
    ApplicationCountsResponse Counts);
