using Competency.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.CareerFramework;

/// <summary>
/// The life of matrix versions: editing, the approval route and publication. Every status change holds, in this order, the tree lock in
/// shared mode, the catalogue lock, because publication makes drafts active, and the matrix row, and needs <c>If-Match</c>; content
/// edits hold the tree and the catalogue lock in shared mode and rely on <c>If-Match</c> for the version row. Who may act is worked out under those locks at the moment of acting.
/// </summary>
internal sealed class MatrixWorkflow(
    AppDbContext context,
    CatalogStore store,
    MatrixRoute route,
    MatrixCatalog catalog,
    ProcessRoles roles,
    IAuditWriter audit,
    ICurrentActor actor,
    TimeProvider time)
{
    private const string NotAllowedMessage = "Действие недоступно в текущем статусе версии";
    private const string EntityType = nameof(MatrixVersion);
    private const string ContentField = "content";
    private const string CommentField = "comment";
    private const string TurnMessage = "Сейчас ход другого участника согласования";
    private const string NoHrMessage = "Нет ответственного HR — обратитесь к администратору";
    private const string ChangedByApproverMessage = "Версия изменена согласующим — верните её с правками";
    private const string NothingToReturnMessage = "Укажите комментарий или внесите правки";
    private const string InWorkExistsMessage = "У матрицы уже есть версия в работе";

    public Task<CatalogViewer> ViewerAsync(CancellationToken cancellationToken) => CatalogViewer.LoadAsync(actor, roles, cancellationToken);

    /// <summary>
    /// Starts a transaction for a change of a matrix's versions and takes its locks: the tree in shared mode, the catalogue, the matrix row.
    /// </summary>
    /// <param name="matrixId">The matrix.</param>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public async Task<IDbContextTransaction> BeginTransitionAsync(Guid matrixId, CancellationToken cancellationToken)
    {
        var transaction = await store.BeginWriteAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT id FROM competency_matrices WHERE id = {matrixId} FOR UPDATE", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// What the user may do with the version now.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The state, or <see langword="null"/> when the user cannot read the version.</returns>
    public async Task<MatrixVersionState?> StateAsync(MatrixVersion version, CatalogViewer viewer, CancellationToken cancellationToken)
    {
        if (!await route.CanReadAsync(viewer, version, isHistory: false, cancellationToken))
        {
            return null;
        }

        var turn = await route.TurnAsync(version, cancellationToken);
        var isTurnHolder = version.Status switch
        {
            MatrixVersionStatus.Draft or MatrixVersionStatus.Returned => viewer.HeadedUnitIds.Contains(version.Matrix.OrgUnitId),
            MatrixVersionStatus.OnReview => turn?.ApproverId == viewer.UserId,
            _ => false,
        };
        var returnedWithChanges = version.Status == MatrixVersionStatus.Returned
            && await context.Set<MatrixDecision>()
                .Where(decision => decision.VersionId == version.Id && decision.Kind == MatrixDecisionKind.Returned)
                .OrderByDescending(decision => decision.Sequence)
                .Select(decision => decision.VariantNumber)
                .FirstOrDefaultAsync(cancellationToken) is not null;
        return new MatrixVersionState(turn, isTurnHolder, returnedWithChanges, ActionsOf(version.Status, isTurnHolder, returnedWithChanges));
    }

    /// <summary>
    /// Describes the version for the API and sets its <c>ETag</c>.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="state">What the user may do with it.</param>
    /// <param name="response">The response being written.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The version as the API returns it.</returns>
    public async Task<MatrixVersionResponse> DescribeAsync(
        MatrixVersion version,
        MatrixVersionState state,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var unitId = version.Matrix.OrgUnitId;
        var superiorId = version.IsInWork ? await route.SuperiorApproverAsync(unitId, cancellationToken) : null;
        var hrId = version.IsInWork ? await roles.ResponsibleHrAsync(unitId, cancellationToken) : null;
        var names = await store.UserNamesAsync([version.AuthorId, superiorId, hrId], cancellationToken);
        var entries = version.Snapshot ?? await catalog.EntriesAsync(version, cancellationToken);

        response.SetETag(version.Version);
        return new MatrixVersionResponse(
            version.Id,
            version.MatrixId,
            version.Number,
            version.Status,
            state.Turn?.Step ?? version.Step,
            version.ScaleId,
            version.AuthorId,
            names.GetValueOrDefault(version.AuthorId),
            version.CreatedAt,
            version.PublishedAt,
            version.Content,
            entries,
            PersonOf(superiorId, names),
            PersonOf(hrId, names),
            state.ReturnedWithChanges,
            state.Actions,
            version.Version);
    }

    /// <summary>
    /// Replaces the content of a version: by a unit head while it is a draft, and by the current approver while it is on review.
    /// The catalogue lock is held shared, so an entry the content refers to cannot be deleted between the reference check and the save.
    /// </summary>
    public async Task<Results<Ok<MatrixVersionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> EditAsync(
        Guid id,
        MatrixContent content,
        IfMatch ifMatch,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var errors = content.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await using var transaction = await store.BeginSharedAsync(cancellationToken);
        var viewer = await ViewerAsync(cancellationToken);
        if (await context.Set<MatrixVersion>().FirstOrDefaultAsync(version => version.Id == id, cancellationToken) is not { } version
            || await StateAsync(version, viewer, cancellationToken) is not { } state)
        {
            return TypedResults.NotFound();
        }

        if (Refusal(version, state, MatrixAction.Edit) is { } refusal)
        {
            return refusal;
        }

        var stored = content.Stored();
        if (await catalog.ReferenceProblemAsync(version, stored, cancellationToken) is { } problem)
        {
            return TypedResults.ValidationProblem(problem);
        }

        ifMatch.ApplyTo(context, version);
        if (!version.Content.SameAs(stored))
        {
            version.Content = stored;
            audit.Stage(new AuditEntry($"{EntityType}.ContentUpdated", EntityType, version.Id.ToString()), context);
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(await DescribeAsync(version, state, response, cancellationToken));
    }

    /// <summary>
    /// Starts the next version of a matrix as a draft copy of its published version; only a unit head may, and only while no version is in work.
    /// </summary>
    public async Task<Results<Created<MatrixVersionResponse>, NotFound, ProblemHttpResult>> CreateVersionAsync(
        Guid matrixId,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginTransitionAsync(matrixId, cancellationToken);
        if (await context.Set<CompetencyMatrix>().FirstOrDefaultAsync(matrix => matrix.Id == matrixId, cancellationToken) is not { } matrix)
        {
            return TypedResults.NotFound();
        }

        var viewer = await ViewerAsync(cancellationToken);
        if (!viewer.HeadedUnitIds.Contains(matrix.OrgUnitId))
        {
            return Rejection.Forbidden();
        }

        var versions = await context.Set<MatrixVersion>().Where(version => version.MatrixId == matrixId).ToListAsync(cancellationToken);
        if (versions.Any(version => version.IsInWork))
        {
            return Rejection.Conflict(InWorkExistsMessage);
        }

        if (versions.FirstOrDefault(version => version.Status == MatrixVersionStatus.Published) is not { } published)
        {
            return Rejection.Conflict(NotAllowedMessage);
        }

        var created = new MatrixVersion
        {
            MatrixId = matrixId,
            Number = versions.Max(version => version.Number) + 1,
            Status = MatrixVersionStatus.Draft,
            ScaleId = published.ScaleId,
            AuthorId = viewer.UserId,
            Content = published.Content,
        };
        context.Add(created);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var state = await StateAsync(created, viewer, cancellationToken);
        return TypedResults.Created($"/api/v1/career/matrix-versions/{created.Id}", await DescribeAsync(created, state!, response, cancellationToken));
    }

    /// <summary>
    /// Performs one step of the approval route on the version, if it is the user's turn and the step is open to them.
    /// </summary>
    public async Task<Results<Ok<MatrixVersionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> TransitAsync(
        Guid id,
        MatrixAction action,
        IfMatch ifMatch,
        string? comment,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var matrixId = await context.Set<MatrixVersion>()
            .Where(version => version.Id == id)
            .Select(version => (Guid?)version.MatrixId)
            .FirstOrDefaultAsync(cancellationToken);
        if (matrixId is null)
        {
            return TypedResults.NotFound();
        }

        await using var transaction = await BeginTransitionAsync(matrixId.Value, cancellationToken);
        var viewer = await ViewerAsync(cancellationToken);
        if (await context.Set<MatrixVersion>().FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken) is not { } version
            || await StateAsync(version, viewer, cancellationToken) is not { } state)
        {
            return TypedResults.NotFound();
        }

        if (Refusal(version, state, action) is { } refusal)
        {
            return refusal;
        }

        ifMatch.ApplyTo(context, version);
        var progress = await ProgressAsync(version, cancellationToken);
        var problem = action switch
        {
            MatrixAction.Submit => await SubmitAsync(progress, cancellationToken),
            MatrixAction.Approve => await ApproveAsync(progress, state.Turn!, cancellationToken),
            MatrixAction.Return => await ReturnAsync(progress, state.Turn!, comment, cancellationToken),
            MatrixAction.AcceptChanges => await AcceptChangesAsync(progress, cancellationToken),
            MatrixAction.AcceptAndRevise => BackToDraft(progress, MatrixDecisionKind.AcceptedAndRevising, version.Content),
            MatrixAction.RollbackChanges => BackToDraft(progress, MatrixDecisionKind.RolledBack, progress.Variants.Last(IsAuthors).Content),
            _ => Rejection.Conflict(NotAllowedMessage),
        };
        if (problem is not null)
        {
            return problem;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var after = await StateAsync(version, viewer, cancellationToken);
        return TypedResults.Ok(await DescribeAsync(version, after!, response, cancellationToken));
    }

    private static IReadOnlyList<MatrixAction> ActionsOf(MatrixVersionStatus status, bool isTurnHolder, bool returnedWithChanges) =>
        !isTurnHolder
            ? []
            : status switch
            {
                MatrixVersionStatus.Draft => [MatrixAction.Edit, MatrixAction.Submit],
                MatrixVersionStatus.OnReview => [MatrixAction.Edit, MatrixAction.Approve, MatrixAction.Return],
                MatrixVersionStatus.Returned when returnedWithChanges =>
                    [MatrixAction.AcceptChanges, MatrixAction.AcceptAndRevise, MatrixAction.RollbackChanges],
                MatrixVersionStatus.Returned => [MatrixAction.AcceptAndRevise, MatrixAction.RollbackChanges],
                _ => [],
            };

    /// <summary>
    /// Refuses an action that is not open: on a version out of work for anyone, to anyone but the participant whose turn it is,
    /// and to that participant when the status does not offer it.
    /// </summary>
    private static ProblemHttpResult? Refusal(MatrixVersion version, MatrixVersionState state, MatrixAction action)
    {
        if (!version.IsInWork)
        {
            return Rejection.Conflict(NotAllowedMessage);
        }

        if (!state.IsTurnHolder)
        {
            return TypedResults.Problem(detail: TurnMessage, statusCode: StatusCodes.Status403Forbidden);
        }

        return state.Actions.Contains(action) ? null : Rejection.Conflict(NotAllowedMessage);
    }

    private static CareerPersonResponse? PersonOf(Guid? userId, IReadOnlyDictionary<Guid, string> names) =>
        userId is { } id && names.TryGetValue(id, out var name) ? new CareerPersonResponse(id, name) : null;

    private static bool IsAuthors(MatrixVariant variant) => variant.Kind == MatrixVariantKind.Author;

    private static bool HasApproverChanges(Progress progress) => !progress.Version.Content.SameAs(progress.Variants[^1].Content);

    private async Task<Progress> ProgressAsync(MatrixVersion version, CancellationToken cancellationToken) => new(
        version,
        actor.UserId ?? Guid.Empty,
        time.GetUtcNow(),
        await context.Set<MatrixVariant>().Where(variant => variant.VersionId == version.Id).OrderBy(variant => variant.Number).ToListAsync(cancellationToken),
        await context.Set<MatrixDecision>().Where(decision => decision.VersionId == version.Id).MaxAsync(decision => (int?)decision.Sequence, cancellationToken) ?? 0);

    /// <summary>
    /// Hands a complete draft on: to the superior approver, or past a superior step nobody can take to the responsible HR, without whom
    /// the version cannot be submitted at all.
    /// </summary>
    private async Task<ProblemHttpResult?> SubmitAsync(Progress progress, CancellationToken cancellationToken)
    {
        if (await IncompleteAsync(progress.Version, cancellationToken) is { } incomplete)
        {
            return incomplete;
        }

        var unitId = progress.Version.Matrix.OrgUnitId;
        if (await roles.ResponsibleHrAsync(unitId, cancellationToken) is not { } hrId)
        {
            return Rejection.Conflict(NoHrMessage);
        }

        var variant = AddVariant(progress, MatrixVariantKind.Author);
        Record(progress, MatrixDecisionKind.Submitted, step: null, progress.ActorId, variantNumber: variant);
        if (await route.SuperiorApproverAsync(unitId, cancellationToken) is not null)
        {
            WaitFor(progress, ApprovalStep.Superior);
            return null;
        }

        Record(progress, MatrixDecisionKind.Skipped, ApprovalStep.Superior, actorId: null);
        return await ToHrAsync(progress, hrId, cancellationToken);
    }

    /// <summary>
    /// Approves the version as submitted; an approver who changed it returns it instead. Completeness is checked again, because the
    /// catalogue entries the version uses may have changed since it was handed on.
    /// </summary>
    private async Task<ProblemHttpResult?> ApproveAsync(Progress progress, MatrixTurn turn, CancellationToken cancellationToken)
    {
        if (HasApproverChanges(progress))
        {
            return Rejection.Conflict(ChangedByApproverMessage);
        }

        if (await IncompleteAsync(progress.Version, cancellationToken) is { } incomplete)
        {
            return incomplete;
        }

        RecordPassedOver(progress, turn);
        Record(progress, MatrixDecisionKind.Approved, turn.Step, progress.ActorId);
        return turn.Step == ApprovalStep.Superior
            ? await AdvanceToHrAsync(progress, cancellationToken)
            : await PublishAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Sends the version back to the unit head with the approver's changes, kept as a variant of their own, and/or a comment.
    /// Changed content must still be complete, because the head may accept it as it is.
    /// </summary>
    private async Task<ProblemHttpResult?> ReturnAsync(Progress progress, MatrixTurn turn, string? comment, CancellationToken cancellationToken)
    {
        var hasChanges = HasApproverChanges(progress);
        if (!hasChanges && comment is null)
        {
            return TypedResults.Problem(new HttpValidationProblemDetails(new Dictionary<string, string[]> { [CommentField] = [NothingToReturnMessage] })
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = NothingToReturnMessage,
            });
        }

        if (hasChanges && await IncompleteAsync(progress.Version, cancellationToken) is { } incomplete)
        {
            return incomplete;
        }

        RecordPassedOver(progress, turn);
        int? variant = hasChanges ? AddVariant(progress, MatrixVariantKind.Approver) : null;
        Record(progress, MatrixDecisionKind.Returned, turn.Step, progress.ActorId, comment, variant);
        progress.Version.Status = MatrixVersionStatus.Returned;
        progress.Version.Step = turn.Step;
        return null;
    }

    /// <summary>
    /// Takes the approver's changes as they are, which counts as the approval of the step that returned the version.
    /// </summary>
    private async Task<ProblemHttpResult?> AcceptChangesAsync(Progress progress, CancellationToken cancellationToken)
    {
        if (await IncompleteAsync(progress.Version, cancellationToken) is { } incomplete)
        {
            return incomplete;
        }

        var step = progress.Version.Step ?? ApprovalStep.Hr;
        Record(progress, MatrixDecisionKind.AcceptedChanges, step, progress.ActorId);
        return step == ApprovalStep.Superior
            ? await AdvanceToHrAsync(progress, cancellationToken)
            : await PublishAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Makes the returned version a draft again with the given content; the next submission starts the route from its first step.
    /// </summary>
    private ProblemHttpResult? BackToDraft(Progress progress, MatrixDecisionKind kind, MatrixContent content)
    {
        Record(progress, kind, progress.Version.Step, progress.ActorId);
        progress.Version.Content = content;
        progress.Version.Status = MatrixVersionStatus.Draft;
        progress.Version.Step = null;
        return null;
    }

    private async Task<ProblemHttpResult?> AdvanceToHrAsync(Progress progress, CancellationToken cancellationToken) =>
        await roles.ResponsibleHrAsync(progress.Version.Matrix.OrgUnitId, cancellationToken) is { } hrId
            ? await ToHrAsync(progress, hrId, cancellationToken)
            : Rejection.Conflict(NoHrMessage);

    /// <summary>
    /// Moves the version to the HR step, which counts as approved at once when the responsible HR submitted the version themselves.
    /// </summary>
    private async Task<ProblemHttpResult?> ToHrAsync(Progress progress, Guid hrId, CancellationToken cancellationToken)
    {
        if (hrId != progress.Variants.Last(IsAuthors).CreatedById)
        {
            WaitFor(progress, ApprovalStep.Hr);
            return null;
        }

        Record(progress, MatrixDecisionKind.AutoApproved, ApprovalStep.Hr, actorId: null);
        return await PublishAsync(progress, cancellationToken);
    }

    /// <summary>
    /// Publishes the approved version: makes the drafts it uses and the draft categories of its competencies active, after checking their names again as approving a draft does,
    /// archives the previously published version in a save of its own, so the published-version index never sees two, and freezes the
    /// entries the version refers to into its snapshot.
    /// </summary>
    private async Task<ProblemHttpResult?> PublishAsync(Progress progress, CancellationToken cancellationToken)
    {
        var version = progress.Version;
        var drafts = await catalog.DraftsAsync(version, cancellationToken);
        foreach (var draft in drafts)
        {
            draft.Status = CatalogStatus.Active;
            if (await IsNameTakenAsync(draft, cancellationToken))
            {
                return Rejection.Conflict($"{CatalogEndpoints.DuplicateNameMessage}: «{draft.Name}»");
            }

            var draftType = draft.GetType().Name;
            audit.Stage(new AuditEntry($"{draftType}.Approved", draftType, draft.Id.ToString()), context);
        }

        var previous = await context.Set<MatrixVersion>()
            .FirstOrDefaultAsync(candidate => candidate.MatrixId == version.MatrixId && candidate.Status == MatrixVersionStatus.Published, cancellationToken);
        if (previous is not null)
        {
            previous.Status = MatrixVersionStatus.Archived;
            audit.Stage(new AuditEntry($"{EntityType}.Archived", EntityType, previous.Id.ToString()), context);
        }

        await context.SaveChangesAsync(cancellationToken);
        context.AddRange(drafts.OfType<CareerCompetency>().Select(competency => CompetencyVersion.Of(competency, progress.ActorId, progress.Now)));

        version.Snapshot = await catalog.EntriesAsync(version, cancellationToken);
        version.Status = MatrixVersionStatus.Published;
        version.Step = null;
        version.PublishedAt = progress.Now;
        Record(progress, MatrixDecisionKind.Published, step: null, actorId: null);
        return null;
    }

    private Task<bool> IsNameTakenAsync(CatalogEntry entry, CancellationToken cancellationToken) => entry switch
    {
        CareerLevel level => store.IsNameTakenAsync(level, CareerLevelEndpoints.Kind.SameScope?.Invoke(level), cancellationToken),
        CareerCompetency competency => store.IsNameTakenAsync(competency, CompetencyEndpoints.Kind.SameScope?.Invoke(competency), cancellationToken),
        CompetencyScale scale => store.IsNameTakenAsync(scale, CompetencyScaleEndpoints.Kind.SameScope?.Invoke(scale), cancellationToken),
        CompetencyCategory category => store.IsNameTakenAsync(category, CompetencyCategoryEndpoints.Kind.SameScope?.Invoke(category), cancellationToken),
        _ => Task.FromResult(false),
    };

    /// <summary>
    /// Rejects an incomplete version with every problem found, the first as the detail.
    /// </summary>
    private async Task<ProblemHttpResult?> IncompleteAsync(MatrixVersion version, CancellationToken cancellationToken)
    {
        var problems = await catalog.CompletenessProblemsAsync(version, cancellationToken);
        return problems.Count == 0
            ? null
            : TypedResults.Problem(new HttpValidationProblemDetails(new Dictionary<string, string[]> { [ContentField] = [.. problems] })
            {
                Status = StatusCodes.Status409Conflict,
                Title = Rejection.ConflictTitle,
                Detail = problems[0],
            });
    }

    /// <summary>
    /// Records that the superior step was passed over when the HR acts on a version still waiting at it, because its approver has gone since.
    /// </summary>
    private void RecordPassedOver(Progress progress, MatrixTurn turn)
    {
        if (turn.Step == ApprovalStep.Hr && progress.Version.Step == ApprovalStep.Superior)
        {
            Record(progress, MatrixDecisionKind.Skipped, ApprovalStep.Superior, actorId: null);
        }
    }

    private static void WaitFor(Progress progress, ApprovalStep step)
    {
        progress.Version.Status = MatrixVersionStatus.OnReview;
        progress.Version.Step = step;
    }

    private int AddVariant(Progress progress, MatrixVariantKind kind)
    {
        var variant = new MatrixVariant
        {
            VersionId = progress.Version.Id,
            Number = progress.Variants.Count == 0 ? 1 : progress.Variants[^1].Number + 1,
            Kind = kind,
            Content = progress.Version.Content,
            CreatedById = progress.ActorId,
            CreatedAt = progress.Now,
        };
        progress.Variants.Add(variant);
        context.Add(variant);
        return variant.Number;
    }

    /// <summary>
    /// Adds a decision to the version's history and journals it as a transition event, without the version's content.
    /// </summary>
    private void Record(Progress progress, MatrixDecisionKind kind, ApprovalStep? step, Guid? actorId, string? comment = null, int? variantNumber = null)
    {
        progress.LastSequence++;
        context.Add(new MatrixDecision
        {
            VersionId = progress.Version.Id,
            Sequence = progress.LastSequence,
            Kind = kind,
            Step = step,
            ActorId = actorId,
            At = progress.Now,
            Comment = comment,
            VariantNumber = variantNumber,
        });
        audit.Stage(new AuditEntry($"{EntityType}.{kind}", EntityType, progress.Version.Id.ToString(), Reason: comment), context);
    }

    /// <summary>
    /// One transition in progress: the version, who acts and when, the version's variants so far and its last history position.
    /// </summary>
    private sealed class Progress(MatrixVersion version, Guid actorId, DateTimeOffset now, List<MatrixVariant> variants, int lastSequence)
    {
        public MatrixVersion Version { get; } = version;

        public Guid ActorId { get; } = actorId;

        public DateTimeOffset Now { get; } = now;

        public List<MatrixVariant> Variants { get; } = variants;

        public int LastSequence { get; set; } = lastSequence;
    }
}
