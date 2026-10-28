using System.Diagnostics;
using System.Globalization;
using Competency.CareerFramework;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Competency.Promotion;

/// <summary>
/// The promotion letters: who is now expected to act when a wave is announced or a stage starts, and the letter that tells them, with a link built
/// from the configured public address. Call it only after the change has committed and released its locks: it reads the state after the change and
/// queues the letters, so the change never waits on the mail server. The letters name no rater and carry no rating.
/// </summary>
internal sealed class PromotionMail(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICareerModel career,
    CommissionReader commissions,
    ICurrentActor actor,
    IOptions<AppOptions> app,
    PromotionMailQueue queue)
{
    private const string AnnouncedKind = "PromotionWaveAnnounced";
    private const string OpenedKind = "PromotionApplicationsOpened";
    private const string FormalStartedKind = "PromotionFormalCheckStarted";
    private const string FormalPassedKind = "PromotionFormalCheckPassed";
    private const string FormalRejectedKind = "PromotionFormalCheckRejected";
    private const string RatingStartedKind = "PromotionRatingStarted";
    private const string RatingReminderKind = "PromotionRatingReminder";
    private const string ReviewStartedKind = "PromotionReviewStarted";
    private const string ResultsKind = "PromotionResultsPublished";
    private const string NoDecisionKind = "PromotionNoDecision";
    private const string FeedbackStartedKind = "PromotionFeedbackStarted";
    private const string FeedbackMissingKind = "PromotionFeedbackMissing";
    private const string Subject = "Повышение в системе «Калибр»";
    private const string DateFormat = "dd.MM.yyyy";
    private const string SignInLine = "Если вы ещё не вошли в систему, сначала откроется страница входа, а после входа —";
    private const string AutomaticLine = "Это автоматическое письмо, отвечать на него не нужно.";
    private const string ApplicationTarget = "эта заявка";
    private const string WaveTarget = "эта волна";
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
                $"объявлена волна повышения. Заявки принимаются с {Day(wave.StartsOn)} по {Day(wave.EndsOn[0])}.",
                "Посмотреть волну:",
                Link("promotion"),
                "раздел «Повышение»"));

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
                $"начался приём заявок на повышение, подать заявку можно до {Day(wave.EndsOn[0])}.",
                "Подать заявку:",
                Link($"promotion/apply/{wave.Id}"),
                "форма заявки"));

    /// <summary>
    /// Tells each responsible HR how many filed applications of their units wait for the dates of the formal check.
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
                    $"начался этап «Формальная проверка». Внесите дату приёма на работу или последнего повышения по заявкам:\n{string.Join('\n', lines)}",
                    "Открыть волну:",
                    Link($"promotion/waves/{wave.Id}"),
                    WaveTarget));
        }
    }

    /// <summary>
    /// Tells each candidate the result of the formal check, and asks the raters of the admitted applications to rate.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task FormalCheckFinishedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        await TellCandidatesAsync(wave, [ApplicationStatus.Admitted], FormalPassedKind, "ваша заявка на повышение прошла формальную проверку.", cancellationToken);
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
                    $"ваша заявка на повышение отклонена по формальным признакам: {string.Join("; ", application.FormalFailures.Select(FailureText))}.",
                    "Открыть заявку:",
                    Link($"promotion/applications/{application.Id}"),
                    ApplicationTarget));
        }

        var admittedIds = await context.Set<PromotionApplication>()
            .AsNoTracking()
            .Where(application => application.WaveId == wave.Id && application.Status == ApplicationStatus.Admitted)
            .Select(application => application.Id)
            .ToListAsync(cancellationToken);
        var raterIds = await context.Set<PromotionRater>()
            .AsNoTracking()
            .Where(rater => admittedIds.Contains(rater.ApplicationId))
            .Select(rater => rater.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);
        Enqueue(
            await AccountsAsync(raterIds, cancellationToken),
            RatingStartedKind,
            Compose(
                $"вас просят оценить кандидатов на повышение. Оценить можно до {Day(wave.EndsOn[2])}.",
                "Открыть список кандидатов:",
                Link("promotion/ratings"),
                "список кандидатов"));
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
    /// Tells the members of each commission that its applications are ready to be reviewed.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ReviewStartedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        var counts = await UnitCountsAsync(wave.Id, ApplicationStatus.UnderReview, cancellationToken);
        var rosters = await commissions.ReadAsync([.. counts.Keys], cancellationToken);
        foreach (var roster in rosters.Values)
        {
            Enqueue(
                await AccountsAsync(roster.Members.Where(member => member.IsActing).Select(member => member.EmployeeId), cancellationToken),
                ReviewStartedKind,
                Compose(
                    $"начался этап «Рассмотрение комиссией»: заявок на рассмотрении по подразделению «{roster.UnitName}» — {counts[roster.UnitId]}.",
                    "Открыть страницу комиссии:",
                    Link($"promotion/commission/{wave.Id}/{roster.UnitId}"),
                    CommissionTarget));
        }
    }

    /// <summary>
    /// Tells each candidate with a result that it is published, and each responsible HR which applications of their units were left without a decision.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task ReviewFinishedAsync(PromotionWave wave, CancellationToken cancellationToken)
    {
        await TellCandidatesAsync(wave, ResultStatuses, ResultsKind, "результаты рассмотрения вашей заявки на повышение опубликованы.", cancellationToken);
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
                    "Открыть волну:",
                    Link($"promotion/waves/{wave.Id}"),
                    WaveTarget));
        }
    }

    /// <summary>
    /// Asks every candidate whose application reached a result to mark that they received the feedback.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public Task FeedbackStartedAsync(PromotionWave wave, CancellationToken cancellationToken) =>
        TellCandidatesAsync(wave, ResultStatuses, FeedbackStartedKind, "начался этап «Обратная связь». Когда получите обратную связь по заявке на повышение, отметьте это в системе.", cancellationToken);

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
        FormalFailure.MissingDate => "не внесена дата приёма на работу или последнего повышения",
        FormalFailure.IntervalNotPassed => "не прошёл минимальный интервал между повышениями",
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

    private void Enqueue(IEnumerable<Guid> recipientIds, string kind, string body)
    {
        var recipients = recipientIds.Distinct().ToList();
        if (recipients.Count > 0)
        {
            queue.Enqueue(new PromotionMailQueue.Letter(recipients, kind, Subject, body, actor.UserId?.ToString(), actor.Role, actor.RequestId));
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
