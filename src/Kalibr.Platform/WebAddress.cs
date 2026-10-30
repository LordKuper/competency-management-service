namespace Competency.Platform;

/// <summary>
/// Checks of addresses of web pages that several modules share.
/// </summary>
public static class WebAddress
{
    /// <summary>
    /// Whether text is an absolute http or https address.
    /// </summary>
    /// <param name="text">The text as given.</param>
    /// <returns><see langword="true"/> when it is an absolute http or https address.</returns>
    public static bool IsHttp(string? text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
