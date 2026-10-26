using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Competency.OrgStructure.Import;

/// <summary>
/// One level of a unit path: the cleaned name and the number of the level column it stood in.
/// </summary>
/// <param name="Name">The cleaned unit name.</param>
/// <param name="Level">The level number of the column, counting from 1.</param>
internal sealed record UnitStep(string Name, int Level);

/// <summary>
/// The parts of an employee's full name as the file gives them.
/// </summary>
/// <param name="LastName">The first word.</param>
/// <param name="FirstName">The second word.</param>
/// <param name="MiddleName">The remaining words; <see langword="null"/> when there are none.</param>
internal sealed record PersonName(string LastName, string FirstName, string? MiddleName)
{
    /// <summary>
    /// The comparison key of the whole name, equal for names that differ only in letter case, «ё» versus «е» and spacing.
    /// </summary>
    public string Key => ImportText.Key(string.Join(' ', new[] { LastName, FirstName, MiddleName }.Where(part => part is not null)));
}

/// <summary>
/// One recognised row of the file.
/// </summary>
/// <param name="Number">The row number in the sheet.</param>
/// <param name="Path">The unit path from the root, the empty levels skipped; never empty.</param>
/// <param name="Person">The employee of the row; <see langword="null"/> for a row that declares a unit or a position of a unit.</param>
/// <param name="Position">The cleaned position name, if the row has one.</param>
/// <param name="LegalEntity">The cleaned legal entity name of an employee row, if it has one.</param>
internal sealed record ImportRow(int Number, IReadOnlyList<UnitStep> Path, PersonName? Person, string? Position, string? LegalEntity);

/// <summary>
/// A problem that makes the file unusable, located as far as it can be.
/// </summary>
/// <param name="Row">The row number; <see langword="null"/> for a problem of the file or of the data as a whole.</param>
/// <param name="Column">The column name; <see langword="null"/> when no single column is at fault.</param>
/// <param name="Message">The reason, addressed to the user.</param>
internal sealed record ImportError(int? Row, string? Column, string Message);

/// <summary>
/// The result of reading a file: the rows when it is usable, the errors when it is not.
/// </summary>
/// <param name="Rows">The recognised rows; empty when there are errors.</param>
/// <param name="Errors">Every problem found, up to <see cref="OrgImportParser.MaxReportedErrors"/>.</param>
internal sealed record ParsedImport(IReadOnlyList<ImportRow> Rows, IReadOnlyList<ImportError> Errors);

/// <summary>
/// Reads the uploaded workbook into rows, checking the container first and every row against the row rules; all or nothing,
/// with every problem reported. Cells are read as text without formats, and a formula is refused instead of being evaluated.
/// </summary>
internal static partial class OrgImportParser
{
    public const int MaxReportedErrors = 10_000;
    public const string UnitColumnPrefix = "Подразделение";
    public const string FullNameColumn = "ФИО";
    public const string PositionColumn = "Должность";
    public const string LegalEntityColumn = "Юридическое лицо";

    private const string SkippedLevel = "-";
    private const int PersonNameMaxLength = Employee.NameMaxLength;
    private const int NameMaxLength = OrgUnit.NameMaxLength;

    /// <summary>
    /// Reads the file. A file the library cannot open for any reason is a recognition error, never an exception.
    /// </summary>
    /// <param name="content">The uploaded file.</param>
    /// <returns>The rows, or the errors.</returns>
    public static ParsedImport Parse(byte[] content)
    {
        if (XlsxArchiveGuard.Problem(content) is { } archiveProblem)
        {
            return Failed(new ImportError(null, null, archiveProblem));
        }

        try
        {
            using var workbook = new XLWorkbook(new MemoryStream(content, writable: false));
            return workbook.Worksheets.FirstOrDefault() is { } sheet
                ? ReadSheet(sheet)
                : Failed(new ImportError(null, null, "В книге нет листов."));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Failed(new ImportError(null, null, XlsxArchiveGuard.NotWorkbookMessage));
        }
    }

    /// <summary>
    /// The name of the unit column of a level, as the header spells it.
    /// </summary>
    /// <param name="level">The level number, counting from 1.</param>
    /// <returns>The column name.</returns>
    public static string UnitColumn(int level) => $"{UnitColumnPrefix} {level.ToString(CultureInfo.InvariantCulture)}-го уровня";

    [GeneratedRegex(@"^подразделение (\d{1,4})-го уровня$")]
    private static partial Regex UnitColumnPattern();

    private static ParsedImport Failed(params ImportError[] errors) => new([], errors);

    private static ParsedImport ReadSheet(IXLWorksheet sheet)
    {
        var rows = sheet.RowsUsed(XLCellsUsedOptions.Contents).ToList();
        if (rows.Count == 0)
        {
            return Failed(new ImportError(null, null, "Лист пуст."));
        }

        var headerRow = rows[0];
        var columns = ReadHeader(headerRow, out var headerErrors);
        if (headerErrors.Count > 0)
        {
            return new ParsedImport([], headerErrors);
        }

        var recognised = new List<ImportRow>();
        var errors = new List<ImportError>();
        foreach (var row in rows.Skip(1))
        {
            if (errors.Count >= MaxReportedErrors)
            {
                errors.Add(new ImportError(null, null, $"Показаны первые {MaxReportedErrors} ошибок: исправьте их и загрузите файл снова."));
                break;
            }

            ReadRow(row, columns, recognised, errors);
        }

        return errors.Count > 0 ? new ParsedImport([], errors) : new ParsedImport(recognised, []);
    }

    private static HeaderColumns ReadHeader(IXLRow headerRow, out List<ImportError> errors)
    {
        errors = [];
        var number = headerRow.RowNumber();
        var levels = new SortedDictionary<int, int>();
        var named = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed(XLCellsUsedOptions.Contents))
        {
            var key = ImportText.Key(ImportText.Clean(cell.GetString()));
            var column = cell.Address.ColumnNumber;
            if (UnitColumnPattern().Match(key) is { Success: true } match)
            {
                var level = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!levels.TryAdd(level, column))
                {
                    errors.Add(new ImportError(number, UnitColumn(level), "Колонка повторяется."));
                }
            }
            else if (key is not "" && !named.TryAdd(key, column) && IsRequired(key))
            {
                errors.Add(new ImportError(number, ImportText.Clean(cell.GetString()), "Колонка повторяется."));
            }
        }

        var maxLevel = levels.Count == 0 ? 0 : levels.Keys.Max();
        var firstMissing = Enumerable.Range(1, Math.Max(maxLevel, 1)).FirstOrDefault(level => !levels.ContainsKey(level));
        if (firstMissing != 0)
        {
            errors.Add(new ImportError(number, UnitColumn(firstMissing), $"В файле нет колонки «{UnitColumn(firstMissing)}»."));
        }

        var fullName = Required(named, FullNameColumn, number, errors);
        var position = Required(named, PositionColumn, number, errors);
        var legalEntity = Required(named, LegalEntityColumn, number, errors);
        return new HeaderColumns([.. levels.Select(pair => (pair.Key, pair.Value))], fullName, position, legalEntity);
    }

    private static bool IsRequired(string key) =>
        key == ImportText.Key(FullNameColumn) || key == ImportText.Key(PositionColumn) || key == ImportText.Key(LegalEntityColumn);

    private static int Required(Dictionary<string, int> named, string column, int headerNumber, List<ImportError> errors)
    {
        if (named.TryGetValue(ImportText.Key(column), out var number))
        {
            return number;
        }

        errors.Add(new ImportError(headerNumber, column, $"В файле нет колонки «{column}»."));
        return 0;
    }

    private static void ReadRow(IXLRow row, HeaderColumns columns, List<ImportRow> recognised, List<ImportError> errors)
    {
        var number = row.RowNumber();
        var problems = new List<ImportError>();
        var path = new List<UnitStep>();
        foreach (var (level, column) in columns.Levels)
        {
            var name = Read(row, column, UnitColumn(level), number, problems);
            if (name is "" or SkippedLevel)
            {
                continue;
            }

            if (name.Length > NameMaxLength)
            {
                problems.Add(new ImportError(number, UnitColumn(level), $"Название длиннее {NameMaxLength} символов."));
            }

            path.Add(new UnitStep(name, level));
        }

        var fullName = Read(row, columns.FullName, FullNameColumn, number, problems);
        var position = Read(row, columns.Position, PositionColumn, number, problems);
        var legalEntity = Read(row, columns.LegalEntity, LegalEntityColumn, number, problems);
        if (path.Count == 0 && (fullName.Length > 0 || position.Length > 0 || legalEntity.Length > 0 || problems.Count > 0))
        {
            problems.Add(new ImportError(number, UnitColumn(columns.Levels[0].Level), "Все колонки подразделений пусты."));
        }

        var person = fullName.Length > 0 ? ReadPerson(fullName, position, number, problems) : null;
        CheckLength(position, PositionColumn, NameMaxLength, number, problems);
        CheckLength(legalEntity, LegalEntityColumn, NameMaxLength, number, problems);

        errors.AddRange(problems);
        if (problems.Count == 0 && path.Count > 0)
        {
            recognised.Add(new ImportRow(number, path, person, position.Length > 0 ? position : null, person is not null && legalEntity.Length > 0 ? legalEntity : null));
        }
    }

    private static PersonName? ReadPerson(string fullName, string position, int number, List<ImportError> problems)
    {
        if (position.Length == 0)
        {
            problems.Add(new ImportError(number, PositionColumn, "У сотрудника не указана должность."));
        }

        var words = fullName.Split(' ');
        if (words.Length < 2)
        {
            problems.Add(new ImportError(number, FullNameColumn, "ФИО должно содержать фамилию и имя."));
            return null;
        }

        var middle = words.Length > 2 ? string.Join(' ', words.Skip(2)) : null;
        foreach (var (part, label) in new[] { (words[0], "Фамилия"), (words[1], "Имя"), (middle, "Отчество") })
        {
            if (part?.Length > PersonNameMaxLength)
            {
                problems.Add(new ImportError(number, FullNameColumn, $"{label} длиннее {PersonNameMaxLength} символов."));
            }
        }

        return new PersonName(words[0], words[1], middle);
    }

    private static void CheckLength(string value, string column, int maxLength, int number, List<ImportError> problems)
    {
        if (value.Length > maxLength)
        {
            problems.Add(new ImportError(number, column, $"Значение длиннее {maxLength} символов."));
        }
    }

    private static string Read(IXLRow row, int column, string columnName, int number, List<ImportError> problems)
    {
        var cell = row.Cell(column);
        if (cell.HasFormula)
        {
            problems.Add(new ImportError(number, columnName, "Ячейка содержит формулу: замените её значением."));
            return string.Empty;
        }

        return ImportText.Clean(cell.GetString());
    }

    private sealed record HeaderColumns(IReadOnlyList<(int Level, int Column)> Levels, int FullName, int Position, int LegalEntity);
}
