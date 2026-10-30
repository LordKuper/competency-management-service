using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// What an application aims at, with the version it is assessed by.
/// </summary>
/// <param name="Target">The unit, track and level as the API shows them.</param>
/// <param name="Version">The version the wave bound, with its frozen composition.</param>
/// <param name="Level">The level of the version.</param>
internal sealed record TargetFacts(ApplicationTargetResponse Target, CareerVersion Version, CareerVersionLevel Level);
