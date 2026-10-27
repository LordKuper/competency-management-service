namespace Competency.Promotion;

/// <summary>
/// The raters of an application. The list is replaced.
/// </summary>
internal sealed record RatersRequest
{
    /// <summary>
    /// The employees asked to rate the candidate.
    /// </summary>
    public required Guid[] RaterEmployeeIds { get; init; }

    /// <summary>
    /// Checks the size of the list.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() =>
        RaterEmployeeIds is { Length: <= ApplicationRequest.MaxRaters }
            ? []
            : new Dictionary<string, string[]> { ["raterEmployeeIds"] = [$"Укажите не больше {ApplicationRequest.MaxRaters} оценивающих"] };
}
