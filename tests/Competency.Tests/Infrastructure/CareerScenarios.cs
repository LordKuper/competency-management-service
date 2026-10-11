using System.Net;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A signed-in participant of the career process: the account, its session and the employee it is bound to, if any.
/// </summary>
/// <param name="User">The account with the credentials it signs in with.</param>
/// <param name="Client">The client holding the session.</param>
public sealed record Member(TestUser User, ApiClient Client)
{
    public Guid Id => User.Id;
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
        return new Member(account, await account.SignInAsync(host));
    }

    public static async Task<Member> HrOfAsync(this ApiClient admin, ApiHost host, Guid unitId)
    {
        var account = await admin.CreateUserAsync();
        (await admin.PutAsync($"/api/v1/career/org-units/{unitId}/hr", new { userId = account.Id })).Expect(HttpStatusCode.NoContent);
        return new Member(account, await account.SignInAsync(host));
    }

    public static async Task<Member> PlainMemberAsync(this ApiClient admin, ApiHost host)
    {
        var account = await admin.CreateUserAsync();
        return new Member(account, await account.SignInAsync(host));
    }

    public static async Task<Guid> CategoryIdAsync(this ApiClient client) =>
        Guid.Parse((await client.GetAsync("/api/v1/career/categories?pageSize=1")).Expect(HttpStatusCode.OK).Json!["items"]![0]!["id"]!.GetValue<string>());

    public static object Points(int top = 4) => Enumerable.Range(0, top + 1).Select(score => new { score, label = $"Балл {score}" }).ToArray();

    public static async Task<ApiResponse> MakeDirectionAsync(this ApiClient client, string? name = null) =>
        (await client.PostAsync("/api/v1/career/directions", new { name = name ?? Scenarios.Unique("Направление") })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeCategoryAsync(this ApiClient client, string? name = null) =>
        (await client.PostAsync("/api/v1/career/categories", new { name = name ?? Scenarios.Unique("Категория") })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeScaleAsync(this ApiClient client, string? name = null, object? points = null) =>
        (await client.PostAsync("/api/v1/career/scales", new { name = name ?? Scenarios.Unique("Шкала"), points = points ?? Points() })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeTrackAsync(this ApiClient client, Guid unitId, Guid directionId, string? name = null) =>
        (await client.PostAsync("/api/v1/career/tracks", new { name = name ?? Scenarios.Unique("Трек"), ownerOrgUnitId = unitId, directionId })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeLevelAsync(this ApiClient client, Guid trackId, string? name = null) =>
        (await client.PostAsync("/api/v1/career/levels", new { name = name ?? Scenarios.Unique("Уровень"), trackId })).Expect(HttpStatusCode.Created);

    public static async Task<ApiResponse> MakeCompetencyAsync(
        this ApiClient client,
        Guid categoryId,
        string? name = null,
        Guid? directionId = null,
        string type = "Profile",
        Guid? scaleId = null) =>
        (await client.PostAsync(
            "/api/v1/career/competencies",
            new { name = name ?? Scenarios.Unique("Компетенция"), description = (string?)null, categoryId, directionId, type, scaleId }))
        .Expect(HttpStatusCode.Created);

    public static string[] ItemIds(this ApiResponse page) =>
        [.. page.Json!["items"]!.AsArray().Select(item => item!["id"]!.GetValue<string>())];

    public static string[] ItemNames(this ApiResponse page) =>
        [.. page.Json!["items"]!.AsArray().Select(item => item!["name"]!.GetValue<string>())];
}
