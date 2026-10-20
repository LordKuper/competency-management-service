using System.Text.Json.Serialization;

namespace Competency.Platform;

/// <summary>
/// What a desk finding is. Variants of one finding that the desk absorbs differently are separate members.
/// Member names are sent in the API, so renaming one changes it; a new member also needs its place in the desk's rules.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskKind>))]
public enum DeskKind
{
    /// <summary>
    /// A version of the matrix of a unit the user heads was returned to them; dated by the return.
    /// </summary>
    VersionReturned,

    /// <summary>
    /// A draft version of the matrix of a unit the user heads is not yet submitted; dated by its creation.
    /// </summary>
    VersionNotSubmitted,

    /// <summary>
    /// A version of a subordinate unit's matrix waits for the user's approval as the superior head; dated by the decision that brought it to them.
    /// </summary>
    VersionAwaitsHeadApproval,

    /// <summary>
    /// A matrix version waits for the user's approval as the responsible HR; dated by the decision that brought it to them.
    /// </summary>
    VersionAwaitsHrApproval,

    /// <summary>
    /// A published matrix of a unit the user heads no longer covers its track's levels.
    /// </summary>
    MatrixLevelsChanged,

    /// <summary>
    /// A published matrix of a unit the user heads relies on an inactive track, direction, position, shared block or competency.
    /// </summary>
    MatrixUsesInactive,

    /// <summary>
    /// A competency of a published matrix changed after the matrix was published; for reference.
    /// </summary>
    MatrixCompetencyChanged,

    /// <summary>
    /// The composition of a shared block a published matrix of a unit the user heads includes now differs from the one the matrix was published with.
    /// </summary>
    MatrixSharedBlockChanged,

    /// <summary>
    /// A version of the matrix of a unit the user heads is under approval; for reference, dated by the decision that brought it to its step.
    /// </summary>
    OwnVersionInApproval,

    /// <summary>
    /// A matrix version of a unit the user is the responsible HR of has waited for the superior head for too long; for reference, dated by its submission.
    /// </summary>
    VersionStuckAtHead,

    /// <summary>
    /// A unit the user heads has no published track matrix although an active track applies to it.
    /// </summary>
    UnitWithoutMatrix,

    /// <summary>
    /// A unit the user is the responsible HR of has no published track matrix; for reference.
    /// </summary>
    ResponsibleUnitWithoutMatrix,

    /// <summary>
    /// An active unit has no head.
    /// </summary>
    UnitWithoutHead,

    /// <summary>
    /// A unit the user is the responsible HR of has no head; for reference.
    /// </summary>
    ResponsibleUnitWithoutHead,

    /// <summary>
    /// An active unit has no responsible HR who can act, its own or inherited.
    /// </summary>
    UnitWithoutHr,

    /// <summary>
    /// An active unit has working employees, but neither it nor any unit above it lists a position.
    /// </summary>
    UnitWithoutPositions,

    /// <summary>
    /// The head of an active unit has no account.
    /// </summary>
    HeadWithoutAccount,

    /// <summary>
    /// The head of an active unit has an invited account, without a password yet.
    /// </summary>
    HeadAccountInvited,

    /// <summary>
    /// The head of an active unit has a blocked account.
    /// </summary>
    HeadAccountBlocked,

    /// <summary>
    /// An active unit has neither working employees nor active units under it; for reference.
    /// </summary>
    EmptyUnit,

    /// <summary>
    /// A draft catalogue entry waits for the user's approval; dated by its creation.
    /// </summary>
    DraftAwaitsApproval,

    /// <summary>
    /// A track has no levels.
    /// </summary>
    TrackWithoutLevels,

    /// <summary>
    /// A track has only one level.
    /// </summary>
    TrackWithOneLevel,

    /// <summary>
    /// No level of a track has an active position linked.
    /// </summary>
    TrackWithoutPositions,

    /// <summary>
    /// Some levels of a track have no active position linked.
    /// </summary>
    LevelWithoutPosition,

    /// <summary>
    /// A draft catalogue entry the user created waits for approval; for reference, dated by its creation.
    /// </summary>
    OwnDraftAwaitsApproval,

    /// <summary>
    /// An active competency is in no matrix version; for reference.
    /// </summary>
    UnusedCompetency,

    /// <summary>
    /// An active scale other than the default one is used by no competency and no matrix version; for reference.
    /// </summary>
    UnusedScale,

    /// <summary>
    /// An active career direction has no tracks; for reference.
    /// </summary>
    DirectionWithoutTracks,

    /// <summary>
    /// An active position on the list of a unit the user is responsible for is linked to no level of an active track that applies there.
    /// </summary>
    PositionWithoutTrack,

    /// <summary>
    /// An inactive position is held by working employees; for reference.
    /// </summary>
    InactivePositionInUse,

    /// <summary>
    /// An active position is on no unit's list and held by no working employee; for reference.
    /// </summary>
    UnusedPosition,

    /// <summary>
    /// A working employee's position is not among those available to the employee's unit.
    /// </summary>
    PositionOutsideList,

    /// <summary>
    /// The user's own position is linked to no level of an active track; for reference.
    /// </summary>
    NoTrackForPosition,

    /// <summary>
    /// An account of the user role is bound to no employee.
    /// </summary>
    AccountWithoutEmployee,

    /// <summary>
    /// An invited account did not accept the invitation, and its link no longer works or soon will not; dated by the link's expiry.
    /// </summary>
    InvitationNotAccepted,
}
