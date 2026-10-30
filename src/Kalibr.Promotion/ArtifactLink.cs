using Competency.Platform;

namespace Competency.Promotion;

/// <summary>
/// One artifact of a competency: what the candidate did and where the document that shows it is kept. The same shape is asked for, stored and served.
/// </summary>
/// <param name="Description">What the candidate did, in words.</param>
/// <param name="Url">The absolute http or https address of the document in another system.</param>
internal sealed record ArtifactLink(string Description, string Url)
{
    public const int MaxDescriptionLength = 1000;
    private const int MaxUrlLength = 2000;

    /// <summary>
    /// Whether a link is an absolute address of a web page.
    /// </summary>
    /// <param name="link">The link as given.</param>
    /// <returns><see langword="true"/> when it is an http or https address of a sensible length.</returns>
    public static bool IsWebLink(string? link) => link is { Length: <= MaxUrlLength } && WebAddress.IsHttp(link);
}
