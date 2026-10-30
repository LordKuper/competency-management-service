using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The ceiling of the items one desk request returns: a group that outgrows it is cut, and its total says so.
/// </summary>
public sealed class DeskCeilingTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const int Ceiling = 2000;
    private const int Positions = Ceiling + 1;

    [Fact]
    public async Task Ac4_AGroupLargerThanTheCeiling_IsCutAtTheCeiling_WhileItsTotalCountsEveryItem()
    {
        var admin = await fixture.Host.AdminAsync();
        await fixture.Host.ExecuteAsync(
            $"INSERT INTO positions (id, version, created_at, name, is_active) SELECT gen_random_uuid(), 0, now(), 'Должность ' || n, true FROM generate_series(1, {Positions}) n");

        var items = (await admin.GetAsync("/api/v1/desk/items?group=UnusedPositions&limit=100000")).Expect(HttpStatusCode.OK).Json!;
        var desk = await admin.DeskAsync();

        items["items"]!.AsArray().Should().HaveCount(Ceiling);
        items["total"]!.GetValue<int>().Should().Be(Positions);
        desk["groups"]!.AsArray().Single()!["total"]!.GetValue<int>().Should().Be(Positions);
        desk["groups"]!.AsArray().Single()!["items"]!.AsArray().Should().HaveCount(10);
        desk["actionCount"]!.GetValue<int>().Should().Be(0, "positions nobody uses are for reference");
    }
}
