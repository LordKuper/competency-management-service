using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// The API of the HR assigned to a unit. Any signed-in user reads the assigned and inherited HR of a unit they can see; only global
/// administrators assign and remove, under the exclusive tree lock taken first, which blocking an account also takes, so the account
/// checked as able to act stays so until the assignment commits.
/// </summary>
internal static class OrgUnitHrEndpoints
{
    private const string Route = "/api/v1/career/org-units/{id:guid}/hr";
    private const string Tag = "Career";
    private const string UserField = "userId";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var readers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        readers.MapGet(string.Empty, GetAsync).WithName("GetOrgUnitHr").ProducesProblem(StatusCodes.Status404NotFound);

        var administrators = readers.MapGroup(string.Empty)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdmin)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        administrators.MapPut(string.Empty, AssignAsync).WithName("AssignOrgUnitHr");
        administrators.MapDelete(string.Empty, RemoveAsync).WithName("RemoveOrgUnitHr");
    }

    /// <summary>
    /// A deactivated unit is visible to global administrators only.
    /// </summary>
    private static async Task<Results<Ok<OrgUnitHrResponse>, NotFound>> GetAsync(
        Guid id,
        AppDbContext context,
        IEmployeeDirectory employees,
        IUserDirectory users,
        ICurrentActor actor,
        CancellationToken cancellationToken)
    {
        var ancestry = await employees.FindAncestryAsync(id, cancellationToken);
        if (ancestry is not [var unit, ..] || (!unit.IsActive && actor.Role != PlatformClaims.GlobalAdminRole))
        {
            return TypedResults.NotFound();
        }

        var ancestryIds = ancestry.Select(candidate => candidate.Id).ToList();
        var assigneeOf = await context.Set<OrgUnitHr>()
            .Where(assignment => ancestryIds.Contains(assignment.OrgUnitId))
            .ToDictionaryAsync(assignment => assignment.OrgUnitId, assignment => assignment.UserId, cancellationToken);
        var inheritedFrom = ancestry.Skip(1).FirstOrDefault(candidate => assigneeOf.ContainsKey(candidate.Id));

        return TypedResults.Ok(new OrgUnitHrResponse(
            await DescribeAsync(unit, assigneeOf, employees, users, cancellationToken),
            await DescribeAsync(inheritedFrom, assigneeOf, employees, users, cancellationToken)));
    }

    /// <summary>
    /// Assigns an account that can act as the unit's HR, replacing the unit's previous assignment; assigning the current HR again changes nothing.
    /// </summary>
    private static async Task<Results<NoContent, NotFound, ValidationProblem>> AssignAsync(
        Guid id,
        OrgUnitHrRequest request,
        AppDbContext context,
        IEmployeeDirectory employees,
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        await using var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        if (await employees.FindUnitAsync(id, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var user = await users.FindAsync(request.UserId, cancellationToken);
        if (user is not { IsActive: true })
        {
            return Rejection.Invalid(UserField, user is null ? "Пользователь не найден" : "Учётная запись заблокирована или не завершила регистрацию");
        }

        var assignment = await context.Set<OrgUnitHr>().FirstOrDefaultAsync(candidate => candidate.OrgUnitId == id, cancellationToken);
        if (assignment is null)
        {
            context.Add(new OrgUnitHr { OrgUnitId = id, UserId = user.Id });
        }
        else
        {
            assignment.UserId = user.Id;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Removes the unit's own assignment, so the unit inherits the HR of the nearest unit above it with one.
    /// </summary>
    private static async Task<Results<NoContent, NotFound>> RemoveAsync(
        Guid id,
        AppDbContext context,
        IEmployeeDirectory employees,
        CancellationToken cancellationToken)
    {
        await using var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        var assignment = await context.Set<OrgUnitHr>().FirstOrDefaultAsync(candidate => candidate.OrgUnitId == id, cancellationToken);
        if (assignment is null)
        {
            return TypedResults.NotFound();
        }

        context.Remove(assignment);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<OrgUnitHrPersonResponse?> DescribeAsync(
        OrgUnitStatus? source,
        IReadOnlyDictionary<Guid, Guid> assigneeOf,
        IEmployeeDirectory employees,
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        if (source is null
            || !assigneeOf.TryGetValue(source.Id, out var userId)
            || await users.FindAsync(userId, cancellationToken) is not { } user)
        {
            return null;
        }

        var employee = user.EmployeeId is { } employeeId ? await employees.FindAsync(employeeId, cancellationToken) : null;
        return new OrgUnitHrPersonResponse(user.Id, employee?.FullName ?? user.Email, user.IsActive, source.Id, source.Name);
    }
}
