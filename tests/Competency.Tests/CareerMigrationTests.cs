using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The career catalogue migrations that move or drop data run on rows written at the schema before them: the initial catalogue data, the
/// removal of the unit from catalogue entries, the removal of the salary range and the reduction of a level to its place in the track.
/// </summary>
public sealed class CareerMigrationTests(TestEnvironment environment)
{
    private const string SourceFile = "docs/Постановка.md";
    private const string Direction = "00000000-0000-0000-0000-0000000000d1";
    private const string Category = "00000000-0000-0000-0000-0000000000c1";
    private const string Root = "00000000-0000-0000-0000-0000000000a1";
    private const string Child = "00000000-0000-0000-0000-0000000000a2";
    private const string OtherScale = "00000000-0000-0000-0000-0000000000e1";
    private const string TrackOfNoUnit = "00000000-0000-0000-0000-0000000000f1";
    private const string TrackOfChild = "00000000-0000-0000-0000-0000000000f2";
    private const string TrackOfSecondChild = "00000000-0000-0000-0000-0000000000f3";

    [Fact]
    public async Task Ac12_Catalogues_StartWithTheTwelveCategoriesAndTheBaseScaleOfTheSource_AndNothingInTheJournal()
    {
        await using var database = await MigrationDatabase.CreateAsync(environment);

        await database.MigrateToAsync(MigrationDatabase.Catalogues);

        var (categories, scale) = ReadSource();
        categories.Should().HaveCount(12, "the source lists twelve example categories");
        scale.Should().HaveCount(5, "the base scale of the source has five points");
        var seeded = (await database.RowsAsync("SELECT name, status, author_id FROM competency_categories ORDER BY name")).ToList();
        seeded.Select(row => row[0]!.ToLowerInvariant()).Should().BeEquivalentTo(categories, "the names are those of the source, each starting with a capital");
        seeded.Should().OnlyContain(row => row[1] == "Active" && row[2] == null);
        var points = await database.ScalarAsync<string>("SELECT points::text FROM competency_scales WHERE id = '3004a734-475a-4e4c-b6a6-18275f5a1d5c'");
        System.Text.Json.JsonDocument.Parse(points).RootElement.EnumerateArray()
            .Select(point => (point.GetProperty("score").GetInt32(), point.GetProperty("label").GetString()))
            .Should().Equal(scale, "the base scale has the points and the texts of the source word for word");
        (await database.ScalarAsync<long>("SELECT count(*) FROM audit_events")).Should().Be(0, "initial data is not journaled");
    }

    [Fact]
    public async Task Ac9_Ownership_GivesATrackOfNoUnitTheRootUnit_DropsTheUnitFromOtherEntries_AndMarksTheBaseScaleAsDefault()
    {
        await using var database = await ArrangeLevelsAsync(MigrationDatabase.BeforeOwnership);
        await database.ExecuteAsync($"""
            INSERT INTO competency_scales (id, points, version, created_at, name, owner_org_unit_id, status, author_id)
            VALUES ('{OtherScale}', '[]', 0, now(), 'Другая шкала', '{Child}', 'Active', NULL)
            """);

        await database.MigrateToAsync(MigrationDatabase.Ownership);

        (await database.RowsAsync($"SELECT id, owner_org_unit_id FROM career_tracks ORDER BY id")).Select(row => (row[0], row[1]))
            .Should().Equal([(TrackOfNoUnit, Root), (TrackOfChild, Child), (TrackOfSecondChild, Child)], "a track without a unit goes to the root unit and the others keep their own");
        foreach (var table in new[] { "career_directions", "career_levels", "competencies", "competency_categories", "competency_scales" })
        {
            (await database.ScalarAsync<long>($"SELECT count(*) FROM information_schema.columns WHERE table_name = '{table}' AND column_name = 'owner_org_unit_id'"))
                .Should().Be(0, table);
        }

        (await database.RowsAsync("SELECT id, is_default FROM competency_scales ORDER BY id")).Select(row => (row[0], row[1]))
            .Should().Equal([("00000000-0000-0000-0000-0000000000e1", "False"), (CareerScenarios.BaseScaleId, "True")], "only the base scale is the default");
        await database.Awaiting(db => db.ExecuteAsync($"UPDATE competency_scales SET is_default = true WHERE id = '{OtherScale}'"))
            .Should().ThrowAsync<PostgresException>("a second default scale breaks the unique index");
    }

    [Fact]
    public async Task Ac9_Ownership_Down_RestoresTheUnitColumnsEmpty_AndLetsATrackHaveNoUnit()
    {
        await using var database = await ArrangeLevelsAsync(MigrationDatabase.BeforeOwnership);
        await database.MigrateToAsync(MigrationDatabase.Ownership);

        await database.MigrateToAsync(MigrationDatabase.BeforeOwnership);

        (await database.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_name = 'competency_scales' AND column_name = 'is_default'")).Should().Be(0);
        (await database.ScalarAsync<long>("SELECT count(*) FROM career_levels WHERE owner_org_unit_id IS NULL")).Should().Be(await database.ScalarAsync<long>("SELECT count(*) FROM career_levels"));
        (await database.ScalarAsync<long>("SELECT count(*) FROM career_tracks")).Should().Be(3);
    }

    [Fact]
    public async Task Ac11_Salary_Removal_DropsTheColumnsAndKeepsTheLevels_AndDownRestoresThemEmpty()
    {
        await using var database = await ArrangeLevelsAsync(MigrationDatabase.Ownership);
        await database.ExecuteAsync("UPDATE career_levels SET salary_from = 100000.50, salary_to = 200000");

        await database.MigrateToAsync(MigrationDatabase.Salary);

        (await database.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_name = 'career_levels' AND column_name LIKE 'salary%'")).Should().Be(0);
        (await database.ScalarAsync<long>("SELECT count(*) FROM career_levels")).Should().Be(8, "the levels stay");
        await database.MigrateToAsync(MigrationDatabase.Ownership);
        (await database.ScalarAsync<long>("SELECT count(*) FROM career_levels WHERE salary_from IS NULL AND salary_to IS NULL")).Should().Be(8);
    }

    [Fact]
    public async Task Ac11_Levels_Simplification_NumbersEachTrackByTheOldChainOfPreviousLevels()
    {
        await using var database = await ArrangeLevelsAsync(MigrationDatabase.Salary);

        await database.MigrateToAsync(MigrationDatabase.Levels);

        var numbered = await database.RowsAsync("SELECT t.name, l.position, l.name FROM career_levels l JOIN career_tracks t ON t.id = l.track_id ORDER BY t.name, l.position");
        numbered.Select(row => $"{row[0]}:{row[1]}:{row[2]}").Should().Equal(
            "Трек А:1:Младший",
            "Трек А:2:Стажёр",
            "Трек А:3:Средний",
            "Трек А:4:Старший",
            "Трек Б:1:В",
            "Трек Б:2:Б",
            "Трек Б:3:Я",
            "Трек В:1:Первый");
        foreach (var column in new[] { "code", "is_managerial", "next_level_ids", "previous_level_id", "role_description", "valid_from", "valid_to" })
        {
            (await database.ScalarAsync<long>($"SELECT count(*) FROM information_schema.columns WHERE table_name = 'career_levels' AND column_name = '{column}'")).Should().Be(0, column);
        }

        await database.Awaiting(db => db.ExecuteAsync("UPDATE career_levels SET position = 1 WHERE name = 'Стажёр'"))
            .Should().ThrowAsync<PostgresException>("a place in the track is held by one level");
    }

    [Fact]
    public async Task Ac11_Levels_Simplification_Down_RestoresTheDroppedColumnsEmpty()
    {
        await using var database = await ArrangeLevelsAsync(MigrationDatabase.Salary);
        await database.MigrateToAsync(MigrationDatabase.Levels);

        await database.MigrateToAsync(MigrationDatabase.Salary);

        (await database.ScalarAsync<long>("SELECT count(*) FROM career_levels WHERE code = '' AND previous_level_id IS NULL AND cardinality(next_level_ids) = 0 AND NOT is_managerial")).Should().Be(8);
    }

    private static (List<string> Categories, List<(int, string?)> Scale) ReadSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("Competency.slnx").Any())
        {
            directory = directory.Parent;
        }

        var lines = File.ReadAllLines(Path.Combine(directory?.FullName ?? throw new FileNotFoundException("Competency.slnx"), SourceFile));
        var categories = lines
            .SkipWhile(line => !line.StartsWith("Примеры категорий", StringComparison.Ordinal))
            .Skip(1)
            .SkipWhile(string.IsNullOrWhiteSpace)
            .TakeWhile(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..].TrimEnd(';', '.').ToLowerInvariant())
            .ToList();
        var scale = lines
            .SkipWhile(line => !line.StartsWith("Базовая шкала", StringComparison.Ordinal))
            .TakeWhile(line => !line.StartsWith("Система", StringComparison.Ordinal))
            .Where(line => line.Length > 2 && line.StartsWith("| ", StringComparison.Ordinal) && char.IsDigit(line[2]))
            .Select(line => line.Trim('|', ' ').Split('|', 2, StringSplitOptions.TrimEntries))
            .Select(cells => (int.Parse(cells[0]), (string?)cells[1]))
            .ToList();
        return (categories, scale);
    }

    private async Task<MigrationDatabase> ArrangeLevelsAsync(string state)
    {
        var database = await MigrationDatabase.CreateAsync(environment);
        await database.MigrateToAsync(MigrationDatabase.Matrices);
        await database.ExecuteAsync($"""
            INSERT INTO org_units (id, name, parent_id, head_employee_id, is_active, version, created_at) VALUES
                ('{Root}', 'Корень', NULL, NULL, true, 0, now()),
                ('{Child}', 'Дочернее', '{Root}', NULL, true, 0, now());
            INSERT INTO career_directions (id, version, created_at, name, owner_org_unit_id, status, author_id)
                VALUES ('{Direction}', 0, now(), 'Направление', '{Child}', 'Active', NULL);
            INSERT INTO competency_categories (id, version, created_at, name, owner_org_unit_id, status, author_id)
                VALUES ('{Category}', 0, now(), 'Своя категория', '{Child}', 'Active', NULL);
            INSERT INTO career_tracks (id, direction_id, version, created_at, name, owner_org_unit_id, status, author_id) VALUES
                ('{TrackOfNoUnit}', '{Direction}', 0, now(), 'Трек А', NULL, 'Active', NULL),
                ('{TrackOfChild}', '{Direction}', 0, now(), 'Трек Б', '{Child}', 'Active', NULL),
                ('{TrackOfSecondChild}', '{Direction}', 0, now(), 'Трек В', '{Child}', 'Active', NULL);
            """);
        var levels = new (string Id, string Track, string Name, string? Previous)[]
        {
            ("00000000-0000-0000-0000-000000000101", TrackOfNoUnit, "Младший", null),
            ("00000000-0000-0000-0000-000000000102", TrackOfNoUnit, "Средний", "00000000-0000-0000-0000-000000000101"),
            ("00000000-0000-0000-0000-000000000103", TrackOfNoUnit, "Старший", "00000000-0000-0000-0000-000000000102"),
            ("00000000-0000-0000-0000-000000000104", TrackOfNoUnit, "Стажёр", null),
            ("00000000-0000-0000-0000-000000000201", TrackOfChild, "Я", "00000000-0000-0000-0000-000000000202"),
            ("00000000-0000-0000-0000-000000000202", TrackOfChild, "Б", "00000000-0000-0000-0000-000000000201"),
            ("00000000-0000-0000-0000-000000000203", TrackOfChild, "В", null),
            ("00000000-0000-0000-0000-000000000301", TrackOfSecondChild, "Первый", "00000000-0000-0000-0000-000000000101"),
        };
        foreach (var level in levels)
        {
            await database.ExecuteAsync($"""
                INSERT INTO career_levels (id, track_id, code, is_managerial, valid_from, valid_to, role_description, previous_level_id, next_level_ids, version, created_at, name, owner_org_unit_id, status, author_id)
                VALUES ('{level.Id}', '{level.Track}', 'L', false, NULL, NULL, NULL, NULL, ARRAY[]::uuid[], 0, now(), '{level.Name}', '{Child}', 'Active', NULL)
                """);
        }

        foreach (var level in levels.Where(level => level.Previous is not null))
        {
            await database.ExecuteAsync($"UPDATE career_levels SET previous_level_id = '{level.Previous}' WHERE id = '{level.Id}'");
        }

        await database.MigrateToAsync(state);
        return database;
    }
}
