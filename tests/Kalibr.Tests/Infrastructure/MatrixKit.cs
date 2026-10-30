using System.Net;
using System.Text.Json.Nodes;

namespace Kalibr.Tests.Infrastructure;

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

    public Guid MatrixId => Matrix.Id;

    public Guid VersionId => Matrix.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();

    /// <summary>
    /// Builds the matrix and its catalogue entries.
    /// </summary>
    /// <param name="org">The people and units.</param>
    /// <param name="unitId">The unit of the matrix; the team by default.</param>
    /// <param name="author">Who creates the matrix; the team's head by default.</param>
    /// <param name="scaleId">The scale of the competencies; the base scale by default.</param>
    /// <param name="competencyAuthor">Who creates the competencies, so a head makes them drafts; an administrator by default.</param>
    /// <returns>The kit.</returns>
    public static async Task<MatrixKit> CreateAsync(CareerOrg org, Guid? unitId = null, ApiClient? author = null, Guid? scaleId = null, ApiClient? competencyAuthor = null)
    {
        var admin = org.Admin;
        var unit = unitId ?? org.Team.Id;
        var direction = await admin.MakeDirectionAsync();
        var track = await admin.MakeTrackAsync(unit, direction.Id);
        var junior = await admin.LinkToNewPositionAsync(await admin.MakeLevelAsync(track.Id, Scenarios.Unique("Младший")), unit);
        var senior = await admin.LinkToNewPositionAsync(await admin.MakeLevelAsync(track.Id, Scenarios.Unique("Старший")), unit);
        track = (await admin.GetAsync($"/api/v1/career/tracks/{track.Id}")).Expect(HttpStatusCode.OK);
        var scale = scaleId ?? Guid.Parse(CareerScenarios.BaseScaleId);
        var critical = await (competencyAuthor ?? admin).MakeCompetencyAsync(Scenarios.Unique("Критичная"), direction.Id, scale);
        var profile = await (competencyAuthor ?? admin).MakeCompetencyAsync(Scenarios.Unique("Профильная"), null, scale);
        var head = author ?? org.TeamHead.Client;
        var matrix = (await head.PostAsync(
            "/api/v1/career/matrices",
            new { orgUnitId = unit, directionId = direction.Id, trackId = track.Id })).Expect(HttpStatusCode.Created);
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
    /// The complete document: both levels with their thresholds, both competencies outside blocks with their types in the version, the whole
    /// of each level's rating in that group, and a required score for every pair of a level and a competency.
    /// </summary>
    /// <param name="seniorCriticalScore">The score the senior level requires of the critical competency; another value makes another document.</param>
    public JsonObject Content(int seniorCriticalScore = 3) => new()
    {
        ["levels"] = new JsonArray(Level(Junior.Id, 60), Level(Senior.Id, 70)),
        ["competencies"] = new JsonArray(
            new JsonObject { ["competencyId"] = Critical.Id, ["blockId"] = null, ["type"] = "Critical" },
            new JsonObject { ["competencyId"] = Profile.Id, ["blockId"] = null, ["type"] = "Additional" }),
        ["blockWeights"] = new JsonArray(Weight(Junior.Id, 100), Weight(Senior.Id, 100)),
        ["requirements"] = new JsonArray(
            Requirement(Junior.Id, Critical.Id, 2),
            Requirement(Junior.Id, Profile.Id, 1),
            Requirement(Senior.Id, Critical.Id, seniorCriticalScore),
            Requirement(Senior.Id, Profile.Id, 2)),
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
    public async Task<ApiResponse> TransitAsync(ApiClient who, string action, object? body = null, string? etag = null, Guid? versionId = null)
    {
        var path = $"/api/v1/career/matrix-versions/{versionId ?? VersionId}";
        var cascade = action is "submit" or "approve" or "accept-changes" ? await CascadeAsync(who, path) : string.Empty;
        return await who.PostAsync($"{path}/{action}{cascade}", body, etag ?? await ETagAsync(versionId));
    }

    /// <summary>
    /// The query that confirms the publication list a step reads; empty when the user cannot read the version.
    /// </summary>
    private static async Task<string> CascadeAsync(ApiClient who, string versionPath)
    {
        var publication = await who.GetAsync($"{versionPath}/publication");
        return publication.Status == HttpStatusCode.OK ? $"?cascade={Uri.EscapeDataString(publication.Json!["cascadeTag"]!.GetValue<string>())}" : string.Empty;
    }

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

    private static JsonObject Weight(Guid levelId, decimal percent) =>
        new() { ["levelId"] = levelId, ["blockId"] = null, ["percent"] = percent };

    private static JsonObject Requirement(Guid levelId, Guid competencyId, int score) =>
        new() { ["levelId"] = levelId, ["competencyId"] = competencyId, ["requiredScore"] = score };
}
