namespace Competency.Promotion;

/// <summary>
/// The commission of a unit as it stands now: the members it has always and those it was given, and the chair if the chair still acts.
/// </summary>
/// <param name="UnitId">The unit.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="Version">The version of the stored commission; 0 while none is stored.</param>
/// <param name="ChairEmployeeId">The chair; null when none is set or the one set is no longer a member who acts.</param>
/// <param name="ResponsibleHrUserId">The account of the responsible HR of the unit; null when the unit has none who can act.</param>
/// <param name="Members">The members, the required ones first.</param>
internal sealed record CommissionRoster(
    Guid UnitId,
    string UnitName,
    int Version,
    Guid? ChairEmployeeId,
    Guid? ResponsibleHrUserId,
    IReadOnlyList<CommissionMember> Members);
