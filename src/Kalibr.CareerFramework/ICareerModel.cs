using Competency.OrgStructure;

namespace Competency.CareerFramework;

/// <summary>
/// Access to the career model for other modules, which never see the module's entities. Implementations are scoped to a request,
/// because they share its database context. A caller that acts on the process roles holds the tree lock first, as the roles themselves require.
/// </summary>
public interface ICareerModel
{
    /// <summary>
    /// Finds the published track matrices in effect in units: a unit's own for a track, otherwise the nearest unit above it that has one.
    /// A matrix in effect in several of the units is returned for each of them.
    /// </summary>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The matrices by track name and then matrix, for each given unit that has one in effect, in the unit's own name; empty when no unit has one in effect.</returns>
    Task<IReadOnlyList<PublishedTrackMatrix>> PublishedTrackMatricesAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a published or archived track matrix version with its frozen composition.
    /// </summary>
    /// <param name="versionId">The version identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The version, or <see langword="null"/> when it does not exist, is still in work or is a position matrix version.</returns>
    Task<CareerVersion?> VersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the rating of a candidate for a level of a version. A competency without a score counts as a share of 0 and does not meet
    /// its requirement.
    /// </summary>
    /// <param name="version">The version the candidate is assessed by.</param>
    /// <param name="levelId">The level of the version.</param>
    /// <param name="scores">The score by competency, in points of the competency's scale.</param>
    /// <returns>The rating, the threshold, the readiness and the standing of every competency.</returns>
    CareerScore Score(CareerVersion version, Guid levelId, IReadOnlyDictionary<Guid, decimal> scores);

    /// <summary>
    /// Finds the levels a holder of a position may apply for in each of the given track matrices. On the track where the position is linked
    /// to an active level, only the next level of the matrix by order is allowed, and none when the position is at the top; on every other
    /// track every level of the matrix is allowed, so a position linked to no level allows every level of every matrix.
    /// </summary>
    /// <param name="positionId">The position of the candidate.</param>
    /// <param name="matrices">The matrices, as <see cref="PublishedTrackMatricesAsync"/> returned them.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The identifiers of the allowed levels in the matrix's order, by matrix; every given matrix has an entry.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> AllowedTargetLevelsAsync(
        Guid positionId,
        IReadOnlyCollection<PublishedTrackMatrix> matrices,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the units whose responsible HR an account is now.
    /// </summary>
    /// <param name="userId">The account identifier.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The units in name order; empty when the account cannot act as an HR.</returns>
    Task<IReadOnlyList<OrgUnitStatus>> ResponsibleUnitsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the responsible HR of units.
    /// </summary>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The HR by unit; units without a responsible HR who can act are absent.</returns>
    Task<IReadOnlyDictionary<Guid, ResponsibleHr>> ResponsibleHrAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken = default);
}
