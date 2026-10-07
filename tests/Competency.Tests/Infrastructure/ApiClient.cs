using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Sdk;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// One caller of the API: its own cookie jar, so a client is either anonymous or holds the session it signed in with.
/// </summary>
public sealed class ApiClient : IDisposable
{
    private readonly HttpClient http;

    public ApiClient(Uri baseAddress)
    {
        http = new HttpClient(new HttpClientHandler { UseCookies = true, AllowAutoRedirect = false }) { BaseAddress = baseAddress };
    }

    public Task<ApiResponse> GetAsync(string path, Action<HttpRequestMessage>? configure = null) =>
        SendAsync(HttpMethod.Get, path, configure: configure);

    public Task<ApiResponse> PostAsync(string path, object? body = null, string? ifMatch = null, Action<HttpRequestMessage>? configure = null) =>
        SendAsync(HttpMethod.Post, path, body, ifMatch, configure);

    public Task<ApiResponse> PutAsync(string path, object? body, string? ifMatch = null, Action<HttpRequestMessage>? configure = null) =>
        SendAsync(HttpMethod.Put, path, body, ifMatch, configure);

    public Task<ApiResponse> DeleteAsync(string path, string? ifMatch = null) =>
        SendAsync(HttpMethod.Delete, path, ifMatch: ifMatch);

    public async Task<ApiResponse> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        string? ifMatch = null,
        Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonSerializerOptions.Web);
        }

        if (ifMatch is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        configure?.Invoke(request);
        using var response = await http.SendAsync(request, TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return new ApiResponse(
            response.StatusCode,
            text,
            response.Headers.ETag?.ToString(),
            response.Headers.TryGetValues("X-Request-Id", out var ids) ? ids.Single() : null,
            response.Content.Headers.ContentType?.MediaType,
            text.Length > 0 && response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.Ordinal) == true
                ? JsonNode.Parse(text)
                : null);
    }

    public void Dispose() => http.Dispose();
}

/// <summary>
/// A response read completely: status, raw body, parsed JSON when the body is JSON, and the headers the tests look at.
/// </summary>
/// <param name="Status">The HTTP status.</param>
/// <param name="Body">The body text.</param>
/// <param name="ETag">The <c>ETag</c> header with its quotes, ready to send back as <c>If-Match</c>.</param>
/// <param name="RequestId">The <c>X-Request-Id</c> header.</param>
/// <param name="ContentType">The media type of the body.</param>
/// <param name="Json">The parsed body, or null when there is none or it is not JSON.</param>
public sealed record ApiResponse(HttpStatusCode Status, string Body, string? ETag, string? RequestId, string? ContentType, JsonNode? Json)
{
    public int Code => (int)Status;

    public Guid Id => Guid.Parse(Json!["id"]!.GetValue<string>());

    /// <summary>
    /// Fails with the whole response when the status differs, so a wrong status shows the server's reason.
    /// </summary>
    /// <param name="expected">The expected status.</param>
    /// <returns>The same response, for chaining.</returns>
    public ApiResponse Expect(HttpStatusCode expected)
    {
        if (Status != expected)
        {
            throw new XunitException($"Expected {(int)expected} {expected} but got {Code} {Status}: {Body}");
        }

        return this;
    }

    /// <summary>
    /// The validation messages of the field, for a problem response.
    /// </summary>
    /// <param name="field">The request field name.</param>
    /// <returns>The messages the server reported for the field; empty when there are none.</returns>
    public string[] FieldErrors(string field) =>
        Json?["errors"]?[field]?.AsArray().Select(message => message!.GetValue<string>()).ToArray() ?? [];
}
