using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A host with a database of its own for the link race tests, so that the locks waited for in that database are theirs alone.
/// </summary>
/// <param name="environment">The shared environment that supplies the database and the mail server.</param>
public sealed class AccountLinkRaceHost(TestEnvironment environment) : HostFixture(environment, null);

/// <summary>
/// Everything that spends or voids an account's link works on the account row under its lock, re-reading the account and checking the link again once it holds it.
/// Each test holds that lock, lines two requests up on it in an order it chooses, and releases them together, so both have read the account before
/// either has changed it: a request that did not re-check would then act on a link the other has already spent or voided.
/// A request queued first is served first, so the outcome of each order is fixed.
/// </summary>
public sealed class AccountLinkRaceTests(AccountLinkRaceHost fixture, TestEnvironment environment) : IClassFixture<AccountLinkRaceHost>
{
    private const string FirstPassword = "First-Pass-12345!";
    private const string SecondPassword = "Second-Pass-12345!";

    [Theory]
    [InlineData("accept")]
    [InlineData("resend")]
    public async Task Ac5_AcceptingAnInvitationAndResendingItAtTheSameMoment_LeavesExactlyTheFirstInTheQueueWinning(string first)
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var invited = await admin.InviteUserAsync();
        var old = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        var version = (await admin.GetUserAsync(invited.Id)).ETag;
        var accept = () => Spend(host, "accept-invitation", old, FirstPassword);
        var resend = () => admin.PostAsync($"/api/v1/users/{invited.Id}/resend-invitation", ifMatch: version);

        var results = first == "accept" ? await QueueAsync(invited.Id, accept, resend) : await QueueAsync(invited.Id, resend, accept);

        if (first == "accept")
        {
            results[0].Status.Should().Be(HttpStatusCode.NoContent, results[0].Body);
            results[1].Status.Should().Be(HttpStatusCode.Conflict, "the invitation was accepted first, so there is nothing to resend");
            (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = invited.Email, password = FirstPassword })).Status.Should().Be(HttpStatusCode.OK);
            (await environment.Mail.FindAsync(invited.Email)).Should().ContainSingle("the refused resend mailed nothing");
        }
        else
        {
            results[0].Status.Should().Be(HttpStatusCode.OK, results[0].Body);
            results[1].Status.Should().Be(HttpStatusCode.BadRequest, "the resend replaced the link first, so the old one is unusable");
            (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = invited.Email, password = FirstPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);
            var fresh = (await environment.Mail.WaitForAsync(invited.Email, 2))[1].Token;
            (await Spend(host, "accept-invitation", fresh, FirstPassword)).Status.Should().Be(HttpStatusCode.NoContent, "the replacing link works");
        }
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("block")]
    public async Task Ac5_AcceptingAnInvitationAndBlockingTheAccountAtTheSameMoment_LeavesExactlyTheFirstInTheQueueWinning(string first)
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var invited = await admin.InviteUserAsync();
        var token = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        var version = (await admin.GetUserAsync(invited.Id)).ETag;
        var accept = () => Spend(host, "accept-invitation", token, FirstPassword);
        var block = () => admin.PostAsync($"/api/v1/users/{invited.Id}/block", ifMatch: version);

        var results = first == "accept" ? await QueueAsync(invited.Id, accept, block) : await QueueAsync(invited.Id, block, accept);

        var state = await admin.GetUserAsync(invited.Id);
        var signIn = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = invited.Email, password = FirstPassword });
        if (first == "accept")
        {
            results[0].Status.Should().Be(HttpStatusCode.NoContent, results[0].Body);
            results[1].Status.Should().Be(HttpStatusCode.PreconditionFailed, "the block was based on a version the registration has since replaced");
            state.Json!["isBlocked"]!.GetValue<bool>().Should().BeFalse();
            signIn.Status.Should().Be(HttpStatusCode.OK);
        }
        else
        {
            results[0].Status.Should().Be(HttpStatusCode.OK, results[0].Body);
            results[1].Status.Should().Be(HttpStatusCode.BadRequest, "blocking voided the link first");
            state.Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue();
            state.Json["isInvited"]!.GetValue<bool>().Should().BeTrue("the refused link set no password");
            signIn.Status.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Theory]
    [InlineData("invitation")]
    [InlineData("reset")]
    public async Task Ac5_TheSameLinkUsedTwiceAtTheSameMoment_IsSpentByTheFirstInTheQueueOnly(string kind)
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var account = kind == "invitation" ? await admin.InviteUserAsync() : await admin.CreateUserAsync();
        var route = kind == "invitation" ? "accept-invitation" : "reset-password";
        if (kind == "reset")
        {
            (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        }

        var token = (await environment.Mail.WaitForAsync(account.Email, kind == "invitation" ? 1 : 2))[^1].Token;

        var results = await QueueAsync(
            account.Id,
            () => Spend(host, route, token, FirstPassword),
            () => Spend(host, route, token, SecondPassword));

        results[0].Status.Should().Be(HttpStatusCode.NoContent, results[0].Body);
        results[1].Status.Should().Be(HttpStatusCode.BadRequest, "the link was spent by the request before it");
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = FirstPassword })).Status.Should().Be(HttpStatusCode.OK);
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = SecondPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized, "the password of the request that lost was never set");
    }

    [Theory]
    [InlineData("reset")]
    [InlineData("change")]
    public async Task Ac8_ResetByLinkAndChangeOfTheOwnPasswordAtTheSameMoment_LeaveOneNewPassword(string first)
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        var token = (await environment.Mail.WaitForAsync(account.Email, 2))[1].Token;
        var reset = () => Spend(host, "reset-password", token, FirstPassword);
        var change = () => session.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = SecondPassword });

        var results = first == "reset" ? await QueueAsync(account.Id, reset, change) : await QueueAsync(account.Id, change, reset);

        var (resetResult, changeResult) = first == "reset" ? (results[0], results[1]) : (results[1], results[0]);
        var resetWon = resetResult.Status == HttpStatusCode.NoContent;
        var changeWon = changeResult.Status == HttpStatusCode.NoContent;
        (resetWon ^ changeWon).Should().BeTrue($"exactly one of them changes the password: reset {resetResult.Code} {resetResult.Body}, change {changeResult.Code} {changeResult.Body}");
        if (first == "reset")
        {
            resetWon.Should().BeTrue("the reset was served first");
            changeResult.Status.Should().Be(HttpStatusCode.BadRequest, "the old password it presents is already replaced");
        }
        else
        {
            (resetWon ? resetResult : changeResult).Status.Should().Be(HttpStatusCode.NoContent);
            (resetWon ? changeResult : resetResult).Status.Should().BeOneOf([HttpStatusCode.BadRequest, HttpStatusCode.PreconditionFailed], "the loser is refused or finds its version replaced, never overwrites");
        }

        var winnerPassword = resetWon ? FirstPassword : SecondPassword;
        var loserPassword = resetWon ? SecondPassword : FirstPassword;
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = winnerPassword })).Status.Should().Be(HttpStatusCode.OK);
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = loserPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await Spend(host, "reset-password", token, loserPassword)).Status.Should().Be(HttpStatusCode.BadRequest, "the link is gone, spent or voided by the password change");
    }

    private static Task<ApiResponse> Spend(ApiHost host, string route, string token, string password) =>
        host.Anonymous().PostAsync($"/api/v1/auth/{route}", new { token, password });

    private async Task<ApiResponse[]> QueueAsync(Guid accountId, params Func<Task<ApiResponse>>[] inOrder)
    {
        await using var held = await RowLock.HoldAsync(fixture.Host, accountId);
        var pending = new List<Task<ApiResponse>>();
        foreach (var request in inOrder)
        {
            pending.Add(request());
            await RowLock.UntilWaitingAsync(fixture.Host, pending.Count);
        }

        await held.ReleaseAsync();
        return await Task.WhenAll(pending);
    }
}
