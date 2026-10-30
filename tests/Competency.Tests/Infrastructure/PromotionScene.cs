using System.Net;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A host with a database of its own whose scheduler passes every second, so that a stage change waits seconds. Each scene is a career session
/// of its own tree of units, so the tests of the class share the host and never the session they move through the stages; the scene of the
/// class is announced on first use and brought to its first stage.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class PromotionHost(TestEnvironment environment)
    : HostFixture(environment, new Dictionary<string, string> { ["Promotion__TickInterval"] = "00:00:01" })
{
    private Task<PromotionScene>? scene;

    /// <summary>
    /// The session shared by the tests of the class, which keep it in its first stage.
    /// </summary>
    /// <returns>The scene.</returns>
    public Task<PromotionScene> SceneAsync() => scene ??= PromotionScene.CreateAsync(Host);

    /// <summary>
    /// A session of its own, in its first stage, for a test that moves it through the stages.
    /// </summary>
    /// <param name="withSibling">Whether the sibling unit takes part too, with a matrix and a chair of its own; the session is then announced by the administrator, otherwise by the HR of the team.</param>
    /// <returns>The scene.</returns>
    public Task<PromotionScene> NewSceneAsync(bool withSibling = false) => PromotionScene.CreateAsync(Host, withSibling);
}

/// <summary>
/// A career session over a published matrix of the team of a <see cref="CareerOrg"/>: the team head chairs the commission, the HR assigned to the
/// root unit answers for the team, and the dates of the stored session can be moved to put it into any stage, which the scheduler of the host then
/// settles on its next pass. The organization's day is Moscow's, an offset of three hours from UTC all year.
/// </summary>
public sealed class PromotionScene
{
    private const int PastMarginDays = 2;
    private const int StageStepDays = 3;
    private const int AnnouncementLeadDays = 9;
    private static readonly TimeSpan PassTimeout = TimeSpan.FromSeconds(60);
    private static readonly int[] StageLengths = [10, 2, 5, 2, 5, 5];

    private PromotionScene()
    {
    }

    /// <summary>
    /// What a candidate aims at: the matrix of a unit, the position of its junior level, and the HR who enters the dates of its applications.
    /// </summary>
    /// <param name="Kit">The matrix of the unit.</param>
    /// <param name="UnitId">The unit.</param>
    /// <param name="JuniorPosition">The position of the junior level, which the candidate holds.</param>
    /// <param name="Hr">The HR responsible for the unit.</param>
    public sealed record Target(MatrixKit Kit, Guid UnitId, string JuniorPosition, Member Hr);

    public ApiHost Host { get; private init; } = null!;

    public CareerOrg Org { get; private init; } = null!;

    public MatrixKit Kit { get; private init; } = null!;

    public Guid WaveId { get; private init; }

    public string JuniorPosition { get; private init; } = null!;

    /// <summary>
    /// The sibling unit's matrix and chair, present when the scene was made with the sibling.
    /// </summary>
    public Target? Sibling { get; private init; }

    /// <summary>
    /// The team's target.
    /// </summary>
    public Target Team => new(Kit, Org.Team.Id, JuniorPosition, Org.Hr);

    /// <summary>
    /// The day of the organization at the moment of the call. It is the only reading of the wall clock in the scene: every stage end it writes lies at
    /// least <see cref="PastMarginDays"/> days before or <see cref="StageStepDays"/> days after this day, so a midnight between the write and the
    /// scheduler's pass changes no stage.
    /// </summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

    public static async Task<PromotionScene> CreateAsync(ApiHost host, bool withSibling = false)
    {
        var org = await CareerOrg.CreateAsync(host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        (await SaveCommissionAsync(org.Hr.Client, org.Team.Id, [org.TeamHead.Employee], org.TeamHead.Employee)).Expect(HttpStatusCode.OK);
        var units = new List<Guid> { org.Team.Id };
        Target? sibling = null;
        if (withSibling)
        {
            var siblingKit = await (await MatrixKit.CreateAsync(org, org.Sibling.Id, org.SiblingHead.Client)).PrepareAsync();
            (await siblingKit.TransitAsync(org.SiblingHead.Client, "submit")).Expect(HttpStatusCode.OK);
            (await siblingKit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
            (await siblingKit.TransitAsync(org.SiblingHr.Client, "approve")).Expect(HttpStatusCode.OK);
            (await SaveCommissionAsync(org.SiblingHr.Client, org.Sibling.Id, [org.SiblingHead.Employee], org.SiblingHead.Employee)).Expect(HttpStatusCode.OK);
            sibling = new Target(siblingKit, org.Sibling.Id, await JuniorPositionAsync(org, siblingKit), org.SiblingHr);
            units.Add(org.Sibling.Id);
        }

        var start = Today.AddDays(AnnouncementLeadDays);
        var announcer = withSibling ? org.Admin : org.Hr.Client;
        var wave = (await announcer.PostAsync("/api/v1/promotion/waves", new { startsOn = start, endsOn = Ends(start), unitIds = units })).Expect(HttpStatusCode.Created);
        var scene = new PromotionScene
        {
            Host = host,
            Org = org,
            Kit = kit,
            WaveId = wave.Id,
            JuniorPosition = await JuniorPositionAsync(org, kit),
            Sibling = sibling,
        };
        await scene.PutInStageAsync(1);
        return scene;
    }

    /// <summary>
    /// Saves the commission of a unit as the given user, against the entity tag the commission has now.
    /// </summary>
    public static async Task<ApiResponse> SaveCommissionAsync(ApiClient who, Guid unitId, Guid[] members, Guid? chair)
    {
        var current = (await who.GetAsync($"/api/v1/promotion/commissions/{unitId}")).Expect(HttpStatusCode.OK);
        return await who.PutAsync($"/api/v1/promotion/commissions/{unitId}", new { memberEmployeeIds = members, chairEmployeeId = chair }, current.ETag);
    }

    /// <summary>
    /// The end dates of a session starting on the day, with the default lengths of the stages.
    /// </summary>
    public static DateOnly[] Ends(DateOnly start)
    {
        var day = start;
        return [.. StageLengths.Select(length => day = day.AddDays(length))];
    }

    /// <summary>
    /// Moves the stored dates so that the given stage is the current one, with days of it left, and waits until the scheduler has settled
    /// every stage before it, and for the first stage until it has fixed the matrix versions.
    /// </summary>
    /// <param name="stage">The stage, 1 to 6, or 7 for a session that is over.</param>
    public async Task PutInStageAsync(int stage)
    {
        var ended = Today.AddDays(-PastMarginDays);
        var ends = Enumerable.Range(0, StageLengths.Length)
            .Select(index => index < stage - 1 ? ended : Today.AddDays(StageStepDays + (index - (stage - 1)) * StageStepDays))
            .Select(day => $"'{day:yyyy-MM-dd}'");
        await Host.ExecuteAsync(
            $"UPDATE promotion_waves SET starts_on = '{Today.AddDays(-30):yyyy-MM-dd}', ends_on = ARRAY[{string.Join(", ", ends)}]::date[] WHERE id = '{WaveId}'");
        await Waiting.UntilAsync(
            async () => await Host.ScalarAsync<bool>($"SELECT versions_fixed FROM promotion_waves WHERE id = '{WaveId}'")
                && await Host.ScalarAsync<int>($"SELECT processed_stage FROM promotion_waves WHERE id = '{WaveId}'") >= stage - 1,
            $"the scheduler settled the stages before stage {stage}",
            PassTimeout);
    }

    /// <summary>
    /// Announces the next session for the team, once the scene's session is over, and brings it to its first stage.
    /// </summary>
    /// <returns>The scene of the next session, over the same people and matrix.</returns>
    public async Task<PromotionScene> AnnounceNextAsync()
    {
        var start = Today.AddDays(AnnouncementLeadDays);
        var wave = (await Org.Hr.Client.PostAsync("/api/v1/promotion/waves", new { startsOn = start, endsOn = Ends(start), unitIds = new[] { Org.Team.Id } })).Expect(HttpStatusCode.Created);
        var next = new PromotionScene { Host = Host, Org = Org, Kit = Kit, WaveId = wave.Id, JuniorPosition = JuniorPosition, Sibling = Sibling };
        await next.PutInStageAsync(1);
        return next;
    }

    /// <summary>
    /// An employee of the team on the position of the junior level, with an account that can sign in.
    /// </summary>
    public async Task<Member> NewCandidateAsync(Guid? unitId = null, string? position = null)
    {
        var employee = await Org.Admin.CreateEmployeeAsync(unitId ?? Org.Team.Id, position: position ?? JuniorPosition);
        var account = await Org.Admin.CreateUserAsync(employeeId: employee.Id);
        return new Member(account, await account.SignInAsync(Host), employee.Id);
    }

    /// <summary>
    /// An employee of the target's unit on the position of its junior level, with an account that can sign in.
    /// </summary>
    public Task<Member> NewCandidateAsync(Target target) => NewCandidateAsync(target.UnitId, target.JuniorPosition);

    /// <summary>
    /// Employees with accounts who can be named as raters: the given number in the candidate's unit and the given number in another one.
    /// </summary>
    public async Task<Member[]> NewRatersAsync(int inside, int outside)
    {
        List<Member> raters = [];
        for (var index = 0; index < inside + outside; index++)
        {
            raters.Add(await NewCandidateAsync(index < inside ? Org.Team.Id : Org.Sibling.Id, position: "Инженер"));
        }

        return [.. raters];
    }

    /// <summary>
    /// The body that files or changes an application for the senior level of the matrix, scored in full.
    /// </summary>
    public object ApplicationBody(IEnumerable<Member> raters, Guid? levelId = null, string? education = "Bachelor", int criticalScore = 3, int additionalScore = 2, Target? target = null)
    {
        var aimed = target ?? Team;
        return new
        {
            waveId = WaveId,
            orgUnitId = aimed.UnitId,
            matrixId = aimed.Kit.MatrixId,
            levelId = levelId ?? aimed.Kit.Senior.Id,
            education,
            selfScores = new[] { new { competencyId = aimed.Kit.Critical.Id, score = criticalScore }, new { competencyId = aimed.Kit.Profile.Id, score = additionalScore } },
            raters = raters.Select(rater => new { employeeId = rater.Employee, email = (string?)null }).ToArray(),
        };
    }

    /// <summary>
    /// Files a complete application and submits it, as the candidate.
    /// </summary>
    public async Task<ApiResponse> SubmitAsync(Member candidate, Member[] raters, Target? target = null)
    {
        var draft = (await candidate.Client.PostAsync("/api/v1/promotion/applications", ApplicationBody(raters, target: target))).Expect(HttpStatusCode.Created);
        return (await candidate.Client.PostAsync($"/api/v1/promotion/applications/{draft.Id}/submit", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Files and submits an application, and has the responsible HR enter an old enough date, so that the formal check admits it.
    /// </summary>
    /// <returns>The application as the HR's change left it.</returns>
    public async Task<ApiResponse> SubmitForAdmissionAsync(Member candidate, Member[] raters, Target? target = null)
    {
        var submitted = await SubmitAsync(candidate, raters, target);
        return (await (target ?? Team).Hr.Client.PutAsync(
            $"/api/v1/promotion/applications/{submitted.Id}/formal",
            new { hiredOrPromotedOn = new DateOnly(2020, 1, 1), ignoreInterval = false },
            submitted.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// The body of a rater's answers to the two competencies of the matrix.
    /// </summary>
    public object RatingBody(int? critical, int? additional, Target? target = null)
    {
        var aimed = target ?? Team;
        return new { answers = new[] { new { competencyId = aimed.Kit.Critical.Id, score = critical }, new { competencyId = aimed.Kit.Profile.Id, score = additional } } };
    }

    /// <summary>
    /// The body of the chair's final scores and decision.
    /// </summary>
    public object ReviewBody(int critical, int additional, string decision, Target? target = null)
    {
        var aimed = target ?? Team;
        return new
        {
            finalScores = new[] { new { competencyId = aimed.Kit.Critical.Id, score = critical }, new { competencyId = aimed.Kit.Profile.Id, score = additional } },
            decision,
        };
    }

    /// <summary>
    /// Has every rater answer the application with the same scores.
    /// </summary>
    public async Task RateAsync(IEnumerable<Member> raters, Guid applicationId, int critical, int additional, Target? target = null)
    {
        foreach (var rater in raters)
        {
            (await rater.Client.PutAsync($"/api/v1/promotion/applications/{applicationId}/rating", RatingBody(critical, additional, target))).Expect(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Blocks the account of a member, so that the commissions he chairs have no chair who acts.
    /// </summary>
    public async Task BlockAsync(Member member)
    {
        var current = await Org.Admin.GetUserAsync(member.Id);
        (await Org.Admin.PostAsync($"/api/v1/users/{member.Id}/block", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Reads an application as the given user.
    /// </summary>
    public Task<ApiResponse> GetApplicationAsync(ApiClient who, Guid id) => who.GetAsync($"/api/v1/promotion/applications/{id}");

    /// <summary>
    /// The status of an application response.
    /// </summary>
    public static string StatusOf(ApiResponse application) => application.Json!["status"]!.GetValue<string>();

    /// <summary>
    /// The status of an application as its candidate sees it now.
    /// </summary>
    public async Task<string> StatusNowAsync(Member candidate, Guid id) => StatusOf((await GetApplicationAsync(candidate.Client, id)).Expect(HttpStatusCode.OK));

    private static async Task<string> JuniorPositionAsync(CareerOrg org, MatrixKit kit)
    {
        var positions = (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(level => level!["id"]!.GetValue<Guid>() == kit.Junior.Id)!["positions"]!.AsArray();
        return positions[0]!["name"]!.GetValue<string>();
    }
}
