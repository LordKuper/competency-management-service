using System.Linq.Expressions;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.Audit;

/// <summary>
/// The read-only journal API for global administrators; the journal has no write endpoint.
/// </summary>
internal static class AuditEndpoints
{
    private const string Route = "/api/v1/audit";
    private const string Tag = "Audit";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, ListAsync).WithName("ListAuditEvents");
    }

    private static async Task<Results<Ok<AuditPageResponse>, ValidationProblem>> ListAsync(
        [AsParameters] AuditQuery query,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var from = query.From?.ToUniversalTime();
        var to = query.To?.ToUniversalTime();
        var matching = context.Set<AuditEvent>()
            .AsNoTracking()
            .WhereIf(from is not null, row => row.Timestamp >= from)
            .WhereIf(to is not null, row => row.Timestamp <= to)
            .WhereIf(!string.IsNullOrEmpty(query.Actor), row => row.Actor == query.Actor)
            .WhereIf(!string.IsNullOrEmpty(query.Action), row => row.Action == query.Action)
            .WhereIf(!string.IsNullOrEmpty(query.EntityType), row => row.EntityType == query.EntityType)
            .WhereIf(!string.IsNullOrEmpty(query.EntityId), row => row.EntityId == query.EntityId)
            .WhereIf(!string.IsNullOrEmpty(query.RequestId), row => row.RequestId == query.RequestId);

        var total = await matching.CountAsync(cancellationToken);
        var rows = await matching
            .OrderByDescending(row => row.Timestamp)
            .ThenBy(row => row.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new AuditPageResponse(rows.Select(AuditEventResponse.From).ToList(), total, query.Page, query.PageSize));
    }

    private static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? source.Where(predicate) : source;
}
