using System.Net;
using AwesomeAssertions;
using Competency.OrgStructure.Import;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The preview of the organization import: who may ask for it, what an unusable or hostile file does, and the plan a usable file gives, group by group.
/// A preview changes nothing, so the tests share one host and tell their data apart by a token in every name.
/// </summary>
public sealed class OrgImportTests(CareerHost fixture, TestEnvironment environment) : IClassFixture<CareerHost>
{
    private const int OverTheBodyLimit = 51 * 1024 * 1024;
    private const long OverTheUnpackedLimit = 257L * 1024 * 1024;

    [Fact]
    public async Task Ac9_Preview_IsOpenToAdministratorsAndHr_AndRefusedToHeadsAndMembers()
    {
        var org = await fixture.OrgAsync();
        var file = new OrgImportSheet().Unit(Scenarios.Unique("Отдел")).ToBytes();

        var byAdmin = await org.Admin.PreviewImportAsync(file);
        var byHr = await org.Hr.Client.PreviewImportAsync(file);
        var byHead = await org.TeamHead.Client.PreviewImportAsync(file);
        var byMember = await org.Plain.Client.PreviewImportAsync(file);
        var anonymous = await fixture.Host.Anonymous().PreviewImportAsync(file);

        byAdmin.Status.Should().Be(HttpStatusCode.OK);
        byHr.Status.Should().Be(HttpStatusCode.OK);
        byHead.Status.Should().Be(HttpStatusCode.Forbidden);
        byMember.Status.Should().Be(HttpStatusCode.Forbidden);
        anonymous.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ac10_Preview_AFileThatIsNotAWorkbook_IsReportedAsAnError_NotAFailure()
    {
        var org = await fixture.OrgAsync();

        var preview = (await org.Admin.PreviewImportAsync("это не книга"u8.ToArray())).Expect(HttpStatusCode.OK);

        preview.Json!["plan"].Should().BeNull();
        var error = preview.Json["errors"]!.AsArray().Should().ContainSingle().Subject!;
        error["row"].Should().BeNull();
        error["message"]!.GetValue<string>().Should().Be(XlsxArchiveGuard.NotWorkbookMessage);
    }

    [Fact]
    public async Task Ac10_Preview_AZipThatInflatesBeyondTheLimit_IsRefusedWithoutBeingUnpacked()
    {
        var org = await fixture.OrgAsync();
        var bomb = OrgImportArchives.OfZeros(OverTheUnpackedLimit);

        var preview = (await org.Admin.PreviewImportAsync(bomb)).Expect(HttpStatusCode.OK);

        bomb.Length.Should().BeLessThan(10 * 1024 * 1024);
        preview.Json!["errors"]!.AsArray().Should().ContainSingle().Which!["message"]!.GetValue<string>().Should().Be(XlsxArchiveGuard.TooLargeMessage);
        (await org.Admin.GetAsync("/api/v1/auth/me")).Status.Should().Be(HttpStatusCode.OK, "the host still serves after the bomb");
    }

    [Fact]
    public async Task Ac10_Preview_ABodyOverTheLimit_IsRefusedWith413()
    {
        var org = await fixture.OrgAsync();

        var preview = await org.Admin.PreviewImportAsync(new byte[OverTheBodyLimit], adjust: request => request.Headers.ExpectContinue = true);

        preview.Status.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Ac13_Preview_ReportsEveryProblemOfTheFileAtOnce_WithRowAndColumn_AndNoPlan()
    {
        var org = await fixture.OrgAsync();
        var file = new OrgImportSheet()
            .Employee("Иванов Иван", string.Empty, Scenarios.Unique("Отдел"))
            .Add([], "Петров Пётр", "Инженер")
            .Employee("Сидоров", "Инженер", Scenarios.Unique("Отдел"))
            .ToBytes();

        var preview = (await org.Admin.PreviewImportAsync(file)).Expect(HttpStatusCode.OK);

        preview.Json!["plan"].Should().BeNull();
        var errors = preview.Json["errors"]!.AsArray();
        errors.Select(error => error!["row"]!.GetValue<int>()).Should().Equal(2, 3, 4);
        errors.Select(error => error!["column"]!.GetValue<string>()).Should().Equal("Должность", "Подразделение 1-го уровня", "ФИО");
        errors.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error!["message"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Ac13_Preview_ChangesNothing()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var before = await StateOfAsync(scene);

        (await org.Admin.PreviewImportAsync(scene.File())).Expect(HttpStatusCode.OK);

        (await StateOfAsync(scene)).Should().Be(before);
    }

    [Fact]
    public async Task Ac14_Preview_MatchesUnitsPositionsAndPeopleIgnoringCaseYoAndSpacing_SoNothingIsCreatedTwice()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = new OrgImportSheet()
            .Employee($"  ЕЖИКОВ{scene.Tag.ToUpperInvariant()}   еж ", $" {scene.EngineerName.ToUpperInvariant()} ", $" {scene.RootName.ToUpperInvariant()}  ")
            .ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        foreach (var group in new[] { "newUnits", "newPositions", "newEmployees", "transfers", "rehiredEmployees", "addedAssignments", "removedAssignments", "newLegalEntities" })
        {
            plan.Lines(group).Should().BeEmpty(group);
        }
    }

    [Fact]
    public async Task Ac14_Preview_MatchesAStoredNameWithADoubleInnerSpace_ToTheNameOfTheFile_NotAsANewEmployeeAndADismissal()
    {
        var org = await fixture.OrgAsync();
        var unitName = Scenarios.Unique("Отдел");
        var unit = await org.Admin.CreateUnitAsync(name: unitName);
        var lastName = Scenarios.Unique("Оглыев");
        var stored = await org.Admin.CreateEmployeeAsync(unit.Id, lastName, "Иван", middleName: "Али  оглы");
        var file = new OrgImportSheet().Employee($"{lastName} Иван Али оглы", "Инженер", unitName).ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        plan.Lines("newEmployees").Should().BeEmpty();
        plan["missingEmployees"]!.AsArray().Select(employee => employee!["id"]!.GetValue<string>()).Should().NotContain(stored.Id.ToString());
    }

    [Fact]
    public async Task Ac14_Preview_RefusesToGuess_WhenTheSystemHoldsTwoPeopleWithOneName()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var lastName = Scenarios.Unique("Двойников");
        await admin.CreateEmployeeAsync(unit.Id, lastName, "Иван");
        await admin.CreateEmployeeAsync(unit.Id, lastName.ToUpperInvariant(), "иван");
        var file = new OrgImportSheet().Employee($"{lastName} Иван", "Инженер", unit.Json!["name"]!.GetValue<string>()).ToBytes();

        var preview = (await admin.PreviewImportAsync(file)).Expect(HttpStatusCode.OK);

        preview.Json!["plan"].Should().BeNull();
        var error = preview.Json["errors"]!.AsArray().Should().ContainSingle().Subject!;
        error["column"]!.GetValue<string>().Should().Be("ФИО");
        error["message"]!.GetValue<string>().Should().Contain("несколько сотрудников").And.Contain("(2)");
    }

    [Fact]
    public async Task Ac12_Preview_AttachesALevelBelowASkippedOneToTheLastUnitAbove()
    {
        var org = await fixture.OrgAsync();
        var top = Scenarios.Unique("Верх");
        var deep = Scenarios.Unique("Низ");
        var file = new OrgImportSheet(3).Add([top, "-", deep]).ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        plan.Lines("newUnits").Should().Equal(top, $"{top} / {deep}");
    }

    [Fact]
    public async Task Ac15_Preview_KeepsTheCurrentMainAssignment_WhateverTheOrderOfTheRows()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = new OrgImportSheet()
            .Add([scene.RootName, scene.GroupName], scene.IvanovFullName, scene.EngineerName)
            .Add([scene.RootName], scene.IvanovFullName, scene.EngineerName, scene.AlphaName)
            .ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        plan.Lines("transfers").Should().BeEmpty("the row that is the current main stays the main one");
        plan.Lines("addedAssignments").Should().BeEmpty();
        plan.Lines("removedAssignments").Should().BeEmpty();
        plan.Lines("changedLegalEntities").Should().BeEmpty();
    }

    [Fact]
    public async Task Ac15_Preview_TakesTheFirstRowAsTheMain_ForAPersonThePlaceOfWhomTheFileDoesNotKeep()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var file = new OrgImportSheet()
            .Add([scene.RootName, scene.GroupName], scene.PetrovFullName, scene.EngineerName)
            .Add([scene.RootName, scene.NewUnitName], scene.PetrovFullName, scene.EngineerName)
            .ToBytes();

        var plan = await org.Admin.PlanOfAsync(file);

        plan.Lines("transfers").Should().Equal($"{scene.PetrovFullName}: {scene.RootPath}, {scene.EngineerName} → {scene.GroupPath}, {scene.EngineerName}");
        plan.Lines("addedAssignments").Should().Equal($"{scene.PetrovFullName}: {scene.NewUnitPath}, {scene.EngineerName}");
        plan.Lines("removedAssignments").Should().BeEmpty();
    }

    [Fact]
    public async Task Ac16_Preview_ListsEveryGroupOfChangesWithTheLineTheUserReads_InAFixedOrder()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);

        var plan = await org.Admin.PlanOfAsync(scene.File());

        plan.Lines("newUnits").Should().Equal(scene.NewUnitPath);
        plan.Lines("activatedUnits").Should().Equal(scene.ArchiveName);
        plan.Lines("newPositions").Should().Equal(scene.FreshName);
        plan.Lines("reactivatedPositions").Should().Equal(scene.RetiredName);
        plan.Lines("newLegalEntities").Should().Equal(scene.BetaName);
        plan.Lines("newEmployees").Should().Equal($"{scene.NewcomerFullName}: {scene.RootPath}, {scene.EngineerName}");
        plan.Lines("rehiredEmployees").Should().Equal(scene.SidorovFullName);
        plan.Lines("transfers").Should().Equal(Ordinal(
            $"{scene.YozhikovFullName}: {scene.RootPath}, {scene.EngineerName} → {scene.RootPath}, {scene.RetiredName}",
            $"{scene.PetrovFullName}: {scene.RootPath}, {scene.EngineerName} → {scene.NewUnitPath}, {scene.EngineerName}"));
        plan.Lines("addedAssignments").Should().Equal($"{scene.NewcomerFullName}: {scene.GroupPath}, {scene.EngineerName}");
        plan.Lines("removedAssignments").Should().Equal($"{scene.IvanovFullName}: {scene.GroupPath}, {scene.EngineerName}");
        plan.Lines("changedLegalEntities").Should().Equal($"{scene.IvanovFullName}, {scene.RootPath}, {scene.EngineerName}: {scene.AlphaName} → {scene.BetaName}");
        plan.Lines("unitPositionsAdded").Should().Equal(Ordinal($"{scene.NewUnitPath}: {scene.EngineerName}", $"{scene.GroupPath}: {scene.FreshName}"));
        plan.Lines("unitPositionsRemoved").Should().Equal($"{scene.RootPath}: {scene.SpareName}");
        plan["fingerprint"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Ac16_Preview_OfTheSameFileTwice_GivesTheSameFingerprint_AndOfAnotherFileAnotherOne()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);

        var file = scene.File();

        var first = await org.Admin.PlanOfAsync(file);
        var second = await org.Admin.PlanOfAsync(file);
        var other = await org.Admin.PlanOfAsync(new OrgImportSheet().Unit(scene.RootName, scene.NewUnitName).ToBytes());

        second["fingerprint"]!.GetValue<string>().Should().Be(first["fingerprint"]!.GetValue<string>());
        other["fingerprint"]!.GetValue<string>().Should().NotBe(first["fingerprint"]!.GetValue<string>());
    }

    [Fact]
    public async Task Ac17_Preview_ListsTheActiveUnitsAndTheWorkingPeopleTheFileDoesNotName_WithTheirUnits()
    {
        var org = await fixture.OrgAsync();
        var scene = await ImportScene.CreateAsync(org.Admin);
        var extraName = $"Лишний {scene.Tag}";
        var extra = await org.Admin.CreateUnitAsync(name: extraName);
        var other = await org.Admin.CreateUnitAsync(name: $"Другой {scene.Tag}");
        var position = await org.Admin.PositionAsync(extra.Id);
        await org.Admin.PositionAsync(other.Id);
        var left = await org.Admin.CreateEmployeeWithAsync(
            OrgScenes.EmployeeWith($"Оставшийся{scene.Tag}", "Олег", extra.Id, position, assignments: [OrgScenes.Assignment(other.Id, position)]));
        var dismissed = await org.Admin.DismissAsync(await org.Admin.CreateEmployeeAsync(extra.Id, $"Уволенный{scene.Tag}"));
        var inactive = await org.Admin.DeactivateUnitAsync(await org.Admin.CreateUnitAsync(name: $"Закрытый {scene.Tag}"));

        var plan = await org.Admin.PlanOfAsync(scene.File());

        var units = plan["missingUnits"]!.AsArray();
        var extraItem = units.Single(unit => unit!["id"]!.GetValue<string>() == extra.Id.ToString())!;
        extraItem["path"]!.GetValue<string>().Should().Be(extraName);
        extraItem["parentId"].Should().BeNull();
        units.Select(unit => unit!["id"]!.GetValue<string>()).Should()
            .NotContain([inactive.Id.ToString(), scene.Root.Id.ToString(), scene.Group.Id.ToString(), scene.Archive.Id.ToString()]);
        var employees = plan["missingEmployees"]!.AsArray();
        var leftItem = employees.Single(employee => employee!["id"]!.GetValue<string>() == left.Id.ToString())!;
        leftItem["fullName"]!.GetValue<string>().Should().Be($"Оставшийся{scene.Tag} Олег");
        leftItem["orgUnitIds"]!.AsArray().Select(id => id!.GetValue<string>()).Should().BeEquivalentTo([extra.Id.ToString(), other.Id.ToString()]);
        leftItem["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeFalse();
        employees.Select(employee => employee!["id"]!.GetValue<string>()).Should()
            .NotContain([dismissed.Id.ToString(), scene.Ivanov.Id.ToString(), scene.Sidorov.Id.ToString()]);
    }

    [Fact]
    public async Task Ac17_Preview_MarksTheMissingEmployeeWhoseAccountIsTheLastActiveAdministrator()
    {
        await using var host = await environment.StartHostAsync();
        var admin = await host.AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var other = await admin.CreateEmployeeAsync(unit.Id);
        await admin.BindAccountAsync(await admin.OwnIdAsync(), employee.Id);
        admin = await host.LoginAsync(ApiHost.AdminEmail, ApiHost.AdminPassword);

        var plan = await admin.PlanOfAsync(new OrgImportSheet().Unit(Scenarios.Unique("Новый")).ToBytes());

        var missing = plan["missingEmployees"]!.AsArray();
        missing.Single(item => item!["id"]!.GetValue<string>() == employee.Id.ToString())!["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeTrue();
        missing.Single(item => item!["id"]!.GetValue<string>() == other.Id.ToString())!["isLastActiveAdministrator"]!.GetValue<bool>().Should().BeFalse();
    }

    private static string[] Ordinal(params string[] lines) => [.. lines.Order(StringComparer.Ordinal)];

    private static async Task<string> StateOfAsync(ImportScene scene)
    {
        var admin = scene.Admin;
        var parts = new List<string>
        {
            (await admin.GetAsync("/api/v1/org-units?pageSize=200")).Body,
            (await admin.GetAsync("/api/v1/employees?pageSize=200")).Body,
            (await admin.GetAsync("/api/v1/positions?pageSize=200")).Body,
            (await admin.GetAsync("/api/v1/legal-entities")).Body,
        };
        return string.Join('\n', parts);
    }
}
