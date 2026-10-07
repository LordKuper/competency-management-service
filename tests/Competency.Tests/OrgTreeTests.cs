using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// AC-9, AC-10 and AC-19: the unit hierarchy stays a forest even when moves race, a unit is deactivated only when empty,
/// and a head is always a working employee of the unit they head.
/// </summary>
public sealed class OrgTreeTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac9_MovingAUnitIntoItselfOrItsDescendant_IsRefusedAndValidMovesWork()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var a = await admin.CreateUnitAsync();
        var b = await admin.CreateUnitAsync(a.Id);
        var c = await admin.CreateUnitAsync(b.Id);
        var inactive = await admin.CreateUnitAsync();
        (await admin.PostAsync($"/api/v1/org-units/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        var intoDescendant = await MoveAsync(admin, a.Id, c.Id);
        var intoItself = await MoveAsync(admin, a.Id, a.Id);
        var toInactive = await MoveAsync(admin, a.Id, inactive.Id);
        var toUnknown = await MoveAsync(admin, a.Id, Guid.NewGuid());
        var toRoot = (await MoveAsync(admin, c.Id, null)).Expect(HttpStatusCode.OK);
        var rebuilt = (await MoveAsync(admin, b.Id, c.Id)).Expect(HttpStatusCode.OK);

        intoDescendant.Status.Should().Be(HttpStatusCode.Conflict);
        intoDescendant.Json!["detail"]!.GetValue<string>().Should().Contain("цикл");
        intoItself.Status.Should().Be(HttpStatusCode.Conflict);
        toInactive.Status.Should().Be(HttpStatusCode.BadRequest);
        toInactive.FieldErrors("parentId").Should().ContainSingle();
        toUnknown.Status.Should().Be(HttpStatusCode.BadRequest);
        toRoot.Json!["parentId"].Should().BeNull();
        rebuilt.Json!["parentId"]!.GetValue<string>().Should().Be(c.Id.ToString());
        (await PathAsync(admin, b.Id)).Should().Equal(c.Id, b.Id);
        (await PathAsync(admin, a.Id)).Should().Equal(a.Id);
    }

    [Fact]
    public async Task Ac9_ConcurrentMovesThatWouldFormACycle_NeverBothSucceed()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        for (var round = 0; round < 5; round++)
        {
            var a = await admin.CreateUnitAsync();
            var b = await admin.CreateUnitAsync();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = MoveWhenAsync(admin, a, b.Id, gate.Task);
            var second = MoveWhenAsync(admin, b, a.Id, gate.Task);
            gate.SetResult();
            var results = await Task.WhenAll(first, second);

            results.Select(result => result.Status).Order().Should().Equal(HttpStatusCode.OK, HttpStatusCode.Conflict);
            (await IsRootedAsync(admin, a.Id)).Should().BeTrue();
            (await IsRootedAsync(admin, b.Id)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task Ac9_ThreeConcurrentMovesAroundACircle_LeaveAForest()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        for (var round = 0; round < 3; round++)
        {
            var a = await admin.CreateUnitAsync();
            var b = await admin.CreateUnitAsync();
            var c = await admin.CreateUnitAsync();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var moves = new[]
            {
                MoveWhenAsync(admin, a, b.Id, gate.Task),
                MoveWhenAsync(admin, b, c.Id, gate.Task),
                MoveWhenAsync(admin, c, a.Id, gate.Task),
            };
            gate.SetResult();
            var results = await Task.WhenAll(moves);

            results.Count(result => result.Status == HttpStatusCode.OK).Should().Be(2);
            results.Count(result => result.Status == HttpStatusCode.Conflict).Should().Be(1);
            foreach (var unit in new[] { a, b, c })
            {
                (await IsRootedAsync(admin, unit.Id)).Should().BeTrue();
            }
        }
    }

    [Fact]
    public async Task Ac9_Deactivation_IsRefusedWhileActiveChildrenOrWorkingEmployeesRemain()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        var child = await admin.CreateUnitAsync(parent.Id);
        var employee = await admin.CreateEmployeeAsync(child.Id);

        var withChild = await Act(admin, parent.Id, "deactivate");
        var withEmployee = await Act(admin, child.Id, "deactivate");
        (await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag)).Expect(HttpStatusCode.OK);
        (await Act(admin, child.Id, "deactivate")).Expect(HttpStatusCode.OK);
        (await Act(admin, parent.Id, "deactivate")).Expect(HttpStatusCode.OK);
        var childUnderInactiveParent = await Act(admin, child.Id, "activate");
        var hireIntoInactive = await admin.PostAsync(
            "/api/v1/employees",
            new { lastName = "Сидоров", firstName = "Олег", middleName = (string?)null, position = "Инженер", orgUnitId = child.Id });
        var rehireIntoInactive = await admin.PostAsync($"/api/v1/employees/{employee.Id}/rehire", ifMatch: (await admin.GetEmployeeAsync(employee.Id)).ETag);
        (await Act(admin, parent.Id, "activate")).Expect(HttpStatusCode.OK);
        (await Act(admin, child.Id, "activate")).Expect(HttpStatusCode.OK);

        withChild.Status.Should().Be(HttpStatusCode.Conflict);
        withChild.Json!["detail"]!.GetValue<string>().Should().Contain("дочерние");
        withEmployee.Status.Should().Be(HttpStatusCode.Conflict);
        withEmployee.Json!["detail"]!.GetValue<string>().Should().Contain("работающие сотрудники");
        childUnderInactiveParent.Status.Should().Be(HttpStatusCode.Conflict);
        hireIntoInactive.Status.Should().Be(HttpStatusCode.BadRequest);
        hireIntoInactive.FieldErrors("orgUnitId").Should().ContainSingle();
        rehireIntoInactive.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ac19_Head_MustBeAWorkingEmployeeOfTheSameUnit()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var empty = await admin.CreateUnitAsync();
        var other = await admin.CreateUnitAsync();
        var working = await admin.CreateEmployeeAsync(unit.Id);
        var dismissed = await admin.CreateEmployeeAsync(unit.Id);
        (await admin.PostAsync($"/api/v1/employees/{dismissed.Id}/dismiss", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);
        var stranger = await admin.CreateEmployeeAsync(other.Id);

        var fromOtherUnit = await admin.SetHeadAsync(unit, stranger.Id);
        var notWorking = await admin.SetHeadAsync(unit, dismissed.Id);
        var unknown = await admin.SetHeadAsync(unit, Guid.NewGuid());
        var forEmptyUnit = await admin.SetHeadAsync(empty, working.Id);
        var accepted = (await admin.SetHeadAsync(unit, working.Id)).Expect(HttpStatusCode.OK);
        var tree = (await admin.GetAsync("/api/v1/org-units/tree")).Expect(HttpStatusCode.OK);
        var cleared = (await admin.SetHeadAsync(unit, null)).Expect(HttpStatusCode.OK);

        fromOtherUnit.Status.Should().Be(HttpStatusCode.BadRequest, fromOtherUnit.Body);
        fromOtherUnit.FieldErrors("headEmployeeId").Single().Should().Contain("другом подразделении");
        notWorking.Status.Should().Be(HttpStatusCode.BadRequest);
        notWorking.FieldErrors("headEmployeeId").Single().Should().Contain("не работает");
        unknown.Status.Should().Be(HttpStatusCode.BadRequest);
        unknown.FieldErrors("headEmployeeId").Single().Should().Contain("не найден");
        forEmptyUnit.Status.Should().Be(HttpStatusCode.BadRequest, "a unit without employees has nobody to head it");
        accepted.Json!["headEmployeeId"]!.GetValue<string>().Should().Be(working.Id.ToString());
        var node = tree.Json!.AsArray().Single(candidate => candidate!["id"]!.GetValue<string>() == unit.Id.ToString())!;
        node["headEmployeeId"]!.GetValue<string>().Should().Be(working.Id.ToString());
        node["headName"]!.GetValue<string>().Should().Be(working.Json!["fullName"]!.GetValue<string>());
        cleared.Json!["headEmployeeId"].Should().BeNull();
    }

    [Fact]
    public async Task Ac11_PathSubtreeAndSummary_FollowTheHierarchyAndCountWorkingEmployeesWithDescendants()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var a = await admin.CreateUnitAsync();
        var b = await admin.CreateUnitAsync(a.Id);
        var c = await admin.CreateUnitAsync(b.Id);
        await admin.CreateEmployeeAsync(a.Id);
        await admin.CreateEmployeeAsync(a.Id);
        await admin.CreateEmployeeAsync(b.Id);
        await admin.CreateEmployeeAsync(c.Id);
        var gone = await admin.CreateEmployeeAsync(c.Id);
        (await admin.PostAsync($"/api/v1/employees/{gone.Id}/dismiss", ifMatch: gone.ETag)).Expect(HttpStatusCode.OK);

        var subtree = (await admin.GetAsync($"/api/v1/org-units/{a.Id}/subtree")).Expect(HttpStatusCode.OK);
        var summary = (await admin.GetAsync($"/api/v1/org-units/{a.Id}/summary")).Expect(HttpStatusCode.OK);
        var leaf = (await admin.GetAsync($"/api/v1/org-units/{c.Id}/summary")).Expect(HttpStatusCode.OK);

        (await PathAsync(admin, c.Id)).Should().Equal(a.Id, b.Id, c.Id);
        subtree.Json!.AsArray().Select(unit => Guid.Parse(unit!["id"]!.GetValue<string>())).Should().BeEquivalentTo([a.Id, b.Id, c.Id]);
        summary.Json!["employeeCount"]!.GetValue<int>().Should().Be(4);
        summary.Json["directEmployeeCount"]!.GetValue<int>().Should().Be(2);
        leaf.Json!["employeeCount"]!.GetValue<int>().Should().Be(1);
    }

    private static async Task<ApiResponse> MoveAsync(ApiClient admin, Guid unitId, Guid? parentId) =>
        await admin.PostAsync($"/api/v1/org-units/{unitId}/move", new { parentId }, (await admin.GetUnitAsync(unitId)).ETag);

    private static async Task<ApiResponse> MoveWhenAsync(ApiClient admin, ApiResponse unit, Guid parentId, Task start)
    {
        await start;
        return await admin.PostAsync($"/api/v1/org-units/{unit.Id}/move", new { parentId }, unit.ETag);
    }

    private static async Task<ApiResponse> Act(ApiClient admin, Guid unitId, string action) =>
        await admin.PostAsync($"/api/v1/org-units/{unitId}/{action}", ifMatch: (await admin.GetUnitAsync(unitId)).ETag);

    private static async Task<Guid[]> PathAsync(ApiClient admin, Guid unitId)
    {
        var path = (await admin.GetAsync($"/api/v1/org-units/{unitId}/path")).Expect(HttpStatusCode.OK);
        return [.. path.Json!.AsArray().Select(unit => Guid.Parse(unit!["id"]!.GetValue<string>()))];
    }

    private static async Task<bool> IsRootedAsync(ApiClient admin, Guid unitId)
    {
        var path = (await admin.GetAsync($"/api/v1/org-units/{unitId}/path")).Expect(HttpStatusCode.OK);
        return path.Json!.AsArray()[0]!["parentId"] is null;
    }
}
