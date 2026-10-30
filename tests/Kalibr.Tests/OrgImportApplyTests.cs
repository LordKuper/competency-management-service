using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Applying an import: the previewed plan is applied as one unit, a changed plan or a decision that breaks a rule is refused with nothing changed,
/// the dismissals cascade as a manual dismissal does, the heads follow the choices of the head step, and the apply is journaled once.
/// </summary>
public sealed class OrgImportApplyTests(CareerHost fixture, TestEnvironment environment) : IClassFixture<CareerHost>
{
    private static readonly string[] PlanGroups =
    [
        "newUnits", "activatedUnits", "newPositions", "reactivatedPositions", "newLegalEntities", "newEmployees", "rehiredEmployees",
        "transfers", "addedAssignments", "removedAssignments", "changedLegalEntities", "unitPositionsAdded", "unitPositionsRemoved",
    ];

    [Fact]
    public async Task Ac19_Apply_MakesTheOrganizationAsTheFileSays_AndReportsTheCountOfEveryGroup()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = scene.File();
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        var counts = applied.Json!;
        counts["newUnits"]!.GetValue<int>().Should().Be(1);
        counts["activatedUnits"]!.GetValue<int>().Should().Be(1);
        counts["newPositions"]!.GetValue<int>().Should().Be(1);
        counts["reactivatedPositions"]!.GetValue<int>().Should().Be(1);
        counts["newLegalEntities"]!.GetValue<int>().Should().Be(1);
        counts["newEmployees"]!.GetValue<int>().Should().Be(1);
        counts["rehiredEmployees"]!.GetValue<int>().Should().Be(1);
        counts["transfers"]!.GetValue<int>().Should().Be(2);
        counts["addedAssignments"]!.GetValue<int>().Should().Be(1);
        counts["removedAssignments"]!.GetValue<int>().Should().Be(1);
        counts["changedLegalEntities"]!.GetValue<int>().Should().Be(1);
        counts["unitPositionsAdded"]!.GetValue<int>().Should().Be(2);
        counts["unitPositionsRemoved"]!.GetValue<int>().Should().Be(1);
        counts["deactivatedUnits"]!.GetValue<int>().Should().Be(0);
        counts["dismissedEmployees"]!.GetValue<int>().Should().Be(0);
        counts["assignedHeads"]!.GetValue<int>().Should().Be(0);

        var petrov = await org.Admin.ViewAsync(scene.Petrov.Id);
        petrov.Unit.Should().Be($"{scene.NewUnitName}");
        var yozhikov = await org.Admin.ViewAsync(scene.Yozhikov.Id);
        yozhikov.Position.Should().Be(scene.RetiredName);
        (await org.Admin.ViewAsync(scene.Sidorov.Id)).IsActive.Should().BeTrue("the dismissed employee the file names works again");
        var ivanov = await org.Admin.ViewAsync(scene.Ivanov.Id);
        ivanov.LegalEntities.Should().Equal(scene.BetaName);
        ivanov.Assignments.Should().BeEmpty();
        var newcomer = await org.Admin.ViewAsync(await org.Admin.FindEmployeeAsync($"Новиков{scene.Tag}"));
        newcomer.Unit.Should().Be(scene.RootName);
        newcomer.Position.Should().Be(scene.EngineerName);
        newcomer.Assignments.Should().Equal($"{scene.GroupName}, {scene.EngineerName}: ");
        (await org.Admin.GetUnitAsync(scene.Archive.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await org.Admin.GetAsync($"/api/v1/positions/{scene.Retired}")).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        var rootList = (await org.Admin.GetAsync($"/api/v1/org-units/{scene.Root.Id}/positions")).Expect(HttpStatusCode.OK).Json!.AsArray();
        rootList.Select(item => item!["positionId"]!.GetValue<Guid>()).Should().NotContain(scene.Spare).And.Contain(scene.Engineer);
    }

    [Fact]
    public async Task Ac23_Apply_OfTheSameFileAgain_FindsNothingToChange()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = scene.File();
        (await org.Admin.ApplyKeepingAsync(file, await org.Admin.PlanOfAsync(file))).Expect(HttpStatusCode.OK);

        var again = await org.Admin.PlanOfAsync(file);
        var applied = (await org.Admin.ApplyKeepingAsync(file, again)).Expect(HttpStatusCode.OK);

        foreach (var group in PlanGroups)
        {
            again.Lines(group).Should().BeEmpty(group);
            applied.Json![group]!.GetValue<int>().Should().Be(0, group);
        }
    }

    [Fact]
    public async Task Ac21_Apply_WritesOneJournalEntryWithTheFileNameAndTheCounts()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = scene.File();
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        var rows = (await org.Admin.AuditOfRequestAsync(applied.RequestId!)).Where(row => row!["action"]!.GetValue<string>() == "OrgImport.Applied").ToList();
        var entry = rows.Should().ContainSingle().Subject!;
        entry["entityType"]!.GetValue<string>().Should().Be("OrgImport");
        var written = entry["newValue"]!;
        var value = written is JsonValue text ? JsonNode.Parse(text.GetValue<string>())! : written;
        value["fileName"]!.GetValue<string>().Should().Be("org.xlsx");
        value["counts"]!["newEmployees"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac19_Apply_ByHr_IsAllowed()
    {
        var org = await fixture.OrgAsync();
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Отдел")).ToBytes();
        var plan = await org.Hr.Client.PlanOfAsync(file);

        var applied = await org.Hr.Client.ApplyKeepingAsync(file, plan);

        applied.Status.Should().Be(HttpStatusCode.OK, applied.Body);
        applied.Json!["newUnits"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task Ac9_Apply_IsRefusedToHeadsAndMembers_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var name = Scenarios.Unique("Отдел");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var byHead = await org.TeamHead.Client.ApplyKeepingAsync(file, plan);
        var byMember = await org.Plain.Client.ApplyKeepingAsync(file, plan);

        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byMember.Status.Should().Be(HttpStatusCode.Forbidden);
        (await org.Admin.PlanOfAsync(file)).Lines("newUnits").Should().Equal(name);
    }

    [Fact]
    public async Task Ac18_Apply_DismissesAndDeactivatesByDecision_WithTheCascadeOfAManualDismissal()
    {
        var org = await fixture.OrgAsync();
        var unit = await org.Admin.CreateUnitAsync();
        var hr = await org.Admin.HrOfAsync(fixture.Host, unit.Id);
        (await org.Admin.SetHeadAsync(unit, hr.EmployeeId)).Expect(HttpStatusCode.OK);
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Отдел")).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan, deactivate: [unit.Id], dismiss: [hr.Employee])).Expect(HttpStatusCode.OK);

        applied.Json!["deactivatedUnits"]!.GetValue<int>().Should().Be(1);
        applied.Json["dismissedEmployees"]!.GetValue<int>().Should().Be(1);
        (await org.Admin.ViewAsync(hr.Employee)).IsActive.Should().BeFalse();
        var closed = await org.Admin.GetUnitAsync(unit.Id);
        closed.Json!["isActive"]!.GetValue<bool>().Should().BeFalse();
        closed.Json["headEmployeeId"].Should().BeNull("the dismissed head leaves the unit without one");
        (await org.Admin.GetUserAsync(hr.Id)).Json!["isBlocked"]!.GetValue<bool>().Should().BeTrue("the account of a dismissed employee is blocked");
        (await hr.Client.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.Unauthorized, "the live session ends with the block");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{unit.Id}'")).Should().Be(0, "the HR assignment goes with the dismissal");
        var journal = (await org.Admin.AuditOfRequestAsync(applied.RequestId!)).Actions();
        journal.Should().Contain(["AppUser.Updated", "Employee.Updated", "OrgUnit.Updated", "OrgImport.Applied"]);
    }

    [Fact]
    public async Task Ac18_Apply_WithoutDecisions_DismissesAndDeactivatesWhatTheFileDoesNotName()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Отдел")).ToBytes();
        var plan = await admin.PlanOfAsync(file);

        var applied = (await admin.ApplyImportAsync(file, plan.FingerprintOf())).Expect(HttpStatusCode.OK);

        applied.Json!["dismissedEmployees"]!.GetValue<int>().Should().Be(1);
        applied.Json["deactivatedUnits"]!.GetValue<int>().Should().Be(1);
        (await admin.ViewAsync(employee.Id)).IsActive.Should().BeFalse();
        (await admin.GetUnitAsync(unit.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeFalse("no decision means the default: deactivate");
    }

    [Fact]
    public async Task Ac18_Apply_KeepsWhatTheUserKeeps()
    {
        var org = await fixture.OrgAsync();
        var unit = await org.Admin.CreateUnitAsync();
        var employee = await org.Admin.CreateEmployeeAsync(unit.Id);
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Отдел")).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        applied.Json!["dismissedEmployees"]!.GetValue<int>().Should().Be(0);
        applied.Json["deactivatedUnits"]!.GetValue<int>().Should().Be(0);
        (await org.Admin.ViewAsync(employee.Id)).IsActive.Should().BeTrue();
        (await org.Admin.GetUnitAsync(unit.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac18_Apply_RefusesToDeactivateAUnitWhoseEmployeesStay_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var unit = await org.Admin.CreateUnitAsync();
        var employee = await org.Admin.CreateEmployeeAsync(unit.Id);
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = await org.Admin.ApplyKeepingAsync(file, plan, deactivate: [unit.Id]);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["code"].Should().BeNull("a rule violation is not a changed plan");
        refused.Json["detail"]!.GetValue<string>().Should().Contain("работающие сотрудники");
        await AssertNothingAppliedAsync(org.Admin, file, name, plan);
        (await org.Admin.GetUnitAsync(unit.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await org.Admin.ViewAsync(employee.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Ac18_Apply_RefusesToDeactivateAUnitWhoseChildStaysActive_AndChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var parent = await org.Admin.CreateUnitAsync();
        var child = await org.Admin.CreateUnitAsync(parent.Id);
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = await org.Admin.ApplyKeepingAsync(file, plan, deactivate: [parent.Id]);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["code"].Should().BeNull();
        refused.Json["detail"]!.GetValue<string>().Should().Contain("дочерние");
        await AssertNothingAppliedAsync(org.Admin, file, name, plan);
        (await org.Admin.GetUnitAsync(parent.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
        (await org.Admin.GetUnitAsync(child.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task Ac18_Apply_DeactivatesAUnitTogetherWithItsChildrenAndPeople_WhenTheDecisionsSayAllOfThem()
    {
        var org = await fixture.OrgAsync();
        var parent = await org.Admin.CreateUnitAsync();
        var child = await org.Admin.CreateUnitAsync(parent.Id);
        var employee = await org.Admin.CreateEmployeeAsync(child.Id);
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Новый")).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = await org.Admin.ApplyKeepingAsync(file, plan, deactivate: [parent.Id, child.Id], dismiss: [employee.Id]);

        applied.Status.Should().Be(HttpStatusCode.OK, applied.Body);
        (await org.Admin.GetUnitAsync(parent.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeFalse();
        (await org.Admin.GetUnitAsync(child.Id)).Json!["isActive"]!.GetValue<bool>().Should().BeFalse();
        (await org.Admin.ViewAsync(employee.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Ac18_Apply_RefusesToDeactivateAUnitWhoseEmployeeKeepsASecondaryAssignmentThere()
    {
        var org = await fixture.OrgAsync();
        var home = await org.Admin.CreateUnitAsync();
        var shared = await org.Admin.CreateUnitAsync();
        var position = await org.Admin.PositionAsync(home.Id);
        await org.Admin.PositionAsync(shared.Id);
        var employee = await org.Admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith(Scenarios.Unique("Совмещенов"), "Иван", home.Id, position, assignments: [OrgScenes.Assignment(shared.Id, position)]));
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Новый")).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = await org.Admin.ApplyKeepingAsync(file, plan, deactivate: [shared.Id]);

        refused.Status.Should().Be(HttpStatusCode.Conflict);
        refused.Json!["detail"]!.GetValue<string>().Should().Contain("работающие сотрудники");
        (await org.Admin.ViewAsync(employee.Id)).IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("employee-deactivated")]
    [InlineData("unit-dismissed")]
    [InlineData("twice")]
    [InlineData("not-json")]
    [InlineData("unknown-word")]
    public async Task Ac18_Apply_RefusesAMalformedOrImpossibleDecision_With400_AndChangesNothing(string kind)
    {
        var org = await fixture.OrgAsync();
        var unit = await org.Admin.CreateUnitAsync();
        var employee = await org.Admin.CreateEmployeeAsync(unit.Id);
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = kind switch
        {
            "unknown" => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [new { id = Guid.NewGuid(), decision = "keep" }]),
            "employee-deactivated" => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [new { id = employee.Id, decision = "deactivate" }]),
            "unit-dismissed" => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [new { id = unit.Id, decision = "dismiss" }]),
            "twice" => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [new { id = unit.Id, decision = "keep" }, new { id = unit.Id, decision = "keep" }]),
            "not-json" => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), decisionsText: "это не JSON"),
            _ => await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), decisionsText: $"[{{\"id\":\"{unit.Id}\",\"decision\":\"уволить\"}}]"),
        };

        refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
        refused.FieldErrors("decisions").Should().ContainSingle();
        await AssertNothingAppliedAsync(org.Admin, file, name, plan);
        (await org.Admin.ViewAsync(employee.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Ac18_Apply_RefusesAHeadsFieldThatIsNotJson_With400()
    {
        var org = await fixture.OrgAsync();
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [], headsText: "{");

        refused.Status.Should().Be(HttpStatusCode.BadRequest);
        refused.FieldErrors("heads").Should().ContainSingle();
        await AssertNothingAppliedAsync(org.Admin, file, name, plan);
    }

    [Theory]
    [InlineData("decisions")]
    [InlineData("heads")]
    public async Task Ac18_Apply_RefusesAListWithANullElement_With400_NotAFailure(string field)
    {
        var org = await fixture.OrgAsync();
        var name = Scenarios.Unique("Новый");
        var file = new OrgImportSheet().Unit(name).ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = field == "decisions"
            ? await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), decisionsText: "[null]")
            : await org.Admin.ApplyImportAsync(file, plan.FingerprintOf(), [], headsText: "[null]");

        refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
        refused.FieldErrors(field).Should().ContainSingle();
        await AssertNothingAppliedAsync(org.Admin, file, name, plan);
    }

    [Theory]
    [InlineData("other-file")]
    [InlineData("changed-data")]
    [InlineData("new-unit-the-file-does-not-name")]
    [InlineData("garbage")]
    [InlineData("empty")]
    [InlineData("unusable-file")]
    public async Task Ac20_Apply_OfAPlanThatNoLongerHolds_IsRefusedWith409PlanChanged_AndChangesNothing(string kind)
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = scene.File();
        var plan = await org.Admin.PlanOfAsync(file);

        var refused = kind switch
        {
            "other-file" => await org.Admin.ApplyImportAsync(scene.File(), plan.FingerprintOf()),
            "changed-data" => await ChangeDataThenApplyAsync(org.Admin, scene, file, plan),
            "new-unit-the-file-does-not-name" => await AddUnitThenApplyAsync(org.Admin, file, plan),
            "garbage" => await org.Admin.ApplyImportAsync(file, "не отпечаток"),
            "empty" => await org.Admin.ApplyImportAsync(file, string.Empty),
            _ => await org.Admin.ApplyImportAsync("это не книга"u8.ToArray(), plan.FingerprintOf()),
        };

        refused.Status.Should().Be(HttpStatusCode.Conflict, refused.Body);
        refused.Json!["code"]!.GetValue<string>().Should().Be("planChanged");
        (await org.Admin.ViewAsync(scene.Petrov.Id)).Unit.Should().NotBe(scene.NewUnitName, "nothing of the plan is applied");
        (await org.Admin.ViewAsync(scene.Sidorov.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Ac30_Preview_OffersTheEmployeesWithAHeadPositionAsCandidates_AndSuggestsTheOnlyOne()
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var file = HeadFile(tag);

        var plan = await org.Admin.PlanOfAsync(file);

        var heads = plan["heads"]!;
        var unit = heads["units"]!.AsArray().Should().ContainSingle().Subject!;
        unit["path"]!.GetValue<string>().Should().Be($"Отдел {tag}");
        unit["unitId"].Should().BeNull();
        unit["currentHead"].Should().BeNull();
        var employees = heads["employees"]!.AsArray().ToDictionary(item => item!["fullName"]!.GetValue<string>(), item => item!["key"]!.GetValue<string>());
        unit["candidates"]!.AsArray().Select(key => key!.GetValue<string>()).Should().Equal(employees[$"Начальников{tag} Нил"]);
        unit["employees"]!.AsArray().Select(key => key!.GetValue<string>()).Should().BeEquivalentTo(employees.Values);
        unit["suggested"]!.GetValue<string>().Should().Be(employees[$"Начальников{tag} Нил"]);
    }

    [Fact]
    public async Task Ac30_Preview_SuggestsNobody_WhenSeveralEmployeesOfAUnitHoldAHeadPosition()
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var file = new OrgImportSheet()
            .Employee($"Первов{tag} Пётр", "Руководитель группы", $"Отдел {tag}")
            .Employee($"Второв{tag} Иван", "Директор по развитию", $"Отдел {tag}")
            .ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        var unit = plan["heads"]!["units"]!.AsArray().Should().ContainSingle().Subject!;
        unit["candidates"]!.AsArray().Should().HaveCount(2);
        unit["suggested"].Should().BeNull();
    }

    [Fact]
    public async Task Ac30_Apply_SetsTheChosenHead_OfAUnitTheImportCreates()
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var file = HeadFile(tag);
        var plan = await org.Admin.PlanOfAsync(file);
        var unit = plan["heads"]!["units"]!.AsArray().Single()!;
        var chosen = unit["candidates"]![0]!.GetValue<string>();

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan, heads: [new { unit = unit["key"]!.GetValue<string>(), employee = chosen }])).Expect(HttpStatusCode.OK);

        applied.Json!["assignedHeads"]!.GetValue<int>().Should().Be(1);
        var head = await org.Admin.FindEmployeeAsync($"Начальников{tag}");
        var created = (await org.Admin.GetAsync($"/api/v1/employees/{head}")).Json!["orgUnitId"]!.GetValue<Guid>();
        (await org.Admin.GetUnitAsync(created)).Json!["headEmployeeId"]!.GetValue<Guid>().Should().Be(head);
    }

    [Fact]
    public async Task Ac30_Apply_WithoutAHeadChoice_LeavesANewUnitWithoutAHead()
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var file = HeadFile(tag);
        var plan = await org.Admin.PlanOfAsync(file);

        var applied = (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        applied.Json!["assignedHeads"]!.GetValue<int>().Should().Be(0);
        var someone = await org.Admin.FindEmployeeAsync($"Начальников{tag}");
        var unitId = (await org.Admin.GetAsync($"/api/v1/employees/{someone}")).Json!["orgUnitId"]!.GetValue<Guid>();
        (await org.Admin.GetUnitAsync(unitId)).Json!["headEmployeeId"].Should().BeNull();
    }

    [Fact]
    public async Task Ac31_Apply_KeepsTheCurrentHead_WhoStillWorksInTheUnit_AndTheHeadStepSuggestsThem()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        (await org.Admin.SetHeadAsync(scene.Root, scene.Petrov.Id)).Expect(HttpStatusCode.OK);
        var file = new OrgImportSheet()
            .Add([scene.RootName], scene.PetrovFullName, scene.EngineerName)
            .Add([scene.RootName], scene.IvanovFullName, "Руководитель отдела", scene.AlphaName)
            .ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        var unit = plan["heads"]!["units"]!.AsArray().Single(item => item!["unitId"]?.GetValue<Guid>() == scene.Root.Id)!;
        var applied = (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        unit["currentHead"]!.GetValue<string>().Should().Be(scene.Petrov.Id.ToString());
        unit["currentHeadStays"]!.GetValue<bool>().Should().BeTrue();
        unit["suggested"]!.GetValue<string>().Should().Be(scene.Petrov.Id.ToString(), "the current head who stays is preferred to the only candidate");
        applied.Json!["assignedHeads"]!.GetValue<int>().Should().Be(0);
        (await org.Admin.GetUnitAsync(scene.Root.Id)).Json!["headEmployeeId"]!.GetValue<Guid>().Should().Be(scene.Petrov.Id);
    }

    [Fact]
    public async Task Ac31_Apply_ReleasesTheHeadshipOfAnEmployeeWhoseAssignmentsLeaveTheUnit()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        (await org.Admin.SetHeadAsync(scene.Root, scene.Petrov.Id)).Expect(HttpStatusCode.OK);
        var file = scene.File();
        var plan = await org.Admin.PlanOfAsync(file);

        (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        (await org.Admin.ViewAsync(scene.Petrov.Id)).Unit.Should().Be(scene.NewUnitName);
        (await org.Admin.GetUnitAsync(scene.Root.Id)).Json!["headEmployeeId"].Should().BeNull("the head moved to another unit and has no assignment left in this one");
    }

    [Fact]
    public async Task Ac31_Apply_KeepsTheHeadshipOfAnEmployeeWhoseMainAssignmentMovedButWhoStillHasASecondaryOneInTheUnit()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        (await org.Admin.SetHeadAsync(scene.Group, scene.Ivanov.Id)).Expect(HttpStatusCode.OK);
        var file = new OrgImportSheet()
            .Add([scene.RootName, scene.NewUnitName], scene.IvanovFullName, scene.EngineerName, scene.AlphaName)
            .Add([scene.RootName, scene.GroupName], scene.IvanovFullName, scene.EngineerName)
            .ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);

        (await org.Admin.ApplyKeepingAsync(file, plan)).Expect(HttpStatusCode.OK);

        (await org.Admin.ViewAsync(scene.Ivanov.Id)).Unit.Should().Be(scene.NewUnitName);
        (await org.Admin.GetUnitAsync(scene.Group.Id)).Json!["headEmployeeId"]!.GetValue<Guid>().Should().Be(scene.Ivanov.Id);
    }

    [Theory]
    [InlineData("employee-not-in-unit")]
    [InlineData("unknown-unit")]
    [InlineData("twice")]
    public async Task Ac30_Apply_RefusesAHeadChoiceThatTheHeadStepDoesNotOffer_With400_AndChangesNothing(string kind)
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var file = HeadFile(tag);
        var outsider = await org.Admin.CreateEmployeeAsync((await org.Admin.CreateUnitAsync()).Id);
        var plan = await org.Admin.PlanOfAsync(file);
        var unit = plan["heads"]!["units"]!.AsArray().Single()!;
        var key = unit["key"]!.GetValue<string>();
        var chosen = unit["candidates"]![0]!.GetValue<string>();

        var refused = kind switch
        {
            "employee-not-in-unit" => await org.Admin.ApplyKeepingAsync(file, plan, heads: [new { unit = key, employee = outsider.Id.ToString() }]),
            "unknown-unit" => await org.Admin.ApplyKeepingAsync(file, plan, heads: [new { unit = "нет такого", employee = chosen }]),
            _ => await org.Admin.ApplyKeepingAsync(file, plan, heads: [new { unit = key, employee = chosen }, new { unit = key, employee = (string?)null }]),
        };

        refused.Status.Should().Be(HttpStatusCode.BadRequest, refused.Body);
        refused.FieldErrors("heads").Should().ContainSingle();
        (await org.Admin.PlanOfAsync(file)).Lines("newEmployees").Should().HaveCount(2, "nothing was created");
    }

    [Theory]
    [InlineData(false, HttpStatusCode.OK)]
    [InlineData(true, HttpStatusCode.BadRequest)]
    public async Task Ac31_Apply_AcceptsAMissingEmployeeWhomTheDecisionsKeep_AsTheHead_AndRefusesOneTheyDismiss(bool dismissed, HttpStatusCode expected)
    {
        var org = await fixture.OrgAsync();
        var tag = OrgScenes.Tag();
        var unit = await org.Admin.CreateUnitAsync(name: $"Отдел {tag}");
        var staying = await org.Admin.CreateEmployeeAsync(unit.Id, $"Остающийся{tag}");
        var file = new OrgImportSheet().Employee($"Начальников{tag} Нил", "Начальник отдела", $"Отдел {tag}").ToBytes();
        var plan = await org.Admin.PlanOfAsync(file);
        var key = plan["heads"]!["units"]!.AsArray().Single()!["key"]!.GetValue<string>();

        var applied = await org.Admin.ApplyKeepingAsync(
            file,
            plan,
            dismiss: dismissed ? [staying.Id] : [],
            heads: [new { unit = key, employee = staying.Id.ToString() }]);

        applied.Status.Should().Be(expected, applied.Body);
        var head = (await org.Admin.GetUnitAsync(unit.Id)).Json!["headEmployeeId"];
        if (dismissed)
        {
            head.Should().BeNull();
        }
        else
        {
            head!.GetValue<Guid>().Should().Be(staying.Id);
        }
    }

    private static byte[] HeadFile(string tag) => new OrgImportSheet()
        .Employee($"Начальников{tag} Нил", "Начальник отдела", $"Отдел {tag}")
        .Employee($"Рабочев{tag} Иван", "Инженер", $"Отдел {tag}")
        .ToBytes();

    private static async Task<ApiResponse> ChangeDataThenApplyAsync(ApiClient admin, ImportScene scene, byte[] file, JsonNode plan)
    {
        var current = await admin.GetEmployeeAsync(scene.Petrov.Id);
        (await admin.PutAsync(
            $"/api/v1/employees/{scene.Petrov.Id}",
            Scenarios.EmployeeBody(scene.Petrov.Json!["lastName"]!.GetValue<string>(), "Павел", scene.Engineer, scene.Group.Id),
            current.ETag)).Expect(HttpStatusCode.OK);
        return await admin.ApplyKeepingAsync(file, plan);
    }

    private static async Task<ApiResponse> AddUnitThenApplyAsync(ApiClient admin, byte[] file, JsonNode plan)
    {
        await admin.CreateUnitAsync();
        return await admin.ApplyKeepingAsync(file, plan);
    }

    private static async Task AssertNothingAppliedAsync(ApiClient admin, byte[] file, string newUnitName, JsonNode plan)
    {
        var again = await admin.PlanOfAsync(file);
        again.Lines("newUnits").Should().Equal([newUnitName], "the unit of the refused file was not created");
        again.FingerprintOf().Should().Be(plan.FingerprintOf(), "the organization is as it was");
    }
}
