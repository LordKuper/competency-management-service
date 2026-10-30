using System.Net;
using AwesomeAssertions;
using Kalibr.Platform;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// Lists the server orders by name follow the standard Russian ICU collation: a name in quotation marks stands before the others,
/// and «ё» stands among the «е», not after «я». The expected order is the one that plain ordinal comparison would break.
/// </summary>
public sealed class RussianCollationTests(TestEnvironment environment)
{
    private static readonly string[] Names = ["Яблоня", "ёлка", "Жук", "Ёж", "Дуб", "Арбуз", "\"Альфа\""];
    private static readonly string[] Expected = ["\"Альфа\"", "Арбуз", "Дуб", "Ёж", "ёлка", "Жук", "Яблоня"];

    [Fact]
    public async Task Ac32_Units_AreOrderedByTheRussianCollation()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        foreach (var name in Names)
        {
            await admin.CreateUnitAsync(parent.Id, name);
        }

        var listed = (await admin.GetAsync($"/api/v1/org-units?parentId={parent.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        listed.Select(item => item!["name"]!.GetValue<string>()).Should().Equal(Expected);
    }

    [Fact]
    public async Task Ac32_Positions_AreOrderedByTheRussianCollation()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var tag = Scenarios.Unique("t")[2..];
        foreach (var name in Names)
        {
            (await admin.PostAsync("/api/v1/positions", new { name = $"{name} {tag}" })).Expect(HttpStatusCode.Created);
        }

        var listed = (await admin.GetAsync($"/api/v1/positions?q={tag}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        listed.Select(item => item!["name"]!.GetValue<string>()).Should().Equal(Expected.Select(name => $"{name} {tag}"));
    }

    [Fact]
    public async Task Ac32_LegalEntities_AreOrderedByTheRussianCollation()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var tag = Scenarios.Unique("t")[2..];
        foreach (var name in Names)
        {
            await admin.LegalEntityAsync($"{name} {tag}");
        }

        var listed = (await admin.GetAsync($"/api/v1/legal-entities?q={tag}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        listed.Select(item => item!["name"]!.GetValue<string>()).Should().Equal(Expected.Select(name => $"{name} {tag}"));
    }

    [Fact]
    public async Task Ac32_Employees_AreOrderedByTheRussianCollation()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var tag = Scenarios.Unique("t")[2..];
        foreach (var lastName in new[] { "Яблонев", "Ёлкин", "Жуков", "Ёжиков", "Дубов", "Арбузов" })
        {
            await admin.CreateEmployeeAsync(unit.Id, $"{lastName}{tag}");
        }

        var listed = (await admin.GetAsync($"/api/v1/employees?orgUnitId={unit.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();

        listed.Select(item => item!["lastName"]!.GetValue<string>()).Should().Equal(
            $"Арбузов{tag}",
            $"Дубов{tag}",
            $"Ёжиков{tag}",
            $"Ёлкин{tag}",
            $"Жуков{tag}",
            $"Яблонев{tag}");
    }

    [Fact]
    public void Ac32_TheComparerOfTheServersMemory_OrdersAsTheDatabaseDoes()
    {
        var sorted = Names.Order(TextCollation.Comparer).ToArray();

        sorted.Should().Equal(Expected);
    }
}
