namespace Competency.Audit;

/// <summary>
/// One page of journal rows, newest first, with the number of rows matching the filters across all pages.
/// </summary>
internal sealed record AuditPageResponse(IReadOnlyList<AuditEventResponse> Items, int Total, int Page, int PageSize);
