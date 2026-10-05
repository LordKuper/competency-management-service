using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Competency.Platform;

namespace Competency.OrgStructure;

/// <summary>
/// An employee as the API returns it, in one of two projections chosen by the caller's role.
/// Global administrators get everything. Other users get the name, e-mail, unit and position only;
/// the personnel number, status and version are then absent from the JSON, and are never read from the database.
/// </summary>
internal sealed record EmployeeResponse(
    Guid Id,
    string FullName,
    string Email,
    Guid OrgUnitId,
    string OrgUnitName,
    string Position,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PersonnelNumber = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsActive = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Version = null)
{
    private static readonly Expression<Func<Employee, EmployeeResponse>> FullProjection = employee => new EmployeeResponse(
        employee.Id,
        employee.FullName,
        employee.Email,
        employee.OrgUnitId,
        employee.OrgUnit.Name,
        employee.Position,
        employee.PersonnelNumber,
        employee.IsActive,
        employee.Version);

    private static readonly Expression<Func<Employee, EmployeeResponse>> RestrictedProjection = employee => new EmployeeResponse(
        employee.Id,
        employee.FullName,
        employee.Email,
        employee.OrgUnitId,
        employee.OrgUnit.Name,
        employee.Position,
        null,
        null,
        null);

    private static readonly Func<Employee, EmployeeResponse> CompiledFull = FullProjection.Compile();

    public static Expression<Func<Employee, EmployeeResponse>> ProjectionFor(ICurrentActor actor) =>
        actor.IsGlobalAdmin() ? FullProjection : RestrictedProjection;

    public static EmployeeResponse Full(Employee employee) => CompiledFull(employee);
}
