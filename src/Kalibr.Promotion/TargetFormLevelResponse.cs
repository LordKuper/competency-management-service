using Kalibr.CareerFramework;
using Kalibr.OrgStructure;

namespace Kalibr.Promotion;

/// <summary>
/// A level of the target form.
/// </summary>
/// <param name="Id">The level.</param>
/// <param name="Name">The level name.</param>
/// <param name="RequiredEducation">The lowest education the level requires; null when it requires none.</param>
internal sealed record TargetFormLevelResponse(Guid Id, string Name, EducationLevel? RequiredEducation);
