using System.Diagnostics;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Competency.CareerFramework;

/// <summary>
/// The career reminders: who is now expected to act after an action, and the letter that tells them, with a link built from the configured
/// public address. Call it only after the action has committed and released its locks: it reads the state after the action and queues the
/// letters, so the action never waits on the mail server. Nobody gets a reminder of their own action. The letters carry no comment, content
/// or difference of a version, which only the version's readers may see.
/// </summary>
internal sealed class CareerMail(
    AppDbContext context,
    CatalogStore store,
    ProcessRoles roles,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ICurrentActor actor,
    IOptions<AppOptions> app,
    CareerMailQueue queue)
{
    private const string AwaitingKind = "MatrixAwaitingApproval";
    private const string ReturnedKind = "MatrixReturned";
    private const string DraftKind = "DraftAwaitingApproval";
    private const string AwaitingSubject = "Версия матрицы ждёт вашего согласования в системе «Калибр»";
    private const string ReturnedSubject = "Версия матрицы возвращена вам в системе «Калибр»";
    private const string DraftSubject = "Черновая запись справочника ждёт утверждения в системе «Калибр»";
    private const string ReviewRoute = "desk/review";
    private const string ReworkRoute = "desk/rework";
    private const string DraftsRoute = "desk/drafts";
    private const string SignInLine = "Если вы ещё не вошли в систему, сначала откроется страница входа, а после входа —";
    private const string AutomaticLine = "Это автоматическое письмо, отвечать на него не нужно.";
    private const string HeadWord = "руководитель подразделения";
    private const string ApproverWord = "согласующий";

    /// <summary>
    /// Tells whoever the version now waits for: the approver of its new step after a submission, a superior approval or an accepted change
    /// that left it on review, and the head of the matrix unit after a return. A transition that published the version or made it a draft again tells nobody.
    /// </summary>
    /// <param name="version">The version as committed.</param>
    /// <param name="action">The transition performed.</param>
    /// <param name="after">The state after it.</param>
    /// <param name="comment">The approver's comment of a return, if any.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public Task VersionTransitedAsync(MatrixVersion version, MatrixAction action, MatrixVersionState after, string? comment, CancellationToken cancellationToken) =>
        action switch
        {
            MatrixAction.Return => ReturnedAsync(version, after.ReturnedWithChanges, comment is not null, cancellationToken),
            MatrixAction.Submit or MatrixAction.Approve or MatrixAction.AcceptChanges when after.Turn is { ApproverId: { } approverId } turn =>
                AwaitingAsync(version, turn.Step, approverId, EventOf(action), cancellationToken),
            _ => Task.CompletedTask,
        };

    /// <summary>
    /// Tells the responsible HR of the units the author heads, each once, about a draft entry a unit head who does not keep it has just
    /// created; for a track, the HR of its unit. A draft level and a draft its keeper created tell nobody.
    /// </summary>
    /// <param name="entry">The created entry.</param>
    /// <param name="viewer">The author's rights at the moment of creating it.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    public async Task DraftCreatedAsync(CatalogEntry entry, CatalogViewer viewer, CancellationToken cancellationToken)
    {
        if (entry is CareerLevel || entry.Status != CatalogStatus.Draft || viewer.CanManage(entry))
        {
            return;
        }

        var unitNames = await store.UnitNamesAsync(
            viewer.HeadedUnitIds.Where(unitId => entry.UnitId is null || unitId == entry.UnitId).Select(unitId => (Guid?)unitId),
            cancellationToken);
        var unitNameOfHr = new Dictionary<Guid, string>();
        foreach (var (unitId, unitName) in unitNames.OrderBy(unit => unit.Value).ThenBy(unit => unit.Key))
        {
            if (await roles.ResponsibleHrAsync(unitId, cancellationToken) is { } hrId && hrId != actor.UserId)
            {
                unitNameOfHr.TryAdd(hrId, unitName);
            }
        }

        if (unitNameOfHr.Count == 0)
        {
            return;
        }

        var kind = CatalogDraftEndpoints.KindOf(entry);
        var scope = entry.UnitId is { } trackUnitId ? $"подразделение «{unitNames[trackUnitId]}»" : "общая запись";
        var authorName = await ActorNameAsync(HeadWord, cancellationToken);
        var link = Link(DraftsRoute, $"{kind.ToString().ToLowerInvariant()}/{entry.Id}");
        foreach (var (hrId, unitName) in unitNameOfHr)
        {
            Enqueue(hrId, DraftKind, DraftSubject, $"""
                Здравствуйте!

                Что ждёт вас: черновая запись справочника — {KindWord(kind)} «{entry.Name}» ({scope}) — ждёт утверждения.
                От кого: {authorName}, руководитель подразделения «{unitName}».

                Открыть запись в очереди утверждения:
                {link}

                {SignInLine} эта запись.

                {AutomaticLine}
                """);
        }
    }

    private static string EventOf(MatrixAction action) => action switch
    {
        MatrixAction.Submit => "отправка версии на согласование",
        MatrixAction.Approve => "согласование шага 1",
        _ => "правки приняты автором",
    };

    private static string StepOf(ApprovalStep step) => step == ApprovalStep.Superior ? "шаг 1, вышестоящий руководитель" : "шаг 2, HR";

    private static string KindWord(CatalogEntryKind kind) => kind switch
    {
        CatalogEntryKind.Direction => "область",
        CatalogEntryKind.Track => "трек",
        CatalogEntryKind.Category => "категория",
        CatalogEntryKind.Competency => "компетенция",
        CatalogEntryKind.Scale => "шкала",
        CatalogEntryKind.Block => "общий блок",
        _ => throw new UnreachableException($"No draft letter names entries of kind {kind}."),
    };

    private static string ReturnedWith(bool hasChanges, bool hasComment) => (hasChanges, hasComment) switch
    {
        (true, true) => "с правками и комментарием согласующего",
        (true, false) => "с правками",
        _ => "с комментарием",
    };

    private async Task AwaitingAsync(MatrixVersion version, ApprovalStep step, Guid approverId, string happened, CancellationToken cancellationToken)
    {
        if (approverId == actor.UserId)
        {
            return;
        }

        Enqueue(approverId, AwaitingKind, AwaitingSubject, $"""
            Здравствуйте!

            Что ждёт вас: {await VersionTextAsync(version, cancellationToken)} ждёт вашего согласования — {StepOf(step)}.
            От кого: {await ActorNameAsync(HeadWord, cancellationToken)} ({happened}).

            Открыть версию и принять решение:
            {Link(ReviewRoute, version.Id.ToString())}

            {SignInLine} эта версия.

            {AutomaticLine}
            """);
    }

    private async Task ReturnedAsync(MatrixVersion version, bool hasChanges, bool hasComment, CancellationToken cancellationToken)
    {
        if (await employees.FindUnitAsync(version.Matrix.OrgUnitId, cancellationToken) is not { } unit
            || await roles.HeadAccountAsync(unit, cancellationToken) is not { } headId
            || headId == actor.UserId)
        {
            return;
        }

        Enqueue(headId, ReturnedKind, ReturnedSubject, $"""
            Здравствуйте!

            Что ждёт вас: {await VersionTextAsync(version, cancellationToken)} возвращена вам — {ReturnedWith(hasChanges, hasComment)}. Решите, принять правки, доработать версию или откатить правки.
            От кого: {await ActorNameAsync(ApproverWord, cancellationToken)} ({StepOf(version.Step ?? ApprovalStep.Hr)}).

            Открыть версию:
            {Link(ReworkRoute, version.Id.ToString())}

            {SignInLine} эта версия.

            {AutomaticLine}
            """);
    }

    private async Task<string> VersionTextAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var matrix = version.Matrix;
        var matrixName = matrix.PositionId is { } positionId
            ? (await store.PositionNamesAsync([positionId], cancellationToken)).GetValueOrDefault(positionId)
            : await context.Set<CareerTrack>()
                .Where(track => track.Id == matrix.TrackId)
                .Select(track => track.Name)
                .FirstAsync(cancellationToken);
        var unitNames = await store.UnitNamesAsync([matrix.OrgUnitId], cancellationToken);
        return $"версия матрицы «{matrixName}» подразделения «{unitNames.GetValueOrDefault(matrix.OrgUnitId)}» (версия {version.Number})";
    }

    /// <summary>
    /// The full name of the employee the acting account is bound to, or else the given word for the actor, never the account e-mail.
    /// </summary>
    private async Task<string> ActorNameAsync(string fallback, CancellationToken cancellationToken) =>
        await users.FindAsync(actor.UserId ?? Guid.Empty, cancellationToken) is { EmployeeId: { } employeeId }
        && await employees.FindAsync(employeeId, cancellationToken) is { } employee
            ? employee.FullName
            : fallback;

    private string Link(string route, string path) => $"{app.Value.PublicBaseUrl.AbsoluteUri.TrimEnd('/')}/{route}/{path}";

    private void Enqueue(Guid recipientId, string kind, string subject, string body) =>
        queue.Enqueue(new CareerMailQueue.Letter(recipientId, kind, subject, body, actor.UserId?.ToString(), actor.Role, actor.RequestId));
}
