using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Competency.Platform;

/// <summary>
/// The entity version a client sends in <c>If-Match</c> to modify a resource.
/// Declaring it as an endpoint parameter makes the header mandatory and documents it with its error responses.
/// </summary>
/// <param name="Version">The version the client last saw.</param>
public sealed record IfMatch(int Version) : IBindableFromHttpContext<IfMatch>, IEndpointParameterMetadataProvider
{
    private const string ProblemContentType = "application/problem+json";

    private static readonly int[] ProblemStatuses =
    [
        StatusCodes.Status400BadRequest,
        StatusCodes.Status412PreconditionFailed,
        StatusCodes.Status428PreconditionRequired,
    ];

    /// <summary>
    /// Guards the next save of the entity: sets the client's version as the original value of <see cref="IVersioned.Version"/>,
    /// so the update only matches a row still at that version and otherwise throws <see cref="DbUpdateConcurrencyException"/>.
    /// </summary>
    /// <param name="context">The context tracking the entity.</param>
    /// <param name="entity">The tracked entity about to be modified.</param>
    public void ApplyTo(DbContext context, IVersioned entity) =>
        context.Entry(entity).Property(nameof(IVersioned.Version)).OriginalValue = Version;

    /// <summary>
    /// Reads <c>If-Match</c>; an absent header is rejected as 428 and anything but a single strong tag issued by this API as 400.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="parameter">The handler parameter being bound.</param>
    /// <returns>The version the client sent.</returns>
    public static ValueTask<IfMatch?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var header = context.Request.Headers.IfMatch;
        if (header.Count == 0)
        {
            throw new BadHttpRequestException("The If-Match header is required.", StatusCodes.Status428PreconditionRequired);
        }

        return VersionETag.TryParse(header, out var version)
            ? ValueTask.FromResult<IfMatch?>(new IfMatch(version))
            : throw new BadHttpRequestException("The If-Match header must be a single strong entity tag.", StatusCodes.Status400BadRequest);
    }

    /// <inheritdoc />
    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new IfMatchRequiredMetadata());
        foreach (var status in ProblemStatuses)
        {
            builder.Metadata.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetails), status, ProblemContentType));
        }
    }
}
