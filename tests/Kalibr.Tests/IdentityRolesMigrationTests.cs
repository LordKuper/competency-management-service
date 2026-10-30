using AwesomeAssertions;
using AwesomeAssertions.Execution;
using Competency.Api;
using Competency.Platform;
using Competency.Tests.Infrastructure;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The data transfer of the migration that moves account roles to Identity roles and the responsible HR of a unit from an account to an employee:
/// rows inserted while the database is on the previous migration must come out right on the next one.
/// Remove this class together with the migrations at the squash before release 1.0.
/// </summary>
public sealed class IdentityRolesMigrationTests(TestEnvironment environment)
{
    private const string PreviousMigration = "20261021091246_InitialCreate";
    private const string TransferMigration = "20261021152612_IdentityRolesAndHrByEmployee";

    [Fact]
    public async Task Ac63_RolesAndHrAssignments_PassToIdentityRolesAndEmployees_WhenTheDatabaseMovesToTheNextMigration()
    {
        var connectionString = await environment.CreateDatabaseAsync();
        await using var provider = ServicesFor(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var migrator = scope.ServiceProvider.GetRequiredService<AppDbContext>().GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration, TestContext.Current.CancellationToken);
        var rows = new Rows();
        await ExecuteAsync(connectionString, rows.InsertSql());

        await migrator.MigrateAsync(TransferMigration, TestContext.Current.CancellationToken);

        using var all = new AssertionScope();
        (await QueryAsync(connectionString, "SELECT id || ' ' || name FROM roles"))
            .Should().BeEquivalentTo(Enum.GetValues<UserRole>().Select(role => $"{UserRoleIds.Of(role)} {role}"), "a role row exists for every member of the enumeration");
        (await QueryAsync(connectionString, "SELECT u.email || ' ' || r.name FROM user_roles ur JOIN users u ON u.id = ur.user_id JOIN roles r ON r.id = ur.role_id"))
            .Should().BeEquivalentTo(
                [
                    $"{rows.Admin.Email} {Scenarios.GlobalAdmin}",
                    $"{rows.HrBound.Email} {Scenarios.Hr}",
                    $"{rows.AdminHr.Email} {Scenarios.GlobalAdmin}",
                    $"{rows.AdminHr.Email} {Scenarios.Hr}",
                ],
                "the administrator keeps the role, and an account assigned as a unit's HR gets the HR role once when it is bound to an employee");
        (await QueryAsync(connectionString, "SELECT org_unit_id || ' ' || employee_id FROM org_unit_hr"))
            .Should().BeEquivalentTo(
                [$"{rows.UnitA} {rows.HrBound.Employee}", $"{rows.UnitB} {rows.HrBound.Employee}", $"{rows.UnitD} {rows.AdminHr.Employee}"],
                "an assignment passes to the employee of its account, and one of an account with no employee is dropped");
        (await QueryAsync(connectionString, "SELECT email || ' ' || coalesce(employee_id::text, '-') FROM users"))
            .Should().BeEquivalentTo(rows.Logins.Select(login => $"{login.Email} {login.Employee?.ToString() ?? "-"}"), "no account is lost and each keeps its employee");
        (await QueryAsync(connectionString, "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'role'"))
            .Should().BeEmpty("the role column is gone");
        (await QueryAsync(connectionString, "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'org_unit_hr'"))
            .Should().BeEquivalentTo(["org_unit_id", "employee_id"], "an assignment is held by an employee, not an account");
    }

    private static ServiceProvider ServicesFor(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{PlatformModule.ConnectionStringName}"] = connectionString })
            .Build();
        return new ServiceCollection().AddApplication(configuration).BuildServiceProvider();
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<string[]> QueryAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var values = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return [.. values];
    }

    /// <summary>
    /// An account of the previous schema: its role column value and the employee it is bound to, if any.
    /// </summary>
    private sealed record Login(string Name, string Role, Guid? Employee = null)
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Email => $"{Name}@migration.test";

        public string InsertSql() =>
            $"('{Id}', 0, '{Email}', '{Email.ToUpperInvariant()}', '{Email}', '{Email.ToUpperInvariant()}', '{Role}', false, {(Employee is { } employee ? $"'{employee}'" : "NULL")}, true, 0)";
    }

    /// <summary>
    /// The rows inserted on the previous migration: accounts of every shape the transfer treats differently, and four units to assign HR to.
    /// </summary>
    private sealed class Rows
    {
        private readonly Guid position = Guid.NewGuid();

        public Guid UnitA { get; } = Guid.NewGuid();

        public Guid UnitB { get; } = Guid.NewGuid();

        public Guid UnitC { get; } = Guid.NewGuid();

        public Guid UnitD { get; } = Guid.NewGuid();

        public Login Admin { get; } = new("admin", Scenarios.GlobalAdmin);

        public Login Plain { get; } = new("plain", Scenarios.User);

        public Login Bound { get; } = new("bound", Scenarios.User, Guid.NewGuid());

        public Login HrBound { get; } = new("hr-bound", Scenarios.User, Guid.NewGuid());

        public Login HrUnbound { get; } = new("hr-unbound", Scenarios.User);

        public Login AdminHr { get; } = new("admin-hr", Scenarios.GlobalAdmin, Guid.NewGuid());

        public IEnumerable<Login> Logins => [Admin, Plain, Bound, HrBound, HrUnbound, AdminHr];

        public string InsertSql() =>
            $"""
            INSERT INTO positions (id, name, is_active, version, created_at) VALUES ('{position}', 'Инженер', true, 0, now());
            INSERT INTO org_units (id, name, parent_id, head_employee_id, is_active, version, created_at) VALUES
                ('{UnitA}', 'A', NULL, NULL, true, 0, now()), ('{UnitB}', 'B', NULL, NULL, true, 0, now()),
                ('{UnitC}', 'C', NULL, NULL, true, 0, now()), ('{UnitD}', 'D', NULL, NULL, true, 0, now());
            INSERT INTO employees (id, last_name, first_name, middle_name, position_id, is_active, org_unit_id, version, created_at) VALUES
                {string.Join(", ", Logins.Where(login => login.Employee is not null).Select(login => $"('{login.Employee}', '{login.Name}', 'Иван', NULL, '{position}', true, '{UnitA}', 0, now())"))};
            INSERT INTO users (id, version, email, normalized_email, user_name, normalized_user_name, role, is_blocked, employee_id, lockout_enabled, access_failed_count) VALUES
                {string.Join(", ", Logins.Select(login => login.InsertSql()))};
            INSERT INTO org_unit_hr (org_unit_id, user_id) VALUES
                ('{UnitA}', '{HrBound.Id}'), ('{UnitB}', '{HrBound.Id}'), ('{UnitC}', '{HrUnbound.Id}'), ('{UnitD}', '{AdminHr.Id}');
            """;
    }
}
