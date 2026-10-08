using System.Net;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Registration by invitation and password reset by e-mailed link, from the administrator's action and the user's request to the screen the
/// link opens: what is mailed and to whom, that a link is spent once and stops working for every reason alike, and that nothing the anonymous
/// requests answer tells whether an account exists. The acceptance ids in the names are those of the sprint that introduced the links.
/// </summary>
public sealed partial class AccountLinkTests(TestEnvironment environment)
{
    private const string AcceptInvitation = "accept-invitation";
    private const string ResetPassword = "reset-password";
    private const string ForgotPassword = "/api/v1/auth/forgot-password";
    private const string NewPassword = "Link-Pass-12345!";
    private const string ChangedPassword = "Changed-Pass-12345!";
    private const string ExpireLink = "UPDATE users SET link_expires_at = now() - interval '1 second' WHERE id = '{0}'";

    [GeneratedRegex("[А-Яа-яЁё]")]
    private static partial Regex Cyrillic();

    [Fact]
    public async Task Ac4_NewAccount_IsInvitedWithoutAPassword_AndMailedOneRussianLinkFromThePublicAddress()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("invite")}@test.local";
        const string IgnoredPassword = "Ignored-Pass-12345!";

        var created = (await admin.PostAsync(
            "/api/v1/users",
            new { email, password = IgnoredPassword, role = Scenarios.User, employeeId = (Guid?)null },
            configure: request => request.Headers.Host = "evil.example")).Expect(HttpStatusCode.Created);

        created.Json!["isInvited"]!.GetValue<bool>().Should().BeTrue();
        created.Json["mailSent"]!.GetValue<bool>().Should().BeTrue();
        created.Json["isBlocked"]!.GetValue<bool>().Should().BeFalse();
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password = IgnoredPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized, "an invited account cannot sign in, whatever password the request carried");
        var mail = (await environment.Mail.WaitForAsync(email)).Should().ContainSingle().Subject;
        mail.Link.ToString().Should().StartWith($"{ApiHost.PublicBaseUrl}/{AcceptInvitation}#token=", "the link is built from the configured address, not from the Host header");
        mail.Screen.Should().Be(AcceptInvitation);
        mail.Token.Length.Should().BeGreaterThanOrEqualTo(43, "a token carries at least 256 bits");
        Cyrillic().IsMatch(mail.Subject).Should().BeTrue("the subject is in Russian");
        Cyrillic().IsMatch(mail.Text).Should().BeTrue("the text is in Russian");
        (await host.ScalarAsync<TimeSpan>($"SELECT link_expires_at - link_issued_at FROM users WHERE id = '{created.Id}'"))
            .Should().Be(TimeSpan.FromHours(72), "an invitation is valid for 72 hours by default");
    }

    [Fact]
    public async Task Ac5_AcceptingAnInvitation_SetsThePasswordOnceAndLetsTheUserSignIn()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var invited = await admin.InviteUserAsync();
        var token = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        var anonymous = host.Anonymous();

        var weak = await SpendAsync(host, AcceptInvitation, token, "short");
        var accepted = await anonymous.PostAsync($"/api/v1/auth/{AcceptInvitation}", new { token, password = NewPassword });
        var replay = await SpendAsync(host, AcceptInvitation, token, ChangedPassword);

        weak.Status.Should().Be(HttpStatusCode.BadRequest);
        weak.FieldErrors("password").Should().NotBeEmpty("a password against the policy is refused and leaves the link unspent");
        accepted.Expect(HttpStatusCode.NoContent);
        (await anonymous.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized, "setting the password does not sign the user in");
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = invited.Email, password = NewPassword })).Status.Should().Be(HttpStatusCode.OK);
        replay.Status.Should().Be(HttpStatusCode.BadRequest, "a link is spent by the first use");
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = invited.Email, password = ChangedPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await admin.GetUserAsync(invited.Id)).Json!["isInvited"]!.GetValue<bool>().Should().BeFalse();
    }

    [Theory]
    [InlineData("resend")]
    [InlineData("email-change")]
    [InlineData("block")]
    [InlineData("expiry")]
    public async Task Ac5_InvitationLink_StopsWorkingWithoutSayingWhy_AndAResendGivesAWorkingOne(string reason)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var baseline = await RefusalOfAsync(host, AcceptInvitation);
        var invited = await admin.InviteUserAsync();
        var email = invited.Email;
        var old = (await environment.Mail.WaitForAsync(email))[0].Token;

        switch (reason)
        {
            case "resend":
                await ResendInvitationAsync(admin, invited.Id);
                break;
            case "email-change":
                email = $"{Scenarios.Unique("moved")}@test.local";
                (await admin.PutAsync(
                    $"/api/v1/users/{invited.Id}",
                    new { email, role = Scenarios.User, employeeId = (Guid?)null },
                    (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);
                break;
            case "block":
                (await admin.PostAsync($"/api/v1/users/{invited.Id}/block", ifMatch: (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);
                break;
            default:
                await host.ExecuteAsync(string.Format(ExpireLink, invited.Id));
                break;
        }

        Shape(await SpendAsync(host, AcceptInvitation, old)).Should().Be(baseline, "a link that stopped working is refused as any unknown link is, whatever the reason");
        if (reason == "block")
        {
            (await admin.PostAsync($"/api/v1/users/{invited.Id}/unblock", ifMatch: (await admin.GetUserAsync(invited.Id)).ETag)).Expect(HttpStatusCode.OK);
            Shape(await SpendAsync(host, AcceptInvitation, old)).Should().Be(baseline, "lifting the block does not revive the link");
        }

        if (reason != "resend")
        {
            await ResendInvitationAsync(admin, invited.Id);
        }

        var fresh = (await environment.Mail.WaitForAsync(email, reason == "email-change" ? 1 : 2))[^1].Token;
        fresh.Should().NotBe(old);
        (await SpendAsync(host, AcceptInvitation, fresh)).Expect(HttpStatusCode.NoContent);
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password = NewPassword })).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac5_InvitationAndResetLinks_AreVoidedWhenTheirEmployeeIsDismissed()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var invitationBaseline = await RefusalOfAsync(host, AcceptInvitation);
        var resetBaseline = await RefusalOfAsync(host, ResetPassword);
        var unit = await admin.CreateUnitAsync();
        var invitedEmployee = await admin.CreateEmployeeAsync(unit.Id);
        var registeredEmployee = await admin.CreateEmployeeAsync(unit.Id);
        var invited = await admin.InviteUserAsync(employeeId: invitedEmployee.Id);
        var invitation = (await environment.Mail.WaitForAsync(invited.Email))[0].Token;
        var registered = await admin.CreateUserAsync(employeeId: registeredEmployee.Id);
        (await admin.PostAsync($"/api/v1/users/{registered.Id}/send-password-reset", ifMatch: registered.ETag)).Expect(HttpStatusCode.OK);
        var reset = (await environment.Mail.WaitForAsync(registered.Email, 2))[1].Token;

        (await admin.PostAsync($"/api/v1/employees/{invitedEmployee.Id}/dismiss", ifMatch: invitedEmployee.ETag)).Expect(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/v1/employees/{registeredEmployee.Id}/dismiss", ifMatch: registeredEmployee.ETag)).Expect(HttpStatusCode.OK);

        Shape(await SpendAsync(host, AcceptInvitation, invitation)).Should().Be(invitationBaseline);
        Shape(await SpendAsync(host, ResetPassword, reset, NewPassword)).Should().Be(resetBaseline);
        (await admin.GetUserAsync(invited.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue("dismissing the employee blocked the account");
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = registered.Email, password = NewPassword })).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac6_ResendInvitation_IsRefusedForRegisteredAndBlockedAccounts_AndTheListFiltersInvitedAccounts()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var tag = Scenarios.Unique("resend")[7..];
        var registered = await admin.CreateUserAsync(email: $"reg{tag}@test.local");
        var invited = await admin.InviteUserAsync(email: $"inv{tag}@test.local");
        var blockedInvited = await admin.InviteUserAsync(email: $"blk{tag}@test.local");
        (await admin.PostAsync($"/api/v1/users/{blockedInvited.Id}/block", ifMatch: blockedInvited.ETag)).Expect(HttpStatusCode.OK);

        var onRegistered = await admin.PostAsync($"/api/v1/users/{registered.Id}/resend-invitation", ifMatch: registered.ETag);
        var onBlocked = await admin.PostAsync($"/api/v1/users/{blockedInvited.Id}/resend-invitation", ifMatch: (await admin.GetUserAsync(blockedInvited.Id)).ETag);
        var onUnknown = await admin.PostAsync($"/api/v1/users/{Guid.NewGuid()}/resend-invitation", ifMatch: "\"1\"");
        var resent = (await admin.PostAsync($"/api/v1/users/{invited.Id}/resend-invitation", ifMatch: invited.ETag)).Expect(HttpStatusCode.OK);

        onRegistered.Status.Should().Be(HttpStatusCode.Conflict, onRegistered.Body);
        onBlocked.Status.Should().Be(HttpStatusCode.Conflict, onBlocked.Body);
        onUnknown.Status.Should().Be(HttpStatusCode.NotFound);
        resent.Json!["isInvited"]!.GetValue<bool>().Should().BeTrue();
        resent.Json["mailSent"]!.GetValue<bool>().Should().BeTrue();
        resent.ETag.Should().NotBe(invited.ETag);
        (await environment.Mail.WaitForAsync(invited.Email, 2)).Select(mail => mail.Screen).Should().OnlyContain(screen => screen == AcceptInvitation);
        (await environment.Mail.FindAsync(registered.Email)).Should().ContainSingle("the refused resend sent nothing more than the first invitation");
        (await Emails(admin, tag, "&isInvited=true")).Should().Equal(blockedInvited.Email, invited.Email);
        (await Emails(admin, tag, "&isInvited=false")).Should().Equal(registered.Email);
        (await Emails(admin, tag, "&isInvited=true&isBlocked=true")).Should().Equal(blockedInvited.Email);
    }

    [Fact]
    public async Task Ac7_ForgotPassword_AnswersAlikeWhateverTheAccount_AndMailsALinkOnlyToAnActiveRegisteredOne()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var active = await admin.CreateUserAsync();
        var blocked = await admin.CreateUserAsync();
        (await admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        var invited = await admin.InviteUserAsync();
        var sentinel = await admin.CreateUserAsync();
        var unknown = $"{Scenarios.Unique("nobody")}@test.local";

        var answers = new List<ApiResponse>();
        foreach (var email in new[] { unknown, blocked.Email, invited.Email, active.Email, sentinel.Email })
        {
            answers.Add(await host.Anonymous().PostAsync(ForgotPassword, new { email }));
        }

        answers.Select(answer => answer.Status).Should().AllBeEquivalentTo(HttpStatusCode.Accepted);
        answers.Select(answer => $"{answer.ContentType}|{answer.Body}").Distinct().Should().ContainSingle("the answer is the same whatever the address");
        await environment.Mail.WaitForAsync(sentinel.Email, 2);
        (await environment.Mail.FindAsync(unknown)).Should().BeEmpty();
        (await environment.Mail.FindAsync(blocked.Email)).Select(mail => mail.Screen).Should().Equal(AcceptInvitation);
        (await environment.Mail.FindAsync(invited.Email)).Select(mail => mail.Screen).Should().Equal(AcceptInvitation);
        (await environment.Mail.FindAsync(active.Email)).Select(mail => mail.Screen).Should().Equal(AcceptInvitation, ResetPassword);
        (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(unknown)}")).Json!["total"]!.GetValue<int>().Should().Be(0, "nothing is stored for an address no account has");
        foreach (var account in new[] { blocked, invited })
        {
            (await RequestedLinksAsync(admin, account.Id)).Should().Be(0, "no link was issued to the account");
        }

        foreach (var account in new[] { active, sentinel })
        {
            (await RequestedLinksAsync(admin, account.Id)).Should().Be(1);
        }
    }

    [Fact]
    public async Task Ac5_LinkEndpoints_NeedNoSession_AndAnswerAnUnusableLinkWith400_NeverWith401()
    {
        var host = await environment.SharedHostAsync();
        var anonymous = host.Anonymous();

        var forgotten = await anonymous.PostAsync(ForgotPassword, new { email = $"{Scenarios.Unique("nobody")}@test.local" });
        var malformed = await anonymous.PostAsync(ForgotPassword, new { email = "not-an-email" });

        forgotten.Status.Should().Be(HttpStatusCode.Accepted);
        malformed.Status.Should().Be(HttpStatusCode.BadRequest, "the address is checked for its shape only");
        foreach (var route in new[] { AcceptInvitation, ResetPassword })
        {
            var unknown = await SpendAsync(host, route, "no-such-token");
            var empty = await SpendAsync(host, route, string.Empty);
            unknown.Status.Should().Be(HttpStatusCode.BadRequest, route);
            unknown.ContentType.Should().Be("application/problem+json");
            unknown.Json!["title"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            unknown.Json["detail"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
            Shape(empty).Should().Be(Shape(unknown), route);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ac8_ALinkOfOneKind_IsNotAcceptedByTheOtherEndpoint(bool invitationLink)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = invitationLink ? await admin.InviteUserAsync() : await admin.CreateUserAsync();
        if (!invitationLink)
        {
            (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        }

        var token = (await environment.Mail.WaitForAsync(account.Email, invitationLink ? 1 : 2))[^1].Token;
        var wrongEndpoint = invitationLink ? ResetPassword : AcceptInvitation;
        var rightEndpoint = invitationLink ? AcceptInvitation : ResetPassword;

        var refused = await SpendAsync(host, wrongEndpoint, token, ChangedPassword);
        var passwordAfterRefusal = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = ChangedPassword });
        var accepted = await SpendAsync(host, rightEndpoint, token);

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        passwordAfterRefusal.Status.Should().Be(HttpStatusCode.Unauthorized, "the refused request set no password");
        accepted.Status.Should().Be(HttpStatusCode.NoContent, "the refusal did not spend the link");
    }

    [Theory]
    [InlineData("change-password")]
    [InlineData("new-link")]
    [InlineData("email-change")]
    [InlineData("block")]
    [InlineData("expiry")]
    public async Task Ac8_ResetLink_StopsWorkingWithoutSayingWhy(string reason)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var baseline = await RefusalOfAsync(host, ResetPassword);
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        var old = (await environment.Mail.WaitForAsync(account.Email, 2))[1].Token;

        switch (reason)
        {
            case "change-password":
                (await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = ChangedPassword })).Expect(HttpStatusCode.NoContent);
                break;
            case "new-link":
                (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: (await admin.GetUserAsync(account.Id)).ETag)).Expect(HttpStatusCode.OK);
                break;
            case "email-change":
                (await admin.PutAsync(
                    $"/api/v1/users/{account.Id}",
                    new { email = $"{Scenarios.Unique("moved")}@test.local", role = Scenarios.User, employeeId = (Guid?)null },
                    (await admin.GetUserAsync(account.Id)).ETag)).Expect(HttpStatusCode.OK);
                break;
            case "block":
                (await admin.PostAsync($"/api/v1/users/{account.Id}/block", ifMatch: (await admin.GetUserAsync(account.Id)).ETag)).Expect(HttpStatusCode.OK);
                break;
            default:
                await host.ExecuteAsync(string.Format(ExpireLink, account.Id));
                break;
        }

        Shape(await SpendAsync(host, ResetPassword, old, NewPassword)).Should().Be(baseline, "a link that stopped working is refused as any unknown link is, whatever the reason");
        if (reason == "new-link")
        {
            var fresh = (await environment.Mail.WaitForAsync(account.Email, 3))[2].Token;
            (await SpendAsync(host, ResetPassword, fresh, NewPassword)).Expect(HttpStatusCode.NoContent);
        }
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("dismissed")]
    public async Task Ac8_ResetByLink_NeverLiftsAnAdministratorsBlockOrLetsInADismissedEmployee(string state)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var account = await admin.CreateUserAsync(employeeId: employee.Id);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);
        var token = (await environment.Mail.WaitForAsync(account.Email, 2))[1].Token;
        await host.ExecuteAsync(state == "blocked"
            ? $"UPDATE users SET is_blocked = true WHERE id = '{account.Id}'"
            : $"UPDATE employees SET is_active = false WHERE id = '{employee.Id}'");

        await SpendAsync(host, ResetPassword, token, NewPassword);

        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = NewPassword })).Status
            .Should().Be(HttpStatusCode.Unauthorized, "a new password lets in neither a blocked account nor a dismissed employee");
        (await admin.GetUserAsync(account.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().Be(state == "blocked", "the reset does not lift a block");
    }

    [Fact]
    public async Task Ac9_AdministratorSendsAResetLinkToAnActiveAccountOnly_AndTheirPasswordStaysAsItWas()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var active = await admin.CreateUserAsync();
        var session = await active.SignInAsync(host);
        var invited = await admin.InviteUserAsync();
        var blocked = await admin.CreateUserAsync();
        (await admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        var leaver = await admin.CreateUserAsync(employeeId: employee.Id);
        (await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag)).Expect(HttpStatusCode.OK);

        var sent = (await admin.PostAsync($"/api/v1/users/{active.Id}/send-password-reset", ifMatch: active.ETag)).Expect(HttpStatusCode.OK);
        var onInvited = await admin.PostAsync($"/api/v1/users/{invited.Id}/send-password-reset", ifMatch: invited.ETag);
        var onBlocked = await admin.PostAsync($"/api/v1/users/{blocked.Id}/send-password-reset", ifMatch: (await admin.GetUserAsync(blocked.Id)).ETag);
        var onLeaver = await admin.PostAsync($"/api/v1/users/{leaver.Id}/send-password-reset", ifMatch: (await admin.GetUserAsync(leaver.Id)).ETag);
        var onUnknown = await admin.PostAsync($"/api/v1/users/{Guid.NewGuid()}/send-password-reset", ifMatch: "\"1\"");
        var removed = await admin.PostAsync($"/api/v1/users/{active.Id}/reset-password", new { newPassword = NewPassword }, active.ETag);

        sent.Json!["mailSent"]!.GetValue<bool>().Should().BeTrue();
        sent.Json["isInvited"]!.GetValue<bool>().Should().BeFalse();
        (await environment.Mail.WaitForAsync(active.Email, 2))[1].Screen.Should().Be(ResetPassword);
        (await host.ScalarAsync<TimeSpan>($"SELECT link_expires_at - link_issued_at FROM users WHERE id = '{active.Id}'"))
            .Should().Be(TimeSpan.FromHours(1), "a password reset link is valid for one hour by default");
        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK, "sending the link ends no session");
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = active.Email, password = active.Password })).Status
            .Should().Be(HttpStatusCode.OK, "the password stays as it was until the user sets a new one");
        foreach (var refused in new[] { onInvited, onBlocked, onLeaver })
        {
            refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        }

        onUnknown.Status.Should().Be(HttpStatusCode.NotFound);
        ((int)removed.Status).Should().BeOneOf([404, 405], "an administrator can no longer set another account's password");
        (await environment.Mail.FindAsync(invited.Email)).Should().ContainSingle("only the invitation was mailed");
    }

    private static async Task<ApiResponse> SpendAsync(ApiHost host, string route, string token, string password = NewPassword) =>
        await host.Anonymous().PostAsync($"/api/v1/auth/{route}", new { token, password });

    private static async Task<string> RefusalOfAsync(ApiHost host, string route)
    {
        var refused = await SpendAsync(host, route, $"no-such-token-{Guid.NewGuid():N}");
        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        return Shape(refused);
    }

    private static string Shape(ApiResponse response) =>
        $"{response.Code} {response.ContentType} " + (response.Json is { } json
            ? string.Join(',', json.AsObject().Where(property => property.Key != "traceId").Select(property => $"{property.Key}={property.Value!.ToJsonString()}"))
            : response.Body);

    private static async Task ResendInvitationAsync(ApiClient admin, Guid id) =>
        (await admin.PostAsync($"/api/v1/users/{id}/resend-invitation", ifMatch: (await admin.GetUserAsync(id)).ETag)).Expect(HttpStatusCode.OK);

    private static async Task<int> RequestedLinksAsync(ApiClient admin, Guid id) =>
        (await admin.GetAsync($"/api/v1/audit?action=Auth.PasswordResetRequested&entityId={id}")).Expect(HttpStatusCode.OK).Json!["total"]!.GetValue<int>();

    private static async Task<string[]> Emails(ApiClient admin, string text, string filters)
    {
        var page = (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(text)}{filters}&pageSize=200")).Expect(HttpStatusCode.OK);
        return [.. page.Json!["items"]!.AsArray().Select(user => user!["email"]!.GetValue<string>()).Order()];
    }
}
