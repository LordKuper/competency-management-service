using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(Competency.Tests.Infrastructure.TestEnvironment))]

namespace Competency.Tests.Infrastructure;

/// <summary>
/// The environment shared by the whole test run: one PostgreSQL container, from which every API host gets a database of its own.
/// Tests that only create their own data share one lazily started host; tests that need a database to themselves start another.
/// </summary>
public sealed class TestEnvironment : IAsyncLifetime
{
    private const string PostgresImage = "postgres:18.6-trixie";
    private const string GssSetting = "GSS Encryption Mode=Disable";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(PostgresImage).Build();
    private readonly Lazy<Task<ApiHost>> shared;

    public TestEnvironment()
    {
        shared = new Lazy<Task<ApiHost>>(() => StartHostAsync());
    }

    /// <summary>
    /// The host shared by tests that never depend on the set of accounts or units existing besides their own.
    /// </summary>
    public Task<ApiHost> SharedHostAsync() => shared.Value;

    /// <summary>
    /// Starts a host on a new empty database, so migrations and the first administrator are created by that host.
    /// </summary>
    /// <param name="settings">Configuration overrides for this host, as environment variable names with <c>__</c> separators.</param>
    public async Task<ApiHost> StartHostAsync(IReadOnlyDictionary<string, string>? settings = null)
    {
        var database = $"t{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(container.GetConnectionString()))
        {
            await admin.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new NpgsqlCommand($"CREATE DATABASE {database}", admin);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;
        return await ApiHost.StartAsync($"{connectionString};{GssSetting}", settings);
    }

    /// <summary>
    /// Drops the database of a running host, so that the host stays up while its database is gone.
    /// </summary>
    /// <param name="host">The host whose database is dropped.</param>
    public async Task DropDatabaseAsync(ApiHost host)
    {
        var database = new NpgsqlConnectionStringBuilder(host.ConnectionString).Database;
        await using var admin = new NpgsqlConnection(container.GetConnectionString());
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using var drop = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        if (shared.IsValueCreated && shared.Value.IsCompletedSuccessfully)
        {
            await shared.Value.Result.DisposeAsync();
        }

        await container.DisposeAsync();
    }
}
