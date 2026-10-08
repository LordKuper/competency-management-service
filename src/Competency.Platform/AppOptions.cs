namespace Competency.Platform;

/// <summary>
/// Settings of the application as a whole, bound from the <c>App</c> configuration section.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Absolute http(s) address users open the application at; links in e-mails are built from it, never from the request's <c>Host</c>.
    /// </summary>
    public Uri PublicBaseUrl { get; set; } = null!;
}
