using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The number of SQL commands a desk request sends does not depend on how much data the desk is built from: counted in the host's own log of
/// database commands, once on a small set of units and once on a larger one.
/// </summary>
public sealed class DeskQueryCountTests(DeskQueryCountTests.SqlLoggingHost fixture) : IClassFixture<DeskQueryCountTests.SqlLoggingHost>
{
    private const int CommandBudget = 25;
    private const string CommandRecord = "\"EventId\":20101,";

    [Theory]
    [InlineData("/api/v1/desk")]
    [InlineData("/api/v1/desk/count")]
    public async Task Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(string route)
    {
        var admin = await fixture.Host.AdminAsync();
        await AddUnitsAsync(admin, 1);
        await CommandsAsync(admin, route);
        var small = await CommandsAsync(admin, route);

        await AddUnitsAsync(admin, 12);
        await CommandsAsync(admin, route);
        var large = await CommandsAsync(admin, route);

        small.Should().BeInRange(1, CommandBudget);
        large.Should().Be(small, "the desk reads each source in sets, not once per unit");
    }

    private async Task<int> CommandsAsync(ApiClient client, string route)
    {
        var probe = Guid.NewGuid().ToString("N");
        var before = fixture.Host.Logs.Length;

        (await client.GetAsync($"{route}?probe={probe}")).Expect(HttpStatusCode.OK);
        var logs = await fixture.Host.LogsAfterAsync($"{probe} - 200");

        return logs[before..].Split(CommandRecord).Length - 1;
    }

    private static async Task AddUnitsAsync(ApiClient admin, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var unit = await admin.CreateUnitAsync();
            var head = await admin.CreateEmployeeAsync(unit.Id);
            (await admin.SetHeadAsync(unit, head.Id)).Expect(HttpStatusCode.OK);
            await admin.InviteUserAsync(employeeId: head.Id);
        }
    }

    /// <summary>
    /// A host that logs the SQL commands it sends and the end of each request, the latter to tell when the console has caught up.
    /// </summary>
    /// <param name="environment">The shared environment that supplies the database.</param>
    public sealed class SqlLoggingHost(TestEnvironment environment) : HostFixture(environment, new Dictionary<string, string>
    {
        ["Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command"] = "Information",
        ["Logging__LogLevel__Microsoft.AspNetCore.Hosting.Diagnostics"] = "Information",
    });
}
