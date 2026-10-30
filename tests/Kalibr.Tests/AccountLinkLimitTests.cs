using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A host with the production interval between the password reset links that an anonymous request may issue to one account, which the
/// other hosts of the run switch off.
/// </summary>
/// <param name="environment">The shared environment that supplies the database and the mail server.</param>
public sealed class RecentLinkHost(TestEnvironment environment)
    : HostFixture(environment, new Dictionary<string, string> { ["AccountLinks__PasswordResetInterval"] = "00:05:00" });

/// <summary>
/// A host where the three anonymous link endpoints may be called only a few times from one address.
/// </summary>
/// <param name="environment">The shared environment that supplies the database and the mail server.</param>
public sealed class StrictPasswordResetHost(TestEnvironment environment)
    : HostFixture(environment, new Dictionary<string, string> { ["RateLimiting__PasswordReset__PermitLimit"] = StrictPasswordResetHost.PermitLimit.ToString() })
{
    public const int PermitLimit = 3;
}

/// <summary>
/// An anonymous request does not issue a link to an account that was sent one a moment ago, so nobody can flood a mailbox through it,
/// while an administrator's explicit action always sends.
/// </summary>
public sealed class ForgotPasswordIntervalTests(RecentLinkHost fixture, TestEnvironment environment) : IClassFixture<RecentLinkHost>
{
    [Fact]
    public async Task Ac7_ForgotPasswordRepeatedWithinTheInterval_IssuesNoSecondLink_ButTheAdministratorCanStillSendOne()
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var sentinel = await admin.CreateUserAsync();

        for (var request = 0; request < 3; request++)
        {
            (await host.Anonymous().PostAsync("/api/v1/auth/forgot-password", new { email = account.Email })).Expect(HttpStatusCode.Accepted);
        }

        (await host.Anonymous().PostAsync("/api/v1/auth/forgot-password", new { email = sentinel.Email })).Expect(HttpStatusCode.Accepted);
        await environment.Mail.WaitForAsync(sentinel.Email, 2);
        var viaQueue = await environment.Mail.FindAsync(account.Email);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/send-password-reset", ifMatch: (await admin.GetUserAsync(account.Id)).ETag)).Expect(HttpStatusCode.OK);
        var viaAdministrator = await environment.Mail.WaitForAsync(account.Email, 3);

        viaQueue.Select(mail => mail.Screen).Should().Equal(["accept-invitation", "reset-password"], "three requests in a row got one link");
        viaAdministrator[^1].Screen.Should().Be("reset-password");
        (await admin.GetAsync($"/api/v1/audit?action=Auth.PasswordResetRequested&entityId={account.Id}")).Expect(HttpStatusCode.OK)
            .Json!["total"]!.GetValue<int>().Should().Be(2, "the queued request and the administrator's action each issued a link, the skipped requests none");
    }
}

/// <summary>
/// The endpoints that set a password from a link or ask for one are limited per client address, all of them against one budget,
/// separate from the sign-in budget.
/// </summary>
public sealed class PasswordResetRateLimitTests(StrictPasswordResetHost fixture) : IClassFixture<StrictPasswordResetHost>
{
    [Fact]
    public async Task Ac7_RequestsBeyondThePermitLimit_AreRefusedWith429_WhicheverLinkEndpointTheyUse()
    {
        var host = fixture.Host;
        var anonymous = host.Anonymous();
        var someone = new { email = $"{Scenarios.Unique("nobody")}@test.local" };

        for (var request = 0; request < StrictPasswordResetHost.PermitLimit; request++)
        {
            (await anonymous.PostAsync("/api/v1/auth/forgot-password", someone)).Status.Should().Be(HttpStatusCode.Accepted);
        }

        var acceptInvitation = await anonymous.PostAsync("/api/v1/auth/accept-invitation", new { token = "any", password = Scenarios.UserPassword });
        var resetPassword = await anonymous.PostAsync("/api/v1/auth/reset-password", new { token = "any", password = Scenarios.UserPassword });
        var forgotPassword = await anonymous.PostAsync("/api/v1/auth/forgot-password", someone);
        var signIn = await anonymous.PostAsync("/api/v1/auth/login", new { email = ApiHost.AdminEmail, password = ApiHost.AdminPassword });

        acceptInvitation.Status.Should().Be(HttpStatusCode.TooManyRequests, "the budget is shared by the three endpoints");
        resetPassword.Status.Should().Be(HttpStatusCode.TooManyRequests);
        forgotPassword.Status.Should().Be(HttpStatusCode.TooManyRequests);
        signIn.Status.Should().Be(HttpStatusCode.OK, "signing in has a budget of its own");
    }
}
