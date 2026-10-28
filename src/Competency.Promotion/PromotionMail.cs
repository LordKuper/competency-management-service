using System.Diagnostics;
using System.Globalization;
using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Competency.Promotion;

/// <summary>
/// The promotion letters: who is now expected to act when a wave is announced or a stage starts, and the letter that tells them, with a link built
/// from the configured public address. A rater on the exclusion list is not invited to the system. Call it only after the change has committed and released its locks: it reads the state after the change and
/// queues the letters, so the change never waits on the mail server. The letters name no rater and carry no rating.
/// </summary>
internal sealed class PromotionMail(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    CommissionReader commissions,
    RaterExclusions exclusions,
    ICurrentActor actor,
    IOptions<AppOptions> app,
    PromotionMailQueue queue,
    ILogger<PromotionMail> logger)
{
    private const string AnnouncedKind = "PromotionWaveAnnounced";
    private const string OpenedKind = "PromotionApplicationsOpened";
    private const string FormalStartedKind = "PromotionFormalCheckStarted";
    private const string FormalPassedKind = "PromotionFormalCheckPassed";
    private const string FormalRejectedKind = "PromotionFormalCheckRejected";
    private const string RatingStartedKind = "PromotionRatingStarted";
    private const string RatingReminderKind = "PromotionRatingReminder";
    private const string ReviewArrivedKind = "PromotionReviewArrived";
    private const string ResultsKind = "PromotionResultsPublished";
    private const string NoDecisionKind = "PromotionNoDecision";
    private const string FeedbackMissingKind = "PromotionFeedbackMissing";
    private const string Subject = "Карьерные переходы в системе «Калибр»";
    private const string DateFormat = "dd.MM.yyyy";
    private const string SignInLine = "Если вы ещё не вошли в систему, сначала откроется страница входа, а после входа —";
    private const string AutomaticLine = "Это автоматическое письмо, отвечать на него не нужно.";
    private const string ApplicationTarget = "эта заявка";
    private const string WaveTarget = "эта карьерная сессия";
    private const string CommissionTarget = "страница комиссии";

    private static readonly ApplicationStatus[] ResultStatuses = [ApplicationStatus.Decided, ApplicationStatus.NoDecision];

    /// <summary>
    /// Tells the working employees of the units of a wave that it is announced and when applications are taken.
    /// </summary>
    /// <param name="wave">The wave as committed.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task WaveAnnouncedAsync(PromotionWave wave, CancellationToken cancellationToken) =>
        Enqueue(
            await EmployeeAccountsOfUnitsAsync(wave, cancellationToken),
            AnnouncedKind,
            Compose(
                $"объявлена карьерная сессия. Заявки на карьерный переход принимаются с {Day(wave.StartsOn)} по {Day(wave.EndsOn[0])}.",
                "Посмотреть карьерную сессию:",
                Link("promotion"),
                "раздел «Карьерные переходы»"));

    /// <summary>
    /// Tells the working employees of the units of a wave that applications are open.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ApplicationsOpenedAsync(PromotionWave wave, CancellationToken cancellationToken) =>
        Enqueue(
            await EmployeeAccountsOfUnitsAsync(wave, cancellationToken),
            OpenedKind,
            Compose(
                $"начался приём заявок на карьерный переход, подать заявку можно до {Day(wave.EndsOn[0])}.",
                "Подать заявку:",
                Link($"promotion/apply/{wave.Id}"),
                "форма заявки"));

    /// <summary>
    /// Tells each responsible HR how many filed applications of their units wait for the formal check, which the dates they enter settle.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task FormalCheckStartedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var counts = await UnitCountsAsync(wave.Id, ApplicationStatus.Submitted, cancellationToken);
        var names = await employees.FindUnitsAsync([.. counts.Keys], cancellationToken);
        foreach (var (hrId, unitIds) in await HrOfUnitsAsync([.. counts.Keys], cancellationToken))
        {
            var lines = unitIds.OrderBy(unitId => names[unitId].Name, TextCollation.Comparer).Select(unitId => $"— «{names[unitId].Name}»: {counts[unitId]}");
            Enqueue(
                [hrId],
                FormalStartedKind,
                Compose(
                    $"начался этап «Формальная проверка». Заявки ждут проверки; если ещё не внесли дату приёма на работу или последнего перехода, внесите её. Заявки по подразделениям:\n{string.Join('\n', lines)}",
                    "Открыть карьерную сессию:",
                    Link($"promotion/waves/{wave.Id}"),
                    WaveTarget));
        }
    }

    /// <summary>
    /// Tells each candidate the result of the formal check, and invites the raters of the admitted applications to rate.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task FormalCheckFinishedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        await TellCandidatesAsync(wave, [ApplicationStatus.Admitted], FormalPassedKind, "ваша заявка на карьерный переход прошла формальную проверку.", cancellationToken);
        var rejected = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.FormalRejected)
            .ToListAsync(cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync([.. rejected.Select(application => application.EmployeeId)], cancellationToken);
        foreach (var application in rejected.Where(application => accounts.ContainsKey(application.EmployeeId)))
        {
            Enqueue(
                [accounts[application.EmployeeId].Id],
                FormalRejectedKind,
                Compose(
                    $"ваша заявка на карьерный переход отклонена по формальным признакам: {string.Join("; ", application.FormalFailures.Select(FailureText))}.",
                    "Открыть заявку:",
                    Link($"promotion/applications/{application.Id}"),
                    ApplicationTarget));
        }

        await InviteAndAskRatersAsync(wave, cancellationToken);
    }

    /// <summary>
    /// Invites the raters of the admitted applications who have no account, at the address a head or the responsible HR confirmed, and then sends each
    /// rater who has an account one letter naming every candidate they are to rate. A rater with no account and no confirmed address gets nothing.
    /// </summary>
    private async Task InviteAndAskRatersAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var admitted = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.Admitted)
            .Select(application => new { application.Id, application.EmployeeId })
            .ToListAsync(cancellationToken);
        var admittedIds = admitted.Select(application => application.Id).ToArray();
        var raters = await context.Set<PromotionRater>().AsNoTracking().Where(rater => admittedIds.Contains(rater.ApplicationId)).ToListAsync(cancellationToken);
        var raterIds = raters.Select(rater => rater.EmployeeId).Distinct().ToList();
        var withAccount = await users.FindByEmployeeIdsAsync(raterIds, cancellationToken);
        var people = await employees.FindManyAsync([.. admitted.Select(application => application.EmployeeId), .. raterIds], cancellationToken);
        var excluded = await exclusions.AmongAsync(raterIds.Where(people.ContainsKey).Select(id => people[id]), cancellationToken);
        foreach (var raterId in raterIds.Where(id => !withAccount.ContainsKey(id)))
        {
            var email = raters.Where(rater => rater.EmployeeId == raterId && rater.IsEmailConfirmed).OrderBy(rater => rater.EmailConfirmedAt).Select(rater => rater.Email).FirstOrDefault();
            if (excluded.Contains(raterId))
            {
                logger.LogWarning("The rater {RaterId} of wave {WaveId} is on the exclusion list, so nobody invites them.", raterId, wave.Id);
            }
            else if (email is null)
            {
                logger.LogWarning("The rater {RaterId} of wave {WaveId} has no account and no confirmed address, so nobody invites them.", raterId, wave.Id);
            }
            else if (await users.InviteEmployeeAsync(raterId, email, cancellationToken) == EmployeeInvitation.Refused)
            {
                logger.LogWarning("The rater {RaterId} of wave {WaveId} was not invited at the confirmed address.", raterId, wave.Id);
            }
        }

        var accounts = await users.FindByEmployeeIdsAsync(raterIds, cancellationToken);
        var candidateOf = admitted.ToDictionary(application => application.Id, application => application.EmployeeId);
        foreach (var raterId in raterIds.Where(accounts.ContainsKey))
        {
            var names = raters
                .Where(rater => rater.EmployeeId == raterId)
                .Select(rater => people[candidateOf[rater.ApplicationId]].FullName)
                .Order(TextCollation.Comparer);
            Enqueue(
                [accounts[raterId].Id],
                RatingStartedKind,
                Compose(
                    $"вас просят оценить кандидатов на карьерный переход. Оценить можно до {Day(wave.EndsOn[PromotionStage.Rating - 1])}. Кандидаты:\n{string.Join('\n', names.Select(name => $"— {name}"))}",
                    "Открыть список кандидатов:",
                    Link("promotion/ratings"),
                    "список кандидатов"),
                toInvited: true);
        }
    }

    /// <summary>
    /// Reminds each rater who has not yet answered about a candidate that the rating stage is about to end.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task RatingReminderAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var admittedIds = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.Admitted)
            .Select(application => application.Id)
            .ToListAsync(cancellationToken);
        var asked = await context.Set<PromotionRater>()
            .AsNoTracking()
            .Where(rater => admittedIds.Contains(rater.ApplicationId))
            .Select(rater => new { rater.ApplicationId, rater.EmployeeId })
            .ToListAsync(cancellationToken);
        var answered = (await context.Set<PromotionRaterScore>()
                .AsNoTracking()
                .Where(answer => admittedIds.Contains(answer.ApplicationId))
                .Select(answer => new { answer.ApplicationId, answer.EmployeeId })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var pending = asked.Where(pair => !answered.Contains(pair)).GroupBy(pair => pair.EmployeeId).ToDictionary(group => group.Key, group => group.Count());
        var accounts = await users.FindByEmployeeIdsAsync([.. pending.Keys], cancellationToken);
        foreach (var (employeeId, count) in pending.Where(pair => accounts.ContainsKey(pair.Key)))
        {
            Enqueue(
                [accounts[employeeId].Id],
                RatingReminderKind,
                Compose(
                    $"до конца этапа «Круговая оценка» ({Day(wave.EndsOn[2])}) осталось меньше двух суток, а вы ещё не оценили кандидатов: {count}.",
                    "Открыть список кандидатов:",
                    Link("promotion/ratings"),
                    "список кандидатов"));
        }
    }

    /// <summary>
    /// Tells the members of each commission that applications have reached it, in one letter for all that arrived since the last.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="arrivals">The number of applications that arrived, by the unit whose commission reviews them.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ReviewArrivedAsync(PromotionWave wave, IReadOnlyDictionary<Guid, int> arrivals, CancellationToken cancellationToken)
    {
        var rosters = await commissions.ReadAsync([.. arrivals.Keys], cancellationToken);
        foreach (var roster in rosters.Values)
        {
            Enqueue(
                await AccountsAsync(roster.Members.Where(member => member.IsActing).Select(member => member.EmployeeId), cancellationToken),
                ReviewArrivedKind,
                Compose(
                    $"на рассмотрение комиссии поступили заявки на карьерный переход по подразделению «{roster.UnitName}»: {arrivals[roster.UnitId]}.",
                    "Открыть страницу комиссии:",
                    Link($"promotion/commission/{wave.Id}/{roster.UnitId}"),
                    CommissionTarget));
        }
    }

    /// <summary>
    /// Publishes the results as the commission stage ends: asks each candidate whose application reached a result to open it and mark the feedback once they
    /// have received it, and tells each responsible HR which applications of their units were left without a decision.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ResultsPublishedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        await TellCandidatesAsync(wave, ResultStatuses, ResultsKind, "результаты рассмотрения вашей заявки на карьерный переход опубликованы. Когда получите обратную связь по заявке, отметьте это в системе.", cancellationToken);
        var undecided = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.NoDecision)
            .ToListAsync(cancellationToken);
        var namesOfCandidates = await NamesByUnitAsync(undecided, cancellationToken);
        var units = await employees.FindUnitsAsync([.. namesOfCandidates.Keys], cancellationToken);
        foreach (var (hrId, unitIds) in await HrOfUnitsAsync([.. namesOfCandidates.Keys], cancellationToken))
        {
            var lines = unitIds.OrderBy(unitId => units[unitId].Name, TextCollation.Comparer).Select(unitId => $"— «{units[unitId].Name}»: {string.Join(", ", namesOfCandidates[unitId])}");
            Enqueue(
                [hrId],
                NoDecisionKind,
                Compose(
                    $"этап «Рассмотрение комиссией» закончился, по заявкам не принято решение:\n{string.Join('\n', lines)}",
                    "Открыть карьерную сессию:",
                    Link($"promotion/waves/{wave.Id}"),
                    WaveTarget));
        }
    }

    /// <summary>
    /// Tells the responsible HR, the chair and the head of each unit which candidates did not mark that they received the feedback.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task FeedbackFinishedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var missing = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && ResultStatuses.Contains(application.Status) && application.FeedbackReceivedAt == null)
            .ToListAsync(cancellationToken);
        var namesOfCandidates = await NamesByUnitAsync(missing, cancellationToken);
        var rosters = await commissions.ReadAsync([.. namesOfCandidates.Keys], cancellationToken);
        foreach (var roster in rosters.Values)
        {
            var recipients = (await AccountsAsync(
                    new Guid?[] { roster.ChairEmployeeId, roster.Members.FirstOrDefault(member => member.IsHead)?.EmployeeId }.OfType<Guid>(),
                    cancellationToken))
                .Concat(roster.ResponsibleHrUserId is { } hrId ? [hrId] : []);
            Enqueue(
                recipients,
                FeedbackMissingKind,
                Compose(
                    $"этап «Обратная связь» закончился. Не отметили получение обратной связи по подразделению «{roster.UnitName}»: {string.Join(", ", namesOfCandidates[roster.UnitId])}.",
                    "Открыть страницу комиссии:",
                    Link($"promotion/commission/{wave.Id}/{roster.UnitId}"),
                    CommissionTarget));
        }
    }

    private static string FailureText(FormalFailure failure) => failure switch
    {
        FormalFailure.MissingDate => "не внесена дата приёма на работу или последнего перехода",
        FormalFailure.IntervalNotPassed => "не прошёл минимальный интервал между карьерными переходами",
        FormalFailure.EducationBelowRequired => "образование ниже требуемого для уровня",
        _ => throw new UnreachableException($"No letter names the failure {failure}."),
    };

    private static string Compose(string what, string openLine, string link, string target) => $"""
        Здравствуйте!

        Что ждёт вас: {what}

        {openLine}
        {link}

        {SignInLine} {target}.

        {AutomaticLine}
        """;

    private static string Day(DateOnly day) => day.ToString(DateFormat, CultureInfo.InvariantCulture);

    private string Link(string route) => $"{app.Value.PublicBaseUrl.AbsoluteUri.TrimEnd('/')}/{route}";

    private void Enqueue(IEnumerable<Guid> recipientIds, string kind, string body, bool toInvited = false)
    {
        var recipients = recipientIds.Distinct().ToList();
        if (recipients.Count > 0)
        {
            queue.Enqueue(new PromotionMailQueue.Letter(recipients, kind, Subject, body, actor.UserId?.ToString(), actor.Role, actor.RequestId, toInvited));
        }
    }

    private async Task<List<Guid>> AccountsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken) =>
        [.. (await users.FindByEmployeeIdsAsync([.. employeeIds.Distinct()], cancellationToken)).Values.Select(account => account.Id)];

    private async Task<List<Guid>> EmployeeAccountsOfUnitsAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var unitIds = await context.Set<PromotionWaveUnit>().AsNoTracking().Where(member => member.WaveId == wave.Id).Select(member => member.OrgUnitId).ToListAsync(cancellationToken);
        return await AccountsAsync(await employees.FindWorkingInUnitsAsync(unitIds, cancellationToken), cancellationToken);
    }

    private async Task TellCandidatesAsync(
        PromotionWave wave,
        IReadOnlyCollection<ApplicationStatus> statuses,
        string kind,
        string what,
        CancellationToken cancellationToken)
    {
        var applications = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && statuses.Contains(application.Status))
            .ToListAsync(cancellationToken);
        var accounts = await users.FindByEmployeeIdsAsync([.. applications.Select(application => application.EmployeeId)], cancellationToken);
        foreach (var application in applications.Where(application => accounts.ContainsKey(application.EmployeeId)))
        {
            Enqueue([accounts[application.EmployeeId].Id], kind, Compose(what, "Открыть заявку:", Link($"promotion/applications/{application.Id}"), ApplicationTarget));
        }
    }

    private async Task<Dictionary<Guid, int>> UnitCountsAsync(Guid waveId, ApplicationStatus status, CancellationToken cancellationToken) =>
        await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == waveId && application.Status == status)
            .GroupBy(application => application.OrgUnitId)
            .Select(group => new { UnitId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.UnitId, group => group.Count, cancellationToken);

    private async Task<Dictionary<Guid, List<string>>> NamesByUnitAsync(IReadOnlyCollection<PromotionApplication> applications, CancellationToken cancellationToken)
    {
        var people = await employees.FindManyAsync([.. applications.Select(application => application.EmployeeId).Distinct()], cancellationToken);
        return applications
            .GroupBy(application => application.OrgUnitId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(application => people[application.EmployeeId].FullName).Order(TextCollation.Comparer).ToList());
    }

    private async Task<IEnumerable<(Guid HrId, List<Guid> UnitIds)>> HrOfUnitsAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken) =>
        (await career.ResponsibleHrAsync(unitIds, cancellationToken))
            .GroupBy(pair => pair.Value.UserId, pair => pair.Key)
            .Select(group => (group.Key, group.ToList()));
}
