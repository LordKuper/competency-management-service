namespace Competency.CareerFramework;

/// <summary>
/// One thing that keeps a matrix version from being handed on, with the level and competency it is about, so a screen can point to them.
/// </summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Message">The problem in words, as the transitions refuse with it.</param>
/// <param name="LevelId">The level concerned; null when the problem is about none.</param>
/// <param name="CompetencyId">The competency concerned; null when the problem is about none.</param>
internal sealed record MatrixProblem(MatrixProblemCode Code, string Message, Guid? LevelId = null, Guid? CompetencyId = null);
