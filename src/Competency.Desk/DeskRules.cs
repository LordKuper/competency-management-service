using Competency.Platform;
using static Competency.Platform.DeskKind;

namespace Competency.Desk;

/// <summary>
/// How the desk turns the findings of every source into items, one per object: the absorption and reason-order tables and the group of each kind.
/// The steps run in a fixed order, because absorption by an explicit pair must happen before the general rule that an action absorbs what is
/// for reference: an empty unit, for reference, absorbs the unit's missing head, an action, which would otherwise absorb it.
/// </summary>
internal static class DeskRules
{
    /// <summary>
    /// Which kind absorbs which while the absorbing finding is visible, about the same object, or about the absorbed finding's subject.
    /// A finding with a matrix version in progress absorbs what concerns the published one, because no new version can start meanwhile.
    /// </summary>
    private static readonly HashSet<(DeskKind Absorbing, DeskKind Absorbed)> Absorptions = Pairs(
        ([VersionReturned, VersionNotSubmitted, VersionAwaitsHeadApproval, VersionAwaitsHrApproval, OwnVersionInApproval, VersionStuckAtHead],
            [MatrixLevelsChanged, MatrixUsesInactive, MatrixSharedBlockChanged, MatrixCompetencyChanged]),
        ([VersionAwaitsHeadApproval], [OwnVersionInApproval, VersionStuckAtHead]),
        ([VersionAwaitsHrApproval], [OwnVersionInApproval]),
        ([MatrixLevelsChanged, MatrixUsesInactive, MatrixSharedBlockChanged], [MatrixCompetencyChanged]),
        ([UnitWithoutHead, ResponsibleUnitWithoutHead], [ResponsibleUnitWithoutMatrix]),
        ([EmptyUnit], [UnitWithoutHead, UnitWithoutHr, ResponsibleUnitWithoutMatrix, ResponsibleUnitWithoutHead]),
        ([TrackWithoutLevels], [TrackWithoutPositions, LevelWithoutPosition]),
        ([TrackWithoutPositions], [LevelWithoutPosition]),
        ([DraftAwaitsApproval], [TrackWithoutLevels, TrackWithoutPositions, LevelWithoutPosition]),
        ([PositionWithoutTrack], [NoTrackForPosition]),
        ([InvitationNotAccepted], [HeadAccountInvited]));

    /// <summary>
    /// The order of an item's reasons, the most important first. Kinds of different object types never share an item,
    /// so one list holds the order of every type: matrices, units, tracks and draft entries, unused entries, positions, employees, accounts.
    /// </summary>
    private static readonly DeskKind[] ReasonOrder =
    [
        VersionReturned, VersionNotSubmitted, VersionAwaitsHeadApproval, VersionAwaitsHrApproval,
        MatrixUsesInactive, MatrixLevelsChanged, MatrixSharedBlockChanged, VersionStuckAtHead, OwnVersionInApproval, MatrixCompetencyChanged,
        UnitWithoutHead, HeadWithoutAccount, HeadAccountInvited, HeadAccountBlocked, UnitWithoutHr, UnitWithoutPositions,
        UnitWithoutMatrix, EmptyUnit, ResponsibleUnitWithoutHead, ResponsibleUnitWithoutMatrix,
        DraftAwaitsApproval, TrackWithoutLevels, TrackWithOneLevel, TrackWithoutPositions, LevelWithoutPosition, OwnDraftAwaitsApproval,
        UnusedCompetency, UnusedScale, UnusedCategory, DirectionWithoutTracks,
        PositionWithoutTrack, InactivePositionInUse, UnusedPosition,
        PositionOutsideList, NoTrackForPosition,
        AccountWithoutEmployee, InvitationNotAccepted,
    ];

    private static readonly Dictionary<DeskKind, int> RankOf = ReasonOrder.Select((kind, rank) => (kind, rank)).ToDictionary(pair => pair.kind, pair => pair.rank);

    /// <summary>
    /// Merges findings into desk items, in desk order: by group, then the longest waiting first, then by name.
    /// </summary>
    /// <param name="findings">The findings of every source.</param>
    /// <returns>The items.</returns>
    public static List<DeskItemResponse> Merge(IEnumerable<DeskFinding> findings) =>
    [
        .. WithoutAbsorbed(WithoutRepeats(findings))
            .GroupBy(finding => Key(finding.Object))
            .Select(ToItem)
            .OrderBy(item => item.Group)
            .ThenBy(item => item.Reasons[0].OccurredAt)
            .ThenBy(item => item.Object.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Object.Id),
    ];

    /// <summary>
    /// Keeps one of the same findings reported through several of the user's roles: an action when any of them is one.
    /// </summary>
    private static List<DeskFinding> WithoutRepeats(IEnumerable<DeskFinding> findings) =>
    [
        .. findings
            .GroupBy(finding => (finding.Kind, Key(finding.Object), finding.Subject is { } subject ? Key(subject) : default))
            .Select(repeats => repeats.OrderByDescending(finding => finding.IsAction).First()),
    ];

    /// <summary>
    /// Drops the findings a visible finding absorbs. An absorbed finding absorbs nothing, so visibility is decided from the absorbing side down.
    /// </summary>
    private static List<DeskFinding> WithoutAbsorbed(List<DeskFinding> findings)
    {
        var byObject = findings.ToLookup(finding => Key(finding.Object));
        var visibility = new Dictionary<DeskFinding, bool>(ReferenceEqualityComparer.Instance);

        bool IsVisible(DeskFinding finding)
        {
            if (!visibility.TryGetValue(finding, out var isVisible))
            {
                isVisible = !AbsorbersOf(finding, byObject).Any(IsVisible);
                visibility[finding] = isVisible;
            }

            return isVisible;
        }

        return [.. findings.Where(IsVisible)];
    }

    private static IEnumerable<DeskFinding> AbsorbersOf(DeskFinding finding, ILookup<(DeskObjectType, Guid), DeskFinding> byObject) =>
        byObject[Key(finding.Object)]
            .Concat(finding.Subject is { } subject ? byObject[Key(subject)] : [])
            .Where(candidate => Absorptions.Contains((candidate.Kind, finding.Kind)));

    /// <summary>
    /// Builds the item of one object: what is for reference gives way to any action on the object, the reasons take their order,
    /// and the first reason decides the group and the link; an invitation stays on the item whichever reason brings it.
    /// </summary>
    private static DeskItemResponse ToItem(IEnumerable<DeskFinding> objectFindings)
    {
        var findings = objectFindings.ToList();
        var hasAction = findings.Exists(finding => finding.IsAction);
        var reasons = findings
            .Where(finding => finding.IsAction == hasAction)
            .OrderBy(finding => RankOf[finding.Kind])
            .ToList();
        var first = reasons[0];
        return new DeskItemResponse(
            first.Object,
            GroupOf(first),
            !hasAction,
            [.. reasons.Select(reason => new DeskReasonResponse(reason.Kind, reason.OccurredAt, reason.Details))],
            first.Target,
            reasons.Select(reason => reason.Invitation).FirstOrDefault(invitation => invitation is not null));
    }

    /// <summary>
    /// The group of an item by its first reason; the kinds about links of positions and tracks come both as actions and for reference.
    /// </summary>
    private static DeskGroup GroupOf(DeskFinding first) => first.Kind switch
    {
        VersionReturned => DeskGroup.ReturnedVersions,
        VersionAwaitsHeadApproval or VersionAwaitsHrApproval => DeskGroup.VersionsToApprove,
        VersionNotSubmitted => DeskGroup.UnsubmittedVersions,
        DraftAwaitsApproval => DeskGroup.DraftsToApprove,
        MatrixUsesInactive or MatrixLevelsChanged or MatrixSharedBlockChanged or UnitWithoutMatrix => DeskGroup.MatricesToBuild,
        TrackWithoutLevels or TrackWithOneLevel or TrackWithoutPositions or LevelWithoutPosition or PositionWithoutTrack =>
            first.IsAction ? DeskGroup.PositionsToLink : DeskGroup.PositionLinksInfo,
        UnitWithoutHead or HeadWithoutAccount or HeadAccountInvited or HeadAccountBlocked or UnitWithoutHr or UnitWithoutPositions =>
            DeskGroup.UnitsToConnect,
        InvitationNotAccepted => DeskGroup.Invitations,
        PositionOutsideList or AccountWithoutEmployee => DeskGroup.OrgCleanup,
        ResponsibleUnitWithoutMatrix or ResponsibleUnitWithoutHead => DeskGroup.UnitsWithoutMatrix,
        OwnVersionInApproval => DeskGroup.VersionsInApproval,
        VersionStuckAtHead => DeskGroup.StuckVersions,
        MatrixCompetencyChanged => DeskGroup.ChangedCompetencies,
        OwnDraftAwaitsApproval => DeskGroup.OwnDrafts,
        UnusedCompetency or UnusedScale or UnusedCategory or DirectionWithoutTracks => DeskGroup.UnusedCatalogEntries,
        InactivePositionInUse => DeskGroup.InactivePositions,
        UnusedPosition => DeskGroup.UnusedPositions,
        EmptyUnit => DeskGroup.EmptyUnits,
        NoTrackForPosition => DeskGroup.NoTrack,
        _ => throw new ArgumentOutOfRangeException(nameof(first), first.Kind, "The desk has no group for this kind."),
    };

    private static (DeskObjectType, Guid) Key(DeskObject deskObject) => (deskObject.Type, deskObject.Id);

    private static HashSet<(DeskKind, DeskKind)> Pairs(params (DeskKind[] Absorbing, DeskKind[] Absorbed)[] rows) =>
    [
        .. rows.SelectMany(row => row.Absorbing.SelectMany(absorbing => row.Absorbed.Select(absorbed => (absorbing, absorbed)))),
    ];
}
