namespace Kalibr.Platform;

/// <summary>
/// One module's contribution to the signed-in user's desk: what the module's data expects of the user, or tells them for reference,
/// and which desk scenarios its data already allows.
/// The desk merges the findings of every source, so a module states facts about its own data and never knows about the others.
/// Implementations are scoped to a request and limit what they report to what the user's roles may see, by the module's own rights.
/// </summary>
public interface IDeskSource
{
    /// <summary>
    /// Works out the findings for the user of <see cref="ICurrentActor"/> from the current data, with the scenario prerequisites the same
    /// data shows, so that knowing them costs no extra read.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The source's findings and the prerequisites it knows to hold.</returns>
    Task<DeskReport> FindAsync(CancellationToken cancellationToken);
}
