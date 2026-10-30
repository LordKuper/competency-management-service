namespace Kalibr.Promotion;

/// <summary>
/// The artifacts of an application. The set is replaced, so a competency left out has none.
/// </summary>
internal sealed record ArtifactsRequest
{
    private const int MaxCompetencies = 500;
    private const string Field = "artifacts";

    /// <summary>
    /// One entry at most per competency.
    /// </summary>
    public required ArtifactInput[] Artifacts { get; init; }

    /// <summary>
    /// Checks the shape of the request: each competency has one to ten artifacts, each with a description and a link that leads to a web page.
    /// </summary>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public Dictionary<string, string[]> Validate()
    {
        if (Artifacts is not { Length: <= MaxCompetencies } || Artifacts.Any(artifact => artifact?.Links is null || artifact.Links.Any(link => link is null)))
        {
            return new Dictionary<string, string[]> { [Field] = ["Добавьте артефакты к компетенциям"] };
        }

        List<string> problems = [];
        if (Artifacts.Select(artifact => artifact.CompetencyId).Distinct().Count() != Artifacts.Length)
        {
            problems.Add("Каждая компетенция указывается один раз");
        }

        if (Artifacts.Any(artifact => artifact.Links.Length is 0 or > ArtifactInput.MaxLinks))
        {
            problems.Add($"К компетенции можно добавить от 1 до {ArtifactInput.MaxLinks} артефактов");
        }

        var links = Artifacts.SelectMany(artifact => artifact.Links).ToList();
        if (links.Any(link => string.IsNullOrWhiteSpace(link.Description) || link.Description.Length > ArtifactLink.MaxDescriptionLength))
        {
            problems.Add($"Опишите, что сделано, не больше {ArtifactLink.MaxDescriptionLength} знаков");
        }

        if (links.Any(link => !ArtifactLink.IsWebLink(link.Url)))
        {
            problems.Add("Ссылка должна начинаться с http:// или https://");
        }

        return problems.Count == 0 ? [] : new Dictionary<string, string[]> { [Field] = [.. problems] };
    }
}
