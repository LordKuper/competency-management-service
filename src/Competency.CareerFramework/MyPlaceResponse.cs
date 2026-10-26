namespace Competency.CareerFramework;

/// <summary>
/// Where the signed-in user stands in the company: their unit, position, head and responsible HR, the career tracks their position is
/// on, the published track matrices of their own unit, and the published position matrix of their unit for their position. A user bound
/// to no employee gets an empty answer with no employee.
/// </summary>
/// <param name="Employee">The employee the user is bound to; null when none.</param>
/// <param name="Unit">The employee's unit with the units above it, from the root, the unit last.</param>
/// <param name="Position">The employee's position.</param>
/// <param name="Head">The head of the employee's unit; null when it has none.</param>
/// <param name="ResponsibleHr">The responsible HR of the unit, its own or inherited from the unit it names; null when there is none.</param>
/// <param name="Tracks">The active tracks with a level the position is linked to, in name order.</param>
/// <param name="PublishedMatrices">The published track matrix versions of the employee's unit, in track name order.</param>
/// <param name="NoTrack">Whether the position is linked to a level of no active track and has no position matrix either.</param>
/// <param name="PositionMatrix">The published position matrix of the employee's unit for the employee's position; null when there is none.</param>
internal sealed record MyPlaceResponse(
    CareerEmployeeResponse? Employee,
    IReadOnlyList<CareerUnitResponse> Unit,
    CareerNamedResponse? Position,
    CareerEmployeeResponse? Head,
    OrgUnitHrPersonResponse? ResponsibleHr,
    IReadOnlyList<MyPlaceTrackResponse> Tracks,
    IReadOnlyList<MyPlaceMatrixResponse> PublishedMatrices,
    bool NoTrack,
    MyPlacePositionMatrixResponse? PositionMatrix = null);
