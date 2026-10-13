namespace Competency.Platform;

/// <summary>
/// The values a desk finding's reason names; the client words the reason from its kind, so each kind fills only the values it names.
/// </summary>
/// <param name="EmployeeCount">The number of working employees the reason counts.</param>
/// <param name="Step">The step the reason names: of the matrix builder, or of the approval route.</param>
/// <param name="Person">The person the reason names, such as a unit head, a responsible HR or whoever holds the turn: their employee, or their account when it is bound to none.</param>
/// <param name="AccountState">Why the account the reason concerns cannot act.</param>
/// <param name="LinkState">Why an invitation link no longer works or soon will not.</param>
/// <param name="Entries">The other records the reason names, in the order it names them.</param>
/// <param name="Number">The number of the matrix version the reason is about.</param>
/// <param name="HasChanges">Whether the approver returned the version with changes of their own.</param>
/// <param name="HasComment">Whether the approver returned the version with a comment.</param>
/// <param name="LevelCount">The number of levels of the track the reason is about, inactive ones aside.</param>
/// <param name="AddedLevels">The levels a track gained since its matrix was published, in track order.</param>
/// <param name="RemovedLevels">The levels a published matrix lists that its track no longer has in use, in matrix order.</param>
/// <param name="MovedLevels">The levels whose place in the track changed since its matrix was published, in track order.</param>
public sealed record DeskDetails(
    int? EmployeeCount = null,
    int? Step = null,
    DeskObject? Person = null,
    DeskAccountState? AccountState = null,
    DeskLinkState? LinkState = null,
    IReadOnlyList<DeskObject>? Entries = null,
    int? Number = null,
    bool? HasChanges = null,
    bool? HasComment = null,
    int? LevelCount = null,
    IReadOnlyList<DeskObject>? AddedLevels = null,
    IReadOnlyList<DeskObject>? RemovedLevels = null,
    IReadOnlyList<DeskObject>? MovedLevels = null);
