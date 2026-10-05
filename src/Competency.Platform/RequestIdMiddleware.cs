using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Competency.Platform;

/// <summary>
/// Gives every request an identifier, taken from a well-formed incoming <c>X-Request-Id</c> or generated,
/// and exposes it as the <c>request_id</c> log scope, in <see cref="HttpContext.Items"/> and on the response.
/// </summary>
internal sealed class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public const string HeaderName = "X-Request-Id";
    public const string ItemKey = "request_id";

    private const string ScopeTemplate = "{" + ItemKey + "}";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = ResolveRequestId(context.Request.Headers[HeaderName].ToString());
        context.Items[ItemKey] = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(ScopeTemplate, requestId);
        await next(context);
    }

    private static string ResolveRequestId(string incoming) =>
        IsWellFormed(incoming) ? incoming : Guid.NewGuid().ToString("N");

    private static bool IsWellFormed(string value) =>
        value.Length is > 0 and <= MaxLength
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}
