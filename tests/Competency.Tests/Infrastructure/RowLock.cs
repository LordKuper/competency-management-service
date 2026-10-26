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
    private const string ActiveAdministrators =
        "SELECT u.id FROM users u WHERE NOT u.is_blocked AND u.password_hash IS NOT NULL AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'GlobalAdmin') ORDER BY u.id FOR UPDATE OF u";

    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;

    private RowLock(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        this.connection = connection;
        this.transaction = transaction;
    }

    public const long TreeKey = 5_001_001;
    public const long CatalogueKey = 5_003_001;

    public static Task<RowLock> HoldAsync(ApiHost host, Guid userId) => HoldAsync(host, $"SELECT id FROM users WHERE id = '{userId}' FOR UPDATE");

    /// <summary>
    /// Holds an advisory lock the application takes with <c>pg_advisory_xact_lock</c>, exclusively or shared, as the writer or the reader of
    /// what the lock guards would hold it, so a test can say which requests wait for it and which pass.
    /// </summary>
    /// <param name="host">The host whose database is locked.</param>
    /// <param name="key">The lock key.</param>
    /// <param name="shared">Whether to hold it in shared mode.</param>
    public static Task<RowLock> HoldAdvisoryAsync(ApiHost host, long key, bool shared = false) =>
        HoldAsync(host, shared ? $"SELECT pg_advisory_xact_lock_shared({key})" : $"SELECT pg_advisory_xact_lock({key})");

    /// <summary>
    /// Holds the row of a matrix, which every change of its versions locks.
    /// </summary>
    /// <param name="host">The host whose database is locked.</param>
    /// <param name="matrixId">The matrix.</param>
    public static Task<RowLock> HoldMatrixAsync(ApiHost host, Guid matrixId) =>
        HoldAsync(host, $"SELECT id FROM competency_matrices WHERE id = '{matrixId}' FOR UPDATE");

    /// <summary>
    /// Sends a request that must stop at a lock another connection holds, and returns it once it is waiting.
    /// </summary>
    /// <param name="host">The host whose database is watched.</param>
    /// <param name="waiters">How many requests, this one included, wait for a lock.</param>
    /// <param name="send">Sends the request.</param>
    /// <returns>The pending request.</returns>
    public static async Task<Task<ApiResponse>> StartWaitingAsync(ApiHost host, int waiters, Func<Task<ApiResponse>> send)
    {
        var request = send();
        await Waiting.UntilAsync(
            async () => request.IsCompleted || await host.ScalarAsync<long>(WaitingForALock) >= waiters,
            $"{waiters} request(s) wait for a lock or the request is answered");
        if (request.IsCompleted)
        {
            throw new Xunit.Sdk.XunitException($"The request did not wait for the lock, it was answered: {await request}");
        }

        return request;
    }

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
