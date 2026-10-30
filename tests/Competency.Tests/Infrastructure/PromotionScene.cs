using System.Net;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A host with a database of its own whose career session is announced once for the class, so that the tests of the class share the one session
/// the scheduler of the host has to bring to its first stage.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class PromotionHost(TestEnvironment environment) : HostFixture(environment, null)
{
    private Task<PromotionScene>? scene;

    /// <summary>
    /// The session of the class, announced for the team of a tree of units and brought to its first stage on the first call.
    /// </summary>
    /// <returns>The scene.</returns>
    public Task<PromotionScene> SceneAsync() => scene ??= PromotionScene.CreateAsync(Host);
}

/// <summary>
/// A career session over a published matrix of the team of a <see cref="CareerOrg"/>: the team head chairs the commission, the HR assigned to the
/// root unit announces the session, and the dates of the stored session can be moved to put it into any stage, which the scheduler of the host then
/// settles on its next pass. The organization's day is Moscow's, an offset of three hours from UTC all year.
/// </summary>
public sealed class PromotionScene
{
    private static readonly TimeSpan PassTimeout = TimeSpan.FromSeconds(180);
    private static readonly int[] StageLengths = [10, 2, 5, 2, 5, 5];

    private PromotionScene()
    {
    }

    public ApiHost Host { get; private init; } = null!;

    public CareerOrg Org { get; private init; } = null!;

    public MatrixKit Kit { get; private init; } = null!;

    public Guid WaveId { get; private init; }

    public string JuniorPosition { get; private init; } = null!;

    /// <summary>
    /// The current day of the organization.
    /// </summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

    public static async Task<PromotionScene> CreateAsync(ApiHost host)
    {
        var org = await CareerOrg.CreateAsync(host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        (await SaveCommissionAsync(org.Hr.Client, org.Team.Id, [org.TeamHead.Employee], org.TeamHead.Employee)).Expect(HttpStatusCode.OK);
        var start = Today.AddDays(9);
        var wave = (await org.Hr.Client.PostAsync("/api/v1/promotion/waves", new { startsOn = start, endsOn = Ends(start), unitIds = new[] { org.Team.Id } })).Expect(HttpStatusCode.Created);
        var positions = (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(level => level!["id"]!.GetValue<Guid>() == kit.Junior.Id)!["positions"]!.AsArray();
        var scene = new PromotionScene
        {
            Host = host,
            Org = org,
            Kit = kit,
            WaveId = wave.Id,
            JuniorPosition = positions[0]!["name"]!.GetValue<string>(),
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
    /// Moves the stored dates so that the given stage is the current one, with three days of it left, and waits until the scheduler has settled
    /// every stage before it, and for the first stage until it has fixed the matrix versions.
    /// </summary>
    /// <param name="stage">The stage, 1 to 6.</param>
    public async Task PutInStageAsync(int stage)
    {
        var yesterday = Today.AddDays(-1);
        var ends = Enumerable.Range(0, StageLengths.Length)
            .Select(index => index < stage - 1 ? yesterday : Today.AddDays(3 + (index - (stage - 1)) * 3))
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
    /// An employee of the team on the position of the junior level, with an account that can sign in.
    /// </summary>
    public async Task<Member> NewCandidateAsync(Guid? unitId = null, string? position = null)
    {
        var employee = await Org.Admin.CreateEmployeeAsync(unitId ?? Org.Team.Id, position: position ?? JuniorPosition);
        var account = await Org.Admin.CreateUserAsync(employeeId: employee.Id);
        return new Member(account, await account.SignInAsync(Host), employee.Id);
    }

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
    public object ApplicationBody(IEnumerable<Member> raters, Guid? levelId = null, string? education = "Bachelor", int criticalScore = 3, int additionalScore = 2) => new
    {
        waveId = WaveId,
        orgUnitId = Org.Team.Id,
        matrixId = Kit.MatrixId,
        levelId = levelId ?? Kit.Senior.Id,
        education,
        selfScores = new[] { new { competencyId = Kit.Critical.Id, score = criticalScore }, new { competencyId = Kit.Profile.Id, score = additionalScore } },
        raters = raters.Select(rater => new { employeeId = rater.Employee, email = (string?)null }).ToArray(),
    };

    /// <summary>
    /// Files a complete application and submits it, as the candidate.
    /// </summary>
    public async Task<ApiResponse> SubmitAsync(Member candidate, Member[] raters)
    {
        var draft = (await candidate.Client.PostAsync("/api/v1/promotion/applications", ApplicationBody(raters))).Expect(HttpStatusCode.Created);
        return (await candidate.Client.PostAsync($"/api/v1/promotion/applications/{draft.Id}/submit", ifMatch: draft.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Reads an application as the given user.
    /// </summary>
    public Task<ApiResponse> GetApplicationAsync(ApiClient who, Guid id) => who.GetAsync($"/api/v1/promotion/applications/{id}");

    /// <summary>
    /// The status of an application response.
    /// </summary>
    public static string StatusOf(ApiResponse application) => application.Json!["status"]!.GetValue<string>();
}
