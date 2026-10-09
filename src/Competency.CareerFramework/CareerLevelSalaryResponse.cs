namespace Competency.CareerFramework;

/// <summary>
/// The salary range of a career level, returned only to the level's keepers. The version is the level's, and is sent back in <c>If-Match</c>.
/// </summary>
internal sealed record CareerLevelSalaryResponse(decimal? SalaryFrom, decimal? SalaryTo, int Version)
{
    public static CareerLevelSalaryResponse From(CareerLevel level) => new(level.SalaryFrom, level.SalaryTo, level.Version);
}
