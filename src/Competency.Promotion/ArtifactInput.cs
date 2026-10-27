namespace Competency.Promotion;

/// <summary>
/// The evidence a candidate offers for one competency.
/// </summary>
/// <param name="CompetencyId">The competency of the matrix version.</param>
/// <param name="Text">What the candidate did, in words.</param>
/// <param name="Links">Addresses of documents in other systems; may be empty.</param>
internal sealed record ArtifactInput(Guid CompetencyId, string Text, string[] Links)
{
    public const int MaxTextLength = 4000;
    public const int MaxLinks = 10;
    private const int MaxLinkLength = 2000;

    /// <summary>
    /// Whether a link is an absolute address of a web page.
    /// </summary>
    /// <param name="link">The link as given.</param>
    /// <returns><see langword="true"/> when it is an http or https address of a sensible length.</returns>
    public static bool IsWebLink(string? link) =>
        link is { Length: <= MaxLinkLength }
        && Uri.TryCreate(link, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
