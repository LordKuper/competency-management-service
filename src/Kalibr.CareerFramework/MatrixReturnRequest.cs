namespace Competency.CareerFramework;

/// <summary>
/// The comment an approver returns a version with; it may be left out when the approver changed the version.
/// </summary>
internal sealed record MatrixReturnRequest
{
    public required string? Comment { get; init; }

    /// <summary>
    /// The comment as stored: trimmed, and null when empty.
    /// </summary>
    /// <returns>The stored comment.</returns>
    public string? TrimmedComment() => string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

    /// <summary>
    /// Checks the request.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() => TrimmedComment()?.Length > MatrixDecision.CommentMaxLength
        ? new Dictionary<string, string[]> { ["comment"] = [$"Не длиннее {MatrixDecision.CommentMaxLength} символов"] }
        : [];
}
