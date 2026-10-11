using Competency.Api;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A database of its own that a test migrates step by step with the application's own migrations, so that data written at an earlier schema
/// meets the migration under test exactly as it would on a deployed database. No host runs on it.
/// </summary>
public sealed class MigrationDatabase : IAsyncDisposable
{
    public const string BeforePositions = "20261008074548_AccountLinks";
    public const string Positions = "20261009090759_PositionCatalogue";
    public const string BeforeCareer = "20261009093207_OrgUnitHr";
    public const string Catalogues = "20261009095543_CareerCatalogues";
    public const string Matrices = "20261009102128_CompetencyMatrices";
    public const string BeforeOwnership = Matrices;
    public const string Ownership = "20261011105012_CatalogOwnership";
    public const string Salary = "20261011144210_RemoveCareerLevelSalary";
    public const string Levels = "20261011152104_SimplifyCareerLevels";

    private readonly ServiceProvider services;

    private MigrationDatabase(ServiceProvider services, string connectionString)
    {
        this.services = services;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<MigrationDatabase> CreateAsync(TestEnvironment environment)
    {
        var connectionString = await environment.CreateDatabaseAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = connectionString })
            .Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication(configuration)
            .BuildServiceProvider();
        return new MigrationDatabase(services, connectionString);
    }

    /// <summary>
    /// Brings the database to the state right after the migration, applying the ones after the current state or undoing the ones above it.
    /// </summary>
    /// <param name="migration">The identifier of the target migration.</param>
    public async Task MigrateToAsync(string migration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.GetService<IMigrator>().MigrateAsync(migration, TestContext.Current.CancellationToken);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        return value is null or DBNull ? default! : (T)value;
    }

    /// <summary>
    /// Reads every row of a query, each as the text of its columns, so a test compares what the database holds without a model of its own.
    /// </summary>
    /// <param name="sql">The query.</param>
    /// <returns>The rows, each an array of column values as text; null stays null.</returns>
    public async Task<List<string?[]>> RowsAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<string?[]>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(index => reader.IsDBNull(index) ? null : Convert.ToString(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture))]);
        }

        return rows;
    }

    public ValueTask DisposeAsync() => services.DisposeAsync();
}
