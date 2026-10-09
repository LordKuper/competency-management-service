namespace Competency.CareerFramework;

/// <summary>
/// An HR assignment as shown on a unit: the account, named by its employee's full name or else by its e-mail, whether it can act,
/// since an assigned account that cannot act leaves the unit without an HR, and the unit the assignment belongs to.
/// </summary>
internal sealed record OrgUnitHrPersonResponse(Guid UserId, string Name, bool IsActive, Guid OrgUnitId, string OrgUnitName);
