namespace Competency.Promotion;

/// <summary>
/// What the filters of the application list would show, counted across all pages within the candidate search but not the status filters.
/// </summary>
/// <param name="All">The applications the reader sees.</param>
/// <param name="NeedsFormalData">The filed applications still without the date for the formal check.</param>
/// <param name="ByStatus">The applications by status; a status without any is absent.</param>
internal sealed record ApplicationCountsResponse(int All, int NeedsFormalData, IReadOnlyList<ApplicationStatusCountResponse> ByStatus);
