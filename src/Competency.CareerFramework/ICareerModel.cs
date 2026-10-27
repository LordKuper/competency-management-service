using Competency.OrgStructure;

namespace Competency.CareerFramework;

/// <summary>
/// Access to the career model for other modules, which never see the module's entities. Implementations are scoped to a request,
/// because they share its database context. A caller that acts on the process roles holds the tree lock first, as the roles themselves require.
/// </summary>
public interface ICareerModel
{
    /// <summary>
    /// Finds the published track matrices of units.
    /// </summary>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The matrices by track name and then matrix; empty when no unit has a published track matrix.</returns>
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
