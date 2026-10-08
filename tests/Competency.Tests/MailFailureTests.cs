using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A host whose mail server is not there: nothing listens on its SMTP port, and the credentials it would present are secrets the logs must not show.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class MailServerDownHost(TestEnvironment environment) : HostFixture(
    environment,
    new Dictionary<string, string>
    {
        ["Smtp__Port"] = ApiHost.FreePort().ToString(),
        ["Smtp__UserName"] = MailServerDownHost.UserName,
        ["Smtp__Password"] = MailServerDownHost.Password,
    })
{
    public static readonly string UserName = $"smtp-user-{Guid.NewGuid():N}";
    public static readonly string Password = $"smtp-secret-{Guid.NewGuid():N}";
}

/// <summary>
/// When the mail server refuses or is unreachable, the account change is kept and the request is not an error: the administrator is told the
/// mail was not sent, an anonymous request is answered as ever, and the failure is journaled and logged without the address, the link or the credentials.
/// </summary>
public sealed class MailFailureTests(MailServerDownHost fixture) : IClassFixture<MailServerDownHost>
{
    [Fact]
    public async Task Ac1_MailThatCannotBeSent_IsReportedToTheAdministratorAndJournaled_WithoutLosingTheChange()
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("down")}@test.local";

        var created = (await admin.PostAsync("/api/v1/users", new { email, role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var resent = (await admin.PostAsync($"/api/v1/users/{created.Id}/resend-invitation", ifMatch: created.ETag)).Expect(HttpStatusCode.OK);
        var registered = await admin.CreateUserAsync();
        var reset = (await admin.PostAsync($"/api/v1/users/{registered.Id}/send-password-reset", ifMatch: registered.ETag)).Expect(HttpStatusCode.OK);

        created.Json!["mailSent"]!.GetValue<bool>().Should().BeFalse();
        created.Json["isInvited"]!.GetValue<bool>().Should().BeTrue("the account is kept");
        resent.Json!["mailSent"]!.GetValue<bool>().Should().BeFalse();
        reset.Json!["mailSent"]!.GetValue<bool>().Should().BeFalse();
        (await admin.GetUserAsync(created.Id)).Json!["email"]!.GetValue<string>().Should().Be(email);
        var failures = (await admin.GetAsync($"/api/v1/audit?action=Mail.SendFailed&entityId={created.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();
        failures.Select(row => row!["reason"]!.GetValue<string>()).Should().Equal("Invitation", "Invitation");
        (await admin.AuditOfRequestAsync(reset.RequestId!)).Actions().Should().Equal("Auth.PasswordResetRequested", "Mail.SendFailed");
        (await admin.GetAsync($"/api/v1/audit?action=AppUser.InvitationSent&entityId={created.Id}")).Json!["total"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Ac1_ForgotPasswordWhenMailCannotBeSent_IsAnsweredAsEverAndTheQueueKeepsServing()
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var first = await admin.CreateUserAsync();
        var second = await admin.CreateUserAsync();

        var answerFirst = await host.Anonymous().PostAsync("/api/v1/auth/forgot-password", new { email = first.Email });
        var answerSecond = await host.Anonymous().PostAsync("/api/v1/auth/forgot-password", new { email = second.Email });

        answerFirst.Status.Should().Be(HttpStatusCode.Accepted);
        $"{answerFirst.ContentType}|{answerFirst.Body}".Should().Be($"{answerSecond.ContentType}|{answerSecond.Body}");
        foreach (var request in new[] { answerFirst, answerSecond })
        {
            await Waiting.UntilAsync(
                async () => (await admin.AuditOfRequestAsync(request.RequestId!)).Actions().SequenceEqual(["Auth.PasswordResetRequested", "Mail.SendFailed"]),
                "the queued request is journaled with its failed mail");
        }
    }

    [Fact]
    public async Task Ac1_Logs_NameTheFailureByTypeOnly_NeverTheAddressTheLinkTheTextOrTheSmtpCredentials()
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("quiet")}@test.local";
        (await admin.PostAsync("/api/v1/users", new { email, role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);

        var logs = await host.LogsAfterAsync("Mail was not sent");

        foreach (var secret in new[] { email, MailServerDownHost.UserName, MailServerDownHost.Password, ApiHost.PublicBaseUrl, "token=", "Здравствуйте".JsonEscaped(), "Приглашение".JsonEscaped() })
        {
            logs.Should().NotContain(secret);
        }
    }
}
