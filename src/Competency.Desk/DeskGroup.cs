using System.Text.Json.Serialization;

namespace Competency.Desk;

/// <summary>
/// The groups of the desk, in the order the desk shows them: actions on the user's turn first, then those about the state of data,
/// then the groups for reference. Member names are sent in the API, so renaming one changes it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeskGroup>))]
internal enum DeskGroup
{
    ReturnedVersions,
    VersionsToApprove,
    UnsubmittedVersions,
    DraftsToApprove,
    PromotionDecisions,
    PromotionFormalChecks,
    PromotionRatings,
    PromotionRaters,
    PromotionArtifacts,
    PromotionFeedback,
    MatricesToBuild,
    PositionsToLink,
    UnitsToConnect,
    CommissionsWithoutChair,
    Invitations,
    OrgCleanup,
    UnitsWithoutMatrix,
    VersionsInApproval,
    StuckVersions,
    ChangedCompetencies,
    OwnDrafts,
    PositionLinksInfo,
    UnusedCatalogEntries,
    InactivePositions,
    UnusedPositions,
    EmptyUnits,
    NoTrack,
}
