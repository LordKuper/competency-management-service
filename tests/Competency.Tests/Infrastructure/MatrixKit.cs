using System.Net;
using System.Text.Json.Nodes;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A matrix of the team of a <see cref="CareerOrg"/> on a fresh direction and track with two levels and two competencies on the base scale,
/// created by the team's head, with the complete document that passes the check for hand-over. The team's head is the author, the department's
/// head is the approver of the superior step and the HR assigned to the root unit is the responsible HR.
/// </summary>
public sealed class MatrixKit
{
    private MatrixKit()
    {
    }

    public CareerOrg Org { get; private init; } = null!;

    public ApiResponse Direction { get; private init; } = null!;

    public ApiResponse Track { get; private init; } = null!;

    public ApiResponse Junior { get; private init; } = null!;

    public ApiResponse Senior { get; private init; } = null!;

    public ApiResponse Critical { get; private init; } = null!;

    public ApiResponse Profile { get; private init; } = null!;

    public ApiResponse Matrix { get; private init; } = null!;

    /// <summary>
    /// The head who created the matrix and edits and submits its versions.
    /// </summary>
    public ApiClient Author { get; private init; } = null!;

    public Guid BlockId { get; } = Guid.NewGuid();

    public Guid MatrixId => Matrix.Id;

    public Guid VersionId => Matrix.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();

    /// <summary>
    /// Builds the matrix and its catalogue entries.
    /// </summary>
    /// <param name="org">The people and units.</param>
    /// <param name="unitId">The unit of the matrix; the team by default.</param>
    /// <param name="author">Who creates the matrix; the team's head by default.</param>
    /// <param name="scaleId">The scale of the version and of its competencies; the base scale by default.</param>
    /// <param name="competencyAuthor">Who creates the competencies, so a head makes them drafts; an administrator by default.</param>
    /// <returns>The kit.</returns>
    public static async Task<MatrixKit> CreateAsync(CareerOrg org, Guid? unitId = null, ApiClient? author = null, Guid? scaleId = null, ApiClient? competencyAuthor = null)
    {
        var admin = org.Admin;
        var unit = unitId ?? org.Team.Id;
        var direction = await admin.MakeDirectionAsync();
        var track = await admin.MakeTrackAsync(unit, direction.Id);
        var junior = await admin.MakeLevelAsync(track.Id, Scenarios.Unique("Младший"));
        var senior = await admin.MakeLevelAsync(track.Id, Scenarios.Unique("Старший"));
        var categoryId = await admin.CategoryIdAsync();
        var scale = scaleId ?? Guid.Parse(CareerScenarios.BaseScaleId);
        var critical = await (competencyAuthor ?? admin).MakeCompetencyAsync(categoryId, Scenarios.Unique("Критичная"), direction.Id, "Critical", scale);
        var profile = await (competencyAuthor ?? admin).MakeCompetencyAsync(categoryId, Scenarios.Unique("Профильная"), null, "Profile", scale);
        var head = author ?? org.TeamHead.Client;
        var matrix = (await head.PostAsync(
            "/api/v1/career/matrices",
            new { orgUnitId = unit, directionId = direction.Id, trackId = track.Id, scaleId = scale })).Expect(HttpStatusCode.Created);
        return new MatrixKit
        {
            Org = org,
            Direction = direction,
            Track = track,
            Junior = junior,
            Senior = senior,
            Critical = critical,
            Profile = profile,
            Matrix = matrix,
            Author = head,
        };
    }

    /// <summary>
    /// The complete document: both levels with their thresholds, one mandatory block with both competencies, the whole of each level's rating
    /// in that block, and a requirement for every pair of a level and a competency.
    /// </summary>
    public JsonObject Content(string behavior = "Делает самостоятельно") => new()
    {
        ["levels"] = new JsonArray(Level(Junior.Id, 60), Level(Senior.Id, 70)),
        ["blocks"] = new JsonArray(new JsonObject { ["id"] = BlockId, ["name"] = "Основные", ["isMandatory"] = true }),
        ["competencies"] = new JsonArray(
            new JsonObject { ["competencyId"] = Critical.Id, ["blockId"] = BlockId },
            new JsonObject { ["competencyId"] = Profile.Id, ["blockId"] = BlockId }),
        ["blockWeights"] = new JsonArray(Weight(Junior.Id, 100), Weight(Senior.Id, 100)),
        ["requirements"] = new JsonArray(
            Requirement(Junior.Id, Critical.Id, 2, "Critical", behavior),
            Requirement(Junior.Id, Profile.Id, 1, "Profile", behavior),
            Requirement(Senior.Id, Critical.Id, 3, "Critical", behavior),
            Requirement(Senior.Id, Profile.Id, 2, "Profile", behavior)),
    };

    /// <summary>
    /// Reads the version as the given user does.
    /// </summary>
    public Task<ApiResponse> GetVersionAsync(ApiClient who, Guid? versionId = null) =>
        who.GetAsync($"/api/v1/career/matrix-versions/{versionId ?? VersionId}");

    /// <summary>
    /// The current entity tag of the version, read as an administrator.
    /// </summary>
    public async Task<string> ETagAsync(Guid? versionId = null) =>
        (await GetVersionAsync(Org.Admin, versionId)).Expect(HttpStatusCode.OK).ETag!;

    /// <summary>
    /// Replaces the version's document as the given user, against the version's current entity tag unless one is given.
    /// </summary>
    public async Task<ApiResponse> SaveAsync(ApiClient who, JsonNode content, string? etag = null, Guid? versionId = null) =>
        await who.PutAsync($"/api/v1/career/matrix-versions/{versionId ?? VersionId}/content", content, etag ?? await ETagAsync(versionId));

    /// <summary>
    /// Performs an action of the approval route as the given user, against the version's current entity tag unless one is given.
    /// </summary>
    public async Task<ApiResponse> TransitAsync(ApiClient who, string action, object? body = null, string? etag = null, Guid? versionId = null) =>
        await who.PostAsync($"/api/v1/career/matrix-versions/{versionId ?? VersionId}/{action}", body, etag ?? await ETagAsync(versionId));

    /// <summary>
    /// Saves the complete document as the head, which is all a version needs to be handed on.
    /// </summary>
    public async Task<MatrixKit> PrepareAsync()
    {
        (await SaveAsync(Author, Content())).Expect(HttpStatusCode.OK);
        return this;
    }

    /// <summary>
    /// Hands the prepared version on and publishes it through both steps, of the matrix of the team.
    /// </summary>
    public async Task PublishAsync()
    {
        (await TransitAsync(Org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await TransitAsync(Org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        (await TransitAsync(Org.Hr.Client, "approve")).Expect(HttpStatusCode.OK);
    }

    public async Task<string> StatusAsync(Guid? versionId = null) =>
        (await GetVersionAsync(Org.Admin, versionId)).Expect(HttpStatusCode.OK).Json!["status"]!.GetValue<string>();

    public static string[] Actions(ApiResponse version) =>
        [.. version.Json!["actions"]!.AsArray().Select(action => action!.GetValue<string>())];

    public async Task<ApiResponse> HistoryAsync(ApiClient who, Guid? versionId = null) =>
        await who.GetAsync($"/api/v1/career/matrix-versions/{versionId ?? VersionId}/history");

    public async Task<string[]> DecisionsAsync(Guid? versionId = null) =>
        [.. (await HistoryAsync(Org.Admin, versionId)).Expect(HttpStatusCode.OK).Json!["decisions"]!.AsArray()
            .Select(decision => $"{decision!["kind"]!.GetValue<string>()}{(decision["step"] is { } step ? $"@{step.GetValue<string>()}" : string.Empty)}")];

    private static JsonObject Level(Guid id, decimal threshold) => new() { ["levelId"] = id, ["minOverallPercent"] = threshold };

    private JsonObject Weight(Guid levelId, decimal percent) =>
        new() { ["levelId"] = levelId, ["blockId"] = BlockId, ["percent"] = percent, ["minPercent"] = 50 };

    private static JsonObject Requirement(Guid levelId, Guid competencyId, int score, string type, string behavior) => new()
    {
        ["levelId"] = levelId,
        ["competencyId"] = competencyId,
        ["requiredScore"] = score,
        ["type"] = type,
        ["weight"] = 1,
        ["isMandatory"] = true,
        ["expectedBehavior"] = behavior,
        ["evidenceExamples"] = null,
        ["validFrom"] = null,
    };
}
