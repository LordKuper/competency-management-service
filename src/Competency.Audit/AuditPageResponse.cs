using Competency.Platform;

namespace Competency.Audit;

/// <summary>
/// One page of journal rows, newest first. It exists only to keep the <c>AuditPageResponse</c> schema name of the published contract.
/// </summary>
internal sealed record AuditPageResponse(IReadOnlyList<AuditEventResponse> Items, int Total, int Page, int PageSize)
    : PageResponse<AuditEventResponse>(Items, Total, Page, PageSize);
