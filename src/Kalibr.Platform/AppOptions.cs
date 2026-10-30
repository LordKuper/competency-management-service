namespace Kalibr.Platform;

/// <summary>
/// Settings of the application as a whole, bound from the <c>App</c> configuration section.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Absolute http(s) address users open the application at; links in e-mails are built from it, never from the request's <c>Host</c>.
    /// </summary>
    public Uri PublicBaseUrl { get; set; } = null!;

    /// <summary>
    /// The IANA identifier of the time zone of the organization, which decides where one day ends and the next begins for dates that carry no time.
    /// </summary>
    public string TimeZone { get; set; } = "Europe/Moscow";
}
