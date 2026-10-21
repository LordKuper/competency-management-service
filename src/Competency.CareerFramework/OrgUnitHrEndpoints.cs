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
/// The API of the employee assigned to a unit as its responsible HR. Any signed-in user reads the assigned and inherited HR of a unit they
/// can see; only global administrators assign and remove, under the exclusive tree lock taken first, which dismissing or deleting an employee
/// and changing the roles, binding or block of an account also take, so the employee and account checked stay so until the assignment commits.
/// </summary>
internal static class OrgUnitHrEndpoints
{
    private const string Route = "/api/v1/career/org-units/{id:guid}/hr";
    private const string Tag = "Career";
    private const string EmployeeField = "employeeId";

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
    /// A deactivated unit is visible to global administrators only. Global administrators also see assignments that do not act, with the
    /// account that says why. Other users see only assignments that act, without the account: an own assignment that does not act leaves the
    /// unit without an HR, so neither it nor the inherited one is shown, and an inherited one that does not act is not shown either.
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
        var isAdmin = actor.IsGlobalAdmin();
        if (ancestry is not [var unit, ..] || (!unit.IsActive && !isAdmin))
        {
            return TypedResults.NotFound();
        }

        var ancestryIds = ancestry.Select(candidate => candidate.Id).ToList();
        var assigneeOf = await context.Set<OrgUnitHr>()
            .Where(assignment => ancestryIds.Contains(assignment.OrgUnitId))
            .ToDictionaryAsync(assignment => assignment.OrgUnitId, assignment => assignment.EmployeeId, cancellationToken);
        var inheritedFrom = ancestry.Skip(1).FirstOrDefault(candidate => assigneeOf.ContainsKey(candidate.Id));

        var assigned = await DescribeAsync(unit, assigneeOf, employees, users, cancellationToken);
        var inherited = await DescribeAsync(inheritedFrom, assigneeOf, employees, users, cancellationToken);
        if (isAdmin)
        {
            return TypedResults.Ok(new OrgUnitHrResponse(assigned, inherited));
        }

        return TypedResults.Ok(assigned is not null && !Acts(assigned)
            ? new OrgUnitHrResponse(null, null)
            : new OrgUnitHrResponse(Acting(assigned), Acting(inherited)));
    }

    /// <summary>
    /// Assigns an employee as the unit's responsible HR, replacing the unit's previous assignment; assigning the current HR again changes nothing.
    /// The employee must work and be bound to an account that can act and has the HR role; the first rule broken is the field error.
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

        if (await CandidateProblemAsync(request.EmployeeId, employees, users, cancellationToken) is { } problem)
        {
            return Rejection.Invalid(EmployeeField, problem);
        }

        var assignment = await context.Set<OrgUnitHr>().FirstOrDefaultAsync(candidate => candidate.OrgUnitId == id, cancellationToken);
        if (assignment is null)
        {
            context.Add(new OrgUnitHr { OrgUnitId = id, EmployeeId = request.EmployeeId });
        }
        else
        {
            assignment.EmployeeId = request.EmployeeId;
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

    /// <summary>
    /// Says why an employee cannot be assigned as a responsible HR, by the rule of <see cref="UserStatus.CanBeHr"/> that also picks the candidates.
    /// </summary>
    /// <returns>The first rule broken, or <see langword="null"/> when the employee can be assigned.</returns>
    private static async Task<string?> CandidateProblemAsync(
        Guid employeeId,
        IEmployeeDirectory employees,
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        var employee = await employees.FindAsync(employeeId, cancellationToken);
        if (employee is not { IsActive: true })
        {
            return employee is null ? "Сотрудник не найден" : "Сотрудник не работает";
        }

        return (await users.FindByEmployeeIdsAsync([employeeId], cancellationToken)).GetValueOrDefault(employeeId) switch
        {
            null => "К сотруднику не привязана учётная запись",
            { IsActive: false } => "Учётная запись заблокирована или не завершила регистрацию",
            { IsHr: false } => "У учётной записи нет роли «HR»",
            _ => null,
        };
    }

    /// <summary>
    /// Whether an assignment acts: the account bound to the assigned employee can act and has the HR role.
    /// </summary>
    /// <param name="person">The assignment, with its account.</param>
    /// <returns><see langword="true"/> when the assignment gives the unit a responsible HR.</returns>
    public static bool Acts(OrgUnitHrPersonResponse person) => person.Account is { State: UnitAccountState.Active };

    /// <summary>
    /// The assignment as users other than global administrators see it: only when it acts, and without its account.
    /// </summary>
    /// <param name="person">The assignment, with its account; null for none.</param>
    /// <returns>The assignment without its account, or <see langword="null"/> when there is none or it does not act.</returns>
    public static OrgUnitHrPersonResponse? Acting(OrgUnitHrPersonResponse? person) =>
        person is not null && Acts(person) ? person with { Account = null } : null;

    /// <summary>
    /// Describes the assignment of a unit with the account bound to the assigned employee now, which tells whether it acts.
    /// </summary>
    /// <param name="source">The unit whose assignment it is; null for none.</param>
    /// <param name="assigneeOf">The assigned employee by unit.</param>
    /// <param name="employees">The employee directory, for the employee's name.</param>
    /// <param name="users">The account directory.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The assignment, or <see langword="null"/> when the unit has none.</returns>
    public static async Task<OrgUnitHrPersonResponse?> DescribeAsync(
        OrgUnitStatus? source,
        IReadOnlyDictionary<Guid, Guid> assigneeOf,
        IEmployeeDirectory employees,
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        if (source is null
            || !assigneeOf.TryGetValue(source.Id, out var employeeId)
            || await employees.FindAsync(employeeId, cancellationToken) is not { } employee)
        {
            return null;
        }

        var account = (await users.FindByEmployeeIdsAsync([employeeId], cancellationToken)).GetValueOrDefault(employeeId);
        return new OrgUnitHrPersonResponse(employee.Id, employee.FullName, source.Id, source.Name, UnitAccountResponse.Of(account, forHr: true));
    }
}
