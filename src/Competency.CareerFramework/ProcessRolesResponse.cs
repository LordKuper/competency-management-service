namespace Competency.CareerFramework;

/// <summary>
/// The career process roles of the signed-in user, for showing menus and actions; the server checks the roles again on every action.
/// </summary>
internal sealed record ProcessRolesResponse(IReadOnlyList<CareerUnitResponse> HeadedUnits, IReadOnlyList<CareerUnitResponse> HrUnits);
