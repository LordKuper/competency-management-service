using System.Globalization;
using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Checks the units a wave is given: they exist and work, the actor answers for them, no unfinished wave has them already, and the commission of
/// each has a chair. A caller holds the exclusive promotion lock, so the answer holds until it commits. Scoped to a request, because it shares
/// the request's database context.
/// </summary>
internal sealed class WaveUnits(
    AppDbContext context,
    IEmployeeDirectory employees,
    ICareerModel career,
    CommissionReader commissions,
    ICurrentActor actor)
{
    private const string DateFormat = "dd.MM.yyyy";

    /// <summary>
    /// Says why units cannot be added to a wave.
    /// </summary>
    /// <param name="unitIds">The units to add.</param>
    /// <param name="waveId">The wave they are added to; null for a wave being announced.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One line per problem, addressed to the user and naming the unit; empty when all can be added.</returns>
    public async Task<List<string>> ProblemsAsync(IReadOnlyCollection<Guid> unitIds, Guid? waveId, CancellationToken cancellationToken)
    {
        var units = await employees.FindUnitsAsync(unitIds, cancellationToken);
        var answerable = actor.IsGlobalAdmin() || actor.UserId is null
            ? null
            : (await career.ResponsibleUnitsAsync(actor.UserId.Value, cancellationToken)).Select(unit => unit.Id).ToHashSet();

        List<string> problems = [];
        foreach (var unitId in unitIds)
        {
            if (!units.TryGetValue(unitId, out var unit))
            {
                problems.Add("Подразделение не найдено");
            }
            else if (!unit.IsActive)
            {
                problems.Add($"Подразделение «{unit.Name}» не действует");
            }
            else if (answerable is not null && !answerable.Contains(unitId))
            {
                problems.Add($"Подразделение «{unit.Name}» вне вашей ответственности");
            }
        }

        var working = units.Values.Where(unit => unit.IsActive).ToDictionary(unit => unit.Id);
        problems.AddRange(await OverlapProblemsAsync(working, waveId, cancellationToken));
        problems.AddRange((await commissions.ReadAsync([.. working.Keys], cancellationToken)).Values
            .Where(roster => roster.ChairEmployeeId is null)
            .Select(roster => $"У комиссии подразделения «{roster.UnitName}» нет председателя"));
        return problems;
    }

    private async Task<List<string>> OverlapProblemsAsync(IReadOnlyDictionary<Guid, OrgUnitStatus> units, Guid? waveId, CancellationToken cancellationToken)
    {
        var unitIds = units.Keys.ToArray();
        var overlaps = await (
            from member in context.Set<PromotionWaveUnit>().AsNoTracking()
            join wave in context.Set<PromotionWave>().AsNoTracking() on member.WaveId equals wave.Id
            where unitIds.Contains(member.OrgUnitId) && wave.ProcessedStage < PromotionWave.StageCount && wave.Id != waveId
            select new { member.OrgUnitId, wave.StartsOn, wave.EndsOn })
            .ToListAsync(cancellationToken);
        return [.. overlaps.Select(overlap =>
            $"Подразделение «{units[overlap.OrgUnitId].Name}» уже входит в незавершённую волну "
            + $"{overlap.StartsOn.ToString(DateFormat, CultureInfo.InvariantCulture)} — {overlap.EndsOn[^1].ToString(DateFormat, CultureInfo.InvariantCulture)}")];
    }
}
