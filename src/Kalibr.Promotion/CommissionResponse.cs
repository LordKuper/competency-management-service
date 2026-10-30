namespace Kalibr.Promotion;

/// <summary>
/// The commission of a unit as the API returns it.
/// </summary>
internal sealed record CommissionResponse(Guid UnitId, string UnitName, Guid? ChairEmployeeId, IReadOnlyList<CommissionMemberResponse> Members)
{
    public static CommissionResponse From(CommissionRoster roster) =>
        new(roster.UnitId, roster.UnitName, roster.ChairEmployeeId, [.. roster.Members.Select(CommissionMemberResponse.From)]);
}
