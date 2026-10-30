using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The legal entity directory: found or added by name regardless of letter case and surrounding spaces, searched by any signed-in user,
/// added by administrators and HR only, and never twice for one name even when two requests race.
/// </summary>
public sealed class LegalEntityTests(TestEnvironment environment)
{
    [Fact]
    public async Task Ac7_QuickAdd_CreatesTheEntity_AndAnotherSpellingOfTheNameFindsTheSame()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var name = Scenarios.Unique("ООО Ромашка");

        var created = await admin.LegalEntityAsync($"  {name}  ");
        var again = await admin.LegalEntityAsync(name.ToUpperInvariant());

        created.Json!["name"]!.GetValue<string>().Should().Be(name, "the name is stored without the surrounding spaces");
        again.Id.Should().Be(created.Id);
        again.Json!["name"]!.GetValue<string>().Should().Be(name, "the stored spelling stays");
        (await admin.GetAsync($"/api/v1/legal-entities?q={Uri.EscapeDataString(name)}")).Json!["total"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac7_QuickAdd_RefusesABlankNameAndOneOverTheLimit_AndAddsNothing()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var token = Scenarios.Unique("t")[2..];

        var blank = await admin.PostAsync("/api/v1/legal-entities/quick-add", new { name = "  " });
        var atLimit = await admin.PostAsync("/api/v1/legal-entities/quick-add", new { name = token + new string('я', 200 - token.Length) });
        var overLimit = await admin.PostAsync("/api/v1/legal-entities/quick-add", new { name = token + new string('я', 201 - token.Length) });

        blank.FieldErrors("name").Should().Equal("Укажите название");
        atLimit.Status.Should().Be(HttpStatusCode.OK);
        overLimit.FieldErrors("name").Should().Equal("Не длиннее 200 символов");
        (await admin.GetAsync($"/api/v1/legal-entities?q={token}")).Json!["total"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac7_List_IsOpenToEverySignedInUser_FiltersByTheSearchText_AndIsOrderedByName()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var user = await (await admin.CreateUserAsync()).SignInAsync(host);
        var token = Scenarios.Unique("t")[2..];
        foreach (var name in new[] { $"Яблоко {token}", $"Арбуз {token}", $"Ёж {token}" })
        {
            await admin.LegalEntityAsync(name);
        }

        var found = (await user.GetAsync($"/api/v1/legal-entities?q={token}")).Expect(HttpStatusCode.OK);
        var none = (await user.GetAsync($"/api/v1/legal-entities?q={token}-нет")).Expect(HttpStatusCode.OK);

        found.Json!["items"]!.AsArray().Select(item => item!["name"]!.GetValue<string>()).Should().Equal($"Арбуз {token}", $"Ёж {token}", $"Яблоко {token}");
        none.Json!["total"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Ac8_QuickAdd_IsOpenToHr_AndRefusedToAUserWithoutTheRole()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var hr = (await admin.HrCandidateAsync(host, unit.Id)).Client;
        var user = await (await admin.CreateUserAsync()).SignInAsync(host);

        var byHr = await hr.PostAsync("/api/v1/legal-entities/quick-add", new { name = Scenarios.Unique("ООО") });
        var byUser = await user.PostAsync("/api/v1/legal-entities/quick-add", new { name = Scenarios.Unique("ООО") });

        byHr.Status.Should().Be(HttpStatusCode.OK);
        byUser.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ac7_TwoRequestsForOneNewName_CreateItOnce()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var name = Scenarios.Unique("ООО Гонка");

        await using var held = await RowLock.HoldAdvisoryAsync(host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(host, 1, () => admin.PostAsync("/api/v1/legal-entities/quick-add", new { name }));
        var second = await RowLock.StartWaitingAsync(host, 2, () => admin.PostAsync("/api/v1/legal-entities/quick-add", new { name = name.ToUpperInvariant() }));
        await held.ReleaseAsync();
        var results = await Task.WhenAll(first, second);

        results.Select(result => result.Status).Should().OnlyContain(status => status == HttpStatusCode.OK);
        results[1].Id.Should().Be(results[0].Id);
        (await host.ScalarAsync<long>($"SELECT count(*) FROM legal_entities WHERE lower(name) = lower('{name}')")).Should().Be(1);
    }
}
