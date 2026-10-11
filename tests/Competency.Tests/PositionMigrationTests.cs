using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The data move that replaces the employees' free-text position with the position catalogue runs on data written at the schema before it:
/// names that differ in case, edge whitespace of every kind, dismissed employees and deactivated units all end up in the catalogue and in the
/// units' lists, and Down brings the text back.
/// </summary>
public sealed class PositionMigrationTests(TestEnvironment environment)
{
    private static readonly Guid Active = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Inactive = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Child = Guid.Parse("00000000-0000-0000-0000-0000000000a3");

    private static readonly (string Name, Guid Unit, string Position, bool IsActive)[] Employees =
    [
        ("Первый", Active, "Инженер", true),
        ("Второй", Active, "инженер", true),
        ("Третий", Inactive, "  Инженер  ", false),
        ("Четвёртый", Active, "ИНЖЕНЕР", true),
        ("Пятый", Active, "Ёлочник", true),
        ("Шестой", Active, "ёлочник", true),
        ("Седьмой", Active, "E'\\u00A0Аналитик\\u00A0'", true),
        ("Восьмой", Child, "E'\\tАналитик\\n'", true),
        ("Девятый", Child, "   ", true),
        ("Десятый", Child, "Бухгалтер", true),
    ];

    private static readonly string[] ExpectedNames = ["Инженер", "Инженер", "Инженер", "Инженер", "Ёлочник", "Ёлочник", "Аналитик", "Аналитик", "-", "Бухгалтер"];

    [Fact]
    public async Task Ac5_Up_MergesTheTextPositionsIntoOneCatalogueEntryEach_AndAllowsEachInTheUnitsWhereItIsUsed()
    {
        await using var database = await ArrangeAsync();

        await database.MigrateToAsync(MigrationDatabase.Positions);

        var held = await database.RowsAsync("SELECT p.name FROM employees e JOIN positions p ON p.id = e.position_id ORDER BY e.created_at");
        held.Select(row => row[0]).Should().Equal(ExpectedNames, "every employee keeps the position they held, spelled as the catalogue spells it");
        (await database.ScalarAsync<long>("SELECT count(*) FROM employees")).Should().Be(Employees.Length, "no employee is lost");
        (await database.RowsAsync("SELECT name FROM positions ORDER BY name")).Select(row => row[0])
            .Should().BeEquivalentTo(["Инженер", "Ёлочник", "Аналитик", "-", "Бухгалтер"], "case, edge whitespace of every kind and a blank text decide what is one position");
        (await database.RowsAsync("SELECT p.name, u.name FROM org_unit_positions l JOIN positions p ON p.id = l.position_id JOIN org_units u ON u.id = l.org_unit_id ORDER BY p.name, u.name"))
            .Select(row => $"{row[0]} / {row[1]}")
            .Should().BeEquivalentTo(
                ["Инженер / Активный", "Инженер / Неактивный", "Ёлочник / Активный", "Аналитик / Активный", "Аналитик / Дочерний", "- / Дочерний", "Бухгалтер / Дочерний"],
                "each pair of a unit and a position its employees hold, dismissed employees and deactivated units included, is allowed once");
        (await database.ScalarAsync<long>("SELECT count(*) FROM audit_events")).Should().Be(0, "moved data is not journaled");
    }

    [Fact]
    public async Task Ac5_Up_SpellsAMergedPositionAsMostEmployeesDo_AndOnATieAsTheEarliestEmployeeDoes()
    {
        await using var database = await ArrangeAsync();

        await database.MigrateToAsync(MigrationDatabase.Positions);

        var names = (await database.RowsAsync("SELECT name FROM positions")).Select(row => row[0]).ToList();
        names.Should().Contain("Инженер", "two employees write it so and one each write the other spellings");
        names.Should().NotContain(["инженер", "ИНЖЕНЕР", "  Инженер  "]);
        names.Should().Contain("Ёлочник", "one employee each, so the earliest employee's spelling wins");
        names.Should().NotContain("ёлочник");
    }

    [Fact]
    public async Task Ac5_Up_LeavesTheEmployeeSearchVectorWithoutThePosition_ButWithTheName()
    {
        await using var database = await ArrangeAsync();

        await database.MigrateToAsync(MigrationDatabase.Positions);

        (await database.ScalarAsync<long>("SELECT count(*) FROM employees WHERE search_vector @@ plainto_tsquery('russian', 'Бухгалтер')"))
            .Should().Be(0, "the position is searched through the catalogue now");
        (await database.ScalarAsync<long>("SELECT count(*) FROM employees WHERE search_vector @@ plainto_tsquery('russian', 'Десятый')"))
            .Should().Be(1);
        (await database.ScalarAsync<long>("SELECT count(*) FROM positions WHERE search_vector @@ plainto_tsquery('russian', 'Бухгалтер')"))
            .Should().Be(1);
    }

    [Fact]
    public async Task Ac5_Down_RestoresTheTextPositionFromTheCatalogue_AndTheSearchVectorWithIt()
    {
        await using var database = await ArrangeAsync();
        await database.MigrateToAsync(MigrationDatabase.Positions);

        await database.MigrateToAsync(MigrationDatabase.BeforePositions);

        (await database.RowsAsync("SELECT position FROM employees ORDER BY created_at")).Select(row => row[0])
            .Should().Equal(ExpectedNames, "the merged spelling comes back for every employee");
        (await database.ScalarAsync<string?>("SELECT to_regclass('positions')::text")).Should().BeNull();
        (await database.ScalarAsync<string?>("SELECT to_regclass('org_unit_positions')::text")).Should().BeNull();
        (await database.ScalarAsync<long>("SELECT count(*) FROM employees WHERE search_vector @@ plainto_tsquery('russian', 'Бухгалтер')"))
            .Should().Be(1, "the position is part of the employee's vector again");
    }

    private async Task<MigrationDatabase> ArrangeAsync()
    {
        var database = await MigrationDatabase.CreateAsync(environment);
        await database.MigrateToAsync(MigrationDatabase.BeforePositions);
        await database.ExecuteAsync($"""
            INSERT INTO org_units (id, name, parent_id, head_employee_id, is_active, version, created_at) VALUES
                ('{Active}', 'Активный', NULL, NULL, true, 0, now()),
                ('{Inactive}', 'Неактивный', NULL, NULL, false, 0, now()),
                ('{Child}', 'Дочерний', '{Active}', NULL, true, 0, now())
            """);
        for (var index = 0; index < Employees.Length; index++)
        {
            var (name, unit, position, isActive) = Employees[index];
            var literal = position.StartsWith("E'", StringComparison.Ordinal) ? position : $"'{position}'";
            await database.ExecuteAsync($"""
                INSERT INTO employees (id, last_name, first_name, middle_name, position, org_unit_id, is_active, version, created_at)
                VALUES (gen_random_uuid(), '{name}', 'Иван', NULL, {literal}, '{unit}', {isActive}, 0, now() + interval '{index} minutes')
                """);
        }

        return database;
    }
}
