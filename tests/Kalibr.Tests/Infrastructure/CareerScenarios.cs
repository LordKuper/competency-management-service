using System.Net;

namespace Kalibr.Tests.Infrastructure;

/// <summary>
/// A signed-in participant of the career process: the account, its session and the employee it is bound to, if any.
/// </summary>
/// <param name="User">The account with the credentials it signs in with.</param>
/// <param name="Client">The client holding the session.</param>
/// <param name="EmployeeId">The employee the account is bound to; none for an account bound to no one.</param>
public sealed record Member(TestUser User, ApiClient Client, Guid? EmployeeId = null)
{
    public Guid Id => User.Id;

    /// <summary>The employee the account is bound to, for a member who has one.</summary>
    public Guid Employee => EmployeeId!.Value;
}

/// <summary>
/// A tree of units of one test, with the people who play the roles of the career process in it: a root unit with its head and an HR assigned to it,
/// a department below it with its head, a team below the department with its head, and a sibling team with its head and an HR of its own.
/// Every call builds its own tree with unique names, so tests on one host never share units.
/// </summary>
public sealed class CareerOrg
{
    private CareerOrg()
    {
    }

    public ApiClient Admin { get; private init; } = null!;

    public ApiResponse Root { get; private init; } = null!;

    public ApiResponse Department { get; private init; } = null!;

    public ApiResponse Team { get; private init; } = null!;

    public ApiResponse Sibling { get; private init; } = null!;

    public Member RootHead { get; private init; } = null!;

    public Member DepartmentHead { get; private init; } = null!;

    public Member TeamHead { get; private init; } = null!;

    public Member SiblingHead { get; private init; } = null!;

    /// <summary>
    /// The HR assigned to the root unit, who answers for the department and the team too.
    /// </summary>
    public Member Hr { get; private init; } = null!;

    /// <summary>
    /// The HR assigned to the sibling team only, who takes the role over from <see cref="Hr"/> there.
    /// </summary>
    public Member SiblingHr { get; private init; } = null!;

    /// <summary>
    /// An account with no role in the process.
    /// </summary>
    public Member Plain { get; private init; } = null!;

    public static async Task<CareerOrg> CreateAsync(ApiHost host)
    {
        var admin = await host.AdminAsync();
        var root = await admin.CreateUnitAsync();
        var department = await admin.CreateUnitAsync(root.Id);
        var team = await admin.CreateUnitAsync(department.Id);
        var sibling = await admin.CreateUnitAsync(department.Id);
        return new CareerOrg
        {
            Admin = admin,
            Root = root,
            Department = department,
            Team = team,
            Sibling = sibling,
            RootHead = await admin.HeadOfAsync(host, root),
            DepartmentHead = await admin.HeadOfAsync(host, department),
            TeamHead = await admin.HeadOfAsync(host, team),
            SiblingHead = await admin.HeadOfAsync(host, sibling),
            Hr = await admin.HrOfAsync(host, root.Id),
            SiblingHr = await admin.HrOfAsync(host, sibling.Id),
            Plain = await admin.PlainMemberAsync(host),
        };
    }
}

/// <summary>
/// Builders of the career process arrangements and of the catalogue entries that tests create through the API.
/// </summary>
public static class CareerScenarios
{
    public const string BaseScaleId = "3004a734-475a-4e4c-b6a6-18275f5a1d5c";

    public static async Task<Member> HeadOfAsync(this ApiClient admin, ApiHost host, ApiResponse unit)
    {
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var account = await admin.CreateUserAsync(employeeId: employee.Id);
        (await admin.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        return new Member(account, await account.SignInAsync(host), employee.Id);
    }

    /// <summary>
    /// A working employee of the unit whose bound account holds the HR role and can sign in, not yet assigned to any unit.
    /// </summary>
    public static async Task<Member> HrCandidateAsync(this ApiClient admin, ApiHost host, Guid unitId)
    {
        var employee = await admin.CreateEmployeeAsync(unitId);
        var account = await admin.CreateUserAsync(Scenarios.Hr, employee.Id);
        return new Member(account, await account.SignInAsync(host), employee.Id);
    }

    /// <summary>
    /// Gives the account of the member the HR role, which assigning the member's employee as the HR of a unit requires.
    /// </summary>
    /// <returns>The member signed in again, since changing the roles ends the account's sessions.</returns>
    public static async Task<Member> GrantHrRoleAsync(this ApiClient admin, ApiHost host, Member member)
    {
        var current = await admin.GetUserAsync(member.Id);
        (await admin.PutAsync(
            $"/api/v1/users/{member.Id}",
            new { email = member.User.Email, roles = Scenarios.RolesOf(Scenarios.Hr), employeeId = member.EmployeeId },
            current.ETag)).Expect(HttpStatusCode.OK);
        return member with { Client = await member.User.SignInAsync(host) };
    }

    public static async Task<ApiResponse> AssignHrAsync(this ApiClient admin, Guid unitId, Guid? employeeId) =>
        await admin.PutAsync($"/api/v1/career/org-units/{unitId}/hr", new { employeeId });

    /// <summary>
    /// An HR candidate assigned to the unit; the employee sits in the unit itself.
    /// </summary>
    public static async Task<Member> HrOfAsync(this ApiClient admin, ApiHost host, Guid unitId)
    {
        var member = await admin.HrCandidateAsync(host, unitId);
        (await admin.AssignHrAsync(unitId, member.EmployeeId)).Expect(HttpStatusCode.NoContent);
        return member;
    }

    public static async Task<Member> PlainMemberAsync(this ApiClient admin, ApiHost host)
    {
        var account = await admin.CreateUserAsync();
        return new Member(account, await account.SignInAsync(host));
    }

    public static object Points(int top = 4) => Enumerable.Range(0, top + 1).Select(score => new { score, label = $"Балл {score}" }).ToArray();

    public static async Task<ApiResponse> MakeDirectionAsync(this ApiClient client, string? name = null) =>
        (await client.PostAsync("/api/v1/career/directions", new { name = name ?? Scenarios.Unique("Направление") })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeScaleAsync(this ApiClient client, string? name = null, object? points = null) =>
        (await client.PostAsync("/api/v1/career/scales", new { name = name ?? Scenarios.Unique("Шкала"), points = points ?? Points() })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeTrackAsync(this ApiClient client, Guid unitId, Guid directionId, string? name = null) =>
        (await client.PostAsync("/api/v1/career/tracks", new { name = name ?? Scenarios.Unique("Трек"), ownerOrgUnitId = unitId, directionId })).Expect(HttpStatusCode.Created);

    /// <summary>
    /// Stands a new active position of the unit for the level, which a track needs of each level before its matrix can be handed on.
    /// </summary>
    /// <returns>The level as the link left it, with its new entity tag.</returns>
    public static async Task<ApiResponse> LinkToNewPositionAsync(this ApiClient admin, ApiResponse level, Guid unitId)
    {
        var position = (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
        (await admin.PostAsync($"/api/v1/org-units/{unitId}/positions", new { positionId = position.Id })).Expect(HttpStatusCode.NoContent);
        return (await admin.PostAsync($"/api/v1/career/levels/{level.Id}/positions", new { positionId = position.Id }, level.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Stands a new active position of the unit for every level of the track, as a track needs before it may be approved.
    /// </summary>
    public static async Task LinkLevelsAsync(this ApiClient admin, Guid trackId, Guid unitId)
    {
        var levelIds = (await admin.GetAsync($"/api/v1/career/levels?trackId={trackId}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();
        foreach (var levelId in levelIds)
        {
            await admin.LinkToNewPositionAsync((await admin.GetAsync($"/api/v1/career/levels/{levelId}")).Expect(HttpStatusCode.OK), unitId);
        }
    }

    /// <summary>
    /// Links the levels of the track to positions and then takes the step to activity that suits the track now: approves a draft, activates an inactive track.
    /// </summary>
    /// <returns>The response to that step.</returns>
    public static async Task<ApiResponse> ActivateAsync(this ApiClient admin, ApiResponse track, Guid unitId, ApiClient? approver = null)
    {
        await admin.LinkLevelsAsync(track.Id, unitId);
        var current = (await admin.GetAsync($"/api/v1/career/tracks/{track.Id}")).Expect(HttpStatusCode.OK);
        var step = current.Json!["status"]!.GetValue<string>() == "Inactive" ? "activate" : "approve";
        var who = approver ?? admin;
        var publication = (await who.GetAsync($"/api/v1/career/tracks/{track.Id}/publication")).Expect(HttpStatusCode.OK);
        var cascade = Uri.EscapeDataString(publication.Json!["cascadeTag"]!.GetValue<string>());
        return (await who.PostAsync($"/api/v1/career/tracks/{track.Id}/{step}?cascade={cascade}", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// A track made active: it has a level standing for a new active position, which a track needs before it may be approved.
    /// </summary>
    public static async Task<ApiResponse> MakeActiveTrackAsync(this ApiClient admin, Guid unitId, Guid directionId, string? name = null)
    {
        var track = await admin.MakeTrackAsync(unitId, directionId, name);
        await admin.MakeLevelAsync(track.Id);
        return await admin.ActivateAsync(track, unitId);
    }

    public static async Task<ApiResponse> MakeLevelAsync(this ApiClient client, Guid trackId, string? name = null) =>
        (await client.PostAsync("/api/v1/career/levels", new { name = name ?? Scenarios.Unique("Уровень"), trackId })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeCompetencyAsync(
        this ApiClient client,
        string? name = null,
        Guid? directionId = null,
        Guid? scaleId = null) =>
        (await client.PostAsync(
            "/api/v1/career/competencies",
            new { name = name ?? Scenarios.Unique("Компетенция"), description = (string?)null, directionId, scaleId }))
        .Expect(HttpStatusCode.Created);

    public static string[] ItemIds(this ApiResponse page) =>
        [.. page.Json!["items"]!.AsArray().Select(item => item!["id"]!.GetValue<string>())];

    public static string[] ItemNames(this ApiResponse page) =>
        [.. page.Json!["items"]!.AsArray().Select(item => item!["name"]!.GetValue<string>())];
}
