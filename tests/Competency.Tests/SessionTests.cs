using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Sign-in reveals nothing about accounts, lockout holds even against the right password, and blocking,
/// a password reset, a password change or sign-out end sessions as specified.
/// </summary>
public sealed class SessionTests(TestEnvironment environment)
{
    private const int AttemptsBeforeLockout = 5;
    private const string WrongPassword = "Wrong-Pass-12345!";

    [Fact]
    public async Task Ac6_EveryFailedSignIn_LooksTheSame()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var blocked = await admin.CreateUserAsync();
        (await admin.PostAsync($"/api/v1/users/{blocked.Id}/block", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        var unit = await admin.CreateUnitAsync();
        var gone = await admin.CreateEmployeeAsync(unit.Id);
        var leaver = await admin.CreateUserAsync(employeeId: gone.Id);
        (await admin.PostAsync($"/api/v1/employees/{gone.Id}/dismiss", ifMatch: gone.ETag)).Expect(HttpStatusCode.OK);
        var known = await admin.CreateUserAsync();

        var failures = new[]
        {
            await SignInAsync(host, $"{Scenarios.Unique("nobody")}@test.local", "Whatever-12345!"),
            await SignInAsync(host, known.Email, "Wrong-Pass-12345!"),
            await SignInAsync(host, blocked.Email, blocked.Password),
            await SignInAsync(host, leaver.Email, leaver.Password),
        };

        failures.Select(failure => failure.Status).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        failures.Select(failure => failure.Json!["title"]!.GetValue<string>()).Distinct().Should().ContainSingle();
        failures.Select(failure => failure.Json!["detail"]!.GetValue<string>()).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task Ac6_LockoutAfterFiveWrongPasswords_HoldsAgainstTheRightPasswordUntilAnAdministratorUnblocks()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            (await SignInAsync(host, account.Email, "Wrong-Pass-12345!")).Status.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.Unauthorized);
        var current = await admin.GetUserAsync(account.Id);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/unblock", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac6_AttemptWhoseCountCannotBeSaved_FailsTheRequestInsteadOfBeingTakenForCounted()
    {
        var host = await environment.SharedHostAsync();
        var account = await (await host.AdminAsync()).CreateUserAsync();
        await host.ExecuteAsync($"UPDATE users SET access_failed_count = 2, user_name = '' WHERE id = '{account.Id}'");

        var wrong = await SignInAsync(host, account.Email, WrongPassword);
        var right = await SignInAsync(host, account.Email, account.Password);

        wrong.Status.Should().Be(HttpStatusCode.InternalServerError, "a wrong password that cannot be counted is not answered as a counted one");
        right.Status.Should().Be(HttpStatusCode.InternalServerError, "a right password that cannot clear the count is not answered as a success");
    }

    [Fact]
    public async Task Ac8_Block_EndsTheLiveSession_AndUnblockLetsTheUserSignInAgain()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);
        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK);

        var blocked = (await admin.PostAsync($"/api/v1/users/{account.Id}/block", ifMatch: account.ETag)).Expect(HttpStatusCode.OK);

        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/unblock", ifMatch: blocked.ETag)).Expect(HttpStatusCode.OK);
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac7_PasswordReset_EndsSessionsAndReplacesThePassword()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);
        const string NewPassword = "Reset-Pass-12345!";

        var weak = await admin.PostAsync($"/api/v1/users/{account.Id}/reset-password", new { newPassword = "short" }, account.ETag);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/reset-password", new { newPassword = NewPassword }, account.ETag)).Expect(HttpStatusCode.OK);

        weak.Status.Should().Be(HttpStatusCode.BadRequest);
        weak.FieldErrors("newPassword").Should().NotBeEmpty();
        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(host, account.Email, NewPassword)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac6_ChangePassword_KeepsTheCurrentSessionAndEndsTheOthers()
    {
        var host = await environment.SharedHostAsync();
        var account = await (await host.AdminAsync()).CreateUserAsync();
        var current = await account.SignInAsync(host);
        var another = await account.SignInAsync(host);
        const string NewPassword = "Changed-Pass-12345!";

        var wrongCurrent = await current.PostAsync("/api/v1/auth/change-password", new { currentPassword = "Wrong-Pass-12345!", newPassword = NewPassword });
        (await current.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = NewPassword })).Expect(HttpStatusCode.NoContent);

        wrongCurrent.Status.Should().Be(HttpStatusCode.BadRequest);
        wrongCurrent.FieldErrors("currentPassword").Should().NotBeEmpty();
        (await current.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK);
        (await another.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(host, account.Email, NewPassword)).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac6_WrongCurrentPasswordInChangePassword_LocksTheAccountLikeWrongSignIns()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var session = await account.SignInAsync(host);
        const string NewPassword = "Guessed-Pass-12345!";

        var attempts = new List<ApiResponse>();
        for (var attempt = 0; attempt < AttemptsBeforeLockout; attempt++)
        {
            var wrong = await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = WrongPassword, newPassword = NewPassword });
            wrong.Status.Should().Be(HttpStatusCode.BadRequest);
            attempts.Add(wrong);
        }

        var signIn = await SignInAsync(host, account.Email, account.Password);
        var rightCurrent = await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = NewPassword });
        var lockouts = (await admin.GetAsync($"/api/v1/audit?action=Auth.LockedOut&entityId={account.Id}")).Expect(HttpStatusCode.OK).Json!["items"]!.AsArray();
        var current = await admin.GetUserAsync(account.Id);
        (await admin.PostAsync($"/api/v1/users/{account.Id}/unblock", ifMatch: current.ETag)).Expect(HttpStatusCode.OK);

        signIn.Status.Should().Be(HttpStatusCode.Unauthorized, "the wrong current passwords counted as failed sign-ins");
        rightCurrent.Status.Should().Be(HttpStatusCode.BadRequest, "a locked account is refused even with the right current password");
        rightCurrent.FieldErrors("currentPassword").Should().NotBeEmpty();
        lockouts.Should().ContainSingle("the lockout that change-password started is journaled once, as at sign-in");
        lockouts[0]!["actor"]!.GetValue<string>().Should().Be(account.Id.ToString());
        lockouts[0]!["role"]!.GetValue<string>().Should().Be(Scenarios.User);
        (await admin.AuditOfRequestAsync(attempts[^1].RequestId!)).Actions().Should().Equal(["Auth.LockedOut"], "the attempt that reached the limit wrote the event");
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.OK, "the refused change left the password as it was");
        (await SignInAsync(host, account.Email, NewPassword)).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac6_SuccessfulPasswordChange_ClearsTheCountOfWrongCurrentPasswords()
    {
        var host = await environment.SharedHostAsync();
        var account = await (await host.AdminAsync()).CreateUserAsync();
        var session = await account.SignInAsync(host);
        const string NewPassword = "Cleared-Pass-12345!";
        const string AnotherPassword = "Another-Pass-12345!";

        for (var attempt = 0; attempt < AttemptsBeforeLockout - 1; attempt++)
        {
            (await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = WrongPassword, newPassword = NewPassword }))
                .Status.Should().Be(HttpStatusCode.BadRequest);
        }

        (await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = NewPassword })).Expect(HttpStatusCode.NoContent);
        for (var attempt = 0; attempt < AttemptsBeforeLockout - 1; attempt++)
        {
            (await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = WrongPassword, newPassword = AnotherPassword }))
                .Status.Should().Be(HttpStatusCode.BadRequest);
        }

        (await SignInAsync(host, account.Email, NewPassword)).Status.Should().Be(
            HttpStatusCode.OK,
            "the wrong attempts before the change no longer count, so the account is not locked");
    }

    [Fact]
    public async Task Ac6_ChangePassword_RefusesANewPasswordThatBreaksThePolicy_AndChangesNothing()
    {
        var host = await environment.SharedHostAsync();
        var account = await (await host.AdminAsync()).CreateUserAsync();
        var session = await account.SignInAsync(host);
        const string WeakPassword = "short";

        var weak = await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = account.Password, newPassword = WeakPassword });

        weak.Status.Should().Be(HttpStatusCode.BadRequest, weak.Body);
        weak.FieldErrors("newPassword").Should().NotBeEmpty();
        (await SignInAsync(host, account.Email, account.Password)).Status.Should().Be(HttpStatusCode.OK, "a refused change leaves the password as it was");
        (await SignInAsync(host, account.Email, WeakPassword)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ac6_SignOut_EndsTheSession()
    {
        var host = await environment.SharedHostAsync();
        var account = await (await host.AdminAsync()).CreateUserAsync();
        var session = await account.SignInAsync(host);

        (await session.PostAsync("/api/v1/auth/logout")).Expect(HttpStatusCode.NoContent);

        (await session.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<ApiResponse> SignInAsync(ApiHost host, string email, string password) =>
        host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password });
}

/// <summary>
/// The sign-in endpoint is rate limited with the configured limit, on a host where the limit is low enough to reach.
/// </summary>
public sealed class LoginRateLimitTests(StrictRateLimitHost fixture) : IClassFixture<StrictRateLimitHost>
{
    [Fact]
    public async Task Ac6_SignInBeyondThePermitLimit_IsRefusedWith429EvenWithTheRightPassword()
    {
        var anonymous = fixture.Host.Anonymous();
        var credentials = new { email = ApiHost.AdminEmail, password = "Wrong-Pass-12345!" };

        for (var attempt = 0; attempt < StrictRateLimitHost.PermitLimit; attempt++)
        {
            (await anonymous.PostAsync("/api/v1/auth/login", credentials)).Status.Should().Be(HttpStatusCode.Unauthorized);
        }

        var limited = await anonymous.PostAsync("/api/v1/auth/login", new { email = ApiHost.AdminEmail, password = ApiHost.AdminPassword });

        limited.Status.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
