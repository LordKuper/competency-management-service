namespace Kalibr.Promotion;

/// <summary>
/// A unit with the state of its commission, a row of the list of commissions.
/// </summary>
/// <param name="UnitId">The unit.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="MemberCount">The members, the required ones included.</param>
/// <param name="HasChair">Whether the unit has a chair who acts.</param>
/// <param name="ChairName">The chair's full name; null while there is none.</param>
internal sealed record CommissionSummaryResponse(Guid UnitId, string UnitName, int MemberCount, bool HasChair, string? ChairName);
