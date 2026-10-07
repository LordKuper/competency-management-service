using System.Diagnostics;
using Xunit;

namespace Competency.Tests.Infrastructure;

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
    public static async Task UntilAsync(Func<Task<bool>> condition, string what)
    {
        var waited = Stopwatch.StartNew();
        while (!await condition())
        {
            if (waited.Elapsed > Timeout)
            {
                throw new TimeoutException($"Gave up after {Timeout.TotalSeconds} s waiting until {what}.");
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }
    }
}
