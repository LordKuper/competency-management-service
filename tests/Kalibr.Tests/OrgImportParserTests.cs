using System.IO.Compression;
using System.Text;
using AwesomeAssertions;
using ClosedXML.Excel;
using Kalibr.OrgStructure.Import;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// How the uploaded workbook is read: the columns it must have, the rules of a row, the full list of errors, and the protection of the server from
/// files that are not what they claim to be. The parser runs in the test's own process, so these cases need no database.
/// </summary>
public sealed class OrgImportParserTests
{
    private const long OverTheUnpackedLimit = 257L * 1024 * 1024;
    private const int LongestName = 200;

    [Fact]
    public void Ac11_TheColumns_AreFoundByNameRegardlessOfLetterCaseAndSpacing_AndOtherColumnsAreIgnored()
    {
        var sheet = new OrgImportSheet { Header = ["  ПОДРАЗДЕЛЕНИЕ   1-го  УРОВНЯ ", "подразделение 2-го уровня", "фио", " ДОЛЖНОСТЬ", "юридическое   лицо", "Комментарий"] }
            .Employee("Иванов Иван", "Инженер", "Отдел", "Группа");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().BeEmpty();
        var row = parsed.Rows.Single();
        row.Path.Select(step => step.Name).Should().Equal("Отдел", "Группа");
        row.Person!.LastName.Should().Be("Иванов");
        row.Position.Should().Be("Инженер");
    }

    [Theory]
    [InlineData("ФИО", "ФИО")]
    [InlineData("Должность", "Должность")]
    [InlineData("Юридическое лицо", "Юридическое лицо")]
    public void Ac11_AMissingColumn_IsAnErrorThatNamesIt(string missing, string named)
    {
        string?[] header = [.. new[] { "Подразделение 1-го уровня", "Подразделение 2-го уровня", "ФИО", "Должность", "Юридическое лицо" }.Where(column => column != missing)];
        var sheet = new OrgImportSheet { Header = header };

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().ContainSingle().Which.Column.Should().Be(named);
    }

    [Fact]
    public void Ac11_TheLevelColumns_MustRunWithoutAGapFromTheFirst_AndAGapIsNamedByItsLevel()
    {
        var sheet = new OrgImportSheet { Header = ["Подразделение 1-го уровня", "Подразделение 3-го уровня", "ФИО", "Должность", "Юридическое лицо"] };

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().ContainSingle().Which.Column.Should().Be("Подразделение 2-го уровня");
    }

    [Fact]
    public void Ac11_AHeaderWithOneLevelColumnOverTheLimit_IsAnErrorOfTheHeaderRow_NamingThatColumn()
    {
        var overTheLimit = OrgImportParser.MaxLevels + 1;
        var sheet = new OrgImportSheet(overTheLimit).Employee("Иванов Иван", "Инженер", "Отдел");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Rows.Should().BeEmpty();
        var error = parsed.Errors.Should().ContainSingle().Subject;
        error.Row.Should().Be(1);
        error.Column.Should().Be(OrgImportParser.UnitColumn(overTheLimit));
        error.Message.Should().Contain(OrgImportParser.MaxLevels.ToString());
    }

    [Fact]
    public void Ac11_AHeaderWithExactlyTheLimitOfLevelColumns_IsAccepted()
    {
        var sheet = new OrgImportSheet(OrgImportParser.MaxLevels).Employee("Иванов Иван", "Инженер", "Отдел");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Single().Path.Should().Equal([new UnitStep("Отдел", 1)]);
    }

    [Fact]
    public void Ac12_TheUnitPath_SkipsDashesAndEmptyCells_KeepsTheLevelsAndCleansTheSpaces()
    {
        var sheet = new OrgImportSheet(4)
            .Add(["  Блок   А ", "-", "Отдел", null], "Иванов Иван", "Инженер")
            .Add([null, "Отдел", "-", "Группа"], null, "Аналитик");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().BeEmpty();
        parsed.Rows[0].Path.Should().Equal([new UnitStep("Блок А", 1), new UnitStep("Отдел", 3)]);
        parsed.Rows[1].Path.Should().Equal([new UnitStep("Отдел", 2), new UnitStep("Группа", 4)]);
    }

    [Fact]
    public void Ac12_ARowDeclaresAUnitOrAPositionOfAUnitOrAnEmployee_ByWhichOfTheNameAndThePositionItHas()
    {
        var sheet = new OrgImportSheet()
            .Unit("Отдел")
            .Add(["Отдел"], null, "Аналитик", "ООО Ромашка")
            .Employee("Петров Пётр Петрович", "Инженер", "Отдел");

        var rows = OrgImportParser.Parse(sheet.ToBytes()).Rows;

        rows[0].Person.Should().BeNull();
        rows[0].Position.Should().BeNull();
        rows[1].Person.Should().BeNull("a row without a name only adds the position to the unit's list");
        rows[1].Position.Should().Be("Аналитик");
        rows[1].LegalEntity.Should().BeNull("the legal entity belongs to an employee's assignment");
        rows[2].Person.Should().Be(new PersonName("Петров", "Пётр", "Петрович"));
    }

    [Fact]
    public void Ac12_TheFullName_IsTheLastNameTheFirstNameAndEveryFurtherWordAsTheMiddleName()
    {
        var sheet = new OrgImportSheet()
            .Employee("Иванов Иван", "Инженер", "Отдел")
            .Employee("Али оглы  Ахмед Бей", "Инженер", "Отдел");

        var rows = OrgImportParser.Parse(sheet.ToBytes()).Rows;

        rows[0].Person.Should().Be(new PersonName("Иванов", "Иван", null));
        rows[1].Person.Should().Be(new PersonName("Али", "оглы", "Ахмед Бей"));
    }

    [Fact]
    public void Ac12_TheNamesOfTheSameWordsInTheOtherLetterCaseAndYo_HaveOneComparisonKey()
    {
        new PersonName("Фёдоров", "Пётр", null).Key.Should().Be(new PersonName("ФЕДОРОВ", "ПЕТР", null).Key);
    }

    [Fact]
    public void Ac12_ARowOfEmptyLevelsThatCarriesAnyOtherCell_IsAnError_ButAWhollyEmptyRowIsIgnored()
    {
        var sheet = new OrgImportSheet()
            .Add([null, "-"], "Иванов Иван", "Инженер")
            .Add([null, null]);

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().ContainSingle();
        parsed.Errors[0].Row.Should().Be(2);
        parsed.Errors[0].Column.Should().Be("Подразделение 1-го уровня");
    }

    [Theory]
    [InlineData("Иванов", "Инженер", "ФИО")]
    [InlineData("Иванов Иван", null, "Должность")]
    public void Ac12_AnEmployeeRow_NeedsAFullNameOfTwoWordsAndAPosition(string fullName, string? position, string column)
    {
        var sheet = new OrgImportSheet().Add(["Отдел"], fullName, position);

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Rows.Should().BeEmpty();
        parsed.Errors.Should().ContainSingle(error => error.Column == column && error.Row == 2);
    }

    [Theory]
    [InlineData("Подразделение 1-го уровня")]
    [InlineData("Должность")]
    [InlineData("Юридическое лицо")]
    public void Ac12_ANameLongerThanTheLimit_IsAnErrorOfItsColumn(string column)
    {
        var tooLong = new string('я', LongestName + 1);
        var sheet = column switch
        {
            "Подразделение 1-го уровня" => new OrgImportSheet().Unit(tooLong),
            "Должность" => new OrgImportSheet().Add(["Отдел"], null, tooLong),
            _ => new OrgImportSheet().Add(["Отдел"], "Иванов Иван", "Инженер", tooLong),
        };

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Errors.Should().ContainSingle().Which.Column.Should().Be(column);
    }

    [Theory]
    [InlineData("Фамилия", 101, 1, 1)]
    [InlineData("Имя", 1, 101, 1)]
    [InlineData("Отчество", 1, 1, 101)]
    public void Ac12_APartOfTheFullNameLongerThanTheLimit_IsAnErrorOfTheNameColumn(string part, int last, int first, int middle)
    {
        var fullName = $"{new string('я', last)} {new string('я', first)} {new string('я', middle)}";
        var sheet = new OrgImportSheet().Add(["Отдел"], fullName, "Инженер");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        var error = parsed.Errors.Should().ContainSingle().Subject;
        error.Column.Should().Be("ФИО");
        error.Message.Should().StartWith(part);
    }

    [Fact]
    public void Ac13_EveryProblemOfTheFile_IsReportedWithItsRowAndColumn_AndNoRowIsKept()
    {
        var sheet = new OrgImportSheet()
            .Employee("Иванов Иван", "Инженер", "Отдел")
            .Add(["Отдел"], "Одиночка", "Инженер")
            .Add([null, null], "Без Подразделения", "Инженер")
            .Add(["Отдел"], "Без Должности Совсем");

        var parsed = OrgImportParser.Parse(sheet.ToBytes());

        parsed.Rows.Should().BeEmpty("a file with errors is refused whole");
        parsed.Errors.Select(error => $"{error.Row}: {error.Column}").Should().BeEquivalentTo(["3: ФИО", "4: Подразделение 1-го уровня", "5: Должность"]);
    }

    [Fact]
    public void Ac12_AFormulaCell_IsRefusedInsteadOfBeingEvaluated()
    {
        var sheet = new OrgImportSheet().Employee("Иванов Иван", "Инженер", "Отдел");

        var parsed = OrgImportParser.Parse(sheet.ToBytes(worksheet => worksheet.Cell(2, 4).FormulaA1 = "1+1"));

        parsed.Errors.Should().Contain(error => error.Row == 2 && error.Column == "Должность" && error.Message.Contains("формул", StringComparison.Ordinal));
    }

    [Fact]
    public void Ac10_ABookWithNoRowsAtAll_IsRefusedAsEmpty()
    {
        using var workbook = new XLWorkbook();
        workbook.Worksheets.Add("Пусто");
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var parsed = OrgImportParser.Parse(stream.ToArray());

        parsed.Errors.Should().ContainSingle().Which.Row.Should().BeNull();
    }

    [Fact]
    public void Ac10_OnlyTheFirstSheet_IsRead()
    {
        using var workbook = new XLWorkbook();
        var first = workbook.Worksheets.Add("Первый");
        first.Cell(1, 1).Value = "Подразделение 1-го уровня";
        first.Cell(1, 2).Value = "ФИО";
        first.Cell(1, 3).Value = "Должность";
        first.Cell(1, 4).Value = "Юридическое лицо";
        first.Cell(2, 1).Value = "Отдел";
        var second = workbook.Worksheets.Add("Второй");
        second.Cell(1, 1).Value = "Мусор";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var parsed = OrgImportParser.Parse(stream.ToArray());

        parsed.Errors.Should().BeEmpty();
        parsed.Rows.Should().ContainSingle().Which.Path.Single().Name.Should().Be("Отдел");
    }

    [Theory]
    [InlineData("not a zip at all")]
    [InlineData("")]
    public void Ac10_AFileThatIsNotAWorkbook_IsRefusedWithTheReason_NotAnException(string text)
    {
        var parsed = OrgImportParser.Parse(Encoding.UTF8.GetBytes(text));

        parsed.Errors.Should().ContainSingle().Which.Message.Should().Be(XlsxArchiveGuard.NotWorkbookMessage);
    }

    [Fact]
    public void Ac10_ATruncatedWorkbook_IsRefusedWithTheReason()
    {
        var bytes = new OrgImportSheet().Employee("Иванов Иван", "Инженер", "Отдел").ToBytes();

        var parsed = OrgImportParser.Parse(bytes[..(bytes.Length / 2)]);

        parsed.Errors.Should().ContainSingle().Which.Message.Should().Be(XlsxArchiveGuard.NotWorkbookMessage);
    }

    [Fact]
    public void Ac10_AZipThatIsNotAWorkbook_IsRefusedWithTheReason()
    {
        var parsed = OrgImportParser.Parse(Zip(("readme.txt", Encoding.UTF8.GetBytes("hello"))));

        parsed.Errors.Should().ContainSingle().Which.Message.Should().Be(XlsxArchiveGuard.NotWorkbookMessage);
    }

    [Fact]
    public void Ac10_AZipWithMoreEntriesThanTheLimit_IsRefusedBeforeItIsRead()
    {
        var entries = Enumerable.Range(0, XlsxArchiveGuard.MaxEntries + 1).Select(index => ($"part{index}.xml", Array.Empty<byte>())).ToArray();

        var parsed = OrgImportParser.Parse(Zip(entries));

        parsed.Errors.Should().ContainSingle().Which.Message.Should().Be(XlsxArchiveGuard.TooLargeMessage);
    }

    [Fact]
    public void Ac10_AZipThatInflatesBeyondTheLimit_IsRefused_WhateverItsSmallSize()
    {
        var bomb = OrgImportArchives.OfZeros(OverTheUnpackedLimit);

        var parsed = OrgImportParser.Parse(bomb);

        bomb.Length.Should().BeLessThan(10 * 1024 * 1024, "the file is small, which is what makes it a bomb");
        parsed.Errors.Should().ContainSingle().Which.Message.Should().Be(XlsxArchiveGuard.TooLargeMessage);
    }

    [Fact]
    public void Ac10_AnEntityDeclaredInTheDocumentTypeOfAnXmlPart_IsNeverExpanded()
    {
        var bytes = WithSharedStrings(
            new OrgImportSheet().Employee("Иванов Иван", "МАРКЕР", "Отдел").ToBytes(),
            text => text.Replace("<sst ", "<!DOCTYPE sst [<!ENTITY bomb \"РАСКРЫТО\">]><sst ", StringComparison.Ordinal)
                .Replace("МАРКЕР", "&bomb;", StringComparison.Ordinal));

        var parsed = OrgImportParser.Parse(bytes);

        parsed.Rows.Select(row => row.Position).Should().NotContain("РАСКРЫТО");
        parsed.Errors.Should().NotBeEmpty("a part with a document type declaration is not a workbook this import reads");
    }

    [Fact]
    public void Ac10_AnExternalEntityOfAnXmlPart_NeverReadsAFileOfTheServer()
    {
        var secret = Path.Combine(Path.GetTempPath(), $"competency-xxe-{Guid.NewGuid():N}.txt");
        File.WriteAllText(secret, "СЕКРЕТ-СЕРВЕРА");
        try
        {
            var bytes = WithSharedStrings(
                new OrgImportSheet().Employee("Иванов Иван", "МАРКЕР", "Отдел").ToBytes(),
                text => text.Replace("<sst ", $"<!DOCTYPE sst [<!ENTITY leak SYSTEM \"{new Uri(secret).AbsoluteUri}\">]><sst ", StringComparison.Ordinal)
                    .Replace("МАРКЕР", "&leak;", StringComparison.Ordinal));

            var parsed = OrgImportParser.Parse(bytes);

            parsed.Rows.Select(row => row.Position).Should().NotContain(position => position != null && position.Contains("СЕКРЕТ", StringComparison.Ordinal));
            parsed.Errors.Select(error => error.Message).Should().NotContain(message => message.Contains("СЕКРЕТ", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(secret);
        }
    }

    [Fact]
    public void Ac10_AnExpandingEntityChain_IsNotExpandedIntoMemory()
    {
        const string chain = "<!DOCTYPE sst [<!ENTITY a \"яяяяяяяяяя\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\"><!ENTITY c \"&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;\"><!ENTITY d \"&c;&c;&c;&c;&c;&c;&c;&c;&c;&c;\">]>";
        var bytes = WithSharedStrings(
            new OrgImportSheet().Employee("Иванов Иван", "МАРКЕР", "Отдел").ToBytes(),
            text => text.Replace("<sst ", $"{chain}<sst ", StringComparison.Ordinal).Replace("МАРКЕР", "&d;", StringComparison.Ordinal));

        var parsed = OrgImportParser.Parse(bytes);

        parsed.Rows.Select(row => row.Position?.Length ?? 0).Should().OnlyContain(length => length < 1000);
    }

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var entry = archive.CreateEntry(name).Open();
                entry.Write(content);
            }
        }

        return stream.ToArray();
    }

    private static byte[] WithSharedStrings(byte[] workbook, Func<string, string> change)
    {
        using var source = new ZipArchive(new MemoryStream(workbook), ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var target = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in source.Entries)
            {
                using var input = entry.Open();
                using var copy = target.CreateEntry(entry.FullName).Open();
                if (entry.FullName == "xl/sharedStrings.xml")
                {
                    using var reader = new StreamReader(input, Encoding.UTF8);
                    var original = reader.ReadToEnd();
                    var changed = change(original);
                    changed.Should().NotBe(original, "the test must change the part it means to attack");
                    copy.Write(Encoding.UTF8.GetBytes(changed));
                }
                else
                {
                    input.CopyTo(copy);
                }
            }
        }

        return output.ToArray();
    }
}
