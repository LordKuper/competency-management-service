using Kalibr.OrgStructure;

namespace Kalibr.CareerFramework;

/// <summary>
/// The one rule for which published matrix applies in a unit: its own for a track or a position, otherwise that of the nearest unit above
/// it. A matrix in work never blocks the inheritance, and a unit's own published matrix replaces the inherited one for the units below it.
/// </summary>
internal static class MatrixInheritance
{
    /// <summary>
    /// The nearest unit of a chain that has a published matrix of the kind in question.
    /// </summary>
    /// <param name="ancestry">A unit and the units above it, nearest first.</param>
    /// <param name="hasMatrix">Whether a unit has its own published matrix of the kind.</param>
    /// <returns>The unit, or <see langword="null"/> when none of the chain has one.</returns>
    public static OrgUnitStatus? Nearest(IReadOnlyList<OrgUnitStatus> ancestry, Func<Guid, bool> hasMatrix) =>
        ancestry.FirstOrDefault(unit => hasMatrix(unit.Id));

    /// <summary>
    /// The published versions in effect in a unit: for each track and each position, the version of the nearest unit of the chain.
    /// </summary>
    /// <param name="ancestry">The unit and the units above it, nearest first.</param>
    /// <param name="published">Published versions with their matrices, of any units.</param>
    /// <returns>At most one version per track and per position.</returns>
    public static IEnumerable<MatrixVersion> InEffect(IReadOnlyList<OrgUnitStatus> ancestry, IEnumerable<MatrixVersion> published)
    {
        var chainIds = ancestry.Select(unit => unit.Id).ToHashSet();
        foreach (var group in published.Where(version => chainIds.Contains(version.Matrix.OrgUnitId)).GroupBy(version => (version.Matrix.TrackId, version.Matrix.PositionId)))
        {
            var byUnit = group.ToDictionary(version => version.Matrix.OrgUnitId);
            if (Nearest(ancestry, byUnit.ContainsKey) is { } owner)
            {
                yield return byUnit[owner.Id];
            }
        }
    }

    /// <summary>
    /// The units where a matrix is in effect: its own unit and every unit below it down to, not including, those that have a published
    /// matrix of their own of the same track or position.
    /// </summary>
    /// <param name="snapshot">The organization snapshot.</param>
    /// <param name="ownerUnitId">The unit of the matrix.</param>
    /// <param name="hasMatrix">Whether a unit has its own published matrix of the same track or position.</param>
    /// <returns>The units, the matrix unit first.</returns>
    public static IEnumerable<OrgUnitStatus> UnitsInEffect(OrgSnapshot snapshot, Guid ownerUnitId, Func<Guid, bool> hasMatrix) =>
        snapshot.Subtree(ownerUnitId).Where(unit => Nearest(snapshot.Ancestry(unit.Id), hasMatrix)?.Id == ownerUnitId);
}
