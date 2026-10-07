using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The platform on a real database: AC-3 (schema applied from an empty database, probes) and AC-15 (the journal is append-only in the database itself).
/// </summary>
public sealed class PlatformTests(TestEnvironment environment)
{
    private const string RestrictViolation = "23001";

    [Fact]
    public async Task Ac3_EmptyDatabase_GetsSchemaExtensionsAndBootstrapAdministrator()
    {
        var host = await environment.SharedHostAsync();

        (await host.Anonymous().GetAsync("/healthz/live")).Status.Should().Be(HttpStatusCode.OK);
        (await host.Anonymous().GetAsync("/healthz/ready")).Status.Should().Be(HttpStatusCode.OK);

        var tables = await ScalarAsync<long>(
            host,
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('users', 'org_units', 'employees', 'audit_events')");
        var trigram = await ScalarAsync<long>(host, "SELECT count(*) FROM pg_extension WHERE extname = 'pg_trgm'");
        tables.Should().Be(4);
        trigram.Should().Be(1);

        var me = (await (await host.AdminAsync()).GetAsync("/api/v1/auth/me")).Expect(HttpStatusCode.OK);
        me.Json!["role"]!.GetValue<string>().Should().Be(Scenarios.GlobalAdmin);
        me.Json["email"]!.GetValue<string>().Should().Be(ApiHost.AdminEmail);
        me.Json["employeeId"].Should().BeNull("the bootstrap administrator is created without an employee");
    }

    [Fact]
    public async Task Ac3_Readiness_ReportsUnavailableWhenTheDatabaseIsGone_WhileLivenessStaysUp()
    {
        await using var host = await environment.StartHostAsync();
        (await host.Anonymous().GetAsync("/healthz/ready")).Status.Should().Be(HttpStatusCode.OK);

        await environment.DropDatabaseAsync(host);

        (await host.Anonymous().GetAsync("/healthz/ready")).Status.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await host.Anonymous().GetAsync("/healthz/live")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac6_StartWithoutAnAdministratorAndWithoutBootstrapSettings_FailsNamingTheMissingSettings()
    {
        var settings = new Dictionary<string, string> { ["Bootstrap__AdminEmail"] = string.Empty, ["Bootstrap__AdminPassword"] = string.Empty };

        var failure = await FluentActions.Awaiting(() => environment.StartHostAsync(settings)).Should().ThrowAsync<InvalidOperationException>();

        failure.Which.Message.Should().Contain("Bootstrap:AdminEmail").And.Contain("Bootstrap:AdminPassword");
    }

    [Fact]
    public async Task Ac6_RestartOnTheSameDatabase_KeepsTheExistingAdministratorAndIgnoresOtherBootstrapSettings()
    {
        var shared = await environment.SharedHostAsync();
        var otherEmail = $"{Scenarios.Unique("restart")}@test.local";
        const string OtherPassword = "Other-Admin-12345!";
        var settings = new Dictionary<string, string> { ["Bootstrap__AdminEmail"] = otherEmail, ["Bootstrap__AdminPassword"] = OtherPassword };

        await using var restarted = await ApiHost.StartAsync(shared.ConnectionString, settings);

        var admin = await restarted.LoginAsync(ApiHost.AdminEmail, ApiHost.AdminPassword);
        (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(otherEmail)}")).Json!["total"]!.GetValue<int>().Should().Be(0);
        (await restarted.Anonymous().PostAsync("/api/v1/auth/login", new { email = otherEmail, password = OtherPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesOrSqlValues()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("logged")}@test.local";
        var password = $"Logged-{Guid.NewGuid():N}-1!";
        var wrongPassword = $"Wrong-{Guid.NewGuid():N}-2!";
        var unit = await admin.CreateUnitAsync(name: Scenarios.Unique("Отдел-лог"));
        var employee = await admin.CreateEmployeeAsync(unit.Id, lastName: Scenarios.Unique("Логов"));
        (await admin.PostAsync("/api/v1/users", new { email, password, role = Scenarios.User, employeeId = employee.Id })).Expect(HttpStatusCode.Created);
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password = wrongPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);

        var stale = await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = "Чужое имя", headEmployeeId = (Guid?)null }, "\"999\"");

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        var logs = host.Logs;
        logs.Should().Contain("Request failed with status 412", "the failure above must have been logged, or the checks below prove nothing");
        logs.Split('\n').Where(line => line.Trim().Length > 0).Should().OnlyContain(line => line.TrimStart().StartsWith('{'), "every log line is a JSON document");
        foreach (var personalValue in new[] { email, password, wrongPassword, unit.Json!["name"]!.GetValue<string>(), employee.Json!["lastName"]!.GetValue<string>() })
        {
            logs.Should().NotContain(personalValue);
        }

        logs.Should().NotContain("DbCommand", "SQL statements and their parameters are not logged");
    }

    [Theory]
    [InlineData("UPDATE audit_events SET reason = 'edited'")]
    [InlineData("DELETE FROM audit_events")]
    [InlineData("TRUNCATE audit_events")]
    public async Task Ac15_Journal_RejectsUpdateDeleteAndTruncateInTheDatabase(string statement)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        await admin.CreateUnitAsync();
        var before = await ScalarAsync<long>(host, "SELECT count(*) FROM audit_events");
        before.Should().BePositive("a mutation was just journaled");

        var rejection = await FluentActions.Awaiting(() => ScalarAsync<long>(host, statement)).Should().ThrowAsync<PostgresException>();

        rejection.Which.SqlState.Should().Be(RestrictViolation);
        (await ScalarAsync<long>(host, "SELECT count(*) FROM audit_events")).Should().BeGreaterThanOrEqualTo(before);
    }

    private static async Task<T> ScalarAsync<T>(ApiHost host, string sql)
    {
        await using var connection = new NpgsqlConnection(host.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        return value is T typed ? typed : default!;
    }
}
