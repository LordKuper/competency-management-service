using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Wrong passwords tried at the same moment on one account, at sign-in or as the current password of a password change, are counted
/// one after another: none is lost, and only the attempt that reaches the limit starts the lockout and journals it.
/// The attempts are held on the account's row lock until all of them wait for it, because that is the window in which attempts
/// that do not wait for each other count against the same stale copy of the account.
/// </summary>
public sealed class SignInRaceTests(SignInRaceHost fixture) : IClassFixture<SignInRaceHost>
{
    private const int AttemptsBeforeLockout = 5;
    private const string WrongPassword = "Wrong-Pass-12345!";
    private const string NewPassword = "Guessed-Pass-12345!";

    public enum Surface
    {
        SignIn,
        ChangePassword,
    }

    [Theory]
    [InlineData(Surface.SignIn)]
    [InlineData(Surface.ChangePassword)]
    public async Task Ac6_TwoWrongPasswordsAtTheLimitAtTheSameMoment_AreRefusedAlike_AndStartTheLockoutOnce(Surface surface)
    {
        var host = fixture.Host;
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();

        var responses = await AttemptTogetherAsync(host, account, surface, attempts: 2, failedBefore: AttemptsBeforeLockout - 1);

        var refusal = surface == Surface.SignIn ? HttpStatusCode.Unauthorized : HttpStatusCode.BadRequest;
        responses.Select(response => response.Status).Should().AllBeEquivalentTo(refusal);
        responses.Select(WithoutTraceId).Distinct().Should().ContainSingle("both attempts are refused with the same response, whichever of them started the lockout");
        var journaled = (await Task.WhenAll(responses.Select(response => admin.AuditOfRequestAsync(response.RequestId!))))
            .SelectMany(rows => rows.Actions())
            .Order();
        string[] expected = surface == Surface.SignIn ? ["Auth.LockedOut", "Auth.LoginFailed", "Auth.LoginFailed"] : ["Auth.LockedOut"];
        journaled.Should().Equal(expected);
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.Unauthorized, "the account is locked");
        var lockouts = (await admin.GetAsync($"/api/v1/audit?action=Auth.LockedOut&entityId={account.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();
        lockouts.Should().ContainSingle("two attempts at the limit started one lockout");
    }

    [Theory]
    [InlineData(Surface.SignIn)]
    [InlineData(Surface.ChangePassword)]
    public async Task Ac6_WrongPasswordsAtTheSameMoment_AreAllCounted(Surface surface)
    {
        var host = fixture.Host;
        var account = await (await host.AdminAsync()).CreateUserAsync();
        const int Attempts = 3;

        await AttemptTogetherAsync(host, account, surface, Attempts, failedBefore: 0);

        (await host.ScalarAsync<int>($"SELECT access_failed_count FROM users WHERE id = '{account.Id}'")).Should().Be(Attempts, "no attempt is lost");
    }

    private static string WithoutTraceId(ApiResponse response) =>
        string.Join(',', response.Json!.AsObject().Where(property => property.Key != "traceId").Select(property => property.Value!.ToJsonString()));

    private static Task<ApiResponse> SignInAsync(ApiHost host, string email, string password) =>
        host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password });

    private static async Task<ApiResponse[]> AttemptTogetherAsync(ApiHost host, TestUser account, Surface surface, int attempts, int failedBefore)
    {
        var session = await account.SignInAsync(host);
        await host.ExecuteAsync($"UPDATE users SET access_failed_count = {failedBefore} WHERE id = '{account.Id}'");
        await using var held = await RowLock.HoldAsync(host, account.Id);

        var pending = Enumerable.Range(0, attempts)
            .Select(_ => surface == Surface.SignIn
                ? SignInAsync(host, account.Email, WrongPassword)
                : session.PostAsync("/api/v1/auth/change-password", new { currentPassword = WrongPassword, newPassword = NewPassword }))
            .ToArray();
        await RowLock.UntilWaitingAsync(host, attempts);
        await held.ReleaseAsync();
        return await Task.WhenAll(pending);
    }
}

/// <summary>
/// A host with a database of its own for the sign-in race tests, so that the locks waited for in that database are theirs alone.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class SignInRaceHost(TestEnvironment environment) : HostFixture(environment, null);
