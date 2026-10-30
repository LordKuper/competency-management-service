using AwesomeAssertions;
using Competency.Desk;
using Competency.Platform;
using Xunit;
using static Competency.Platform.DeskKind;

namespace Competency.Tests;

/// <summary>
/// How the desk turns the findings of every source into items: one item per object, what an absorbing finding hides, what is for
/// reference giving way to an action, the order of reasons and of items. The rules run on findings alone, so they are checked without a host.
/// </summary>
public sealed class DeskRulesTests
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<DeskKind> AllKinds => new(Enum.GetValues<DeskKind>());

    public static TheoryData<DeskKind, DeskKind> AbsorptionsOfOneObject => new()
    {
        { TrackWithoutLevels, TrackWithoutPositions },
        { TrackWithoutLevels, LevelWithoutPosition },
        { TrackWithoutPositions, LevelWithoutPosition },
        { DraftAwaitsApproval, TrackWithoutLevels },
        { DraftAwaitsApproval, TrackWithoutPositions },
        { DraftAwaitsApproval, LevelWithoutPosition },
        { UnitWithoutHead, ResponsibleUnitWithoutMatrix },
        { ResponsibleUnitWithoutHead, ResponsibleUnitWithoutMatrix },
        { EmptyUnit, UnitWithoutHead },
        { EmptyUnit, UnitWithoutHr },
        { EmptyUnit, ResponsibleUnitWithoutMatrix },
        { EmptyUnit, ResponsibleUnitWithoutHead },
        { VersionAwaitsHeadApproval, OwnVersionInApproval },
        { VersionAwaitsHeadApproval, VersionStuckAtHead },
        { VersionAwaitsHrApproval, OwnVersionInApproval },
        { VersionReturned, MatrixLevelsChanged },
        { VersionNotSubmitted, MatrixUsesInactive },
        { OwnVersionInApproval, MatrixSharedBlockChanged },
        { VersionStuckAtHead, MatrixCompetencyChanged },
        { MatrixLevelsChanged, MatrixCompetencyChanged },
        { MatrixUsesInactive, MatrixCompetencyChanged },
        { MatrixSharedBlockChanged, MatrixCompetencyChanged },
    };

    [Theory]
    [MemberData(nameof(AllKinds))]
    public void Ac154_EveryKind_HasAGroupAndAPlaceInTheOrderOfReasons(DeskKind kind)
    {
        var matrix = Matrix();
        var findings = new[] { Finding(kind, matrix), Finding(kind, matrix, subject: Matrix("Другой")) };

        var items = DeskRules.Merge(findings);

        items.Should().ContainSingle().Which.Reasons.Select(reason => reason.Kind).Should().Equal(kind, kind);
    }

    [Theory]
    [MemberData(nameof(AbsorptionsOfOneObject))]
    public void Ac155_AnAbsorbedFinding_IsNotShownWhileTheAbsorbingOneIsVisible(DeskKind absorbing, DeskKind absorbed)
    {
        var subject = Matrix();

        var items = DeskRules.Merge([Finding(absorbed, subject), Finding(absorbing, subject)]);

        items.Should().ContainSingle().Which.Reasons.Select(reason => reason.Kind).Should().Equal(absorbing);
    }

    [Fact]
    public void Ac155_AnAbsorbingFinding_HidesOnlyWhatConcernsItsOwnObject()
    {
        var emptyUnit = new DeskObject(DeskObjectType.OrgUnit, Guid.NewGuid(), "Пустой");
        var otherUnit = new DeskObject(DeskObjectType.OrgUnit, Guid.NewGuid(), "Другой");

        var items = DeskRules.Merge([Finding(EmptyUnit, emptyUnit), Finding(UnitWithoutHead, otherUnit)]);

        items.Select(item => item.Object).Should().BeEquivalentTo([emptyUnit, otherUnit]);
    }

    [Fact]
    public void Ac155_AFindingThatIsNotAnAbsorbedKind_StaysBesideTheAbsorbingOne()
    {
        var track = new DeskObject(DeskObjectType.Track, Guid.NewGuid(), "Трек");

        var items = DeskRules.Merge([Finding(TrackWithoutLevels, track), Finding(TrackWithOneLevel, track)]);

        items.Single().Reasons.Select(reason => reason.Kind).Should().Equal(TrackWithoutLevels, TrackWithOneLevel);
    }

    [Fact]
    public void Ac159_AnInvitationNotAccepted_AbsorbsTheHeadAccountInvitedOfTheUnitThroughTheSubject()
    {
        var unit = new DeskObject(DeskObjectType.OrgUnit, Guid.NewGuid(), "Отдел");
        var account = new DeskObject(DeskObjectType.UserAccount, Guid.NewGuid(), "head@test.local");

        var items = DeskRules.Merge([Finding(HeadAccountInvited, unit, subject: account), Finding(InvitationNotAccepted, account)]);

        items.Should().ContainSingle().Which.Object.Should().Be(account);
        items.Single().Reasons.Select(reason => reason.Kind).Should().Equal(InvitationNotAccepted);
    }

    [Fact]
    public void Ac159_AHeadAccountInvited_StaysWhenNoInvitationNotAcceptedIsVisible()
    {
        var unit = new DeskObject(DeskObjectType.OrgUnit, Guid.NewGuid(), "Отдел");
        var account = new DeskObject(DeskObjectType.UserAccount, Guid.NewGuid(), "head@test.local");

        var items = DeskRules.Merge([Finding(HeadAccountInvited, unit, subject: account)]);

        items.Should().ContainSingle().Which.Object.Should().Be(unit);
    }

    [Fact]
    public void Ac155_APositionWithoutATrack_AbsorbsTheNoTrackFindingOfAnEmployeeOnThatPositionOnly()
    {
        var position = new DeskObject(DeskObjectType.Position, Guid.NewGuid(), "Инженер");
        var otherPosition = new DeskObject(DeskObjectType.Position, Guid.NewGuid(), "Аналитик");
        var onPosition = new DeskObject(DeskObjectType.Employee, Guid.NewGuid(), "Иванов");
        var onOther = new DeskObject(DeskObjectType.Employee, Guid.NewGuid(), "Петров");

        var items = DeskRules.Merge(
        [
            Finding(PositionWithoutTrack, position),
            Finding(NoTrackForPosition, onPosition, false, position),
            Finding(NoTrackForPosition, onOther, false, otherPosition),
        ]);

        items.Select(item => item.Object).Should().BeEquivalentTo([position, onOther]);
    }

    [Fact]
    public void Ac156_AFindingForReference_GivesWayToAnActionOnTheSameObject_AndTheItemCountsAsAnAction()
    {
        var track = new DeskObject(DeskObjectType.Track, Guid.NewGuid(), "Трек");

        var items = DeskRules.Merge([Finding(OwnDraftAwaitsApproval, track, false), Finding(DraftAwaitsApproval, track)]);

        var item = items.Should().ContainSingle().Subject;
        item.IsReference.Should().BeFalse();
        item.Reasons.Select(reason => reason.Kind).Should().Equal(DraftAwaitsApproval);
    }

    [Fact]
    public void Ac156_FindingsForReference_AreTwoReasonsOfOneItemForReference()
    {
        var track = new DeskObject(DeskObjectType.Track, Guid.NewGuid(), "Трек");

        var items = DeskRules.Merge([Finding(OwnDraftAwaitsApproval, track, false), Finding(TrackWithOneLevel, track, false)]);

        var item = items.Should().ContainSingle().Subject;
        item.IsReference.Should().BeTrue();
        item.Reasons.Select(reason => reason.Kind).Should().BeEquivalentTo([OwnDraftAwaitsApproval, TrackWithOneLevel]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ac157_TheSameFindingFromSeveralRoles_IsShownOnce_AsAnActionWhenAnyOfThemIs(bool actionComesFirst)
    {
        var track = new DeskObject(DeskObjectType.Track, Guid.NewGuid(), "Трек");
        var action = Finding(TrackWithoutLevels, track);
        var reference = Finding(TrackWithoutLevels, track, false);

        var items = DeskRules.Merge(actionComesFirst ? [action, reference] : [reference, action]);

        var item = items.Should().ContainSingle().Subject;
        item.IsReference.Should().BeFalse();
        item.Reasons.Should().ContainSingle();
    }

    [Fact]
    public void Ac154_ReasonsFollowTheOrderOfTheTable_AndTheFirstReasonDecidesTheGroup()
    {
        var unit = new DeskObject(DeskObjectType.OrgUnit, Guid.NewGuid(), "Отдел");

        var items = DeskRules.Merge([Finding(UnitWithoutPositions, unit), Finding(UnitWithoutHr, unit), Finding(HeadWithoutAccount, unit)]);

        var item = items.Should().ContainSingle().Subject;
        item.Reasons.Select(reason => reason.Kind).Should().Equal(HeadWithoutAccount, UnitWithoutHr, UnitWithoutPositions);
        item.Group.Should().Be(DeskGroup.UnitsToConnect);
    }

    [Fact]
    public void Ac160_TheLinkOfAnItem_IsTheTargetOfItsFirstReason()
    {
        var matrix = Matrix();
        var firstTarget = new DeskObject(DeskObjectType.MatrixVersion, Guid.NewGuid(), "Версия 1");
        var otherTarget = new DeskObject(DeskObjectType.MatrixVersion, Guid.NewGuid(), "Версия 2");

        var items = DeskRules.Merge(
        [
            Finding(OwnVersionInApproval, matrix, false, target: otherTarget),
            Finding(VersionStuckAtHead, matrix, false, target: firstTarget),
        ]);

        items.Single().Target.Should().Be(firstTarget, "the stuck version comes before the own version in approval in the table of reasons");
    }

    [Fact]
    public void Ac160_AnInvitationToSendAgain_StaysOnTheItemWhicheverReasonBringsIt()
    {
        var account = new DeskObject(DeskObjectType.UserAccount, Guid.NewGuid(), "user@test.local");
        var invitation = new DeskInvitation(account.Id, 3);

        var items = DeskRules.Merge([Finding(AccountWithoutEmployee, account), Finding(InvitationNotAccepted, account, invitation: invitation)]);

        var item = items.Should().ContainSingle().Subject;
        item.Reasons.First().Kind.Should().Be(AccountWithoutEmployee);
        item.Invitation.Should().Be(invitation);
    }

    [Fact]
    public void Ac7_Items_FollowTheGroupThenTheOldestEventThenTheNameThenTheId()
    {
        var undated = Matrix("В");
        var oldest = Matrix("Б");
        var tiedLowerCase = Matrix("а");
        var tiedUpperCase = Matrix("Б");
        var newer = Matrix("А");
        var twins = new[] { Matrix("Д"), Matrix("Д") }.OrderBy(twin => twin.Id).ToArray();
        var awaiting = Matrix("Я");

        var items = DeskRules.Merge(
        [
            Finding(VersionAwaitsHeadApproval, awaiting, at: Noon.AddDays(-1)),
            Finding(VersionReturned, twins[1], at: Noon.AddHours(3)),
            Finding(VersionReturned, newer, at: Noon.AddHours(2)),
            Finding(VersionReturned, twins[0], at: Noon.AddHours(3)),
            Finding(VersionReturned, tiedUpperCase, at: Noon.AddHours(1)),
            Finding(VersionReturned, oldest, at: Noon),
            Finding(VersionReturned, tiedLowerCase, at: Noon.AddHours(1)),
            Finding(VersionReturned, undated),
        ]);

        items.Select(item => item.Object).Should().Equal(undated, oldest, tiedLowerCase, tiedUpperCase, newer, twins[0], twins[1], awaiting);
    }

    private static DeskObject Matrix(string name = "Трек") => new(DeskObjectType.Matrix, Guid.NewGuid(), name);

    private static DeskFinding Finding(
        DeskKind kind,
        DeskObject subjectOfFinding,
        bool isAction = true,
        DeskObject? subject = null,
        DateTimeOffset? at = null,
        DeskObject? target = null,
        DeskInvitation? invitation = null) =>
        new(kind, subjectOfFinding, isAction, subject, at, Target: target, Invitation: invitation);
}
