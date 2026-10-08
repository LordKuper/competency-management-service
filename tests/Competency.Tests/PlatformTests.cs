using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Platform;
using Competency.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The platform on a real database: schema applied from an empty database, probes, the mail and link settings it refuses to start with,
/// what the logs carry, the account role limited in the database itself, and the journal append-only in the database itself,
/// for the application account too.
/// </summary>
public sealed class PlatformTests(TestEnvironment environment)
{
    private const string RestrictViolation = "23001";
    private const string CheckViolation = "23514";
    private const string UnknownRole = "Auditor";
    private const string FailureLogCategory = "Competency.Platform.ProblemExceptionHandler";
    private const string LongestSmtpTimeout = "49.17:02:47.294";
    private const string OneMillisecondOverTheLongestSmtpTimeout = "49.17:02:47.295";

    [Fact]
    public async Task Ac3_EmptyDatabase_GetsSchemaExtensionsAndBootstrapAdministrator()
    {
        var host = await environment.SharedHostAsync();

        (await host.Anonymous().GetAsync("/healthz/live")).Status.Should().Be(HttpStatusCode.OK);
        (await host.Anonymous().GetAsync("/healthz/ready")).Status.Should().Be(HttpStatusCode.OK);

        var tables = await host.ScalarAsync<long>(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('users', 'org_units', 'employees', 'audit_events')");
        var trigram = await host.ScalarAsync<long>("SELECT count(*) FROM pg_extension WHERE extname = 'pg_trgm'");
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

        ApiHost? started = null;
        var failure = await Record.ExceptionAsync(async () => started = await environment.StartHostAsync(settings));
        if (started is not null)
        {
            await started.DisposeAsync();
        }

        failure.Should().BeOfType<InvalidOperationException>("a host without any administrator must refuse to start");
        failure!.Message.Should().Contain("Bootstrap:AdminEmail").And.Contain("Bootstrap:AdminPassword");
    }

    [Fact]
    public async Task Ac6_RestartOnTheSameDatabase_KeepsTheExistingAdministratorAndIgnoresOtherBootstrapSettings()
    {
        var shared = await environment.SharedHostAsync();
        var otherEmail = $"{Scenarios.Unique("restart")}@test.local";
        const string OtherPassword = "Other-Admin-12345!";
        var settings = new Dictionary<string, string> { ["Bootstrap__AdminEmail"] = otherEmail, ["Bootstrap__AdminPassword"] = OtherPassword };

        await using var restarted = await ApiHost.StartAsync(shared.ConnectionString, environment.WithMail(settings));

        var admin = await restarted.LoginAsync(ApiHost.AdminEmail, ApiHost.AdminPassword);
        (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(otherEmail)}")).Json!["total"]!.GetValue<int>().Should().Be(0);
        (await restarted.Anonymous().PostAsync("/api/v1/auth/login", new { email = otherEmail, password = OtherPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Smtp__Host", "", "Smtp:Host")]
    [InlineData("Smtp__Host", "<SMTP_HOST>", "Smtp:Host")]
    [InlineData("Smtp__SecureSocketOptions", "99", "Smtp:SecureSocketOptions")]
    [InlineData("Smtp__Timeout", "50.00:00:00", "Smtp:Timeout")]
    [InlineData("Smtp__Timeout", OneMillisecondOverTheLongestSmtpTimeout, "Smtp:Timeout")]
    [InlineData("Smtp__From", "not a mailbox", "Smtp:From")]
    [InlineData("Smtp__UserName", "only-a-name", "Smtp:UserName")]
    [InlineData("App__PublicBaseUrl", "ftp://calibr.test.local", "App:PublicBaseUrl")]
    [InlineData("AccountLinks__InvitationLifetime", "00:00:00", "AccountLinks:InvitationLifetime")]
    public async Task Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_FailsNamingTheSetting(string name, string value, string setting)
    {
        ApiHost? started = null;
        var failure = await Record.ExceptionAsync(async () => started = await environment.StartHostAsync(new Dictionary<string, string> { [name] = value }));
        if (started is not null)
        {
            await started.DisposeAsync();
        }

        failure.Should().BeOfType<InvalidOperationException>("a host that cannot send its mail or build its links must refuse to start");
        failure!.Message.Should().Contain($"'{setting}'");
    }

    [Fact]
    public async Task Ac1_TheLongestSmtpTimeoutTheStartAccepts_StillSendsTheMail()
    {
        await using var host = await environment.StartHostAsync(new Dictionary<string, string> { ["Smtp__Timeout"] = LongestSmtpTimeout });
        var email = $"{Scenarios.Unique("longest")}@test.local";

        var created = (await (await host.AdminAsync()).PostAsync("/api/v1/users", new { email, role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);

        created.Json!["mailSent"]!.GetValue<bool>().Should().BeTrue("the send timer accepts the longest timeout the start allows");
        (await environment.Mail.WaitForAsync(email)).Should().ContainSingle();
    }

    [Fact]
    public void Ac1_ShippedSmtpSettings_CheckCertificateRevocation_UntilAnOperatorSwitchesItOff()
    {
        var shipped = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("Smtp").Get<SmtpOptions>();

        shipped!.CheckCertificateRevocation.Should().BeTrue("only an operator who cannot reach the revocation endpoints turns the check off");
    }

    [Fact]
    public async Task Ac4_BootstrapAdministrator_IsCreatedWhenTheOnlyAdministratorHasNotSetAPasswordYet()
    {
        await using var first = await environment.StartHostAsync();
        await first.ExecuteAsync($"UPDATE users SET password_hash = NULL WHERE email = '{ApiHost.AdminEmail}'");
        var otherEmail = $"{Scenarios.Unique("bootstrap")}@test.local";
        const string OtherPassword = "Other-Admin-12345!";
        var settings = new Dictionary<string, string> { ["Bootstrap__AdminEmail"] = otherEmail, ["Bootstrap__AdminPassword"] = OtherPassword };

        await using var restarted = await ApiHost.StartAsync(first.ConnectionString, environment.WithMail(settings));

        var created = await restarted.LoginAsync(otherEmail, OtherPassword);
        (await created.GetAsync("/api/v1/users?role=GlobalAdmin")).Expect(HttpStatusCode.OK);
        (await restarted.Anonymous().PostAsync("/api/v1/auth/login", new { email = ApiHost.AdminEmail, password = ApiHost.AdminPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized, "an administrator without a password cannot sign in");
    }

    /// <summary>
    /// Runs on a host of its own, so that only this test's records can satisfy the wait for the 412 log line.
    /// </summary>
    [Fact]
    public async Task Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesLinksOrSqlValues()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("logged")}@test.local";
        var password = $"Logged-{Guid.NewGuid():N}-1!";
        var wrongPassword = $"Wrong-{Guid.NewGuid():N}-2!";
        var unit = await admin.CreateUnitAsync(name: Scenarios.Unique("Отдел-лог"));
        var employee = await admin.CreateEmployeeAsync(unit.Id, lastName: Scenarios.Unique("Логов"));
        (await admin.PostAsync("/api/v1/users", new { email, role = Scenarios.User, employeeId = employee.Id })).Expect(HttpStatusCode.Created);
        var invitation = (await environment.Mail.WaitForAsync(email))[0];
        (await host.Anonymous().PostAsync("/api/v1/auth/accept-invitation", new { token = invitation.Token, password })).Expect(HttpStatusCode.NoContent);
        (await host.Anonymous().PostAsync("/api/v1/auth/forgot-password", new { email })).Expect(HttpStatusCode.Accepted);
        var reset = (await environment.Mail.WaitForAsync(email, 2))[1];
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password = wrongPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);

        var stale = await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = "Чужое имя", headEmployeeId = (Guid?)null }, "\"999\"");

        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        var logs = await host.LogsAfterAsync("\"Status\":412");
        logs.Split('\n').Where(line => line.Trim().Length > 0).Should().OnlyContain(line => line.TrimStart().StartsWith('{'), "every log line is a JSON document");
        var personalValues = new[]
        {
            email, password, wrongPassword, unit.Json!["name"]!.GetValue<string>(), employee.Json!["lastName"]!.GetValue<string>(),
            invitation.Token, reset.Token, invitation.Link.ToString(), reset.Link.ToString(), ApiHost.PublicBaseUrl, invitation.Subject, invitation.Subject.JsonEscaped(), "Здравствуйте".JsonEscaped(),
        };
        foreach (var personalValue in personalValues)
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
        var before = await host.ScalarAsync<long>("SELECT count(*) FROM audit_events");
        before.Should().BePositive("a mutation was just journaled");

        var rejection = await FluentActions.Awaiting(() => host.ScalarAsync<long>(statement)).Should().ThrowAsync<PostgresException>();

        rejection.Which.SqlState.Should().Be(RestrictViolation);
        (await host.ScalarAsync<long>("SELECT count(*) FROM audit_events")).Should().BeGreaterThanOrEqualTo(before);
    }

    [Theory]
    [InlineData("UPDATE audit_events SET reason = 'edited'")]
    [InlineData("DELETE FROM audit_events")]
    [InlineData("TRUNCATE audit_events")]
    public async Task Ac15_Journal_RejectsUpdateDeleteAndTruncate_EvenWhenOrdinaryTriggersAreSilencedByTheReplicaRole(string statement)
    {
        var host = await environment.SharedHostAsync();
        await (await host.AdminAsync()).CreateUnitAsync();

        await using var connection = new NpgsqlConnection(host.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "SET LOCAL session_replication_role = replica");
        var rejection = await FluentActions.Awaiting(() => ExecuteAsync(connection, statement)).Should().ThrowAsync<PostgresException>(
            "the application account may switch to the replica role, which silences every trigger not enabled always");

        rejection.Which.SqlState.Should().Be(RestrictViolation);
    }

    [Fact]
    public async Task Ac7_UserRole_IsLimitedToTheKnownValuesInTheDatabaseItself()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();

        var rejection = await FluentActions.Awaiting(() => host.ScalarAsync<long>($"UPDATE users SET role = '{UnknownRole}' WHERE id = '{account.Id}'"))
            .Should().ThrowAsync<PostgresException>();

        rejection.Which.SqlState.Should().Be(CheckViolation);
        rejection.Which.ConstraintName.Should().Be("ck_users_role");
        (await admin.GetUserAsync(account.Id)).Json!["role"]!.GetValue<string>().Should().Be(Scenarios.User);
    }

    [Fact]
    public async Task Ac5_ServerErrorLog_CarriesTheStackTraceButNotTheExceptionMessage_AndOtherFailuresCarryNoStack()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var stale = await admin.PutAsync($"/api/v1/org-units/{unit.Id}", new { name = "Чужое имя", headEmployeeId = (Guid?)null }, "\"999\"");
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        var forbiddenName = Scenarios.Unique("Запрещено");
        var constraint = $"ck_unit_name_marker_{Guid.NewGuid():N}";
        await host.ScalarAsync<long>($"ALTER TABLE org_units ADD CONSTRAINT {constraint} CHECK (name <> '{forbiddenName}')");

        var failed = await admin.PostAsync("/api/v1/org-units", new { name = forbiddenName, parentId = (Guid?)null });

        failed.Status.Should().Be(HttpStatusCode.InternalServerError, "a check the application does not know fails in the database and surfaces as a server error");
        var records = FailureRecords(await host.LogsAfterAsync("\"Status\":500")).ToList();
        var serverError = records.Should().ContainSingle(record => record.Status == 500).Subject;
        var clientError = records.Should().ContainSingle(record => record.Status == 412).Subject;
        var stack = serverError.State["StackTrace"]?.GetValue<string>();
        stack.Should().Contain(" at ", "a server error is logged with the place it happened");
        serverError.Line.Should().NotContain(constraint, "the exception message names the violated constraint and is never logged");
        clientError.State["StackTrace"].Should().BeNull("only a server error is logged with its stack trace");
    }

    private static IEnumerable<(string Line, int Status, JsonNode State)> FailureRecords(string logs) =>
        logs.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith('{'))
            .Select(line => (Line: line, Json: JsonNode.Parse(line)!))
            .Where(record => record.Json["Category"]?.GetValue<string>() == FailureLogCategory)
            .Select(record => (record.Line, record.Json["State"]!["Status"]!.GetValue<int>(), record.Json["State"]!));

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
