namespace Kalibr.CareerFramework;

/// <summary>
/// A shared block a version includes by reference, whose composition the block itself keeps. The types of the competencies of the
/// composition belong to the version, so two matrices with one block set them apart.
/// </summary>
internal sealed record MatrixSharedBlock(Guid BlockId, IReadOnlyList<MatrixCompetencyType>? CompetencyTypes = null)
{
    /// <summary>
    /// The types of the competencies of the composition in the version; a competency missing here is an additional one.
    /// </summary>
    public IReadOnlyList<MatrixCompetencyType> CompetencyTypes { get; init; } = CompetencyTypes ?? [];

    /// <summary>
    /// The type of a competency of the composition in the version.
    /// </summary>
    /// <param name="competencyId">The competency.</param>
    /// <returns>Its type, additional when none is set.</returns>
    public CompetencyType TypeOf(Guid competencyId) =>
        CompetencyTypes.FirstOrDefault(item => item.CompetencyId == competencyId)?.Type ?? CompetencyType.Additional;
}
