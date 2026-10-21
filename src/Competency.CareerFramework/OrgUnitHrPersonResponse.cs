using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// An HR assignment as shown on a unit: the assigned employee, named by full name, the unit the assignment belongs to, and, for global
/// administrators only, the account bound to the employee with its state, which tells whether the assignment acts and if not, why. Other users
/// get only assignments that act, and the account is then absent from the JSON.
/// </summary>
internal sealed record OrgUnitHrPersonResponse(
    Guid EmployeeId,
    string Name,
    Guid OrgUnitId,
    string OrgUnitName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UnitAccountResponse? Account = null);
