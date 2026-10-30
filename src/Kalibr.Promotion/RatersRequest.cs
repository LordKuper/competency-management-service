namespace Competency.Promotion;

/// <summary>
/// The raters of an application. The list is replaced.
/// </summary>
internal sealed record RatersRequest
{
    /// <summary>
    /// The employees asked to rate the candidate, each with the address to invite them at when they have no account.
    /// </summary>
    public required RaterInput[] Raters { get; init; }

    /// <summary>
    /// Checks the size of the list.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate() =>
        Raters is { Length: <= ApplicationRequest.MaxRaters } && Raters.All(rater => rater is not null)
            ? []
            : new Dictionary<string, string[]> { ["raters"] = [$"Укажите не больше {ApplicationRequest.MaxRaters} оценивающих"] };
}
