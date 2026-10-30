using Kalibr.OrgStructure;

namespace Kalibr.CareerFramework;

/// <summary>
/// A career level of a matrix version with what the version requires of a candidate for it overall.
/// </summary>
/// <param name="Id">The level identifier.</param>
/// <param name="Name">The level name as the version froze it.</param>
/// <param name="Order">The place in the track, from 0, the most junior level first.</param>
/// <param name="MinOverallPercent">The rating, in percent, a candidate must reach; null when the version sets no threshold.</param>
/// <param name="RequiredEducation">The lowest education the level requires; null when it requires none.</param>
public sealed record CareerVersionLevel(Guid Id, string Name, int Order, decimal? MinOverallPercent, EducationLevel? RequiredEducation);
