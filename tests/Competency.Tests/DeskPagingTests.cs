using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// How much of the desk is sent: the first items of each group with the group's total, the items of chosen groups up to a limit,
/// and what the two reading methods refuse. The host starts empty, so the groups hold exactly what the test puts in them.
/// </summary>
public sealed class DeskPagingTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const int UnitsInTheGroup = 12;
    private const int FirstItems = 10;

    [Fact]
    public async Task Ac4_TheDesk_SendsTheFirstTenItemsOfAGroupWithItsTotal_AndTheItemsMethodSendsAsManyAsAsked()
    {
        var admin = await fixture.Host.AdminAsync();
        for (var count = 0; count < UnitsInTheGroup; count++)
        {
            await admin.CreateEmployeeAsync((await admin.CreateUnitAsync()).Id, position: Scenarios.Unique("Должность"));
        }

        await admin.CreateUnitAsync();

        var desk = await admin.DeskAsync();
        var firstEleven = (await admin.GetAsync("/api/v1/desk/items?group=UnitsToConnect&limit=11")).Expect(HttpStatusCode.OK).Json!;
        var all = (await admin.GetAsync("/api/v1/desk/items?group=UnitsToConnect&group=EmptyUnits&limit=100000")).Expect(HttpStatusCode.OK).Json!;
        var emptyOnly = (await admin.GetAsync("/api/v1/desk/items?group=EmptyUnits")).Expect(HttpStatusCode.OK).Json!;

        var groups = desk["groups"]!.AsArray();
        groups.Select(group => group!["group"]!.GetValue<string>()).Should().Equal("UnitsToConnect", "EmptyUnits");
        var connect = groups[0]!;
        connect["total"]!.GetValue<int>().Should().Be(UnitsInTheGroup);
        connect["items"]!.AsArray().Should().HaveCount(FirstItems);
        connect["isReference"]!.GetValue<bool>().Should().BeFalse();
        groups[1]!["isReference"]!.GetValue<bool>().Should().BeTrue();
        desk["actionCount"]!.GetValue<int>().Should().Be(UnitsInTheGroup, "the empty unit is for reference");
        var names = connect["items"]!.AsArray().Select(item => item!["object"]!["name"]!.GetValue<string>()).ToList();
        names.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase, "items without a date of an event follow their names");
        firstEleven["items"]!.AsArray().Should().HaveCount(11);
        firstEleven["total"]!.GetValue<int>().Should().Be(UnitsInTheGroup);
        firstEleven["items"]!.AsArray().Take(FirstItems).Select(item => item!["object"]!["id"]!.GetValue<Guid>())
            .Should().Equal(connect["items"]!.AsArray().Select(item => item!["object"]!["id"]!.GetValue<Guid>()), "asking for more rereads the group from its start");
        all["items"]!.AsArray().Should().HaveCount(UnitsInTheGroup + 1);
        all["total"]!.GetValue<int>().Should().Be(UnitsInTheGroup + 1);
        emptyOnly["items"]!.AsArray().Should().ContainSingle();
        emptyOnly["total"]!.GetValue<int>().Should().Be(1);
    }

    [Theory]
    [InlineData("limit=0", "limit")]
    [InlineData("limit=-3", "limit")]
    public async Task Ac4_AnItemsLimitBelowOne_IsRefusedWithAFieldError(string query, string field)
    {
        var admin = await fixture.Host.AdminAsync();

        var refused = await admin.GetAsync($"/api/v1/desk/items?{query}");

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors(field).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Ac4_AnUnknownGroup_IsRefused()
    {
        var admin = await fixture.Host.AdminAsync();

        (await admin.GetAsync("/api/v1/desk/items?group=NoSuchGroup")).Status.Should().Be(HttpStatusCode.BadRequest);
    }
}
