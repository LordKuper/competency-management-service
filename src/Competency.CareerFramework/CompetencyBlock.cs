using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// A shared block of competencies, of any scales, which matrix versions include by reference and give requirements and coefficients of
/// their own. The composition is stored on the block itself, so changing it changes the block's version and is journaled with it, and a
/// version in work always sees the block as it stands now. Global administrators and every HR keep every block; the unit it applies to,
/// if any, decides which matrices may include it, who sees it as a draft and which unit heads may propose and edit it.
/// </summary>
[Audited]
internal sealed class CompetencyBlock : CatalogEntry
{
    /// <summary>
    /// The unit the block applies to, whose matrices and those of the units below it may include it; null for a block that applies to all.
    /// </summary>
    [Audited]
    public Guid? ScopeUnitId { get; set; }

    /// <summary>
    /// The competencies of the block, each once, each on its own scale.
    /// </summary>
    [Audited]
    public List<Guid> CompetencyIds { get; set; } = [];

    /// <inheritdoc />
    public override Guid? ListedUnitId => ScopeUnitId;
}
