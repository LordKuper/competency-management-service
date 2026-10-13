namespace Competency.Platform;

/// <summary>
/// One module's contribution to the signed-in user's desk: what the module's data expects of the user, or tells them for reference.
/// The desk merges the findings of every source, so a module states facts about its own data and never knows about the others.
/// Implementations are scoped to a request and limit what they report to what the user's roles may see, by the module's own rights.
/// </summary>
public interface IDeskSource
{
    /// <summary>
    /// Works out the findings for the user of <see cref="ICurrentActor"/> from the current data.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The findings, at most one of each kind per object and subject; empty when the user's roles give the source nothing to report.</returns>
    Task<IReadOnlyList<DeskFinding>> FindAsync(CancellationToken cancellationToken);
}
