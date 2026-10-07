using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// An account is never created for, bound to or unblocked with an employee whose dismissal is in progress: the change waits for the
/// dismissal, finds the employee not working and is refused, so a dismissed employee never ends with an unblocked account.
/// The dismissal is held inside its transaction, after it has read which accounts it must block, because that is the window in which
/// a change that does not wait for it is missed by the dismissal and commits an unblocked account on the dismissed employee.
/// </summary>
public sealed class DismissalRaceTests(TestEnvironment environment)
{
    private const long GateKey = 7;

    [Theory]
    [InlineData("create", HttpStatusCode.BadRequest)]
    [InlineData("bind", HttpStatusCode.BadRequest)]
    [InlineData("unblock", HttpStatusCode.Conflict)]
    public async Task Ac8_AccountChangeArrivingDuringADismissal_WaitsForItAndIsRefused(string change, HttpStatusCode refusal)
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var account = change == "create" ? null : await admin.CreateUserAsync(employeeId: change == "unblock" ? employee.Id : null);
        var version = account?.ETag;
        if (change == "unblock")
        {
            version = (await admin.PostAsync($"/api/v1/users/{account!.Id}/block", ifMatch: version)).Expect(HttpStatusCode.OK).ETag;
        }

        await host.ExecuteAsync($"""
            CREATE FUNCTION wait_for_gate() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                PERFORM pg_advisory_lock({GateKey});
                PERFORM pg_advisory_unlock({GateKey});
                RETURN NEW;
            END $$
            """);
        await host.ExecuteAsync("CREATE TRIGGER wait_for_gate BEFORE UPDATE ON employees FOR EACH ROW EXECUTE FUNCTION wait_for_gate()");
        await using var gate = await Gate.CloseAsync(host);
        var dismissal = admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag);
        await Waiting.UntilAsync(async () => await WaitersAsync(host, $"l.locktype = 'advisory' AND l.objid = {GateKey}") > 0, "the dismissal waits at the gate");

        var late = change switch
        {
            "create" => admin.PostAsync(
                "/api/v1/users",
                new { email = $"{Scenarios.Unique("late")}@test.local", password = Scenarios.UserPassword, role = Scenarios.User, employeeId = employee.Id }),
            "bind" => admin.PutAsync(
                $"/api/v1/users/{account!.Id}",
                new { email = account.Email, role = Scenarios.User, employeeId = employee.Id },
                version),
            _ => admin.PostAsync($"/api/v1/users/{account!.Id}/unblock", ifMatch: version),
        };
        await Waiting.UntilAsync(
            async () => late.IsCompleted || await WaitersAsync(host, $"NOT (l.locktype = 'advisory' AND l.objid = {GateKey})") > 0,
            "the change has either finished or waits for the dismissal");
        await gate.OpenAsync();
        var (dismissed, changed) = (await dismissal, await late);

        dismissed.Status.Should().Be(HttpStatusCode.OK, dismissed.Body);
        changed.Status.Should().Be(refusal, changed.Body);
        (await host.ScalarAsync<long>($"SELECT count(*) FROM users WHERE employee_id = '{employee.Id}' AND NOT is_blocked"))
            .Should().Be(0, "a dismissed employee never keeps an unblocked account");
    }

    private static Task<long> WaitersAsync(ApiHost host, string lockFilter) => host.ScalarAsync<long>($"""
        SELECT count(*) FROM pg_locks AS l JOIN pg_stat_activity AS a ON a.pid = l.pid
        WHERE NOT l.granted AND a.datname = current_database() AND {lockFilter}
        """);

    /// <summary>
    /// An advisory lock held on a connection of its own, which the trigger the test installs on employee updates waits for,
    /// so that a request that updates an employee stops inside its transaction until the gate is opened.
    /// </summary>
    private sealed class Gate : IAsyncDisposable
    {
        private readonly NpgsqlConnection connection;

        private Gate(NpgsqlConnection connection) => this.connection = connection;

        public static async Task<Gate> CloseAsync(ApiHost host)
        {
            var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(host.ConnectionString) { Pooling = false }.ConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await Run(connection, $"SELECT pg_advisory_lock({GateKey})");
            return new Gate(connection);
        }

        public Task OpenAsync() => Run(connection, $"SELECT pg_advisory_unlock({GateKey})");

        public ValueTask DisposeAsync() => connection.DisposeAsync();

        private static async Task Run(NpgsqlConnection connection, string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
    }
}
