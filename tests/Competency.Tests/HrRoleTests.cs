using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The HR role of an account: what it lets the account do, who may be assigned as a unit's HR and listed as a candidate, and that the role
/// stays on an account while its employee answers for units.
/// </summary>
public sealed class HrRoleTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac62_AnAccountWithTheHrRole_MaintainsThePositionCatalogue_ButNotTheAccountsOrTheHrAssignments()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var hr = org.Hr.Client;

        var created = (await hr.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
        var renamed = (await hr.PutAsync($"/api/v1/positions/{created.Id}", new { name = Scenarios.Unique("Другая") }, created.ETag)).Expect(HttpStatusCode.OK);
        var deactivated = (await hr.PostAsync($"/api/v1/positions/{created.Id}/deactivate", ifMatch: renamed.ETag)).Expect(HttpStatusCode.OK);
        (await hr.PostAsync($"/api/v1/positions/{created.Id}/activate", ifMatch: deactivated.ETag)).Expect(HttpStatusCode.OK);

        foreach (var forbidden in new Func<Task<ApiResponse>>[]
        {
            () => hr.PostAsync("/api/v1/users", new { email = $"{Scenarios.Unique("x")}@test.local", roles = Scenarios.NoRoles, employeeId = (Guid?)null }),
            () => hr.PutAsync($"/api/v1/career/org-units/{org.Team.Id}/hr", new { employeeId = org.SiblingHr.EmployeeId }),
        })
        {
            (await forbidden()).Status.Should().Be(HttpStatusCode.Forbidden);
        }

        (await org.Plain.Client.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Чужая") })).Status.Should().Be(HttpStatusCode.Forbidden);
        (await org.TeamHead.Client.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Чужая") })).Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac62_TheHrRole_CannotBeTakenOffAnAccountWhoseEmployeeIsAUnitsHr_UntilThatUnitHasAnother()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var account = await org.Admin.GetUserAsync(org.SiblingHr.Id);
        object Without() => new { email = org.SiblingHr.User.Email, roles = Scenarios.NoRoles, employeeId = (Guid?)org.SiblingHr.EmployeeId };

        var refused = await org.Admin.PutAsync($"/api/v1/users/{org.SiblingHr.Id}", Without(), account.ETag);
        (await org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr")).Expect(HttpStatusCode.NoContent);
        var freed = (await org.Admin.PutAsync($"/api/v1/users/{org.SiblingHr.Id}", Without(), account.ETag)).Expect(HttpStatusCode.OK);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain("Роль «HR» нельзя снять").And.Contain($"«{org.Sibling.Json!["name"]!.GetValue<string>()}»");
        freed.Json!["roles"]!.AsArray().Should().BeEmpty();
        (await org.SiblingHr.Client.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized, "a change of roles ends the sessions of the account");
    }

    [Fact]
    public async Task Ac62_TheRolesOfAnAccount_AreASet_WithSeveralMembers_AndOnlyKnownRoles()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var both = await org.Admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("both")}@test.local", roles = new[] { Scenarios.Hr, Scenarios.GlobalAdmin, Scenarios.Hr }, employeeId = (Guid?)null });
        var unknown = await org.Admin.PostAsync("/api/v1/users", new { email = $"{Scenarios.Unique("nobody")}@test.local", roles = new[] { "Nonsense" }, employeeId = (Guid?)null });

        both.Expect(HttpStatusCode.Created).Json!["roles"]!.AsArray().Select(role => role!.GetValue<string>()).Should().BeEquivalentTo([Scenarios.Hr, Scenarios.GlobalAdmin]);
        unknown.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ac216_TheCandidatesForAUnitsHr_AreWorkingEmployeesWithAnActiveAccountWithTheHrRole_ListedToAdministratorsAlone()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var admin = org.Admin;
        var invited = await admin.CreateEmployeeAsync(org.Team.Id);
        await admin.InviteUserAsync(Scenarios.Hr, invited.Id);
        var blocked = await admin.HrCandidateAsync(fixture.Host, org.Team.Id);
        (await admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: (await admin.GetUserAsync(blocked.Id)).ETag)).Expect(HttpStatusCode.OK);
        var withoutRole = await admin.CreateEmployeeAsync(org.Team.Id);
        await admin.CreateUserAsync(employeeId: withoutRole.Id);
        var withoutAccount = await admin.CreateEmployeeAsync(org.Team.Id);
        var dismissed = await admin.HrCandidateAsync(fixture.Host, org.Team.Id);
        var dismissedEmployee = await admin.GetEmployeeAsync(dismissed.EmployeeId!.Value);
        (await admin.PostAsync($"/api/v1/employees/{dismissed.Employee}/dismiss", ifMatch: dismissedEmployee.ETag)).Expect(HttpStatusCode.OK);
        var regular = await admin.HrCandidateAsync(fixture.Host, org.Team.Id);

        var candidates = (await admin.GetAsync("/api/v1/employees?hrCandidate=true&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();

        candidates.Should().Contain([regular.Employee.ToString(), org.Hr.Employee.ToString(), org.SiblingHr.Employee.ToString()]);
        candidates.Should().NotContain(
            [invited.Id.ToString(), blocked.Employee.ToString(), withoutRole.Id.ToString(), withoutAccount.Id.ToString(), dismissed.Employee.ToString(), org.TeamHead.Employee.ToString()]);
        foreach (var refused in new[] { org.Hr.Client, org.TeamHead.Client, org.Plain.Client })
        {
            (await refused.GetAsync("/api/v1/employees?hrCandidate=true")).Status.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
