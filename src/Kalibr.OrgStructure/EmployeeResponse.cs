using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;

namespace Kalibr.OrgStructure;

/// <summary>
/// An employee as the API returns it, in one of two projections chosen by the caller's roles.
/// Global administrators and HR get everything. Other users get the name, e-mail, unit and position only;
/// the status and version are then absent from the JSON, and are never read from the database.
/// The full name is the display form of the name parts, derived by the database and read-only.
/// The e-mail is the one of the account bound to the employee, read in the same query, and is null when there is no account.
/// <see cref="HasAccount"/> says whether an account is bound, so a picker knows without reading the e-mail, which is the same fact.
/// The position comes as its catalogue identifier and its current name.
/// <see cref="Education"/> is the level of education, null when it is not known; the restricted projection leaves it out.
/// The unit, position and <see cref="LegalEntities"/> are the main assignment; <see cref="Assignments"/> are the secondary ones.
/// <see cref="UnitAssignments"/> is empty unless the list is filtered by unit: it then holds the employee's assignments, main or secondary, in that unit
/// (or below it when descendants are included), which is what a unit's card of the employee shows.
/// The projections leave the three lists empty, and <see cref="EmployeeAssignmentReader"/> fills them from the assignment tables.
/// </summary>
internal sealed record EmployeeResponse(
    Guid Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string FullName,
    string? Email,
    Guid OrgUnitId,
    string OrgUnitName,
    Guid PositionId,
    string Position,
    IReadOnlyList<LegalEntityResponse> LegalEntities,
    IReadOnlyList<AssignmentResponse> Assignments,
    IReadOnlyList<AssignmentResponse> UnitAssignments,
    EducationLevel? Education,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsActive = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Version = null)
{
    /// <summary>Whether an account is bound to the employee.</summary>
    public bool HasAccount => Email is not null;

    private static readonly IReadOnlyList<LegalEntityResponse> NoLegalEntities = [];
    private static readonly IReadOnlyList<AssignmentResponse> NoAssignments = [];

    public static Expression<Func<Employee, EmployeeResponse>> FullProjection(AppDbContext context)
    {
        var accounts = context.Set<EmployeeAccount>();
        return employee => new EmployeeResponse(
            employee.Id,
            employee.LastName,
            employee.FirstName,
            employee.MiddleName,
            employee.FullName,
            accounts.Where(account => account.EmployeeId == employee.Id).Select(account => account.Email).FirstOrDefault(),
            employee.OrgUnitId,
            employee.OrgUnit.Name,
            employee.PositionId,
            employee.Position.Name,
            NoLegalEntities,
            NoAssignments,
            NoAssignments,
            employee.Education,
            employee.IsActive,
            employee.Version);
    }

    public static Expression<Func<Employee, EmployeeResponse>> ProjectionFor(AppDbContext context, ICurrentActor actor)
    {
        if (actor.SeesAll())
        {
            return FullProjection(context);
        }

        var accounts = context.Set<EmployeeAccount>();
        return employee => new EmployeeResponse(
            employee.Id,
            employee.LastName,
            employee.FirstName,
            employee.MiddleName,
            employee.FullName,
            accounts.Where(account => account.EmployeeId == employee.Id).Select(account => account.Email).FirstOrDefault(),
            employee.OrgUnitId,
            employee.OrgUnit.Name,
            employee.PositionId,
            employee.Position.Name,
            NoLegalEntities,
            NoAssignments,
            NoAssignments,
            null,
            null,
            null);
    }
}
