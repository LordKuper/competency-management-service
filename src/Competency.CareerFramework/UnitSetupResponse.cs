namespace Competency.CareerFramework;

/// <summary>
/// What a unit has and lacks to work: its head, its responsible HR, their accounts and its positions, with the status of each.
/// </summary>
/// <param name="Unit">The unit's path from the root, the unit last.</param>
/// <param name="Employees">The working employees of the unit itself.</param>
/// <param name="Head">The unit's head; null when it has none.</param>
/// <param name="HeadAccount">The head's account; null when the unit has no head.</param>
/// <param name="Hr">The HR assignment that applies to the unit, its own or inherited from the unit it names; null when none applies.</param>
/// <param name="HrAccount">The assigned HR's account; null when no assignment applies.</param>
/// <param name="Positions">The positions the unit's employees may hold.</param>
/// <param name="Rows">The status of each thing the unit needs, in screen order.</param>
internal sealed record UnitSetupResponse(
    IReadOnlyList<CareerUnitResponse> Unit,
    int Employees,
    CareerEmployeeResponse? Head,
    UnitAccountResponse? HeadAccount,
    OrgUnitHrPersonResponse? Hr,
    UnitAccountResponse? HrAccount,
    UnitPositionsResponse Positions,
    IReadOnlyList<UnitSetupRowResponse> Rows);
