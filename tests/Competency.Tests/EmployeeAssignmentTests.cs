using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// An employee works at one main assignment and any number of secondary ones, each with its own legal entities: the request replaces the whole set,
/// the rules of an assignment are checked with the field that is at fault, lists and counts see every assignment, and a headship needs an assignment in the unit.
/// </summary>
public sealed class EmployeeAssignmentTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac1_Create_KeepsTheMainAssignmentAndTheSecondaryOnesWithTheirLegalEntities()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var alpha = await admin.LegalEntityAsync(Scenarios.Unique("ООО Альфа"));
        var beta = await admin.LegalEntityAsync(Scenarios.Unique("ООО Бета"));

        var created = await admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith(Scenarios.Unique("Совмещенов"), "Иван", a.Id, position, [alpha.Id], [OrgScenes.Assignment(b.Id, position, beta.Id, alpha.Id)]));

        var view = await admin.ViewAsync(created.Id);
        view.LegalEntities.Should().Equal(alpha.Json!["name"]!.GetValue<string>());
        var secondary = created.Json!["assignments"]!.AsArray().Should().ContainSingle().Subject!;
        secondary["orgUnitId"]!.GetValue<Guid>().Should().Be(b.Id);
        secondary["isSecondary"]!.GetValue<bool>().Should().BeTrue();
        secondary["legalEntities"]!.AsArray().Select(entity => entity!["id"]!.GetValue<Guid>()).Should().BeEquivalentTo([alpha.Id, beta.Id]);
        created.Json["orgUnitId"]!.GetValue<Guid>().Should().Be(a.Id);
    }

    [Fact]
    public async Task Ac2_Replace_SetsTheWholeSetOfAssignments_AndALegalEntityChangeAloneMovesTheVersion()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var c = await admin.CreateUnitAsync();
        await admin.PositionAsync(c.Id);
        var entity = await admin.LegalEntityAsync(Scenarios.Unique("ООО"));
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Замен"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));
        var lastName = created.Json!["lastName"]!.GetValue<string>();

        var replaced = (await admin.PutAsync(
            $"/api/v1/employees/{created.Id}",
            OrgScenes.EmployeeWith(lastName, "Иван", a.Id, position, assignments: [OrgScenes.Assignment(c.Id, position)]),
            created.ETag)).Expect(HttpStatusCode.OK);
        var withEntity = (await admin.PutAsync(
            $"/api/v1/employees/{created.Id}",
            OrgScenes.EmployeeWith(lastName, "Иван", a.Id, position, [entity.Id], [OrgScenes.Assignment(c.Id, position)]),
            replaced.ETag)).Expect(HttpStatusCode.OK);

        replaced.Json!["assignments"]!.AsArray().Select(item => item!["orgUnitId"]!.GetValue<Guid>()).Should().Equal(c.Id);
        replaced.ETag.Should().NotBe(created.ETag);
        withEntity.ETag.Should().NotBe(replaced.ETag, "a change of the legal entities alone is a change of the employee");
        withEntity.Json!["legalEntities"]!.AsArray().Should().ContainSingle();
    }

    [Fact]
    public async Task Ac3_MakingASecondaryAssignmentTheMain_SwapsTheTwoInOneRequest()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Обменов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));

        var swapped = (await admin.PutAsync(
            $"/api/v1/employees/{created.Id}",
            OrgScenes.EmployeeWith(created.Json!["lastName"]!.GetValue<string>(), "Иван", b.Id, position, assignments: [OrgScenes.Assignment(a.Id, position)]),
            created.ETag)).Expect(HttpStatusCode.OK);

        swapped.Json!["orgUnitId"]!.GetValue<Guid>().Should().Be(b.Id);
        swapped.Json["assignments"]!.AsArray().Select(item => item!["orgUnitId"]!.GetValue<Guid>()).Should().Equal(a.Id);
    }

    [Fact]
    public async Task Ac4_Create_RefusesABrokenAssignment_NamingTheField_AndCreatesNothing()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var closed = await admin.DeactivateUnitAsync(await admin.CreateUnitAsync());
        var elsewhere = await admin.CreateUnitAsync();
        var lastName = Scenarios.Unique("Отказов");
        var unknown = Guid.NewGuid();

        var sameAsMain = await Create(admin, lastName, a, position, [OrgScenes.Assignment(a.Id, position)]);
        var repeated = await Create(admin, lastName, a, position, [OrgScenes.Assignment(b.Id, position), OrgScenes.Assignment(b.Id, position)]);
        var inactiveUnit = await Create(admin, lastName, a, position, [OrgScenes.Assignment(closed.Id, position)]);
        var positionNotInUnit = await Create(admin, lastName, a, position, [OrgScenes.Assignment(elsewhere.Id, position)]);
        var unknownEntityOnMain = await admin.PostAsync("/api/v1/employees", OrgScenes.EmployeeWith(lastName, "Иван", a.Id, position, [unknown]));
        var unknownEntityOnSecondary = await Create(admin, lastName, a, position, [OrgScenes.Assignment(b.Id, position, unknown)]);

        sameAsMain.FieldErrors("assignments[0].positionId").Should().ContainSingle().Which.Should().Contain("уже есть");
        repeated.FieldErrors("assignments[1].positionId").Should().ContainSingle().Which.Should().Contain("уже есть");
        inactiveUnit.FieldErrors("assignments[0].orgUnitId").Should().ContainSingle();
        positionNotInUnit.FieldErrors("assignments[0].positionId").Should().ContainSingle();
        unknownEntityOnMain.FieldErrors("legalEntityIds").Should().ContainSingle();
        unknownEntityOnSecondary.FieldErrors("assignments[0].legalEntityIds").Should().ContainSingle();
        (await admin.GetAsync($"/api/v1/employees?q={lastName}")).Json!["total"]!.GetValue<int>().Should().Be(0);
    }

    [Theory]
    [InlineData("assignments-null")]
    [InlineData("legal-entities-null")]
    [InlineData("assignments-absent")]
    [InlineData("legal-entities-absent")]
    [InlineData("secondary-entities-null")]
    public async Task Ac4_Create_RefusesARequestWithoutTheListsOfAssignmentsAndLegalEntities(string kind)
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var name = Scenarios.Unique("Безспис");

        var refused = kind switch
        {
            "assignments-null" => await admin.PostAsync("/api/v1/employees", new { lastName = name, firstName = "Иван", positionId = position, orgUnitId = a.Id, legalEntityIds = Array.Empty<Guid>(), assignments = (object?)null }),
            "legal-entities-null" => await admin.PostAsync("/api/v1/employees", new { lastName = name, firstName = "Иван", positionId = position, orgUnitId = a.Id, legalEntityIds = (object?)null, assignments = Array.Empty<object>() }),
            "assignments-absent" => await admin.PostAsync("/api/v1/employees", new { lastName = name, firstName = "Иван", positionId = position, orgUnitId = a.Id, legalEntityIds = Array.Empty<Guid>() }),
            "legal-entities-absent" => await admin.PostAsync("/api/v1/employees", new { lastName = name, firstName = "Иван", positionId = position, orgUnitId = a.Id, assignments = Array.Empty<object>() }),
            _ => await admin.PostAsync("/api/v1/employees", new { lastName = name, firstName = "Иван", positionId = position, orgUnitId = a.Id, legalEntityIds = Array.Empty<Guid>(), assignments = new[] { new { orgUnitId = b.Id, positionId = position, legalEntityIds = (object?)null } } }),
        };

        refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
        (await admin.GetAsync($"/api/v1/employees?q={name}")).Json!["total"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Ac5_List_ByUnit_FindsAnEmployeeByAnyAssignment_AndShowsTheAssignmentsInThatUnit()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Списков"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));

        var ofMain = (await admin.GetAsync($"/api/v1/employees?orgUnitId={a.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();
        var ofSecondary = (await admin.GetAsync($"/api/v1/employees?orgUnitId={b.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        ofMain.Select(item => item!["id"]!.GetValue<Guid>()).Should().Equal(created.Id);
        ofSecondary.Select(item => item!["id"]!.GetValue<Guid>()).Should().Equal(created.Id);
        var inSecondary = ofSecondary.Single()!["unitAssignments"]!.AsArray().Should().ContainSingle().Subject!;
        inSecondary["orgUnitId"]!.GetValue<Guid>().Should().Be(b.Id);
        inSecondary["isSecondary"]!.GetValue<bool>().Should().BeTrue();
        ofMain.Single()!["unitAssignments"]!.AsArray().Single()!["isSecondary"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task Ac5_Search_FindsAnEmployeeByThePositionOfASecondaryAssignment()
    {
        var (admin, a, b, _) = await ArrangeAsync();
        var main = await admin.PositionAsync(a.Id, "Бухгалтер");
        var tag = OrgScenes.Tag();
        var rare = await admin.PositionAsync(b.Id, $"Метролог {tag}");
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Поисков"), "Иван", a.Id, main, assignments: [OrgScenes.Assignment(b.Id, rare)]));

        var found = (await admin.GetAsync($"/api/v1/employees?q={tag}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        found.Select(item => item!["id"]!.GetValue<Guid>()).Should().Contain(created.Id);
    }

    [Fact]
    public async Task Ac6_UnitAndPositionCounts_CountAnEmployeeOnceInEveryUnitTheyWorkIn()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Счётов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));

        var ofMain = (await admin.GetAsync($"/api/v1/org-units/{a.Id}/summary")).Expect(HttpStatusCode.OK).Json!;
        var ofSecondary = (await admin.GetAsync($"/api/v1/org-units/{b.Id}/summary")).Expect(HttpStatusCode.OK).Json!;

        ofMain["employeeCount"]!.GetValue<int>().Should().Be(1);
        ofSecondary["employeeCount"]!.GetValue<int>().Should().Be(1, "a secondary assignment counts the employee in its unit");
        (await admin.GetAsync($"/api/v1/positions/{position}/impact")).Json!["employeeCount"]!.GetValue<int>().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Ac7_DismissAndRehire_KeepTheAssignments_AndRehireIsRefusedWhileTheUnitOfASecondaryOneIsInactive()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Уволов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));
        var dismissed = await admin.DismissAsync(created);
        var closed = await admin.DeactivateUnitAsync(await admin.GetUnitAsync(b.Id));

        var refused = await admin.PostAsync($"/api/v1/employees/{created.Id}/rehire", ifMatch: dismissed.ETag);
        (await admin.PostAsync($"/api/v1/org-units/{b.Id}/activate", ifMatch: closed.ETag)).Expect(HttpStatusCode.OK);
        var rehired = await admin.PostAsync($"/api/v1/employees/{created.Id}/rehire", ifMatch: dismissed.ETag);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain("совмещение");
        rehired.Status.Should().Be(HttpStatusCode.OK, rehired.Body);
        (await admin.ViewAsync(created.Id)).Assignments.Should().HaveCount(1);
    }

    [Fact]
    public async Task Ac7_Delete_RemovesTheEmployeeWithTheirAssignments()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var host = admin.Host;
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Удалёнов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));

        (await admin.DeleteAsync($"/api/v1/employees/{created.Id}", created.ETag)).Expect(HttpStatusCode.NoContent);

        (await host.ScalarAsync<long>($"SELECT count(*) FROM employee_assignments WHERE employee_id = '{created.Id}'")).Should().Be(0);
        (await admin.GetAsync($"/api/v1/employees/{created.Id}")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac29_Head_MayBeAnEmployeeWithASecondaryAssignmentInTheUnit_AndIsReleasedWhenTheLastAssignmentThereGoes()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Главков"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));
        var lastName = created.Json!["lastName"]!.GetValue<string>();

        (await admin.SetHeadAsync(b, created.Id)).Expect(HttpStatusCode.OK);
        var kept = (await admin.PutAsync(
            $"/api/v1/employees/{created.Id}",
            OrgScenes.EmployeeWith(lastName, "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]),
            created.ETag)).Expect(HttpStatusCode.OK);
        var dropped = (await admin.PutAsync($"/api/v1/employees/{created.Id}", OrgScenes.EmployeeWith(lastName, "Иван", a.Id, position), kept.ETag)).Expect(HttpStatusCode.OK);

        dropped.Status.Should().Be(HttpStatusCode.OK);
        (await admin.GetUnitAsync(b.Id)).Json!["headEmployeeId"].Should().BeNull("the employee has no assignment left in the unit");
    }

    [Fact]
    public async Task Ac29_Head_IsRefusedForAnEmployeeWithNoAssignmentInTheUnit()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var created = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Чужов"), "Иван", a.Id, position));

        var refused = await admin.SetHeadAsync(b, created.Id);

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors("headEmployeeId").Should().ContainSingle().Which.Should().Contain("нет назначения");
    }

    private static Task<ApiResponse> Create(ApiClient admin, string lastName, ApiResponse main, Guid position, params object[] assignments) =>
        admin.PostAsync("/api/v1/employees", OrgScenes.EmployeeWith(lastName, "Иван", main.Id, position, assignments: assignments));

    private async Task<(ApiClient Admin, ApiResponse A, ApiResponse B, Guid Position)> ArrangeAsync()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var a = await admin.CreateUnitAsync();
        var b = await admin.CreateUnitAsync();
        var position = await admin.PositionAsync(a.Id);
        await admin.PositionAsync(b.Id);
        return (admin, a, b, position);
    }
}
