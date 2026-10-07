using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Employee search combines case-insensitive substring and Russian full-text matching (stems, «ё» as «е», positions),
/// treats <c>%</c> and <c>_</c> as characters, and validates its paging and text limits.
/// </summary>
public sealed class EmployeeSearchTests(TestEnvironment environment)
{
    private const int MaxSearchLength = 200;

    [Fact]
    public async Task Ac11_Search_MatchesSubstringsStemsYoAndPositionsIgnoringCase()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var ivanov = await admin.CreateEmployeeAsync(unit.Id, "Иванов", "Пётр", position: "Бухгалтер");
        var ivanova = await admin.CreateEmployeeAsync(unit.Id, "Иванова", "Анна", position: "Бухгалтер");
        var fyodorov = await admin.CreateEmployeeAsync(unit.Id, "Фёдоров", "Сергей", position: "Ведущий инженер");
        var sidorov = await admin.CreateEmployeeAsync(unit.Id, "Сидоров", "Олег", position: "Менеджер");

        var bySubstring = await SearchAsync(admin, unit.Id, "ИВАНОВ");
        var byInflection = await SearchAsync(admin, unit.Id, "Ивановой");
        var byYo = await SearchAsync(admin, unit.Id, "Федоров");
        var byPosition = await SearchAsync(admin, unit.Id, "инженер");
        var byFragment = await SearchAsync(admin, unit.Id, "идор");
        var nobody = await SearchAsync(admin, unit.Id, "Несуществующий");

        bySubstring.Should().BeEquivalentTo([ivanov.Id, ivanova.Id]);
        byInflection.Should().Contain(ivanova.Id);
        byYo.Should().Equal(fyodorov.Id);
        byPosition.Should().Equal(fyodorov.Id);
        byFragment.Should().Equal(sidorov.Id);
        nobody.Should().BeEmpty();
    }

    [Fact]
    public async Task Ac11_PercentAndUnderscore_AreSearchedAsCharacters()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var underscore = await admin.CreateEmployeeAsync(unit.Id, "Пет_ров");
        var percent = await admin.CreateEmployeeAsync(unit.Id, "Проц%ентов");
        await admin.CreateEmployeeAsync(unit.Id, "Петаров");

        (await SearchAsync(admin, unit.Id, "_")).Should().Equal(underscore.Id);
        (await SearchAsync(admin, unit.Id, "%")).Should().Equal(percent.Id);
        (await SearchAsync(admin, unit.Id, "Пет_ров")).Should().Equal(underscore.Id);
    }

    [Fact]
    public async Task Ac11_DescendantsAreSearchedOnlyOnRequest_AndPagesDoNotOverlap()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var inParent = await admin.CreateEmployeeAsync(parent.Id, "Агафонов");
        var inChild = await admin.CreateEmployeeAsync(child.Id, "Борисов");
        var inChildToo = await admin.CreateEmployeeAsync(child.Id, "Васильев");

        var direct = await IdsAsync(admin, $"orgUnitId={parent.Id}");
        var withDescendants = await IdsAsync(admin, $"orgUnitId={parent.Id}&includeDescendants=true");
        var firstPage = (await admin.GetAsync($"/api/v1/employees?orgUnitId={parent.Id}&includeDescendants=true&pageSize=2&page=1")).Expect(HttpStatusCode.OK);
        var secondPage = (await admin.GetAsync($"/api/v1/employees?orgUnitId={parent.Id}&includeDescendants=true&pageSize=2&page=2")).Expect(HttpStatusCode.OK);

        direct.Should().Equal(inParent.Id);
        withDescendants.Should().BeEquivalentTo([inParent.Id, inChild.Id, inChildToo.Id]);
        firstPage.Json!["total"]!.GetValue<int>().Should().Be(3);
        Ids(firstPage).Concat(Ids(secondPage)).Should().Equal(inParent.Id, inChild.Id, inChildToo.Id);
    }

    [Fact]
    public async Task Ac11_UnitSearch_MatchesNameBySubstringAndStem()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var token = Scenarios.Unique("x")[2..];
        var unit = await admin.CreateUnitAsync(name: $"Бухгалтерия {token}");
        await admin.CreateUnitAsync(name: $"Склад {token}");

        var found = (await admin.GetAsync($"/api/v1/org-units?q={Uri.EscapeDataString($"бухгалтер {token}")}")).Expect(HttpStatusCode.OK);

        found.Json!["items"]!.AsArray().Select(item => item!["id"]!.GetValue<string>()).Should().Contain(unit.Id.ToString());
    }

    [Fact]
    public async Task Ac11_TooLongTextOrOutOfRangePaging_IsRefusedWith400()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var tooLong = await admin.GetAsync($"/api/v1/employees?q={new string('я', MaxSearchLength + 1)}");
        var pageSize = await admin.GetAsync("/api/v1/employees?pageSize=201");
        var page = await admin.GetAsync("/api/v1/employees?page=0");
        var unitPageSize = await admin.GetAsync("/api/v1/org-units?pageSize=0");

        tooLong.Status.Should().Be(HttpStatusCode.BadRequest);
        tooLong.FieldErrors("q").Should().ContainSingle();
        pageSize.FieldErrors("pageSize").Should().ContainSingle();
        page.FieldErrors("page").Should().ContainSingle();
        unitPageSize.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Guid[]> SearchAsync(ApiClient client, Guid unitId, string text) =>
        await IdsAsync(client, $"orgUnitId={unitId}&q={Uri.EscapeDataString(text)}");

    private static async Task<Guid[]> IdsAsync(ApiClient client, string query) =>
        Ids((await client.GetAsync($"/api/v1/employees?{query}&pageSize=200")).Expect(HttpStatusCode.OK));

    private static Guid[] Ids(ApiResponse page) =>
        [.. page.Json!["items"]!.AsArray().Select(item => Guid.Parse(item!["id"]!.GetValue<string>()))];
}
