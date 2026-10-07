using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// An account of any role is bound to at most one working employee and an employee has at most one account;
/// binding changes end the account's sessions and are journaled; the e-mail is unique regardless of letter case.
/// </summary>
public sealed class UserBindingTests(TestEnvironment environment)
{
    [Theory]
    [InlineData(Scenarios.User)]
    [InlineData(Scenarios.GlobalAdmin)]
    public async Task Ac8_AccountOfAnyRole_CanBeBoundToAWorkingEmployee(string role)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, lastName: "Кузнецов", firstName: "Пётр", middleName: "Иванович");

        var account = await admin.CreateUserAsync(role, employee.Id);

        var me = (await (await account.SignInAsync(host)).GetAsync("/api/v1/auth/me")).Expect(HttpStatusCode.OK);
        me.Json!["role"]!.GetValue<string>().Should().Be(role);
        me.Json["employeeId"]!.GetValue<string>().Should().Be(employee.Id.ToString());
        me.Json["employeeLastName"]!.GetValue<string>().Should().Be("Кузнецов");
        me.Json["employeeFirstName"]!.GetValue<string>().Should().Be("Пётр");
        me.Json["employeeMiddleName"]!.GetValue<string>().Should().Be("Иванович");
        (await admin.GetUserAsync(account.Id)).Json!["employeeName"]!.GetValue<string>().Should().Be("Кузнецов Пётр Иванович");
    }

    [Fact]
    public async Task Ac8_SecondAccountOnTheSameEmployee_IsRefusedOnCreateAndOnUpdate()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        await admin.CreateUserAsync(employeeId: employee.Id);
        var other = await admin.CreateUserAsync();

        var onCreate = await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("second")}@test.local", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = employee.Id });
        var onUpdate = await admin.PutAsync(
            $"/api/v1/users/{other.Id}",
            new { email = other.Email, role = Scenarios.User, employeeId = employee.Id },
            other.ETag);

        onCreate.Status.Should().Be(HttpStatusCode.Conflict, onCreate.Body);
        onUpdate.Status.Should().Be(HttpStatusCode.Conflict, onUpdate.Body);
        (await admin.GetUserAsync(other.Id)).Json!["employeeId"].Should().BeNull();
    }

    [Fact]
    public async Task Ac8_UnknownOrDismissedEmployee_CannotBeBound()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var dismissed = await admin.CreateEmployeeAsync(unit.Id);
        (await admin.PostAsync($"/api/v1/employees/{dismissed.Id}/dismiss", ifMatch: dismissed.ETag)).Expect(HttpStatusCode.OK);

        var unknown = await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("ghost")}@test.local", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = Guid.NewGuid() });
        var notWorking = await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("gone")}@test.local", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = dismissed.Id });

        unknown.Status.Should().Be(HttpStatusCode.BadRequest, unknown.Body);
        unknown.FieldErrors("employeeId").Should().ContainSingle();
        notWorking.Status.Should().Be(HttpStatusCode.BadRequest, notWorking.Body);
        notWorking.FieldErrors("employeeId").Should().ContainSingle();
    }

    [Fact]
    public async Task Ac8_BindingAndUnbinding_EndTheSessionAndAreJournaledWithOldAndNewEmployee()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);

        var bound = (await admin.PutAsync(
            $"/api/v1/users/{account.Id}",
            new { email = account.Email, role = Scenarios.User, employeeId = employee.Id },
            account.ETag)).Expect(HttpStatusCode.OK);
        var unbound = (await admin.PutAsync(
            $"/api/v1/users/{account.Id}",
            new { email = account.Email, role = Scenarios.User, employeeId = (Guid?)null },
            bound.ETag)).Expect(HttpStatusCode.OK);

        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized, "a changed binding ends the sessions of the account");
        var bindingRow = (await admin.AuditOfRequestAsync(bound.RequestId!)).Single();
        bindingRow!["action"]!.GetValue<string>().Should().Be("AppUser.Updated");
        bindingRow["oldValue"]!["employeeId"].Should().BeNull();
        bindingRow["newValue"]!["employeeId"]!.GetValue<string>().Should().Be(employee.Id.ToString());
        var unbindingRow = (await admin.AuditOfRequestAsync(unbound.RequestId!)).Single();
        unbindingRow!["oldValue"]!["employeeId"]!.GetValue<string>().Should().Be(employee.Id.ToString());
        unbindingRow["newValue"]!["employeeId"].Should().BeNull();
    }

    [Fact]
    public async Task Ac7_Email_IsRequiredValidAndUniqueRegardlessOfCase_AndSignInIgnoresCase()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var local = Scenarios.Unique("Case");
        var first = await admin.CreateUserAsync(email: $"{local}@Test.Local");

        var duplicate = await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{local.ToUpperInvariant()}@TEST.local", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = (Guid?)null });
        var malformed = await admin.PostAsync(
            "/api/v1/users",
            new { email = "not-an-email", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = (Guid?)null });
        var weakPassword = await admin.PostAsync(
            "/api/v1/users",
            new { email = $"{Scenarios.Unique("weak")}@test.local", password = "short", role = Scenarios.User, employeeId = (Guid?)null });

        duplicate.Status.Should().Be(HttpStatusCode.Conflict, duplicate.Body);
        duplicate.FieldErrors("email").Should().ContainSingle();
        malformed.Status.Should().Be(HttpStatusCode.BadRequest);
        malformed.FieldErrors("email").Should().ContainSingle();
        weakPassword.Status.Should().Be(HttpStatusCode.BadRequest);
        weakPassword.FieldErrors("password").Should().NotBeEmpty();
        (await host.LoginAsync(first.Email.ToLowerInvariant(), first.Password)).Should().NotBeNull();
    }

    [Fact]
    public async Task Ac7_UserSearch_TreatsPercentAndUnderscoreLiterally_AndFiltersByRoleAndBlock()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var token = Scenarios.Unique("srch")[5..];
        var literal = await admin.CreateUserAsync(email: $"a_b{token}@test.local");
        var lookalike = await admin.CreateUserAsync(email: $"axb{token}@test.local");
        var administrator = await admin.CreateUserAsync(Scenarios.GlobalAdmin, email: $"adm{token}@test.local");
        (await admin.PostAsync($"/api/v1/users/{lookalike.Id}/block", ifMatch: lookalike.ETag)).Expect(HttpStatusCode.OK);

        var underscore = await Emails(admin, $"a_b{token}");
        var percent = await Emails(admin, $"%{token}");
        var admins = await Emails(admin, token, $"&role={Scenarios.GlobalAdmin}");
        var blocked = await Emails(admin, token, "&isBlocked=true");

        underscore.Should().Equal(literal.Email);
        percent.Should().BeEmpty("a percent sign is searched as a character, not as a wildcard");
        admins.Should().Equal(administrator.Email);
        blocked.Should().Equal(lookalike.Email);
    }

    private static async Task<string[]> Emails(ApiClient admin, string text, string filters = "")
    {
        var page = (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(text)}{filters}&pageSize=200")).Expect(HttpStatusCode.OK);
        return [.. page.Json!["items"]!.AsArray().Select(user => user!["email"]!.GetValue<string>()).Order()];
    }
}
