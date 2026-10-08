using Npgsql;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// The row lock of one account, or of the set of active administrators, held in a transaction on a connection of its own, so that every
/// request that locks or changes those rows stops until the lock is released. Holding it lets a test line requests up on the lock in an order
/// it chooses, and release them together, which is the window in which requests that do not wait for each other would act on the same stale
/// copy of the rows. Run such a test on a host of its own: the waiters are counted over the whole database.
/// </summary>
public sealed class RowLock : IAsyncDisposable
{
    private const string WaitingForALock = "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock'";
    private const string ActiveAdministrators = $"SELECT id FROM users WHERE role = '{Scenarios.GlobalAdmin}' AND NOT is_blocked AND password_hash IS NOT NULL ORDER BY id FOR UPDATE";

    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;

    private RowLock(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        this.connection = connection;
        this.transaction = transaction;
    }

    public static Task<RowLock> HoldAsync(ApiHost host, Guid userId) => HoldAsync(host, $"SELECT id FROM users WHERE id = '{userId}' FOR UPDATE");

    /// <summary>
    /// Holds the rows of every active administrator, the lock that guards the rule that one always exists.
    /// </summary>
    /// <param name="host">The host whose database is locked.</param>
    public static Task<RowLock> HoldActiveAdministratorsAsync(ApiHost host) => HoldAsync(host, ActiveAdministrators);

    private static async Task<RowLock> HoldAsync(ApiHost host, string lockQuery)
    {
        var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(host.ConnectionString) { Pooling = false }.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(lockQuery, connection, transaction);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        return new RowLock(connection, transaction);
    }

    /// <summary>
    /// Waits until the given number of requests wait for a lock in the host's database.
    /// </summary>
    /// <param name="host">The host whose database is watched.</param>
    /// <param name="requests">How many requests must wait.</param>
    public static Task UntilWaitingAsync(ApiHost host, int requests) =>
        Waiting.UntilAsync(async () => await host.ScalarAsync<long>(WaitingForALock) >= requests, $"{requests} request(s) wait for a lock");

    public Task ReleaseAsync() => transaction.CommitAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }
}
