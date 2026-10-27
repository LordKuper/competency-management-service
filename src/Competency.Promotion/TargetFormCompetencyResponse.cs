using Competency.CareerFramework;

namespace Competency.Promotion;

/// <summary>
/// A competency of the target form.
/// </summary>
/// <param name="Id">The competency.</param>
/// <param name="Name">The competency name.</param>
/// <param name="ScalePoints">The points the candidate can give.</param>
internal sealed record TargetFormCompetencyResponse(Guid Id, string Name, IReadOnlyList<ScalePoint> ScalePoints);
