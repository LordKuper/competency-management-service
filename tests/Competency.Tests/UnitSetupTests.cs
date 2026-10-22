using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the connect-a-unit screen reads for a global administrator: the unit's head, responsible HR, positions and the accounts that
/// let them act, row by row as each gap is closed. The class has the walk alone, because it starts on a host without any HR account.
/// </summary>
public sealed class UnitSetupTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac81_Ac82_Ac84_TheRowsOfAUnit_FollowItsHeadItsHrItsAccountsAndItsPositions_AsEachGapIsClosed()
    {
        var admin = await fixture.Host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var path = $"/api/v1/career/org-units/{unit.Id}/setup";

        var empty = await SetupAsync(admin, path);
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var withEmployee = await SetupAsync(admin, path);
        (await admin.SetHeadAsync(unit, employee.Id)).Expect(HttpStatusCode.OK);
        var withHead = await SetupAsync(admin, path);
        var invited = await admin.InviteUserAsync(employeeId: employee.Id);
        var withInvited = await SetupAsync(admin, path);
        (await admin.PostAsync($"/api/v1/users/{invited.Id}/block", ifMatch: (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);
        var withBlocked = await SetupAsync(admin, path);
        var candidate = await admin.HrCandidateAsync(fixture.Host, (await admin.CreateUnitAsync()).Id);
        var withCandidate = await SetupAsync(admin, path);
        (await admin.AssignHrAsync(unit.Id, candidate.EmployeeId)).Expect(HttpStatusCode.NoContent);
        var withHr = await SetupAsync(admin, path);
        (await admin.PostAsync($"/api/v1/users/{candidate.Id}/block", ifMatch: (await admin.GetUserAsync(candidate.Id)).ETag)).Expect(HttpStatusCode.OK);
        var withBlockedHr = await SetupAsync(admin, path);

        empty.Rows.Should().Equal(
            "Head/Missing/NoHead", "HeadAccount/Waiting/NoHead", "Hr/Missing/NoHrCandidates", "HrAccount/Waiting/NoHr", "Positions/Missing/NoPositions");
        withEmployee.Rows.Should().Equal("Head/Missing/NoHead", "HeadAccount/Waiting/NoHead", "Hr/Missing/NoHrCandidates", "HrAccount/Waiting/NoHr", "Positions/Ready/");
        withEmployee.Json["positions"]!["own"]!.AsArray().Should().ContainSingle();
        withHead.Rows[..2].Should().Equal("Head/Ready/", "HeadAccount/Missing/NoAccount");
        withHead.Json["head"]!["id"]!.GetValue<Guid>().Should().Be(employee.Id);
        withInvited.Rows[1].Should().Be("HeadAccount/Missing/Invited");
        withBlocked.Rows[1].Should().Be("HeadAccount/Missing/Blocked");
        withCandidate.Rows[2..4].Should().Equal("Hr/Missing/NoHr", "HrAccount/Waiting/NoHr");
        withHr.Rows[2..4].Should().Equal("Hr/Ready/", "HrAccount/Ready/");
        withHr.Json["hr"]!["employeeId"]!.GetValue<Guid>().Should().Be(candidate.Employee);
        withBlockedHr.Rows[2..4].Should().Equal("Hr/Missing/HrCannotAct", "HrAccount/Missing/Blocked");
        withBlockedHr.Json["hr"]!["account"]!["state"]!.GetValue<string>().Should().Be("Blocked");
        withBlockedHr.Json["employees"]!.GetValue<int>().Should().Be(1, "the HR candidate works in another unit");
    }

    private static string[] Rows(JsonNode setup) =>
        [.. setup["rows"]!.AsArray().Select(row => $"{row!["row"]}/{row["status"]}/{row["reason"]}")];

    private static async Task<Setup> SetupAsync(ApiClient admin, string path)
    {
        var json = (await admin.GetAsync(path)).Expect(HttpStatusCode.OK).Json!;
        return new Setup(json, Rows(json));
    }

    private sealed record Setup(JsonNode Json, string[] Rows);
}
