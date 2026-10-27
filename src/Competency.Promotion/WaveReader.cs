using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Reads waves for those who manage them: who sees and changes which wave, and how a wave is described. A global administrator sees every wave,
/// the author of a wave sees and changes it, and the responsible HR of a unit of a wave sees it. Scoped to a request, because it shares the
/// request's database context.
/// </summary>
internal sealed class WaveReader(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    PromotionCalendar calendar,
    ICurrentActor actor)
{
    /// <summary>
    /// Whether the signed-in user changes the wave: a global administrator or its author.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <returns><see langword="true"/> when the user may change it.</returns>
    public bool CanEdit(PromotionWave wave) => actor.IsGlobalAdmin() || (actor.UserId is not null && wave.AuthorId == actor.UserId);

    /// <summary>
    /// Whether the signed-in user sees the wave: those who change it, and the responsible HR of any of its units.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns><see langword="true"/> when the user may read it.</returns>
    public async Task<bool> CanSeeAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        if (CanEdit(wave))
        {
            return true;
        }

        var answerable = await AnswerableUnitIdsAsync(cancellationToken);
        return answerable.Length > 0
            && await context.Set<PromotionWaveUnit>().AnyAsync(member => member.WaveId == wave.Id && answerable.Contains(member.OrgUnitId), cancellationToken);
    }

    /// <summary>
    /// Narrows a query of waves to those the signed-in user sees.
    /// </summary>
    /// <param name="waves">The waves.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The query of the visible waves.</returns>
    public async Task<IQueryable<PromotionWave>> VisibleAsync(IQueryable<PromotionWave> waves, CancellationToken cancellationToken)
    {
        if (actor.IsGlobalAdmin() || actor.UserId is not { } userId)
        {
            return waves;
        }

        var answerable = await AnswerableUnitIdsAsync(cancellationToken);
        return waves.Where(wave => wave.AuthorId == userId
            || context.Set<PromotionWaveUnit>().Any(member => member.WaveId == wave.Id && answerable.Contains(member.OrgUnitId)));
    }

    /// <summary>
    /// Describes waves with their units, their authors and the stage each is in now.
    /// </summary>
    /// <param name="waves">The waves.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>One description per wave, in the order given.</returns>
    public async Task<IReadOnlyList<WaveResponse>> DescribeAsync(IReadOnlyCollection<PromotionWave> waves, CancellationToken cancellationToken)
    {
        var waveIds = waves.Select(wave => wave.Id).ToArray();
        var members = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => waveIds.Contains(member.WaveId)).ToListAsync(cancellationToken);
        var units = await employees.FindUnitsAsync([.. members.Select(member => member.OrgUnitId).Distinct()], cancellationToken);
        var authors = await AccountNamesAsync([.. waves.Select(wave => wave.AuthorId).Distinct()], cancellationToken);
        return [.. waves.Select(wave => new WaveResponse(
            wave.Id,
            wave.StartsOn,
            wave.EndsOn,
            calendar.StageOf(wave),
            wave.IsFinished,
            wave.AuthorId,
            authors.GetValueOrDefault(wave.AuthorId, string.Empty),
            [.. members
                .Where(member => member.WaveId == wave.Id)
                .Select(member => units[member.OrgUnitId])
                .OrderBy(unit => unit.Name, TextCollation.Comparer)
                .ThenBy(unit => unit.Id)
                .Select(unit => new WaveUnitResponse(unit.Id, unit.Name))]))];
    }

    private async Task<Guid[]> AnswerableUnitIdsAsync(CancellationToken cancellationToken) =>
        actor.UserId is { } userId ? [.. (await career.ResponsibleUnitsAsync(userId, cancellationToken)).Select(unit => unit.Id)] : [];

    /// <summary>
    /// Finds how people know accounts: the name of the employee an account is bound to, or else its e-mail address.
    /// </summary>
    /// <param name="userIds">The accounts.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The name by account; accounts that do not exist are absent.</returns>
    public async Task<Dictionary<Guid, string>> AccountNamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var accounts = await users.FindManyAsync(userIds, cancellationToken);
        var people = await employees.FindManyAsync([.. accounts.Values.Select(account => account.EmployeeId).OfType<Guid>()], cancellationToken);
        return accounts.ToDictionary(
            account => account.Key,
            account => account.Value.EmployeeId is { } employeeId && people.TryGetValue(employeeId, out var person) ? person.FullName : account.Value.Email);
    }
}
