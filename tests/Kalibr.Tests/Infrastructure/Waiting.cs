using System.Diagnostics;
using Xunit;

namespace Kalibr.Tests.Infrastructure;

/// <summary>
/// Waiting for a state of the database or the host without sleeping for a fixed time.
/// </summary>
public static class Waiting
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Polls the condition until it holds.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="what">What is waited for, for the message of the timeout.</param>
    /// <param name="timeout">How long to wait; a short default for a condition the host reaches at once.</param>
    public static async Task UntilAsync(Func<Task<bool>> condition, string what, TimeSpan? timeout = null)
    {
        var limit = timeout ?? Timeout;
        var waited = Stopwatch.StartNew();
        while (!await condition())
        {
            if (waited.Elapsed > limit)
            {
                throw new TimeoutException($"Gave up after {limit.TotalSeconds} s waiting until {what}.");
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }
    }
}
