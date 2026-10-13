using Competency.Platform;
using Microsoft.Extensions.Logging;

namespace Competency.Desk;

/// <summary>
/// Works out the signed-in user's desk from the current data of every module on each call; the desk keeps nothing between requests,
/// so the user's own action changes it at once.
/// </summary>
internal sealed class DeskBoard(IEnumerable<IDeskSource> sources, ICurrentActor actor, TimeProvider time, ILogger<DeskBoard> logger)
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Asks every source in turn, since they share the request's database context, and merges their findings.
    /// A slow run is logged with the role and the time each source took, never with whose desk it was.
    /// </summary>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <returns>The desk items in desk order.</returns>
    public async Task<List<DeskItemResponse>> ItemsAsync(CancellationToken cancellationToken)
    {
        var started = time.GetTimestamp();
        var findings = new List<DeskFinding>();
        var timings = new List<string>();
        foreach (var source in sources)
        {
            var sourceStarted = time.GetTimestamp();
            findings.AddRange(await source.FindAsync(cancellationToken));
            timings.Add($"{source.GetType().Name} {time.GetElapsedTime(sourceStarted).TotalMilliseconds:F0} ms");
        }

        var items = DeskRules.Merge(findings);
        var elapsed = time.GetElapsedTime(started);
        if (elapsed > SlowThreshold)
        {
            logger.LogWarning(
                "The desk took {ElapsedMs:F0} ms for the role {Role}; sources: {SourceTimings}.",
                elapsed.TotalMilliseconds,
                actor.Role,
                string.Join(", ", timings));
        }

        return items;
    }
}
