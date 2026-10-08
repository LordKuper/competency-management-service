using System.Net;
using System.Net.Sockets;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A mail server that requires a sign-in, and two hosts that send to it: one with the right credentials and one with a wrong password.
/// </summary>
/// <param name="environment">The shared environment that supplies the databases.</param>
public sealed class AuthenticatedMailHosts(TestEnvironment environment) : IAsyncLifetime
{
    public const string UserName = "smtp-user";
    public const string Password = "smtp-pass";

    public MailCatcher Mail { get; } = new(UserName, Password);

    public ApiHost Right { get; private set; } = null!;

    public ApiHost Wrong { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await Mail.StartAsync();
        Right = await environment.StartHostAsync(Mail.HostSettings);
        Wrong = await environment.StartHostAsync(new Dictionary<string, string>(Mail.HostSettings) { ["Smtp__Password"] = "not-the-password" });
    }

    public async ValueTask DisposeAsync()
    {
        await Right.DisposeAsync();
        await Wrong.DisposeAsync();
        await Mail.DisposeAsync();
    }
}

/// <summary>
/// A host whose mail server accepts the connection and then never answers, so that every send waits for its timeout.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class SilentMailHost(TestEnvironment environment) : IAsyncLifetime
{
    private readonly TcpListener silentServer = new(IPAddress.Loopback, 0);

    public ApiHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        silentServer.Start();
        var port = ((IPEndPoint)silentServer.LocalEndpoint).Port;
        Host = await environment.StartHostAsync(new Dictionary<string, string> { ["Smtp__Port"] = port.ToString(), ["Smtp__Host"] = "127.0.0.1", ["Smtp__Timeout"] = "00:00:08" });
    }

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync();
        silentServer.Stop();
    }
}

/// <summary>
/// Whether the credentials of the mail server are used when configured and kept out of sight, and that a server that does not answer
/// delays nothing but the request that waits for it.
/// </summary>
public sealed class MailTransportTests(AuthenticatedMailHosts authenticated, SilentMailHost silent)
    : IClassFixture<AuthenticatedMailHosts>, IClassFixture<SilentMailHost>
{
    [Fact]
    public async Task Ac1_ConfiguredSmtpCredentials_AreUsedToSendTheMail_AndAWrongPasswordMeansTheMailIsNotSent()
    {
        var emailRight = $"{Scenarios.Unique("auth")}@test.local";
        var emailWrong = $"{Scenarios.Unique("auth")}@test.local";

        var right = (await (await authenticated.Right.AdminAsync()).PostAsync("/api/v1/users", new { email = emailRight, role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var wrong = (await (await authenticated.Wrong.AdminAsync()).PostAsync("/api/v1/users", new { email = emailWrong, role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);

        right.Json!["mailSent"]!.GetValue<bool>().Should().BeTrue();
        (await authenticated.Mail.WaitForAsync(emailRight)).Should().ContainSingle();
        wrong.Json!["mailSent"]!.GetValue<bool>().Should().BeFalse("the server refused the credentials");
        (await authenticated.Mail.FindAsync(emailWrong)).Should().BeEmpty();
        authenticated.Wrong.Logs.Should().NotContain("not-the-password").And.NotContain(AuthenticatedMailHosts.Password);
    }

    [Fact]
    public async Task Ac1_AMailServerThatNeverAnswers_HoldsNoLockAndNoRow_AndTheRequestEndsAtTheTimeout()
    {
        var host = silent.Host;
        var admin = await host.AdminAsync();
        var email = $"{Scenarios.Unique("silent")}@test.local";

        var creation = admin.PostAsync("/api/v1/users", new { email, role = Scenarios.User, employeeId = (Guid?)null });
        await Waiting.UntilAsync(async () => await host.ScalarAsync<long>($"SELECT count(*) FROM users WHERE email = '{email}'") == 1, "the account is saved");
        var saved = (await admin.GetAsync($"/api/v1/users?q={Uri.EscapeDataString(email)}")).Expect(HttpStatusCode.OK).Json!["items"]![0]!;
        var treeChange = await admin.CreateUnitAsync();
        var accountChange = await admin.PutAsync(
            $"/api/v1/users/{saved["id"]!.GetValue<string>()}",
            new { email, role = Scenarios.GlobalAdmin, employeeId = (Guid?)null },
            $"\"{saved["version"]!.GetValue<int>()}\"");
        var pendingWhileOthersWorked = !creation.IsCompleted;
        var created = await creation;

        treeChange.Status.Should().Be(HttpStatusCode.Created);
        accountChange.Status.Should().Be(HttpStatusCode.OK, "neither the organization lock nor the account row is held while the mail is sent");
        pendingWhileOthersWorked.Should().BeTrue("the creating request itself still waits for the mail server");
        created.Status.Should().Be(HttpStatusCode.Created, created.Body);
        created.Json!["mailSent"]!.GetValue<bool>().Should().BeFalse("the wait ended at the timeout, not with an error");
    }
}
