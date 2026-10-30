using Microsoft.AspNetCore.Http;

namespace Competency.Platform;

/// <summary>
/// Rejects state-changing requests that a browser reports as cross-site, as a second CSRF defence next to SameSite cookies.
/// A request is cross-site when <c>Sec-Fetch-Site</c> is anything but same-origin or none, or when <c>Origin</c> names another host.
/// Requests carrying neither header, such as those from non-browser clients, pass.
/// The origin scheme is not compared: behind a TLS-terminating proxy the request scheme differs from the browser's.
/// </summary>
internal sealed class CrossSiteRequestMiddleware(RequestDelegate next)
{
    private const string FetchSiteHeader = "Sec-Fetch-Site";
    private const string SameOrigin = "same-origin";
    private const string UserInitiated = "none";

    public Task InvokeAsync(HttpContext context)
    {
        if (ChangesState(context.Request.Method) && IsCrossSite(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        return next(context);
    }

    private static bool ChangesState(string method) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method));

    private static bool IsCrossSite(HttpRequest request)
    {
        var fetchSite = request.Headers[FetchSiteHeader].ToString();
        if (fetchSite.Length > 0 && fetchSite is not (SameOrigin or UserInitiated))
        {
            return true;
        }

        var origin = request.Headers.Origin;
        return origin.Count > 0
            && !(Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var originUri)
                && string.Equals(originUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase));
    }
}
