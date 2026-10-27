using System.Text.Json;
using Competency.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure.Import;

/// <summary>
/// The organization import API for global administrators and HR: a stateless preview of what a workbook would change, and an apply that reads the same workbook
/// again under the tree lock and applies exactly the previewed plan, or nothing. Antiforgery is off because the platform's cross-site check rejects any
/// cross-site state-changing request whatever its content type; the body limit is raised for these two endpoints alone.
/// </summary>
internal static class OrgImportEndpoints
{
    public const string PlanChangedCode = "planChanged";

    private const string Route = "/api/v1/org-import";
    private const string Tag = "OrgImport";
    private const long MaxBodyBytes = 50L * 1024 * 1024;
    private const int MaxFileNameLength = 255;
    private const string AppliedAction = "OrgImport.Applied";
    private const string AuditEntityType = "OrgImport";
    private const string ApplyDescription =
        "Form fields: file, the fingerprint of the previewed plan, optional decisions, a JSON array of { id, decision } where decision is keep, "
        + "deactivate (a unit) or dismiss (an employee); a missing unit or employee without a decision is deactivated or dismissed; and optional heads, "
        + "a JSON array of { unit, employee } with the unit key and the employee key of the preview's heads group, or null as the employee for no head. "
        + "A missing employee the decisions keep may be chosen by their identifier. A unit without a head choice keeps its head if the employee still works in it, "
        + "else it is left without a head; the choice is applied after the employees and dismissals, and a choice that names an employee who does not work in the unit is a 400. "
        + "A 409 whose code is planChanged means the file or the organization no longer give the previewed plan.";
    private const string PlanChangedMessage = "План изменился после предпросмотра: файл или оргструктура другие. Пересчитайте предпросмотр.";
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var keepers = endpoints.MapGroup(Route)
            .WithTags(Tag)
            .RequireAuthorization(AuthorizationPolicies.GlobalAdminOrHr)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxBodyBytes))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        keepers.MapPost("/preview", PreviewAsync).WithName("PreviewOrgImport");
        keepers.MapPost("/apply", ApplyAsync).WithName("ApplyOrgImport")
            .WithDescription(ApplyDescription)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    /// <summary>
    /// Reads the workbook and plans it against the organization, changing nothing. An unusable workbook is a normal outcome, reported as the full list of errors.
    /// </summary>
    private static async Task<Ok<OrgImportPreviewResponse>> PreviewAsync(
        IFormFile file,
        AppDbContext context,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        var content = await ReadAsync(file, cancellationToken);
        var parsed = OrgImportParser.Parse(content);
        if (parsed.Errors.Count > 0)
        {
            return TypedResults.Ok(Failed(parsed.Errors));
        }

        await using var transaction = await context.BeginSharedAsync(cancellationToken);
        var outcome = await OrgImportPlanner.PlanAsync(parsed.Rows, await ImportSnapshot.LoadAsync(context, cancellationToken), accounts, cancellationToken);
        if (outcome.Plan is not { } plan)
        {
            return TypedResults.Ok(Failed(outcome.Errors));
        }

        var shown = plan.ToResponse();
        return TypedResults.Ok(new OrgImportPreviewResponse([], shown with { Fingerprint = OrgImportPlanner.Fingerprint(content, shown) }));
    }

    /// <summary>
    /// Applies the plan of the workbook in one transaction under the tree lock. It plans again with the code of the preview and refuses with 409 unless the fingerprint
    /// equals the one the preview gave; a decision about an entity the plan does not list is a 400, and a decision that would break a rule of the organization is a 409
    /// with the reason. Missing units and employees without a decision are deactivated and dismissed. The decisions are a JSON array of <c>{ id, decision }</c>,
    /// the head choices a JSON array of <c>{ unit, employee }</c>; a choice naming an employee who does not work in the unit after the apply is a 400.
    /// </summary>
    private static async Task<Results<Ok<OrgImportResultResponse>, ValidationProblem, ProblemHttpResult>> ApplyAsync(
        IFormFile file,
        [FromForm] string fingerprint,
        [FromForm] string? decisions,
        [FromForm] string? heads,
        AppDbContext context,
        IEmployeeAccounts accounts,
        IHrAssignments hrAssignments,
        IAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (!TryReadList<ImportDecisionRequest>(decisions, out var requested))
        {
            return Rejection.Invalid("decisions", "Решения переданы в неверном формате.");
        }

        if (!TryReadList<ImportHeadDecisionRequest>(heads, out var requestedHeads))
        {
            return Rejection.Invalid("heads", "Выбор руководителей передан в неверном формате.");
        }

        var content = await ReadAsync(file, cancellationToken);
        var parsed = OrgImportParser.Parse(content);
        if (parsed.Errors.Count > 0)
        {
            return PlanChanged();
        }

        await using var transaction = await context.BeginExclusiveAsync(cancellationToken);
        var data = await ImportSnapshot.LoadAsync(context, cancellationToken);
        var outcome = await OrgImportPlanner.PlanAsync(parsed.Rows, data, accounts, cancellationToken);
        if (outcome.Plan is not { } plan)
        {
            return PlanChanged();
        }

        var shown = plan.ToResponse();
        if (OrgImportPlanner.Fingerprint(content, shown) != fingerprint)
        {
            return PlanChanged();
        }

        if (ImportDecisions.Resolve(shown, requested, out var effective) is { } invalid)
        {
            return Rejection.Invalid("decisions", invalid);
        }

        if (ImportHeads.Resolve(plan, shown, requestedHeads, effective, out var chosenHeads) is { } invalidHeads)
        {
            return Rejection.Invalid("heads", invalidHeads);
        }

        if (await ImportDecisions.BrokenRuleAsync(shown, effective, data, accounts, cancellationToken) is { } broken)
        {
            return Rejection.Conflict(broken);
        }

        var assignedHeads = await new OrgImportApplier(context, accounts, hrAssignments).ApplyAsync(plan, effective, chosenHeads, cancellationToken);

        var result = OrgImportResultResponse.From(shown, effective.UnitsToDeactivate.Count, effective.EmployeesToDismiss.Count, assignedHeads);
        audit.Stage(new AuditEntry(AppliedAction, AuditEntityType, NewValue: AppliedValue(file.FileName, result)), context);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static OrgImportPreviewResponse Failed(IReadOnlyList<ImportError> errors) =>
        new([.. errors.Select(ImportErrorResponse.From)], null);

    private static async Task<byte[]> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var content = new byte[file.Length];
        await using var stream = file.OpenReadStream();
        await stream.ReadExactlyAsync(content, cancellationToken);
        return content;
    }

    private static bool TryReadList<T>(string? json, out IReadOnlyList<T> requested)
    {
        requested = [];
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            requested = JsonSerializer.Deserialize<List<T>>(json, WebJson) ?? [];
            return requested.All(item => item is not null);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ProblemHttpResult PlanChanged() =>
        TypedResults.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = Rejection.ConflictTitle,
            Detail = PlanChangedMessage,
            Extensions = { ["code"] = PlanChangedCode },
        });

    private static string AppliedValue(string fileName, OrgImportResultResponse counts)
    {
        var name = Path.GetFileName(fileName);
        return JsonSerializer.Serialize(new { fileName = name.Length > MaxFileNameLength ? name[..MaxFileNameLength] : name, counts }, WebJson);
    }
}
