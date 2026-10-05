using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Competency.Platform;

/// <summary>
/// The strong entity tag that carries an entity version in HTTP headers.
/// </summary>
public static class VersionETag
{
    /// <summary>
    /// Formats a version as a quoted strong entity tag.
    /// </summary>
    /// <param name="version">The entity version.</param>
    /// <returns>The entity tag, for example <c>"3"</c> including the quotes.</returns>
    public static string Format(int version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    /// <summary>
    /// Sets the <c>ETag</c> response header to the given version.
    /// </summary>
    /// <param name="response">The response being written.</param>
    /// <param name="version">The current version of the returned entity.</param>
    public static void SetETag(this HttpResponse response, int version) => response.Headers.ETag = Format(version);

    /// <summary>
    /// Declares that the endpoint returns an <c>ETag</c> header, so the API description documents it.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or group to describe.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder ProducesETag<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder => builder.WithMetadata(new ETagResponseMetadata());

    internal static bool TryParse(StringValues header, out int version)
    {
        version = 0;
        return EntityTagHeaderValue.TryParse(header.ToString(), out var tag)
            && !tag.IsWeak
            && int.TryParse(tag.Tag.AsSpan().Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out version);
    }
}
