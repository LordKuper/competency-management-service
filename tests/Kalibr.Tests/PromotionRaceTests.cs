using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The two rules that span the rows of several sessions and commissions, held by the promotion lock that the announcement takes exclusively and every
/// commission change takes shared. Each test holds that lock, lines two requests up on it in an order it chooses, and releases them together, so
/// both have read the units before either has written: a request that did not take the lock would then act on a state the other has already changed.
/// A request queued first is served first, so the outcome of each order is fixed.
/// </summary>
public sealed class PromotionRaceTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string Waves = "/api/v1/promotion/waves";

    private static object Announcement(Guid unitId)
    {
        var start = PromotionScene.Today.AddDays(9);
        return new { startsOn = start, endsOn = PromotionScene.Ends(start), unitIds = new[] { unitId } };
    }

    private async Task<ApiResponse[]> QueueAsync(params Func<Task<ApiResponse>>[] inOrder)
    {
        await using var held = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.PromotionKey);
        List<Task<ApiResponse>> pending = [];
        foreach (var request in inOrder)
        {
            pending.Add(await RowLock.StartWaitingAsync(fixture.Host, pending.Count + 1, request));
        }

        await held.ReleaseAsync();
        return await Task.WhenAll(pending);
    }

    [Fact]
    public async Task Ac2_TwoAnnouncementsOfTheSameUnitAtTheSameMoment_LeaveOneUnfinishedSession()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        (await PromotionScene.SaveCommissionAsync(org.Admin, org.Sibling.Id, [org.SiblingHead.Employee], org.SiblingHead.Employee)).Expect(HttpStatusCode.OK);
        var announce = () => org.Admin.PostAsync(Waves, Announcement(org.Sibling.Id));

        var results = await QueueAsync(announce, announce);

        results[0].Status.Should().Be(HttpStatusCode.Created, results[0].Body);
        results[1].Status.Should().Be(HttpStatusCode.BadRequest, "the unit is already in the session the first request announced");
        results[1].FieldErrors("unitIds").Should().ContainSingle().Which.Should().Contain("уже входит в незавершённую");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM promotion_wave_units WHERE org_unit_id = '{org.Sibling.Id}'")).Should().Be(1);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("announce")]
    public async Task Ac66_RemovingTheChairAndAnnouncingTheUnitAtTheSameMoment_NeverLeavesAnUnfinishedSessionWithoutAChair(string first)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        (await PromotionScene.SaveCommissionAsync(org.Admin, org.Sibling.Id, [org.SiblingHead.Employee], org.SiblingHead.Employee)).Expect(HttpStatusCode.OK);
        var commission = $"/api/v1/promotion/commissions/{org.Sibling.Id}";
        var version = (await org.Admin.GetAsync(commission)).Expect(HttpStatusCode.OK).ETag;
        var removeChair = () => org.Admin.PutAsync(commission, new { memberEmployeeIds = new[] { org.SiblingHead.Employee }, chairEmployeeId = (Guid?)null }, version);
        var announce = () => org.Admin.PostAsync(Waves, Announcement(org.Sibling.Id));

        var results = first == "edit" ? await QueueAsync(removeChair, announce) : await QueueAsync(announce, removeChair);

        var (removal, announcement) = first == "edit" ? (results[0], results[1]) : (results[1], results[0]);
        (removal.Status == HttpStatusCode.OK ^ announcement.Status == HttpStatusCode.Created)
            .Should().BeTrue($"exactly one of them succeeds: removal {removal.Code} {removal.Body}, announcement {announcement.Code} {announcement.Body}");
        var chair = (await org.Admin.GetAsync(commission)).Expect(HttpStatusCode.OK).Json!["chairEmployeeId"];
        var sessions = await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM promotion_wave_units WHERE org_unit_id = '{org.Sibling.Id}'");
        if (first == "edit")
        {
            removal.Status.Should().Be(HttpStatusCode.OK);
            announcement.Status.Should().Be(HttpStatusCode.BadRequest, "the chair was taken off first, so the unit cannot be announced");
            chair.Should().BeNull();
            sessions.Should().Be(0);
        }
        else
        {
            announcement.Status.Should().Be(HttpStatusCode.Created);
            removal.Status.Should().Be(HttpStatusCode.Conflict, "the unit entered a session first, so its chair stays");
            chair!.GetValue<Guid>().Should().Be(org.SiblingHead.Employee);
            sessions.Should().Be(1);
        }
    }
}
