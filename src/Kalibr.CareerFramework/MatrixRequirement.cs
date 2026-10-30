using System.Text.Json.Serialization;

namespace Kalibr.CareerFramework;

/// <summary>
/// What a level requires of a competency: a required score, a point of the competency's own scale; for a competency critical in the
/// version it is also the threshold. The score must be sent, since a missing number would read as 0, a point of most scales.
/// </summary>
internal sealed record MatrixRequirement(Guid LevelId, Guid CompetencyId, [property: JsonRequired] int RequiredScore);
