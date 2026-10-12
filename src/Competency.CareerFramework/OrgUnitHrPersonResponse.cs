using System.Text.Json.Serialization;

namespace Competency.CareerFramework;

/// <summary>
/// An HR assignment as shown on a unit: the account, named by its employee's full name or else by its e-mail, the unit the assignment
/// belongs to, and whether the account can act, since an assigned account that cannot act leaves the unit without an HR. Only global
/// administrators get the flag; other users get only assignments whose account can act, and the flag is then absent from the JSON.
/// </summary>
internal sealed record OrgUnitHrPersonResponse(
    Guid UserId,
    string Name,
    Guid OrgUnitId,
    string OrgUnitName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsActive = null);
