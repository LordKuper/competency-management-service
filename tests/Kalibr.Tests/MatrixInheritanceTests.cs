using System.Net;
using AwesomeAssertions;
using Kalibr.CareerFramework;
using Kalibr.OrgStructure;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// A unit's published matrix of a track applies in the units below it that have none of their own: the one rule over a tree of units, and what
/// an employee of a lower unit, the matrix page and the HR see of it. A matrix still in work never blocks the inheritance.
/// </summary>
public sealed class MatrixInheritanceTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private const string Place = "/api/v1/career/me/place";

    private static readonly Guid Root = Guid.NewGuid();
    private static readonly Guid Department = Guid.NewGuid();
    private static readonly Guid Team = Guid.NewGuid();
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid Sibling = Guid.NewGuid();

    private static OrgSnapshot Tree()
    {
        OrgUnitStatus Unit(Guid id, Guid? parent) => new(id, id.ToString(), parent, null, true);
        return new OrgSnapshot(
            new[] { Unit(Root, null), Unit(Department, Root), Unit(Team, Department), Unit(Branch, Team), Unit(Sibling, Department) }.ToDictionary(unit => unit.Id),
            Array.Empty<Guid>().ToLookup(id => id, id => id),
            new Dictionary<Guid, PositionStatus>(),
            new Dictionary<(Guid, Guid), int>(),
            new Dictionary<(Guid, Guid), int>());
    }

    [Fact]
    public void Ac91_AMatrixIsInEffectInItsUnitAndEveryUnitBelowIt_ThatHasNoneOfItsOwn()
    {
        var units = MatrixInheritance.UnitsInEffect(Tree(), Department, own => own == Department);

        units.Select(unit => unit.Id).Should().BeEquivalentTo([Department, Team, Branch, Sibling]);
    }

    [Fact]
    public void Ac91_AUnitsOwnMatrixReplacesTheInheritedOneForItAndForTheUnitsBelowItWithoutOne()
    {
        var published = new HashSet<Guid> { Department, Team };

        var department = MatrixInheritance.UnitsInEffect(Tree(), Department, published.Contains).Select(unit => unit.Id);
        var team = MatrixInheritance.UnitsInEffect(Tree(), Team, published.Contains).Select(unit => unit.Id);

        department.Should().BeEquivalentTo([Department, Sibling]);
        team.Should().BeEquivalentTo([Team, Branch]);
    }

    [Fact]
    public void Ac91_TheNearestUnitAboveWithAMatrix_IsTheOneThatApplies()
    {
        var ancestry = Tree().Ancestry(Branch);

        MatrixInheritance.Nearest(ancestry, id => id == Root || id == Department)!.Id.Should().Be(Department);
        MatrixInheritance.Nearest(ancestry, _ => false).Should().BeNull();
    }

    [Fact]
    public async Task Ac91_AnEmployeeOfALowerUnit_SeesTheMatrixOfTheUnitAbove_AsTheirOwn()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var branch = await org.Admin.CreateUnitAsync(org.Team.Id);
        var employee = await EmployeeOnAsync(org, kit, branch.Id);

        var place = (await employee.GetAsync(Place)).Expect(HttpStatusCode.OK).Json!;

        place["tracks"]!.AsArray().Single()!["matrix"]!["matrixId"]!.GetValue<Guid>().Should().Be(kit.MatrixId);
        place["tracks"]![0]!["nextLevels"]!.AsArray().Single()!["requirements"].Should().NotBeNull();
        place["publishedMatrices"]!.AsArray().Select(matrix => matrix!["matrixId"]!.GetValue<Guid>()).Should().Equal(kit.MatrixId);
    }

    [Fact]
    public async Task Ac91_TheMatrixPage_NamesTheUnitsWhereTheMatrixApplies_AndDropsOneThatPublishedItsOwn()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var branch = await org.Admin.CreateUnitAsync(org.Team.Id);
        var sibling = await org.Admin.CreateUnitAsync(org.Team.Id);

        var before = await EffectiveUnitsAsync(kit);
        var branchHead = await org.Admin.HeadOfAsync(fixture.Host, branch);
        var ownMatrix = (await branchHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = branch.Id, directionId = kit.Direction.Id, trackId = kit.Track.Id })).Expect(HttpStatusCode.Created);
        var duringWork = await EffectiveUnitsAsync(kit);
        var versionId = ownMatrix.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();
        (await kit.SaveAsync(branchHead.Client, kit.Content(), versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(branchHead.Client, "submit", versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.TeamHead.Client, "approve", versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: versionId)).Expect(HttpStatusCode.OK);
        var after = await EffectiveUnitsAsync(kit);

        before.Should().Contain([org.Team.Id, branch.Id, sibling.Id]);
        duringWork.Should().Contain(branch.Id, "a matrix in work does not take the unit out of the inheritance");
        after.Should().Contain([org.Team.Id, sibling.Id]).And.NotContain(branch.Id);
    }

    [Fact]
    public async Task Ac91_AnEmployeeOfAUnitWithItsOwnPublishedMatrix_SeesThatOne_AndTheUnitAboveKeepsItsOwn()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await PublishedAsync(org);
        var branch = await org.Admin.CreateUnitAsync(org.Team.Id);
        var branchHead = await org.Admin.HeadOfAsync(fixture.Host, branch);
        var own = (await branchHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = branch.Id, directionId = kit.Direction.Id, trackId = kit.Track.Id })).Expect(HttpStatusCode.Created);
        var versionId = own.Json!["inWorkVersion"]!["id"]!.GetValue<Guid>();
        (await kit.SaveAsync(branchHead.Client, kit.Content(seniorCriticalScore: 4), versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(branchHead.Client, "submit", versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.TeamHead.Client, "approve", versionId: versionId)).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.Hr.Client, "approve", versionId: versionId)).Expect(HttpStatusCode.OK);
        var inBranch = await EmployeeOnAsync(org, kit, branch.Id);
        var inTeam = await EmployeeOnAsync(org, kit, org.Team.Id);

        var branchPlace = (await inBranch.GetAsync(Place)).Expect(HttpStatusCode.OK).Json!;
        var teamPlace = (await inTeam.GetAsync(Place)).Expect(HttpStatusCode.OK).Json!;

        branchPlace["tracks"]![0]!["matrix"]!["matrixId"]!.GetValue<Guid>().Should().Be(own.Id);
        teamPlace["tracks"]![0]!["matrix"]!["matrixId"]!.GetValue<Guid>().Should().Be(kit.MatrixId);
    }

    private async Task<Guid[]> EffectiveUnitsAsync(MatrixKit kit) =>
        [.. (await kit.Org.Admin.GetAsync($"/api/v1/career/matrices/{kit.MatrixId}")).Expect(HttpStatusCode.OK).Json!["effectiveUnits"]!.AsArray()
            .Select(unit => unit!["id"]!.GetValue<Guid>())];

    private static async Task<MatrixKit> PublishedAsync(CareerOrg org)
    {
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        return kit;
    }

    private async Task<ApiClient> EmployeeOnAsync(CareerOrg org, MatrixKit kit, Guid unitId)
    {
        var positions = (await org.Admin.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}/positions")).Expect(HttpStatusCode.OK).Json!["levels"]!.AsArray()
            .Single(level => level!["id"]!.GetValue<Guid>() == kit.Junior.Id)!["positions"]!.AsArray();
        var employee = await org.Admin.CreateEmployeeAsync(unitId, position: positions[0]!["name"]!.GetValue<string>());
        return await (await org.Admin.CreateUserAsync(employeeId: employee.Id)).SignInAsync(fixture.Host);
    }
}
