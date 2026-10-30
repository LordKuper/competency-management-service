using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// The target an application names, found in the wave: the bound matrix, the version it is assessed by and the level.
/// </summary>
/// <param name="Matrix">The matrix the wave bound for a unit.</param>
/// <param name="Version">The bound version with its frozen composition.</param>
/// <param name="Level">The level of the version.</param>
internal sealed record ResolvedTarget(PromotionWaveMatrix Matrix, CareerVersion Version, CareerVersionLevel Level);
