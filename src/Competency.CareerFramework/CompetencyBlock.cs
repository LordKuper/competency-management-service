using Competency.Platform;

namespace Competency.CareerFramework;

/// <summary>
/// A shared block of competencies of one scale, which matrix versions include by reference and give requirements and coefficients of
/// their own. The composition is stored on the block itself, so changing it changes the block's version and is journaled with it, and a
/// version in work always sees the block as it stands now.
/// </summary>
[Audited]
internal sealed class CompetencyBlock : CatalogEntry
{
    [Audited]
    public Guid ScaleId { get; set; }

    /// <summary>
    /// The competencies of the block, each once, all on the block's scale.
    /// </summary>
    [Audited]
    public List<Guid> CompetencyIds { get; set; } = [];
}
