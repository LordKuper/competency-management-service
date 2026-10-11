using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Who reads what of a matrix: everyone reads matrices and their published and archived versions; a version in work is read by its author, the
/// head and the responsible HR of the matrix unit, the approver of the superior step, whoever acted on it and administrators, and answers 404
/// to the rest; the history of a version is for those participants only, whatever the version's status.
/// </summary>
public sealed class MatrixAccessTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    [Fact]
    public async Task Ac18_AVersionInWork_IsReadByItsParticipantsAndAdministrators_AndNotFoundForTheRest()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();

        foreach (var reader in new[] { org.TeamHead, org.DepartmentHead, org.Hr })
        {
            (await kit.GetVersionAsync(reader.Client)).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
        }

        (await kit.GetVersionAsync(org.Admin)).Status.Should().Be(HttpStatusCode.OK);
        foreach (var stranger in new[] { org.SiblingHr, org.SiblingHead, org.RootHead, org.Plain })
        {
            (await kit.GetVersionAsync(stranger.Client)).Status.Should().Be(HttpStatusCode.NotFound, stranger.User.Email);
        }

        (await fixture.Host.Anonymous().GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}")).Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac18_PublishedAndArchivedVersions_AreReadByEveryone_WithTheirSnapshot()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        var first = kit.VersionId;
        var second = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created).Json!["id"]!.GetValue<Guid>();
        (await kit.TransitAsync(org.TeamHead.Client, "submit", versionId: second)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve", versionId: second)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: second)).Expect(HttpStatusCode.OK);

        foreach (var id in new[] { first, second })
        {
            foreach (var reader in new[] { org.Plain, org.SiblingHead, org.SiblingHr })
            {
                var version = (await kit.GetVersionAsync(reader.Client, id)).Expect(HttpStatusCode.OK);
                version.Json!["entries"]!["levels"]!.AsArray().Should().HaveCount(2);
                version.Json["entries"]!["scale"]!["points"]!.AsArray().Should().HaveCount(5);
            }
        }

        (await kit.GetVersionAsync(org.Plain.Client, first)).Json!["status"]!.GetValue<string>().Should().Be("Archived");
    }

    [Fact]
    public async Task Ac18_TheMatrix_IsReadByEveryone_AndShowsTheVersionInWorkOnlyToItsReaders()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created);

        var forAuthor = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}")).Expect(HttpStatusCode.OK).Json!;
        var forPlain = (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}")).Expect(HttpStatusCode.OK).Json!;
        var versionsForAuthor = (await org.TeamHead.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.OK).Json!.AsArray();
        var versionsForPlain = (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.OK).Json!.AsArray();
        var listForPlain = (await org.Plain.Client.GetAsync($"/api/v1/career/matrices?trackId={kit.Track.Id}")).Expect(HttpStatusCode.OK);

        forAuthor["inWorkVersion"]!["number"]!.GetValue<int>().Should().Be(2);
        forAuthor["publishedVersion"]!["number"]!.GetValue<int>().Should().Be(1);
        forPlain["inWorkVersion"].Should().BeNull();
        forPlain["publishedVersion"]!["number"]!.GetValue<int>().Should().Be(1);
        versionsForAuthor.Select(version => version!["number"]!.GetValue<int>()).Should().Equal(2, 1);
        versionsForPlain.Select(version => version!["number"]!.GetValue<int>()).Should().Equal(1);
        listForPlain.Json!["items"]![0]!["inWorkVersion"].Should().BeNull();
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{Guid.NewGuid()}")).Status.Should().Be(HttpStatusCode.NotFound);
        (await org.Plain.Client.GetAsync($"/api/v1/career/matrices/{Guid.NewGuid()}/versions")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac18_TheHistory_VariantsAndDiff_AreForTheParticipantsOnly_WhateverTheStatusOfTheVersion()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        var paths = new[] { "history", "variants/1", "variants/1/diff" };

        foreach (var reader in new[] { org.TeamHead, org.DepartmentHead, org.Hr })
        {
            foreach (var path in paths)
            {
                (await org.Admin.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/{path}")).Status.Should().Be(HttpStatusCode.OK);
                (await reader.Client.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/{path}")).Status.Should().Be(HttpStatusCode.OK, $"{reader.User.Email} {path}");
            }
        }

        foreach (var stranger in new[] { org.Plain, org.SiblingHr, org.SiblingHead })
        {
            foreach (var path in paths)
            {
                (await stranger.Client.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/{path}")).Status.Should().Be(HttpStatusCode.NotFound, $"{stranger.User.Email} {path}");
            }
        }

        (await kit.GetVersionAsync(org.Plain.Client)).Status.Should().Be(HttpStatusCode.OK, "the published version itself stays open to everyone");
        (await org.Admin.GetAsync($"/api/v1/career/matrix-versions/{kit.VersionId}/variants/9")).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac18_History_NamesTheVariantsAndTheDecisionsWithWhoActedAndWhen()
    {
        var org = await fixture.OrgAsync();
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();

        var history = (await kit.HistoryAsync(org.Hr.Client)).Expect(HttpStatusCode.OK).Json!;

        var variants = history["variants"]!.AsArray();
        variants.Should().ContainSingle();
        variants[0]!["kind"]!.GetValue<string>().Should().Be("Author");
        variants[0]!["createdById"]!.GetValue<Guid>().Should().Be(org.TeamHead.Id);
        var decisions = history["decisions"]!.AsArray();
        decisions.Select(decision => decision!["sequence"]!.GetValue<int>()).Should().Equal(1, 2, 3, 4);
        decisions[0]!["actorId"]!.GetValue<Guid>().Should().Be(org.TeamHead.Id);
        decisions[0]!["variantNumber"]!.GetValue<int>().Should().Be(1);
        decisions[1]!["actorId"]!.GetValue<Guid>().Should().Be(org.DepartmentHead.Id);
        decisions[2]!["actorId"]!.GetValue<Guid>().Should().Be(org.Hr.Id);
        decisions[3]!["actorId"].Should().BeNull("the publication is the route's own act");
        decisions.Select(decision => decision!["at"]!.GetValue<DateTimeOffset>()).Should().BeInAscendingOrder();
        decisions[0]!["actorName"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Ac18_AParticipantWhoActed_KeepsReadingTheVersion_EvenAfterLosingTheRole()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        var employee = await org.Admin.CreateEmployeeAsync(org.Department.Id);
        (await org.Admin.SetHeadAsync(org.Department, employee.Id)).Expect(HttpStatusCode.OK);

        (await kit.GetVersionAsync(org.DepartmentHead.Client)).Status.Should().Be(HttpStatusCode.OK);
        (await kit.HistoryAsync(org.DepartmentHead.Client)).Status.Should().Be(HttpStatusCode.OK);
        (await kit.GetVersionAsync(org.Plain.Client)).Status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ac18_TheDraftEntriesOfAVersion_AreReadByWhoeverReadsTheVersion()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var scale = (await org.TeamHead.Client.PostAsync("/api/v1/career/scales", new { name = Scenarios.Unique("Своя шкала"), points = CareerScenarios.Points() })).Expect(HttpStatusCode.Created);
        var kit = await (await MatrixKit.CreateAsync(org, scaleId: scale.Id, competencyAuthor: org.TeamHead.Client)).PrepareAsync();

        foreach (var reader in new[] { org.TeamHead, org.DepartmentHead, org.Hr })
        {
            (await reader.Client.GetAsync($"/api/v1/career/scales/{scale.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
            (await reader.Client.GetAsync($"/api/v1/career/competencies/{kit.Critical.Id}")).Status.Should().Be(HttpStatusCode.OK, reader.User.Email);
        }

        foreach (var stranger in new[] { org.Plain, org.SiblingHead, org.RootHead })
        {
            (await stranger.Client.GetAsync($"/api/v1/career/scales/{scale.Id}")).Status.Should().Be(HttpStatusCode.NotFound, stranger.User.Email);
            (await stranger.Client.GetAsync($"/api/v1/career/competencies/{kit.Critical.Id}")).Status.Should().Be(HttpStatusCode.NotFound, stranger.User.Email);
        }
    }

    [Fact]
    public async Task Ac16_Approvals_ListTheVersionsWaitingForTheUser_LongestWaitingFirst_AndFilterByUnitAndStep()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var first = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var second = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var third = await (await MatrixKit.CreateAsync(org, org.Sibling.Id, org.SiblingHead.Client)).PrepareAsync();
        (await first.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await second.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await third.TransitAsync(org.SiblingHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await second.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);

        var superior = await ApprovalsAsync(org.DepartmentHead.Client);
        var hr = await ApprovalsAsync(org.Hr.Client);
        var superiorOfTeam = await ApprovalsAsync(org.DepartmentHead.Client, $"orgUnitId={org.Team.Id}");
        var superiorAtHrStep = await ApprovalsAsync(org.DepartmentHead.Client, "step=Hr");
        var hrAtHrStep = await ApprovalsAsync(org.Hr.Client, "step=Hr");

        superior.Select(row => row.VersionId).Should().Equal(first.VersionId, third.VersionId);
        hr.Select(row => row.VersionId).Should().Equal(second.VersionId);
        superiorOfTeam.Select(row => row.VersionId).Should().Equal(first.VersionId);
        superiorAtHrStep.Should().BeEmpty();
        hrAtHrStep.Select(row => row.VersionId).Should().Equal(second.VersionId);
        superior[0].Step.Should().Be("Superior");
        superior[0].Unit.Should().Be(org.Team.Id);
        (await ApprovalsAsync(org.Plain.Client)).Should().BeEmpty();
        (await ApprovalsAsync(org.TeamHead.Client)).Should().BeEmpty("the author waits for the others");
        (await ApprovalsAsync(org.Admin)).Should().BeEmpty("an administrator has no turn of their own on the route");
    }

    [Fact]
    public async Task Ac24_Matrices_AreFilteredByUnitDirectionAndTrack()
    {
        var org = await fixture.OrgAsync();
        var first = await MatrixKit.CreateAsync(org);
        var second = await MatrixKit.CreateAsync(org, org.Sibling.Id, org.SiblingHead.Client);

        var byUnit = await MatrixIdsAsync(org.Plain.Client, $"orgUnitId={org.Sibling.Id}&directionId={second.Direction.Id}");
        var byDirection = await MatrixIdsAsync(org.Plain.Client, $"directionId={first.Direction.Id}");
        var byTrack = await MatrixIdsAsync(org.Plain.Client, $"trackId={second.Track.Id}");
        var mismatch = await MatrixIdsAsync(org.Plain.Client, $"orgUnitId={org.Team.Id}&trackId={second.Track.Id}");

        byUnit.Should().Equal(second.MatrixId.ToString());
        byDirection.Should().Equal(first.MatrixId.ToString());
        byTrack.Should().Equal(second.MatrixId.ToString());
        mismatch.Should().BeEmpty();
    }

    private static async Task<List<Approval>> ApprovalsAsync(ApiClient client, string query = "") =>
        [.. (await client.GetAsync($"/api/v1/career/approvals?{query}")).Expect(HttpStatusCode.OK).Json!.AsArray()
            .Select(row => new Approval(row!["versionId"]!.GetValue<Guid>(), row["step"]!.GetValue<string>(), row["orgUnitId"]!.GetValue<Guid>()))];

    private static async Task<string[]> MatrixIdsAsync(ApiClient client, string query) =>
        (await client.GetAsync($"/api/v1/career/matrices?{query}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds();

    private sealed record Approval(Guid VersionId, string Step, Guid Unit);
}
