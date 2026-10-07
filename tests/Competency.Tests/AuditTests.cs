using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Npgsql;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// AC-15: every mutation and every sign-in event is journaled with actor, role and request id, a refused change leaves no row,
/// only allow-listed properties are recorded so no secret reaches the journal, and the journal API is read-only and filterable.
/// </summary>
public sealed class AuditTests(TestEnvironment environment)
{
    private static readonly string[] UserProperties = ["email", "employeeId", "isBlocked", "role"];

    [Fact]
    public async Task Ac15_Mutations_AreJournaledWithActorRoleRequestIdAndOnlyTheChangedValues()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var adminId = (await admin.GetAsync("/api/v1/auth/me")).Id;
        var name = Scenarios.Unique("Отдел-журнал");
        var requestId = $"req-{Guid.NewGuid():N}";

        var created = (await admin.PostAsync("/api/v1/org-units", new { name }, configure: Header("X-Request-Id", requestId))).Expect(HttpStatusCode.Created);
        var renamed = Scenarios.Unique("Переименован");
        var updated = (await admin.PutAsync($"/api/v1/org-units/{created.Id}", new { name = renamed, headEmployeeId = (Guid?)null }, created.ETag)).Expect(HttpStatusCode.OK);
        var unchanged = (await admin.PutAsync($"/api/v1/org-units/{created.Id}", new { name = renamed, headEmployeeId = (Guid?)null }, updated.ETag)).Expect(HttpStatusCode.OK);

        created.RequestId.Should().Be(requestId, "a well-formed incoming request id is kept");
        var creation = (await admin.AuditOfRequestAsync(requestId)).Single()!;
        creation["action"]!.GetValue<string>().Should().Be("OrgUnit.Created");
        creation["entityType"]!.GetValue<string>().Should().Be("OrgUnit");
        creation["entityId"]!.GetValue<string>().Should().Be(created.Id.ToString());
        creation["actor"]!.GetValue<string>().Should().Be(adminId.ToString());
        creation["role"]!.GetValue<string>().Should().Be(Scenarios.GlobalAdmin);
        creation["oldValue"].Should().BeNull();
        creation["newValue"]!["name"]!.GetValue<string>().Should().Be(name);
        var change = (await admin.AuditOfRequestAsync(updated.RequestId!)).Single()!;
        change["action"]!.GetValue<string>().Should().Be("OrgUnit.Updated");
        Keys(change["oldValue"]).Should().Equal("name");
        change["oldValue"]!["name"]!.GetValue<string>().Should().Be(name);
        change["newValue"]!["name"]!.GetValue<string>().Should().Be(renamed);
        (await admin.AuditOfRequestAsync(unchanged.RequestId!)).Should().BeEmpty("a save that changes nothing is not a mutation");
        unchanged.ETag.Should().Be(updated.ETag);
    }

    [Fact]
    public async Task Ac15_MalformedIncomingRequestId_IsReplacedByAGeneratedOne()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var response = await admin.GetAsync("/api/v1/auth/me", Header("X-Request-Id", "not valid: has spaces"));

        response.RequestId.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task Ac15_RefusedMutations_LeaveNoJournalRow()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        await admin.CreateUnitAsync(parent.Id);

        var conflict = await admin.PostAsync($"/api/v1/org-units/{parent.Id}/deactivate", ifMatch: parent.ETag);
        var invalid = await admin.PostAsync("/api/v1/org-units", new { name = " " });
        var stale = await admin.PutAsync($"/api/v1/org-units/{parent.Id}", new { name = "Другое", headEmployeeId = (Guid?)null }, "\"999\"");

        conflict.Status.Should().Be(HttpStatusCode.Conflict);
        invalid.Status.Should().Be(HttpStatusCode.BadRequest);
        stale.Status.Should().Be(HttpStatusCode.PreconditionFailed);
        foreach (var refused in new[] { conflict, invalid, stale })
        {
            (await admin.AuditOfRequestAsync(refused.RequestId!)).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Ac15_Journal_NeverContainsPasswordsHashesOrStamps()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var secrets = new[] { $"Create-{Guid.NewGuid():N}-1!", $"Reset-{Guid.NewGuid():N}-2!", $"Change-{Guid.NewGuid():N}-3!", $"Wrong-{Guid.NewGuid():N}-4!" };
        var email = $"{Scenarios.Unique("secret")}@test.local";
        var created = (await admin.PostAsync("/api/v1/users", new { email, password = secrets[0], role = Scenarios.User, employeeId = (Guid?)null })).Expect(HttpStatusCode.Created);
        var reset = (await admin.PostAsync($"/api/v1/users/{created.Id}/reset-password", new { newPassword = secrets[1] }, created.ETag)).Expect(HttpStatusCode.OK);
        var session = await host.LoginAsync(email, secrets[1]);
        (await session.PostAsync("/api/v1/auth/change-password", new { currentPassword = secrets[1], newPassword = secrets[2] })).Expect(HttpStatusCode.NoContent);
        (await host.Anonymous().PostAsync("/api/v1/auth/login", new { email, password = secrets[3] })).Status.Should().Be(HttpStatusCode.Unauthorized);

        var creation = (await admin.AuditOfRequestAsync(created.RequestId!)).Single()!;
        Keys(creation["newValue"]).Should().Equal(UserProperties);
        (await admin.AuditOfRequestAsync(reset.RequestId!)).Actions().Should().Equal("AppUser.PasswordReset");
        foreach (var secret in secrets)
        {
            (await CountRowsAsync(host, $"concat_ws(' ', old_value::text, new_value::text, reason, entity_id, actor) LIKE '%{secret}%'"))
                .Should().Be(0, "no journal row may contain a password");
        }

        (await CountRowsAsync(host, "concat_ws(' ', old_value::text, new_value::text) ~* '(password|hash|stamp|lockout|concurrency)'"))
            .Should().Be(0, "only allow-listed properties are journaled");
    }

    [Fact]
    public async Task Ac15_SignInEvents_AreJournaledWithTheAccountIdentityAndNothingTyped()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var account = await admin.CreateUserAsync();
        var unknownEmail = $"{Scenarios.Unique("nobody")}@test.local";
        var typedPassword = $"Typed-{Guid.NewGuid():N}-1!";

        var unknown = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = unknownEmail, password = typedPassword });
        var attempts = new List<ApiResponse>();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            attempts.Add(await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = typedPassword }));
        }

        var locked = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = account.Email, password = account.Password });
        var other = await admin.CreateUserAsync();
        var session = await host.Anonymous().PostAsync("/api/v1/auth/login", new { email = other.Email, password = other.Password });
        var otherSession = await other.SignInAsync(host);
        var logout = await otherSession.PostAsync("/api/v1/auth/logout");

        var unknownRow = (await admin.AuditOfRequestAsync(unknown.RequestId!)).Single()!;
        unknownRow["action"]!.GetValue<string>().Should().Be("Auth.LoginFailed");
        unknownRow["actor"]!.GetValue<string>().Should().Be("unknown");
        unknownRow["role"]!.GetValue<string>().Should().Be("anonymous");
        unknownRow["reason"]!.GetValue<string>().Should().Be("UnknownUser");
        unknownRow["entityId"].Should().BeNull();
        foreach (var wrong in attempts.Take(4))
        {
            var row = (await admin.AuditOfRequestAsync(wrong.RequestId!)).Single()!;
            row["action"]!.GetValue<string>().Should().Be("Auth.LoginFailed");
            row["reason"]!.GetValue<string>().Should().Be("WrongPassword");
            row["actor"]!.GetValue<string>().Should().Be(account.Id.ToString());
            row["role"]!.GetValue<string>().Should().Be(Scenarios.User);
        }

        (await admin.AuditOfRequestAsync(attempts[4].RequestId!)).Actions().Should().Equal("Auth.LockedOut", "Auth.LoginFailed");
        locked.Status.Should().Be(HttpStatusCode.Unauthorized, "a locked account refuses even the right password");
        (await admin.AuditOfRequestAsync(locked.RequestId!)).Single()!["reason"]!.GetValue<string>().Should().Be("LockedOut");
        session.Expect(HttpStatusCode.OK);
        var success = (await admin.AuditOfRequestAsync(session.RequestId!)).Single()!;
        success["action"]!.GetValue<string>().Should().Be("Auth.LoginSucceeded");
        success["actor"]!.GetValue<string>().Should().Be(other.Id.ToString());
        var signOut = (await admin.AuditOfRequestAsync(logout.RequestId!)).Single()!;
        signOut["action"]!.GetValue<string>().Should().Be("Auth.Logout");
        signOut["entityId"]!.GetValue<string>().Should().Be(other.Id.ToString());
        (await CountRowsAsync(host, $"concat_ws(' ', old_value::text, new_value::text, reason, entity_id, actor) LIKE '%{unknownEmail}%' OR concat_ws(' ', reason, actor) LIKE '%{typedPassword}%'"))
            .Should().Be(0, "nothing the client typed may enter the journal");
    }

    [Fact]
    public async Task Ac15_JournalApi_FiltersPagesNewestFirstAndHasNoWriteEndpoint()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var adminId = (await admin.GetAsync("/api/v1/auth/me")).Id;
        var first = await admin.CreateUnitAsync();
        var second = await admin.CreateUnitAsync();

        var byEntity = (await admin.GetAsync($"/api/v1/audit?action=OrgUnit.Created&entityId={first.Id}")).Expect(HttpStatusCode.OK);
        var byActor = (await admin.GetAsync($"/api/v1/audit?actor={adminId}&pageSize=1")).Expect(HttpStatusCode.OK);
        var future = (await admin.GetAsync($"/api/v1/audit?from={Uri.EscapeDataString("2999-01-01T00:00:00Z")}")).Expect(HttpStatusCode.OK);
        var recent = (await admin.GetAsync($"/api/v1/audit?entityType=OrgUnit&actor={adminId}&pageSize=200")).Expect(HttpStatusCode.OK);
        var reversed = await admin.GetAsync($"/api/v1/audit?from={Uri.EscapeDataString("2030-01-01T00:00:00Z")}&to={Uri.EscapeDataString("2020-01-01T00:00:00Z")}");

        byEntity.Json!["total"]!.GetValue<int>().Should().Be(1);
        byActor.Json!["items"]!.AsArray().Should().ContainSingle();
        byActor.Json["total"]!.GetValue<int>().Should().BeGreaterThan(1);
        future.Json!["total"]!.GetValue<int>().Should().Be(0);
        var timestamps = recent.Json!["items"]!.AsArray().Select(row => row!["timestamp"]!.GetValue<DateTimeOffset>()).ToArray();
        timestamps.Should().BeInDescendingOrder();
        recent.Json["items"]!.AsArray().Select(row => row!["entityId"]!.GetValue<string>()).Should().Contain([first.Id.ToString(), second.Id.ToString()]);
        reversed.Status.Should().Be(HttpStatusCode.BadRequest);
        (await admin.GetAsync("/api/v1/audit?pageSize=201")).Status.Should().Be(HttpStatusCode.BadRequest);
        (await admin.GetAsync("/api/v1/audit?page=0")).Status.Should().Be(HttpStatusCode.BadRequest);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete, HttpMethod.Patch })
        {
            var attempt = await admin.SendAsync(method, $"/api/v1/audit/{first.Id}", new { action = "forged" });
            ((int)attempt.Status).Should().BeOneOf([404, 405], method.Method);
        }
    }

    private static Action<HttpRequestMessage> Header(string name, string value) => request => request.Headers.Add(name, value);

    private static string[] Keys(JsonNode? values) => [.. values!.AsObject().Select(pair => pair.Key).Order()];

    private static async Task<long> CountRowsAsync(ApiHost host, string condition)
    {
        await using var connection = new NpgsqlConnection(host.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM audit_events WHERE {condition}", connection);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}
