using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The scenario prerequisites the desk reports follow the current data on a host that starts empty: each appears when the data
/// allows its scenario, goes when the data stops allowing it, and the one about accounts is told to the administrator alone.
/// </summary>
public sealed class DeskPrerequisiteTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac204_Ac205_Ac209_Ac210_Ac218_PrerequisitesAppearAndGoWithTheData_InTheOrderOfTheirDeclaration()
    {
        var admin = await fixture.Host.AdminAsync();
        var plain = await (await admin.CreateUserAsync()).SignInAsync(fixture.Host);

        var empty = await PrerequisitesAsync(admin);
        var unit = await admin.CreateUnitAsync();
        var withUnit = await PrerequisitesAsync(admin);
        (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
        var withPosition = await PrerequisitesAsync(admin);
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: Scenarios.Unique("Должность"));
        var withEmployee = await PrerequisitesAsync(admin);
        var direction = await admin.MakeDirectionAsync();
        var track = await admin.MakeTrackAsync(unit.Id, direction.Id);
        var adminSeesDraft = await PrerequisitesAsync(admin);
        var plainBeforeApproval = await PrerequisitesAsync(plain);
        await admin.MakeLevelAsync(track.Id);
        (await admin.ActivateAsync(track, unit.Id)).Expect(HttpStatusCode.OK);
        var plainAfterApproval = await PrerequisitesAsync(plain);
        await admin.InviteUserAsync(Scenarios.Hr);
        var withInvitedHr = await PrerequisitesAsync(admin);
        var hr = await admin.CreateUserAsync(Scenarios.Hr);
        var withHr = await PrerequisitesAsync(admin);
        var plainWithHr = await PrerequisitesAsync(plain);
        (await admin.PostAsync($"/api/v1/users/{hr.Id}/block", ifMatch: (await admin.GetUserAsync(hr.Id)).ETag)).Expect(HttpStatusCode.OK);
        var blockedHr = await PrerequisitesAsync(admin);
        (await admin.PostAsync($"/api/v1/employees/{employee.Id}/dismiss", ifMatch: employee.ETag)).Expect(HttpStatusCode.OK);
        var dismissed = await PrerequisitesAsync(admin);
        (await admin.PostAsync($"/api/v1/org-units/{unit.Id}/deactivate", ifMatch: (await admin.GetUnitAsync(unit.Id)).ETag)).Expect(HttpStatusCode.OK);
        var noActiveUnit = await PrerequisitesAsync(admin);

        empty.Should().BeEmpty();
        withUnit.Should().Equal("ActiveUnit");
        withPosition.Should().Equal("ActiveUnit", "ActivePosition");
        withEmployee.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee");
        adminSeesDraft.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack");
        plainBeforeApproval.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee");
        plainAfterApproval.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack");
        withInvitedHr.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack");
        withHr.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack", "HrAccount");
        plainWithHr.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack");
        blockedHr.Should().Equal("ActiveUnit", "ActivePosition", "WorkingEmployee", "VisibleTrack");
        dismissed.Should().Equal("ActiveUnit", "ActivePosition", "VisibleTrack");
        noActiveUnit.Should().Equal("ActivePosition", "VisibleTrack");
    }

    private static async Task<string[]> PrerequisitesAsync(ApiClient client) =>
        [.. (await client.DeskAsync())["prerequisites"]!.AsArray().Select(prerequisite => prerequisite!.GetValue<string>())];
}
