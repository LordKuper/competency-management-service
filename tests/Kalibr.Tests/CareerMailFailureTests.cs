using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// When the mail server is not there, a career reminder is lost without undoing the action that caused it: the action stands and the
/// failure is journaled by the kind of reminder and the account, never by the address.
/// </summary>
public sealed class CareerMailFailureTests(MailServerDownHost fixture) : IClassFixture<MailServerDownHost>
{
    [Fact]
    public async Task Ac111_Ac112_AReminderThatCannotBeSent_LeavesTheActionDone_AndIsJournaledByItsKindWithoutTheAddress()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();

        var submitted = (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        var awaiting = await FailureAsync(org, org.DepartmentHead.Id, "MatrixAwaitingApproval");
        var returned = (await kit.TransitAsync(org.DepartmentHead.Client, "return", new { comment = "Доработать" })).Expect(HttpStatusCode.OK);
        var returnedFailure = await FailureAsync(org, org.TeamHead.Id, "MatrixReturned");
        var competency = await org.TeamHead.Client.MakeCompetencyAsync();
        var draftFailure = await FailureAsync(org, org.Hr.Id, "DraftAwaitingApproval");

        submitted.Json!["status"]!.GetValue<string>().Should().Be("OnReview");
        returned.Json!["status"]!.GetValue<string>().Should().Be("Returned");
        (await org.Admin.GetAsync($"/api/v1/career/competencies/{competency.Id}")).Expect(HttpStatusCode.OK);
        awaiting["requestId"]!.GetValue<string>().Should().Be(submitted.RequestId);
        awaiting["actor"]!.GetValue<string>().Should().Be(org.TeamHead.Id.ToString(), "the journal names who caused the reminder");
        returnedFailure["requestId"]!.GetValue<string>().Should().Be(returned.RequestId);
        foreach (var row in new[] { awaiting, returnedFailure, draftFailure })
        {
            row["entityType"]!.GetValue<string>().Should().Be("AppUser");
            row.ToJsonString().Should().NotContain("@test.local", "the journal never carries the address");
        }
    }

    private static async Task<System.Text.Json.Nodes.JsonNode> FailureAsync(CareerOrg org, Guid recipientId, string kind)
    {
        System.Text.Json.Nodes.JsonNode? found = null;
        await Waiting.UntilAsync(
            async () =>
            {
                var rows = (await org.Admin.GetAsync($"/api/v1/audit?action=Mail.SendFailed&entityId={recipientId}&pageSize=200")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();
                found = rows.FirstOrDefault(row => row!["reason"]!.GetValue<string>() == kind);
                return found is not null;
            },
            $"the failure of a {kind} reminder to {recipientId} is journaled");
        return found!;
    }
}
