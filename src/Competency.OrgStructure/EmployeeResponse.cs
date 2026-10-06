using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// An employee as the API returns it, in one of two projections chosen by the caller's role.
/// Global administrators get everything. Other users get the name, e-mail, unit and position only;
/// the status and version are then absent from the JSON, and are never read from the database.
/// The e-mail is the one of the account bound to the employee, read in the same query, and is null when there is no account.
/// </summary>
internal sealed record EmployeeResponse(
    Guid Id,
    string FullName,
    string? Email,
    Guid OrgUnitId,
    string OrgUnitName,
    string Position,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsActive = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Version = null)
{
    public static Expression<Func<Employee, EmployeeResponse>> FullProjection(AppDbContext context)
    {
        var accounts = context.Set<EmployeeAccount>();
        return employee => new EmployeeResponse(
            employee.Id,
            employee.FullName,
            accounts.Where(account => account.EmployeeId == employee.Id).Select(account => account.Email).FirstOrDefault(),
            employee.OrgUnitId,
            employee.OrgUnit.Name,
            employee.Position,
            employee.IsActive,
            employee.Version);
    }

    public static Expression<Func<Employee, EmployeeResponse>> ProjectionFor(AppDbContext context, ICurrentActor actor)
    {
        if (actor.IsGlobalAdmin())
        {
            return FullProjection(context);
        }

        var accounts = context.Set<EmployeeAccount>();
        return employee => new EmployeeResponse(
            employee.Id,
            employee.FullName,
            accounts.Where(account => account.EmployeeId == employee.Id).Select(account => account.Email).FirstOrDefault(),
            employee.OrgUnitId,
            employee.OrgUnit.Name,
            employee.Position,
            null,
            null);
    }
}
