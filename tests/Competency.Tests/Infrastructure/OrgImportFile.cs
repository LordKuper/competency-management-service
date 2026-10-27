using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClosedXML.Excel;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// A workbook of the organization import as a test writes it: the header of the level columns, «ФИО», «Должность» and «Юридическое лицо»,
/// then one row per call, with every cell a plain text and an absent value an empty cell.
/// </summary>
public sealed class OrgImportSheet
{
    private readonly List<string?[]> rows = [];
    private readonly int levels;

    /// <summary>
    /// Starts a sheet whose rows name a unit path of up to the given number of levels.
    /// </summary>
    /// <param name="levels">How many «Подразделение N-го уровня» columns the header has.</param>
    public OrgImportSheet(int levels = 2) => this.levels = levels;

    /// <summary>
    /// The header cells in column order; a test replaces them to leave a column out or to spell one differently.
    /// </summary>
    public string?[] Header { get; set; } = [];

    /// <summary>
    /// Adds a row: the unit path, one entry per level column (a dash or null for a skipped level), and the employee or position cells.
    /// </summary>
    /// <param name="path">The level cells from the first level on; shorter than the header's levels leaves the rest empty.</param>
    /// <param name="fullName">The employee's full name, or null for a row that declares a unit or a position.</param>
    /// <param name="position">The position name.</param>
    /// <param name="legalEntity">The legal entity name.</param>
    /// <returns>The same sheet.</returns>
    public OrgImportSheet Add(string?[] path, string? fullName = null, string? position = null, string? legalEntity = null)
    {
        var cells = new string?[levels + 3];
        path.CopyTo(cells, 0);
        cells[levels] = fullName;
        cells[levels + 1] = position;
        cells[levels + 2] = legalEntity;
        rows.Add(cells);
        return this;
    }

    /// <summary>
    /// Adds a row that declares a unit and nothing else.
    /// </summary>
    /// <param name="path">The unit path.</param>
    /// <returns>The same sheet.</returns>
    public OrgImportSheet Unit(params string?[] path) => Add(path);

    /// <summary>
    /// Adds an employee row.
    /// </summary>
    /// <param name="fullName">The full name.</param>
    /// <param name="position">The position name.</param>
    /// <param name="path">The unit path.</param>
    /// <returns>The same sheet.</returns>
    public OrgImportSheet Employee(string fullName, string position, params string?[] path) => Add(path, fullName, position);

    /// <summary>
    /// Writes the workbook.
    /// </summary>
    /// <param name="customise">A change of the written sheet, such as a formula in a cell.</param>
    /// <returns>The bytes of the .xlsx file.</returns>
    public byte[] ToBytes(Action<IXLWorksheet>? customise = null)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Структура");
        var header = Header.Length > 0
            ? Header
            : [.. Enumerable.Range(1, levels).Select(level => $"Подразделение {level}-го уровня"), "ФИО", "Должность", "Юридическое лицо"];
        Write(sheet, 1, header);
        for (var index = 0; index < rows.Count; index++)
        {
            Write(sheet, index + 2, rows[index]);
        }

        customise?.Invoke(sheet);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void Write(IXLWorksheet sheet, int row, string?[] cells)
    {
        for (var column = 0; column < cells.Length; column++)
        {
            if (!string.IsNullOrEmpty(cells[column]))
            {
                sheet.Cell(row, column + 1).Value = cells[column];
            }
        }
    }
}

/// <summary>
/// The calls of the organization import API as the wizard makes them: a multipart form with the file, and for the apply its fingerprint and decisions.
/// </summary>
public static class OrgImportCalls
{
    private const string XlsxType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Asks for the preview of a file.
    /// </summary>
    /// <param name="client">The caller.</param>
    /// <param name="file">The bytes of the file.</param>
    /// <param name="fileName">The file name in the form.</param>
    /// <param name="adjust">A change of the request before it is sent, such as a header.</param>
    /// <returns>The response.</returns>
    public static Task<ApiResponse> PreviewImportAsync(this ApiClient client, byte[] file, string fileName = "org.xlsx", Action<HttpRequestMessage>? adjust = null) =>
        client.PostAsync(
            "/api/v1/org-import/preview",
            configure: request =>
            {
                request.Content = Form(file, fileName, []);
                adjust?.Invoke(request);
            });

    /// <summary>
    /// Asks to apply a file under the fingerprint its preview gave.
    /// </summary>
    /// <param name="client">The caller.</param>
    /// <param name="file">The bytes of the file.</param>
    /// <param name="fingerprint">The fingerprint of the previewed plan.</param>
    /// <param name="decisions">The decisions as <c>{ id, decision }</c>; null sends none, so the defaults apply.</param>
    /// <param name="heads">The head choices as <c>{ unit, employee }</c>; null sends none.</param>
    /// <param name="fileName">The file name in the form.</param>
    /// <param name="decisionsText">The decisions field as raw text, which replaces <paramref name="decisions"/>; for a field that is not valid JSON.</param>
    /// <param name="headsText">The heads field as raw text, which replaces <paramref name="heads"/>.</param>
    /// <returns>The response.</returns>
    public static Task<ApiResponse> ApplyImportAsync(
        this ApiClient client,
        byte[] file,
        string fingerprint,
        IEnumerable<object>? decisions = null,
        IEnumerable<object>? heads = null,
        string fileName = "org.xlsx",
        string? decisionsText = null,
        string? headsText = null)
    {
        var fields = new List<(string Name, string Value)> { ("fingerprint", fingerprint) };
        if (decisionsText is not null || decisions is not null)
        {
            fields.Add(("decisions", decisionsText ?? JsonSerializer.Serialize(decisions, JsonSerializerOptions.Web)));
        }

        if (headsText is not null || heads is not null)
        {
            fields.Add(("heads", headsText ?? JsonSerializer.Serialize(heads, JsonSerializerOptions.Web)));
        }

        return client.PostAsync("/api/v1/org-import/apply", configure: request => request.Content = Form(file, fileName, fields));
    }

    /// <summary>
    /// Previews a file that must be usable and returns its plan.
    /// </summary>
    /// <param name="client">The caller.</param>
    /// <param name="file">The bytes of the file.</param>
    /// <returns>The plan object of the preview.</returns>
    public static async Task<JsonNode> PlanOfAsync(this ApiClient client, byte[] file)
    {
        var preview = (await client.PreviewImportAsync(file)).Expect(HttpStatusCode.OK);
        if (preview.Json!["errors"]!.AsArray().Count > 0)
        {
            throw new Xunit.Sdk.XunitException($"The file was expected to be usable, but it has errors: {preview.Body}");
        }

        return preview.Json["plan"]!;
    }

    /// <summary>
    /// Applies a previewed plan, keeping every missing unit and employee except the given ones, which are deactivated and dismissed.
    /// </summary>
    /// <param name="client">The caller.</param>
    /// <param name="file">The bytes of the file.</param>
    /// <param name="plan">The plan the preview gave.</param>
    /// <param name="deactivate">The ids of the missing units to deactivate.</param>
    /// <param name="dismiss">The ids of the missing employees to dismiss.</param>
    /// <param name="heads">The head choices, or null for none.</param>
    /// <returns>The response.</returns>
    public static Task<ApiResponse> ApplyKeepingAsync(
        this ApiClient client,
        byte[] file,
        JsonNode plan,
        IEnumerable<Guid>? deactivate = null,
        IEnumerable<Guid>? dismiss = null,
        IEnumerable<object>? heads = null)
    {
        var toDeactivate = (deactivate ?? []).ToHashSet();
        var toDismiss = (dismiss ?? []).ToHashSet();
        var decisions = plan["missingUnits"]!.AsArray()
            .Select(unit => Guid.Parse(unit!["id"]!.GetValue<string>()))
            .Select(id => (object)new { id, decision = toDeactivate.Contains(id) ? "deactivate" : "keep" })
            .Concat(plan["missingEmployees"]!.AsArray()
                .Select(employee => Guid.Parse(employee!["id"]!.GetValue<string>()))
                .Select(id => (object)new { id, decision = toDismiss.Contains(id) ? "dismiss" : "keep" }));
        return client.ApplyImportAsync(file, plan["fingerprint"]!.GetValue<string>(), decisions, heads ?? []);
    }

    /// <summary>
    /// The lines of a group of a plan.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="group">The group name as the API spells it.</param>
    /// <returns>The group's lines.</returns>
    public static string[] Lines(this JsonNode plan, string group) =>
        [.. plan[group]!.AsArray().Select(line => line!.GetValue<string>())];

    /// <summary>
    /// The fingerprint of a plan.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <returns>The fingerprint.</returns>
    public static string FingerprintOf(this JsonNode plan) => plan["fingerprint"]!.GetValue<string>();

    /// <summary>
    /// The ids of the missing units or employees of a plan that match a predicate on their JSON.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <param name="group">Either <c>missingUnits</c> or <c>missingEmployees</c>.</param>
    /// <param name="match">The condition on one item.</param>
    /// <returns>The ids of the matching items.</returns>
    public static Guid[] MissingIds(this JsonNode plan, string group, Func<JsonNode, bool> match) =>
        [.. plan[group]!.AsArray().Where(item => match(item!)).Select(item => Guid.Parse(item!["id"]!.GetValue<string>()))];

    private static MultipartFormDataContent Form(byte[] file, string fileName, IEnumerable<(string Name, string Value)> fields)
    {
        var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(XlsxType);
        content.Add(part, "file", fileName);
        foreach (var (name, value) in fields)
        {
            content.Add(new StringContent(value), name);
        }

        return content;
    }
}

/// <summary>
/// Archives that are not workbooks, for the tests of the upload limits.
/// </summary>
public static class OrgImportArchives
{
    /// <summary>
    /// A ZIP of one part that inflates to the given number of zero bytes and is small on disk, the shape of a ZIP bomb.
    /// </summary>
    /// <param name="unpackedBytes">The size the part inflates to.</param>
    /// <returns>The bytes of the archive.</returns>
    public static byte[] OfZeros(long unpackedBytes)
    {
        using var stream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", System.IO.Compression.CompressionLevel.SmallestSize).Open();
            var block = new byte[1024 * 1024];
            for (long written = 0; written < unpackedBytes; written += block.Length)
            {
                entry.Write(block);
            }
        }

        return stream.ToArray();
    }
}
