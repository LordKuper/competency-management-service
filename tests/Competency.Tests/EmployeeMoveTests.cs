using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Moving one assignment of an employee, the main or a secondary one, to another unit together with its position and legal entities:
/// a position outside the new unit's list joins the list, an inactive position, a pair the employee already holds or an inactive unit refuses the move,
/// and an employee who keeps no assignment in a unit they head stops heading it.
/// </summary>
public sealed class EmployeeMoveTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac28_Move_OfTheMainAssignment_ChangesItsUnit_KeepsThePositionAndTheLegalEntities_AndAddsThePositionToTheNewList()
    {
        var (admin, a, b, position) = await ArrangeAsync(listPositionInTarget: false);
        var entity = await admin.LegalEntityAsync(Scenarios.Unique("ООО"));
        var employee = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Перемещенов"), "Иван", a.Id, position, [entity.Id]));

        var moved = (await Move(admin, employee, a.Id, position, b.Id)).Expect(HttpStatusCode.OK);

        moved.Json!["orgUnitId"]!.GetValue<Guid>().Should().Be(b.Id);
        moved.Json["positionId"]!.GetValue<Guid>().Should().Be(position);
        moved.Json["legalEntities"]!.AsArray().Select(item => item!["id"]!.GetValue<Guid>()).Should().Equal(entity.Id);
        moved.ETag.Should().NotBe(employee.ETag);
        var list = (await admin.GetAsync($"/api/v1/org-units/{b.Id}/positions")).Expect(HttpStatusCode.OK).Json!.AsArray();
        list.Select(item => item!["positionId"]!.GetValue<Guid>()).Should().Contain(position);
        var journal = (await admin.AuditOfRequestAsync(moved.RequestId!)).Actions();
        journal.Should().Contain(["Employee.Updated", "OrgUnitPosition.Created"]);
    }

    [Fact]
    public async Task Ac28_Move_OfASecondaryAssignment_ChangesItsUnitOnly_WithItsLegalEntities()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var c = await admin.CreateUnitAsync();
        await admin.PositionAsync(c.Id);
        var entity = await admin.LegalEntityAsync(Scenarios.Unique("ООО"));
        var employee = await admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith(Scenarios.Unique("Совмещенов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position, entity.Id)]));

        var moved = (await Move(admin, employee, b.Id, position, c.Id)).Expect(HttpStatusCode.OK);

        moved.Json!["orgUnitId"]!.GetValue<Guid>().Should().Be(a.Id, "the main assignment stays");
        var secondary = moved.Json["assignments"]!.AsArray().Should().ContainSingle().Subject!;
        secondary["orgUnitId"]!.GetValue<Guid>().Should().Be(c.Id);
        secondary["legalEntities"]!.AsArray().Select(item => item!["id"]!.GetValue<Guid>()).Should().Equal(entity.Id);
    }

    [Fact]
    public async Task Ac28_Move_IsRefusedWithTheReason_AndChangesNothing()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var closed = await admin.DeactivateUnitAsync(await admin.CreateUnitAsync());
        var other = await admin.PositionAsync(a.Id, Scenarios.Unique("Другая"));
        var retired = await admin.PositionAsync(a.Id, Scenarios.Unique("Закрытая"));
        var employee = await admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith(Scenarios.Unique("Отказов"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(b.Id, position)]));
        var holder = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Занятов"), "Иван", a.Id, retired));
        await admin.DeactivatePositionAsync(retired);

        var sameUnit = await Move(admin, employee, a.Id, position, a.Id);
        var heldPair = await Move(admin, employee, a.Id, position, b.Id);
        var inactiveUnit = await Move(admin, employee, a.Id, position, closed.Id);
        var unknownTarget = await Move(admin, employee, a.Id, position, Guid.NewGuid());
        var notAssigned = await Move(admin, employee, a.Id, other, b.Id);
        var inactivePosition = await Move(admin, holder, a.Id, retired, b.Id);

        sameUnit.FieldErrors("toOrgUnitId").Should().Equal("Выберите другое подразделение.");
        heldPair.FieldErrors("toOrgUnitId").Should().ContainSingle().Which.Should().Contain("уже есть");
        inactiveUnit.FieldErrors("toOrgUnitId").Should().ContainSingle();
        unknownTarget.FieldErrors("toOrgUnitId").Should().ContainSingle();
        notAssigned.FieldErrors("positionId").Should().ContainSingle().Which.Should().Contain("нет такого назначения");
        inactivePosition.FieldErrors("positionId").Should().ContainSingle().Which.Should().Contain("неактивна");
        (await admin.GetEmployeeAsync(employee.Id)).ETag.Should().Be(employee.ETag);
        (await admin.GetEmployeeAsync(holder.Id)).ETag.Should().Be(holder.ETag);
    }

    [Fact]
    public async Task Ac28_Move_OfAnUnknownEmployee_Is404_AndOfAStaleCopy_Is412()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var employee = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Устаревов"), "Иван", a.Id, position));
        (await admin.PutAsync(
            $"/api/v1/employees/{employee.Id}",
            OrgScenes.EmployeeWith(employee.Json!["lastName"]!.GetValue<string>(), "Павел", a.Id, position),
            employee.ETag)).Expect(HttpStatusCode.OK);

        var unknown = await admin.PostAsync($"/api/v1/employees/{Guid.NewGuid()}/move", new { fromOrgUnitId = a.Id, positionId = position, toOrgUnitId = b.Id }, "\"1\"");
        var stale = await Move(admin, employee, a.Id, position, b.Id);

        unknown.Status.Should().Be(HttpStatusCode.NotFound);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        (await admin.ViewAsync(employee.Id)).Unit.Should().Be(a.Json!["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task Ac28_Move_OfTheHeadsOnlyAssignmentInTheUnit_ReleasesTheHeadship()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var employee = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Руководов"), "Иван", a.Id, position));
        (await admin.SetHeadAsync(a, employee.Id)).Expect(HttpStatusCode.OK);

        (await Move(admin, employee, a.Id, position, b.Id)).Expect(HttpStatusCode.OK);

        (await admin.GetUnitAsync(a.Id)).Json!["headEmployeeId"].Should().BeNull();
        (await admin.GetUnitAsync(b.Id)).Json!["headEmployeeId"].Should().BeNull("the move does not make the employee the head of the new unit");
    }

    [Fact]
    public async Task Ac28_Move_OfTheMainAssignment_KeepsTheHeadship_WhileASecondaryAssignmentRemainsInTheUnit()
    {
        var (admin, a, b, position) = await ArrangeAsync();
        var second = await admin.PositionAsync(a.Id, Scenarios.Unique("Вторая"));
        var employee = await admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith(Scenarios.Unique("Остатков"), "Иван", a.Id, position, assignments: [OrgScenes.Assignment(a.Id, second)]));
        (await admin.SetHeadAsync(a, employee.Id)).Expect(HttpStatusCode.OK);

        (await Move(admin, employee, a.Id, position, b.Id)).Expect(HttpStatusCode.OK);

        (await admin.GetUnitAsync(a.Id)).Json!["headEmployeeId"]!.GetValue<Guid>().Should().Be(employee.Id);
    }

    [Fact]
    public async Task Ac28_Move_IsOpenToHr()
    {
        var host = await environment.SharedHostAsync();
        var (admin, a, b, position) = await ArrangeAsync();
        var hr = (await admin.HrCandidateAsync(host, a.Id)).Client;
        var employee = await admin.CreateEmployeeWithAsync(OrgScenes.EmployeeWith(Scenarios.Unique("Кадровов"), "Иван", a.Id, position));

        var moved = await Move(hr, employee, a.Id, position, b.Id);

        moved.Status.Should().Be(HttpStatusCode.OK, moved.Body);
    }

    private static Task<ApiResponse> Move(ApiClient client, ApiResponse employee, Guid from, Guid position, Guid to) =>
        client.PostAsync($"/api/v1/employees/{employee.Id}/move", new { fromOrgUnitId = from, positionId = position, toOrgUnitId = to }, employee.ETag);

    private async Task<(ApiClient Admin, ApiResponse A, ApiResponse B, Guid Position)> ArrangeAsync(bool listPositionInTarget = true)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var a = await admin.CreateUnitAsync();
        var b = await admin.CreateUnitAsync();
        var position = await admin.PositionAsync(a.Id);
        if (listPositionInTarget)
        {
            await admin.PositionAsync(b.Id);
        }

        return (admin, a, b, position);
    }
}
