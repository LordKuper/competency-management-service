using System.Net;
using System.Text.Json.Nodes;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// Arrangements of organization data with assignments and legal entities, built through the API as an administrator.
/// </summary>
public static class OrgScenes
{
    /// <summary>
    /// Finds or adds the legal entity of the name, the way the employee form does.
    /// </summary>
    /// <param name="client">An administrator or HR.</param>
    /// <param name="name">The legal entity name.</param>
    /// <returns>The legal entity.</returns>
    public static async Task<ApiResponse> LegalEntityAsync(this ApiClient client, string name) =>
        (await client.PostAsync("/api/v1/legal-entities/quick-add", new { name })).Expect(HttpStatusCode.OK);

    /// <summary>
    /// A secondary assignment as the employee request carries it.
    /// </summary>
    /// <param name="unitId">The unit of the assignment.</param>
    /// <param name="positionId">The position of the assignment.</param>
    /// <param name="legalEntityIds">The legal entities of the assignment.</param>
    /// <returns>The assignment object.</returns>
    public static object Assignment(Guid unitId, Guid positionId, params Guid[] legalEntityIds) =>
        new { orgUnitId = unitId, positionId, legalEntityIds };

    /// <summary>
    /// The body that creates or replaces an employee with legal entities on the main assignment and secondary assignments.
    /// </summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="unitId">The unit of the main assignment.</param>
    /// <param name="positionId">The position of the main assignment.</param>
    /// <param name="legalEntityIds">The legal entities of the main assignment.</param>
    /// <param name="assignments">The secondary assignments, from <see cref="Assignment"/>.</param>
    /// <returns>The request body.</returns>
    public static object EmployeeWith(
        string lastName,
        string firstName,
        Guid unitId,
        Guid positionId,
        IEnumerable<Guid>? legalEntityIds = null,
        IEnumerable<object>? assignments = null) =>
        new
        {
            lastName,
            firstName,
            middleName = (string?)null,
            positionId,
            orgUnitId = unitId,
            legalEntityIds = legalEntityIds ?? [],
            assignments = assignments ?? [],
        };

    /// <summary>
    /// Creates an employee with legal entities and secondary assignments.
    /// </summary>
    /// <param name="admin">An administrator or HR.</param>
    /// <param name="body">The body from <see cref="EmployeeWith"/>.</param>
    /// <returns>The created employee.</returns>
    public static async Task<ApiResponse> CreateEmployeeWithAsync(this ApiClient admin, object body) =>
        (await admin.PostAsync("/api/v1/employees", body)).Expect(HttpStatusCode.Created);

    /// <summary>
    /// Deactivates a unit that has no employees and no active children.
    /// </summary>
    /// <param name="admin">An administrator or HR.</param>
    /// <param name="unit">The unit as created or read, with its entity tag.</param>
    /// <returns>The deactivated unit.</returns>
    public static async Task<ApiResponse> DeactivateUnitAsync(this ApiClient admin, ApiResponse unit) =>
        (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/deactivate", ifMatch: unit.ETag)).Expect(HttpStatusCode.OK);

    /// <summary>
    /// Dismisses a working employee.
    /// </summary>
    /// <param name="admin">An administrator or HR.</param>
    /// <param name="employee">The employee as created or read, with its entity tag.</param>
    /// <returns>The dismissed employee.</returns>
    public static async Task<ApiResponse> DismissAsync(this ApiClient admin, ApiResponse employee) =>
        (await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag)).Expect(HttpStatusCode.OK);

    /// <summary>
    /// Deactivates a catalogue position.
    /// </summary>
    /// <param name="admin">An administrator or HR.</param>
    /// <param name="positionId">The position.</param>
    public static async Task DeactivatePositionAsync(this ApiClient admin, Guid positionId)
    {
        var position = (await admin.GetAsync($"/api/v1/positions/{positionId}")).Expect(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/v1/positions/{positionId}/deactivate", ifMatch: position.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// Binds an existing account to an employee, keeping its email and administrator role.
    /// </summary>
    /// <param name="admin">An administrator.</param>
    /// <param name="userId">The account.</param>
    /// <param name="employeeId">The employee.</param>
    public static async Task BindAccountAsync(this ApiClient admin, Guid userId, Guid employeeId)
    {
        var account = await admin.GetUserAsync(userId);
        (await admin.PutAsync(
            $"/api/v1/users/{userId}",
            new { email = account.Json!["email"]!.GetValue<string>(), roles = Scenarios.AdminRoles, employeeId },
            account.ETag)).Expect(HttpStatusCode.OK);
    }

    /// <summary>
    /// The account id of the signed-in user.
    /// </summary>
    /// <param name="client">The signed-in client.</param>
    /// <returns>The account id.</returns>
    public static async Task<Guid> OwnIdAsync(this ApiClient client) => (await client.GetAsync("/api/v1/auth/me")).Expect(HttpStatusCode.OK).Id;

    /// <summary>
    /// Reads an employee the way a test checks the result of a change: the status, the main assignment and the secondary ones, with legal entities by name.
    /// </summary>
    /// <param name="client">An administrator or HR.</param>
    /// <param name="id">The employee.</param>
    /// <returns>The view.</returns>
    public static async Task<EmployeeView> ViewAsync(this ApiClient client, Guid id)
    {
        var json = (await client.GetAsync($"/api/v1/employees/{id}")).Expect(HttpStatusCode.OK).Json!;
        string[] Names(JsonNode? entities) => [.. entities!.AsArray().Select(entity => entity!["name"]!.GetValue<string>()).Order(StringComparer.Ordinal)];
        return new EmployeeView(
            json["isActive"]!.GetValue<bool>(),
            json["orgUnitName"]!.GetValue<string>(),
            json["position"]!.GetValue<string>(),
            Names(json["legalEntities"]),
            [
                .. json["assignments"]!.AsArray()
                    .Select(item => $"{item!["orgUnitName"]!.GetValue<string>()}, {item["position"]!.GetValue<string>()}: {string.Join(", ", Names(item["legalEntities"]))}")
                    .Order(StringComparer.Ordinal),
            ]);
    }

    /// <summary>
    /// Finds the one employee whose name contains the text.
    /// </summary>
    /// <param name="client">An administrator or HR.</param>
    /// <param name="text">A part of the full name that no other employee has.</param>
    /// <returns>The employee identifier.</returns>
    public static async Task<Guid> FindEmployeeAsync(this ApiClient client, string text)
    {
        var page = (await client.GetAsync($"/api/v1/employees?q={Uri.EscapeDataString(text)}")).Expect(HttpStatusCode.OK);
        return Guid.Parse(page.Json!["items"]!.AsArray().Single()!["id"]!.GetValue<string>());
    }

    /// <summary>
    /// A short token that makes every name of one test unique across the tests of a host, written in letters and digits so it sits in any name.
    /// </summary>
    /// <returns>The token.</returns>
    public static string Tag() => Guid.NewGuid().ToString("N")[..8];
}

/// <summary>
/// An organization the import tests start from, and the workbook that touches every group of the preview: each name carries the scene's tag,
/// so scenes of different tests on one host never match each other's units or employees.
/// </summary>
public sealed class ImportScene
{
    private ImportScene()
    {
    }

    public required ApiClient Admin { get; init; }

    public required string Tag { get; init; }

    public required ApiResponse Root { get; init; }

    public required ApiResponse Group { get; init; }

    public required ApiResponse Archive { get; init; }

    public required Guid Engineer { get; init; }

    public required Guid Retired { get; init; }

    public required Guid Spare { get; init; }

    public required ApiResponse Alpha { get; init; }

    public required ApiResponse Ivanov { get; init; }

    public required ApiResponse Petrov { get; init; }

    public required ApiResponse Sidorov { get; init; }

    public required ApiResponse Yozhikov { get; init; }

    public string RootName => Name("Отдел");

    public string GroupName => Name("Группа");

    public string ArchiveName => Name("Архив");

    public string NewUnitName => Name("Новый");

    public string EngineerName => Name("Инженер");

    public string RetiredName => Name("Старый");

    public string FreshName => Name("Новая");

    public string SpareName => Name("Лишняя");

    public string AlphaName => Name("ООО Альфа");

    public string BetaName => Name("ООО Бета");

    public string IvanovFullName => $"Иванов{Tag} Иван";

    public string PetrovFullName => $"Петров{Tag} Пётр";

    public string SidorovFullName => $"Сидоров{Tag} Сидор";

    public string YozhikovFullName => $"Ёжиков{Tag} Ёж";

    public string NewcomerFullName => $"Новиков{Tag} Нил";

    public string RootPath => RootName;

    public string GroupPath => $"{RootName} / {GroupName}";

    public string NewUnitPath => $"{RootName} / {NewUnitName}";

    /// <summary>
    /// Arranges a root unit with a child, an inactive unit, a catalogue position that is off and one the root lists without anyone using it,
    /// four employees (one with a legal entity and a secondary assignment, one dismissed), and the legal entity.
    /// </summary>
    /// <param name="admin">An administrator or HR.</param>
    /// <returns>The scene.</returns>
    public static async Task<ImportScene> CreateAsync(ApiClient admin)
    {
        var tag = OrgScenes.Tag();
        var root = await admin.CreateUnitAsync(name: $"Отдел {tag}");
        var group = await admin.CreateUnitAsync(root.Id, $"Группа {tag}");
        var archive = await admin.DeactivateUnitAsync(await admin.CreateUnitAsync(name: $"Архив {tag}"));
        var engineer = await admin.PositionAsync(root.Id, $"Инженер {tag}");
        await admin.PositionAsync(group.Id, $"Инженер {tag}");
        var spare = await admin.PositionAsync(root.Id, $"Лишняя {tag}");
        var retired = await admin.PositionAsync(root.Id, $"Старый {tag}");
        await admin.DeactivatePositionAsync(retired);
        var alpha = await admin.LegalEntityAsync($"ООО Альфа {tag}");
        return new ImportScene
        {
            Admin = admin,
            Tag = tag,
            Root = root,
            Group = group,
            Archive = archive,
            Engineer = engineer,
            Retired = retired,
            Spare = spare,
            Alpha = alpha,
            Ivanov = await admin.CreateEmployeeWithAsync(
                OrgScenes.EmployeeWith($"Иванов{tag}", "Иван", root.Id, engineer, [alpha.Id], [OrgScenes.Assignment(group.Id, engineer)])),
            Petrov = await admin.CreateEmployeeAsync(root.Id, $"Петров{tag}", "Пётр", position: $"Инженер {tag}"),
            Sidorov = await admin.DismissAsync(await admin.CreateEmployeeAsync(root.Id, $"Сидоров{tag}", "Сидор", position: $"Инженер {tag}")),
            Yozhikov = await admin.CreateEmployeeAsync(root.Id, $"Ёжиков{tag}", "Ёж", position: $"Инженер {tag}"),
        };
    }

    /// <summary>
    /// The workbook that moves one employee to a new unit, changes another's legal entity and drops their secondary assignment, brings a dismissed one back,
    /// gives the fourth the inactive position, adds a new employee with a secondary assignment, names the inactive unit, and adds a new position to the child.
    /// </summary>
    /// <returns>The bytes of the file.</returns>
    public byte[] File() => new OrgImportSheet()
        .Add([RootName], IvanovFullName, EngineerName, BetaName)
        .Add([RootName, NewUnitName], PetrovFullName, EngineerName)
        .Add([RootName], SidorovFullName, EngineerName)
        .Add([RootName], $"Ежиков{Tag} Еж", RetiredName)
        .Add([RootName], NewcomerFullName, EngineerName)
        .Add([RootName, GroupName], NewcomerFullName, EngineerName)
        .Unit(ArchiveName)
        .Add([RootName, GroupName], null, FreshName)
        .ToBytes();

    private string Name(string kind) => $"{kind} {Tag}";
}

/// <summary>
/// An employee as a test reads it back.
/// </summary>
/// <param name="IsActive">Whether the employee works.</param>
/// <param name="Unit">The main unit's name.</param>
/// <param name="Position">The main position's name.</param>
/// <param name="LegalEntities">The legal entities of the main assignment, by name.</param>
/// <param name="Assignments">The secondary assignments, each as unit, position and legal entities.</param>
public sealed record EmployeeView(bool IsActive, string Unit, string Position, string[] LegalEntities, string[] Assignments);
