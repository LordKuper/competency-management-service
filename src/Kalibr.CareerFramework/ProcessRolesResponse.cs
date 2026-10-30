namespace Kalibr.CareerFramework;

/// <summary>
/// The career process roles of the signed-in user, for showing menus and actions; the server checks the roles again on every action.
/// The matrix units are those whose matrices the user leads: the headed units and the units below them without a head.
/// </summary>
internal sealed record ProcessRolesResponse(
    IReadOnlyList<CareerUnitResponse> HeadedUnits,
    IReadOnlyList<CareerUnitResponse> MatrixUnits,
    IReadOnlyList<CareerUnitResponse> HrUnits);
