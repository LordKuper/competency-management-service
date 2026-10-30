using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The shared position catalogue: administrators create, rename, deactivate and reactivate positions, names are unique regardless of
/// letter case and edge whitespace, every signed-in user reads the active positions, and every change is versioned and journaled.
/// </summary>
public sealed class PositionCatalogueTests(TestEnvironment environment)
{
    private const string DuplicateMessage = "Должность с таким названием уже есть";
    private const int LongestName = 200;

    [Fact]
    public async Task Ac1_Create_StoresTheTrimmedName_AsAnActivePositionWithAnEntityTag()
    {
        var admin = await AdminAsync();
        var name = Scenarios.Unique("Аналитик");

        var created = (await admin.PostAsync("/api/v1/positions", new { name = $"  {name}\t" })).Expect(HttpStatusCode.Created);

        created.Json!["name"]!.GetValue<string>().Should().Be(name);
        created.Json["isActive"]!.GetValue<bool>().Should().BeTrue();
        created.ETag.Should().Be($"\"{created.Json["version"]!.GetValue<int>()}\"");
        (await admin.GetAsync($"/api/v1/positions/{created.Id}")).Expect(HttpStatusCode.OK).Json!["name"]!.GetValue<string>().Should().Be(name);
    }

    [Fact]
    public async Task Ac1_Name_IsRequired_AndNotLongerThan200CharactersAfterTrimming()
    {
        var admin = await AdminAsync();

        var blank = await admin.PostAsync("/api/v1/positions", new { name = "  " });
        var tooLong = await admin.PostAsync("/api/v1/positions", new { name = new string('я', LongestName + 1) });
        var paddedLongest = new string('ж', LongestName - 8) + Scenarios.Unique("")[1..];
        var longest = await admin.PostAsync("/api/v1/positions", new { name = $"  {paddedLongest}  " });

        blank.Status.Should().Be(HttpStatusCode.BadRequest);
        blank.FieldErrors("name").Should().Equal("Укажите название");
        tooLong.Status.Should().Be(HttpStatusCode.BadRequest);
        tooLong.FieldErrors("name").Should().Equal("Не длиннее 200 символов");
        longest.Status.Should().Be(HttpStatusCode.Created, longest.Body);
        longest.Json!["name"]!.GetValue<string>().Should().HaveLength(LongestName);
    }

    [Fact]
    public async Task Ac1_Name_IsUniqueRegardlessOfCaseAndEdgeWhitespace_OnCreateAndRename_AndYoIsNotE()
    {
        var admin = await AdminAsync();
        var token = Scenarios.Unique("t")[2..];
        var first = (await admin.PostAsync("/api/v1/positions", new { name = $"Ведущий инженер {token}" })).Expect(HttpStatusCode.Created);
        var other = (await admin.PostAsync("/api/v1/positions", new { name = $"Другая должность {token}" })).Expect(HttpStatusCode.Created);
        var yo = (await admin.PostAsync("/api/v1/positions", new { name = $"Ёж {token}" })).Expect(HttpStatusCode.Created);

        var lower = await admin.PostAsync("/api/v1/positions", new { name = $"ведущий инженер {token}" });
        var upper = await admin.PostAsync("/api/v1/positions", new { name = $"  ВЕДУЩИЙ ИНЖЕНЕР {token}\n" });
        var yoCase = await admin.PostAsync("/api/v1/positions", new { name = $"ёЖ {token}" });
        var notYo = await admin.PostAsync("/api/v1/positions", new { name = $"Еж {token}" });
        var renameToTaken = await admin.PutAsync($"/api/v1/positions/{other.Id}", new { name = $" ведущий ИНЖЕНЕР {token} " }, other.ETag);
        var ownCase = await admin.PutAsync($"/api/v1/positions/{first.Id}", new { name = $"ВЕДУЩИЙ ИНЖЕНЕР {token}" }, first.ETag);

        foreach (var refused in new[] { lower, upper, yoCase, renameToTaken })
        {
            refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
            refused.FieldErrors("name").Should().Equal(DuplicateMessage);
        }

        notYo.Status.Should().Be(HttpStatusCode.Created, "the letters «е» and «ё» are different letters");
        ownCase.Status.Should().Be(HttpStatusCode.OK, "a position may change the case of its own name");
        ownCase.Json!["name"]!.GetValue<string>().Should().Be($"ВЕДУЩИЙ ИНЖЕНЕР {token}");
        (await admin.GetAsync($"/api/v1/positions/{yo.Id}")).Json!["name"]!.GetValue<string>().Should().Be($"Ёж {token}");
    }

    [Fact]
    public async Task Ac1_Deactivation_AndReactivation_ChangeTheStatusAlone_WithNewEntityTags_AndRefuseAStaleOne()
    {
        var admin = await AdminAsync();
        var position = await CreateAsync(admin);

        var deactivated = (await admin.PostAsync($"/api/v1/positions/{position.Id}/deactivate", ifMatch: position.ETag)).Expect(HttpStatusCode.OK);
        var stale = await admin.PostAsync($"/api/v1/positions/{position.Id}/activate", ifMatch: position.ETag);
        var reactivated = (await admin.PostAsync($"/api/v1/positions/{position.Id}/activate", ifMatch: deactivated.ETag)).Expect(HttpStatusCode.OK);
        var unknown = await admin.PostAsync($"/api/v1/positions/{Guid.NewGuid()}/deactivate", ifMatch: position.ETag);

        deactivated.Json!["isActive"]!.GetValue<bool>().Should().BeFalse();
        deactivated.Json["name"]!.GetValue<string>().Should().Be(position.Json!["name"]!.GetValue<string>());
        deactivated.ETag.Should().NotBe(position.ETag);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        reactivated.Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac2_EmployeesWhoHoldAPosition_KeepIt_WhenItIsDeactivated()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Сметчик");
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: name);
        var positionId = employee.Json!["positionId"]!.GetValue<string>();
        var position = (await admin.GetAsync($"/api/v1/positions/{positionId}")).Expect(HttpStatusCode.OK);

        (await admin.PostAsync($"/api/v1/positions/{positionId}/deactivate", ifMatch: position.ETag)).Expect(HttpStatusCode.OK);

        var after = await admin.GetEmployeeAsync(employee.Id);
        after.Json!["positionId"]!.GetValue<string>().Should().Be(positionId);
        after.Json["position"]!.GetValue<string>().Should().Be(name);
    }

    [Fact]
    public async Task Ac1_List_FiltersByStatusAndSearchesBySubstringAndStem_InNameOrder()
    {
        var admin = await AdminAsync();
        var token = Scenarios.Unique("t")[2..];
        await CreateAsync(admin, $"Бета {token}");
        await CreateAsync(admin, $"Альфа {token}");
        var gamma = await CreateAsync(admin, $"Гамма {token}");
        var engineer = await CreateAsync(admin, $"Ведущий инженер {token}");
        (await admin.PostAsync($"/api/v1/positions/{gamma.Id}/deactivate", ifMatch: gamma.ETag)).Expect(HttpStatusCode.OK);

        var all = await ListAsync(admin, $"q={token}");
        var active = await ListAsync(admin, $"q={token}&isActive=true");
        var inactive = await ListAsync(admin, $"q={token}&isActive=false");
        var bySubstring = await ListAsync(admin, $"q={Uri.EscapeDataString($"льфа {token}")}");
        var byStem = await ListAsync(admin, $"q={Uri.EscapeDataString($"инженеры {token}")}");

        all.ItemNames().Should().Equal($"Альфа {token}", $"Бета {token}", $"Ведущий инженер {token}", $"Гамма {token}");
        active.ItemNames().Should().Equal($"Альфа {token}", $"Бета {token}", $"Ведущий инженер {token}");
        inactive.ItemNames().Should().Equal($"Гамма {token}");
        bySubstring.ItemNames().Should().Equal($"Альфа {token}");
        byStem.ItemIds().Should().Equal(engineer.Id.ToString());
    }

    [Fact]
    public async Task Ac1_List_TreatsPercentAndUnderscoreInTheSearchTextAsCharacters()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var percent = await CreateAsync(admin, "Скидка 5%");
        var underscore = await CreateAsync(admin, "Скидка_5");
        await CreateAsync(admin, "Скидка 5");

        var byPercent = await ListAsync(admin, "q=%25");
        var byUnderscore = await ListAsync(admin, "q=_");

        byPercent.ItemIds().Should().Equal(percent.Id.ToString());
        byUnderscore.ItemIds().Should().Equal(underscore.Id.ToString());
    }

    [Fact]
    public async Task Ac1_User_ReadsActivePositionsOnly_WhileAnAdministratorReadsAll()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var user = (await admin.PlainMemberAsync(host)).Client;
        var token = Scenarios.Unique("t")[2..];
        var visible = await CreateAsync(admin, $"Видимая {token}");
        var hidden = await CreateAsync(admin, $"Скрытая {token}");
        (await admin.PostAsync($"/api/v1/positions/{hidden.Id}/deactivate", ifMatch: hidden.ETag)).Expect(HttpStatusCode.OK);

        var list = await ListAsync(user, $"q={token}");
        var inactiveFilter = await ListAsync(user, $"q={token}&isActive=false");
        var readHidden = await user.GetAsync($"/api/v1/positions/{hidden.Id}");
        var readVisible = await user.GetAsync($"/api/v1/positions/{visible.Id}");

        list.ItemIds().Should().Equal(visible.Id.ToString());
        inactiveFilter.ItemIds().Should().BeEmpty();
        readHidden.Status.Should().Be(HttpStatusCode.NotFound);
        readVisible.Status.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync($"/api/v1/positions/{hidden.Id}")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac1_Impact_CountsTheWorkingEmployeesWhoHoldThePosition()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Кладовщик");
        var working = await admin.CreateEmployeeAsync(unit.Id, position: name);
        var leaver = await admin.CreateEmployeeAsync(unit.Id, position: name);
        await admin.CreateEmployeeAsync(unit.Id);
        (await admin.PostAsync($"/api/v1/employees/{leaver.Id}/dismiss", ifMatch: leaver.ETag)).Expect(HttpStatusCode.OK);
        var positionId = working.Json!["positionId"]!.GetValue<string>();

        var impact = (await admin.GetAsync($"/api/v1/positions/{positionId}/impact")).Expect(HttpStatusCode.OK);
        var unknown = await admin.GetAsync($"/api/v1/positions/{Guid.NewGuid()}/impact");

        impact.Json!["employeeCount"]!.GetValue<int>().Should().Be(1, "a dismissed employee does not count");
        unknown.Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac6_Journal_RecordsTheCreationRenameAndStatusChangesOfAPosition()
    {
        var admin = await AdminAsync();
        var name = Scenarios.Unique("Журнал");
        var created = (await admin.PostAsync("/api/v1/positions", new { name })).Expect(HttpStatusCode.Created);
        var renamed = (await admin.PutAsync($"/api/v1/positions/{created.Id}", new { name = $"{name}-2" }, created.ETag)).Expect(HttpStatusCode.OK);
        var deactivated = (await admin.PostAsync($"/api/v1/positions/{created.Id}/deactivate", ifMatch: renamed.ETag)).Expect(HttpStatusCode.OK);

        var creation = (await admin.AuditOfRequestAsync(created.RequestId!)).Single()!;
        var rename = (await admin.AuditOfRequestAsync(renamed.RequestId!)).Single()!;
        var status = (await admin.AuditOfRequestAsync(deactivated.RequestId!)).Single()!;

        creation["action"]!.GetValue<string>().Should().Be("Position.Created");
        creation["newValue"]!["name"]!.GetValue<string>().Should().Be(name);
        rename["action"]!.GetValue<string>().Should().Be("Position.Updated");
        rename["oldValue"]!["name"]!.GetValue<string>().Should().Be(name);
        rename["newValue"]!["name"]!.GetValue<string>().Should().Be($"{name}-2");
        status["newValue"]!["isActive"]!.GetValue<bool>().Should().BeFalse();
        status["oldValue"]!["isActive"]!.GetValue<bool>().Should().BeTrue();
    }

    private async Task<ApiClient> AdminAsync() => await (await environment.SharedHostAsync()).AdminAsync();

    private static async Task<ApiResponse> CreateAsync(ApiClient admin, string? name = null) =>
        (await admin.PostAsync("/api/v1/positions", new { name = name ?? Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);

    private static async Task<ApiResponse> ListAsync(ApiClient client, string query) =>
        (await client.GetAsync($"/api/v1/positions?{query}&pageSize=200")).Expect(HttpStatusCode.OK);
}
