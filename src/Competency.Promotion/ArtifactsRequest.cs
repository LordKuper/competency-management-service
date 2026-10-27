namespace Competency.Promotion;

/// <summary>
/// The artifacts of an application. The set is replaced, so a competency left out has none.
/// </summary>
internal sealed record ArtifactsRequest
{
    private const int MaxArtifacts = 500;
    private const string Field = "artifacts";

    /// <summary>
    /// One artifact at most per competency.
    /// </summary>
    public required ArtifactInput[] Artifacts { get; init; }

    /// <summary>
    /// Checks the shape of the request: a description for each artifact, and links that lead to a web page.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        if (Artifacts is not { Length: <= MaxArtifacts } || Artifacts.Any(artifact => artifact is null))
        {
            return new Dictionary<string, string[]> { [Field] = ["Добавьте артефакты к компетенциям"] };
        }

        List<string> problems = [];
        if (Artifacts.Select(artifact => artifact.CompetencyId).Distinct().Count() != Artifacts.Length)
        {
            problems.Add("К компетенции можно добавить один артефакт");
        }

        if (Artifacts.Any(artifact => string.IsNullOrWhiteSpace(artifact.Text) || artifact.Text.Length > ArtifactInput.MaxTextLength))
        {
            problems.Add($"Опишите артефакт, не больше {ArtifactInput.MaxTextLength} знаков");
        }

        if (Artifacts.Any(artifact => artifact.Links is not { Length: <= ArtifactInput.MaxLinks }))
        {
            problems.Add($"Ссылок не больше {ArtifactInput.MaxLinks} на компетенцию");
        }
        else if (Artifacts.SelectMany(artifact => artifact.Links).Any(link => !ArtifactInput.IsWebLink(link)))
        {
            problems.Add("Ссылка должна начинаться с http:// или https://");
        }

        return problems.Count == 0 ? [] : new Dictionary<string, string[]> { [Field] = [.. problems] };
    }
}
