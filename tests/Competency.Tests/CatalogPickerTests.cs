using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What a picker of the web client asks the catalogue lists for, so that a long catalogue is searched on the server: <c>inWork</c> leaves out the
/// inactive entries, <c>withGeneral</c> adds the general competencies to those of a direction, and <c>forOrgUnitId</c> keeps the tracks a matrix of a
/// unit may be built on, those of the unit itself and of the units above it.
/// </summary>
public sealed class CatalogPickerTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    public static TheoryData<string> Kinds => new() { "directions", "scales", "competencies", "tracks" };

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task Ac9_InWork_LeavesOutTheInactiveEntries_AndKeepsTheActiveOnesAndTheDrafts(string kind)
    {
        var org = await fixture.OrgAsync();
        var prefix = Scenarios.Unique("Выбор");
        var names = new[] { $"{prefix}-действует", $"{prefix}-выключена", $"{prefix}-черновик" };
        await NewAsync(kind, org, org.Admin, names[0]);
        var inactive = await NewAsync(kind, org, org.Admin, names[1]);
        await NewAsync(kind, org, org.TeamHead.Client, names[2]);
        (await org.Admin.PostAsync($"/api/v1/career/{kind}/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        async Task<string[]> Listed(string filter) =>
            (await org.Admin.GetAsync($"/api/v1/career/{kind}?q={Uri.EscapeDataString(prefix)}&pageSize=200{filter}")).Expect(HttpStatusCode.OK).ItemNames();

        (await Listed("&inWork=true")).Should().BeEquivalentTo([names[0], names[2]]);
        (await Listed(string.Empty)).Should().BeEquivalentTo(names);
        (await Listed("&inWork=false")).Should().BeEquivalentTo(names);
    }

    [Fact]
    public async Task Ac14_WithGeneral_AddsTheGeneralCompetenciesToThoseOfTheDirection_WhileTheScaleAndInWorkStillNarrowThem()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var other = await org.Admin.MakeDirectionAsync();
        var baseScale = Guid.Parse(CareerScenarios.BaseScaleId);
        var otherScale = (await org.Admin.MakeScaleAsync()).Id;
        var prefix = Scenarios.Unique("Общие");
        var names = new
        {
            Own = $"{prefix}-направления",
            General = $"{prefix}-общая",
            OfOther = $"{prefix}-чужого",
            OnOtherScale = $"{prefix}-другая шкала",
            Inactive = $"{prefix}-выключена",
        };
        await org.Admin.MakeCompetencyAsync(names.Own, direction.Id, scaleId: baseScale);
        await org.Admin.MakeCompetencyAsync(names.General, scaleId: baseScale);
        await org.Admin.MakeCompetencyAsync(names.OfOther, other.Id, scaleId: baseScale);
        await org.Admin.MakeCompetencyAsync(names.OnOtherScale, scaleId: otherScale);
        var inactive = await org.Admin.MakeCompetencyAsync(names.Inactive, scaleId: baseScale);
        (await org.Admin.PostAsync($"/api/v1/career/competencies/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        async Task<string[]> Listed(string filter) =>
            (await org.Admin.GetAsync($"/api/v1/career/competencies?q={Uri.EscapeDataString(prefix)}&pageSize=200&directionId={direction.Id}{filter}")).Expect(HttpStatusCode.OK).ItemNames();

        (await Listed(string.Empty)).Should().BeEquivalentTo([names.Own]);
        (await Listed("&withGeneral=true")).Should().BeEquivalentTo([names.Own, names.General, names.OnOtherScale, names.Inactive]);
        (await Listed($"&withGeneral=true&scaleId={baseScale}&inWork=true")).Should().BeEquivalentTo([names.Own, names.General]);
    }

    [Fact]
    public async Task Ac14_TracksForAUnit_AreThoseOfTheUnitAndOfTheUnitsAboveIt_OnTheDirection_AndNoOtherUnit()
    {
        var org = await fixture.OrgAsync();
        var direction = await org.Admin.MakeDirectionAsync();
        var other = await org.Admin.MakeDirectionAsync();
        var ofRoot = await org.Admin.MakeActiveTrackAsync(org.Root.Id, direction.Id);
        var ofDepartment = await org.Admin.MakeActiveTrackAsync(org.Department.Id, direction.Id);
        var ofTeam = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        var ofSibling = await org.Admin.MakeActiveTrackAsync(org.Sibling.Id, direction.Id);
        await org.Admin.MakeActiveTrackAsync(org.Team.Id, other.Id);
        var inactive = await org.Admin.MakeActiveTrackAsync(org.Team.Id, direction.Id);
        (await org.Admin.PostAsync($"/api/v1/career/tracks/{inactive.Id}/deactivate", ifMatch: inactive.ETag)).Expect(HttpStatusCode.OK);

        async Task<string[]> ForUnit(Guid unitId, string filter = "") =>
            (await org.Hr.Client.GetAsync($"/api/v1/career/tracks?directionId={direction.Id}&forOrgUnitId={unitId}&pageSize=200{filter}")).Expect(HttpStatusCode.OK).ItemIds();

        (await ForUnit(org.Team.Id, "&inWork=true")).Should().BeEquivalentTo(Ids(ofRoot, ofDepartment, ofTeam));
        (await ForUnit(org.Team.Id)).Should().BeEquivalentTo(Ids(ofRoot, ofDepartment, ofTeam, inactive), "without inWork the inactive track of the unit stays");
        (await ForUnit(org.Sibling.Id)).Should().BeEquivalentTo(Ids(ofRoot, ofDepartment, ofSibling));
        (await ForUnit(org.Department.Id)).Should().BeEquivalentTo(Ids(ofRoot, ofDepartment));
        (await ForUnit(org.Root.Id)).Should().BeEquivalentTo(Ids(ofRoot));
        (await ForUnit(Guid.NewGuid())).Should().BeEmpty("an unknown unit has no ancestry, so no track may serve its matrix");
    }

    private static string[] Ids(params ApiResponse[] tracks) => [.. tracks.Select(track => track.Id.ToString())];

    private static async Task<ApiResponse> NewAsync(string kind, CareerOrg org, ApiClient client, string name)
    {
        object body = kind switch
        {
            "directions" => new { name },
            "scales" => new { name, points = CareerScenarios.Points() },
            "competencies" => new { name, description = (string?)null, directionId = (Guid?)null, scaleId = (Guid?)null },
            _ => new { name, ownerOrgUnitId = org.Team.Id, directionId = (await org.Admin.MakeDirectionAsync()).Id },
        };
        return (await client.PostAsync($"/api/v1/career/{kind}", body)).Expect(HttpStatusCode.Created);
    }
}
