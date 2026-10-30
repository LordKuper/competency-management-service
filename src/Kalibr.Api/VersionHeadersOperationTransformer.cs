using Competency.Platform;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Competency.Api;

/// <summary>
/// Describes the concurrency headers of operations the platform marks: <c>If-Match</c> on requests and <c>ETag</c> on successful responses.
/// </summary>
internal sealed class VersionHeadersOperationTransformer : IOpenApiOperationTransformer
{
    private const string IfMatchHeader = "If-Match";
    private const string ETagHeader = "ETag";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IfMatchRequiredMetadata>().Any())
        {
            AddIfMatchParameter(operation);
        }

        if (metadata.OfType<ETagResponseMetadata>().Any())
        {
            AddETagHeader(operation);
        }

        return Task.CompletedTask;
    }

    private static void AddIfMatchParameter(OpenApiOperation operation)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IfMatchHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description = "The strong entity tag of the version being modified, as returned in the ETag header of the resource.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
        });
    }

    private static void AddETagHeader(OpenApiOperation operation)
    {
        foreach (var (status, response) in operation.Responses ?? [])
        {
            if (!status.StartsWith('2') || response is not OpenApiResponse concrete)
            {
                continue;
            }

            concrete.Headers ??= new Dictionary<string, IOpenApiHeader>();
            concrete.Headers[ETagHeader] = new OpenApiHeader
            {
                Description = "The strong entity tag of the returned version; send it back in If-Match to modify the resource.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            };
        }
    }
}
