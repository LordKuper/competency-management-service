using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// Which lock each operation of the sprint takes, and in which mode, checked by holding that lock on a connection of the test: an operation that
/// must wait stops until the lock is released and then completes, and one that must not wait completes while the lock is held. The global order is
/// the tree lock, the catalogue lock, the matrix row. These tests hold database-wide locks, so they run on a host of their own, one after another.
/// </summary>
public sealed class CareerLockTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    public static TheoryData<string> TreeExclusiveOperations => new()
    {
        "position-create", "position-rename", "position-deactivate", "unit-position-add", "unit-position-remove", "quick-add", "employee-create", "hr-assign", "hr-remove",
    };

    public static TheoryData<string> CatalogueOperations => new()
    {
        "direction-create", "direction-rename", "direction-approve", "direction-deactivate", "direction-delete", "scale-make-default", "level-order", "level-create",
        "matrix-create", "matrix-submit", "matrix-delete", "matrix-version-create", "matrix-content-edit",
    };

    [Theory]
    [MemberData(nameof(TreeExclusiveOperations))]
    public async Task Ac7_ARequestThatChangesWhatTheRightsAreBuiltOn_WaitsForEveryReaderOfTheTree(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var send = await PrepareAsync(operation, org);

        await using var shared = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey, shared: true);
        var pending = await RowLock.StartWaitingAsync(fixture.Host, 1, send);
        await shared.ReleaseAsync();

        (await pending).Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(CatalogueOperations))]
    public async Task Ac7_ARequestThatActsOnRightsFromTheTree_WaitsForAnExclusiveChangeOfTheTree(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var send = await PrepareAsync(operation, org);

        await using var exclusive = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var pending = await RowLock.StartWaitingAsync(fixture.Host, 1, send);
        await exclusive.ReleaseAsync();

        (await pending).Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(CatalogueOperations))]
    public async Task Ac7_ARequestThatActsOnRightsFromTheTree_RunsAlongsideOtherReadersOfTheTree(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var send = await PrepareAsync(operation, org);

        await using var shared = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey, shared: true);
        var response = await send().WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

        response.Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(CatalogueOperations))]
    public async Task Ac9_ACatalogueOrMatrixChange_WaitsForAWriterOfTheCatalogues(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var send = await PrepareAsync(operation, org);

        await using var exclusive = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var pending = await RowLock.StartWaitingAsync(fixture.Host, 1, send);
        await exclusive.ReleaseAsync();

        (await pending).Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [MemberData(nameof(CatalogueOperations))]
    public async Task Ac9_AMatrixContentEdit_RunsAlongsideReadersOfTheCatalogues_WhileEveryOtherChangeWaitsForThem(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var send = await PrepareAsync(operation, org);

        await using var shared = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey, shared: true);
        if (operation == "matrix-content-edit")
        {
            (await send().WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken)).Status.Should().Be(HttpStatusCode.OK);
            return;
        }

        var pending = await RowLock.StartWaitingAsync(fixture.Host, 1, send);
        await shared.ReleaseAsync();

        (await pending).Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("matrix-submit")]
    [InlineData("matrix-version-create")]
    [InlineData("matrix-delete")]
    public async Task Ac15_AChangeOfTheVersionsOfAMatrix_WaitsForTheRowOfTheMatrix(string operation)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var (send, matrixId) = await PrepareWithMatrixAsync(operation, org);

        await using var row = await RowLock.HoldMatrixAsync(fixture.Host, matrixId);
        var pending = await RowLock.StartWaitingAsync(fixture.Host, 1, send);
        await row.ReleaseAsync();

        (await pending).Status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Created)]
    [InlineData(false, HttpStatusCode.BadRequest)]
    public async Task Ac3_AnEmployeeSavedWhileThePositionIsTakenOffTheUnitsList_IsDecidedByWhichRequestIsFirst(bool employeeFirst, HttpStatusCode employeeStatus)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var positionId = await org.Admin.PositionAsync(org.Team.Id, Scenarios.Unique("Гонка"));
        var hire = () => org.Admin.PostAsync("/api/v1/employees", new { lastName = Scenarios.Unique("Гонкин"), firstName = "Иван", middleName = (string?)null, positionId, orgUnitId = org.Team.Id });
        var remove = () => org.Admin.DeleteAsync($"/api/v1/org-units/{org.Team.Id}/positions/{positionId}");

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, employeeFirst ? hire : remove);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, employeeFirst ? remove : hire);
        await gate.ReleaseAsync();
        var (hired, removed) = employeeFirst ? (await first, await second) : (await second, await first);

        hired.Status.Should().Be(employeeStatus, hired.Body);
        removed.Status.Should().Be(HttpStatusCode.NoContent);
        if (!employeeFirst)
        {
            hired.FieldErrors("positionId").Should().Equal(["Должность недоступна в выбранном подразделении"]);
        }
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Created)]
    [InlineData(false, HttpStatusCode.BadRequest)]
    public async Task Ac3_AnEmployeeSavedWhileThePositionIsDeactivated_IsDecidedByWhichRequestIsFirst(bool employeeFirst, HttpStatusCode employeeStatus)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var positionId = await org.Admin.PositionAsync(org.Team.Id, Scenarios.Unique("Гонка"));
        var position = (await org.Admin.GetAsync($"/api/v1/positions/{positionId}")).Expect(HttpStatusCode.OK);
        var hire = () => org.Admin.PostAsync("/api/v1/employees", new { lastName = Scenarios.Unique("Гонкин"), firstName = "Иван", middleName = (string?)null, positionId, orgUnitId = org.Team.Id });
        var deactivate = () => org.Admin.PostAsync($"/api/v1/positions/{positionId}/deactivate", ifMatch: position.ETag);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, employeeFirst ? hire : deactivate);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, employeeFirst ? deactivate : hire);
        await gate.ReleaseAsync();
        var (hired, deactivated) = employeeFirst ? (await first, await second) : (await second, await first);

        hired.Status.Should().Be(employeeStatus, hired.Body);
        deactivated.Status.Should().Be(HttpStatusCode.OK);
        if (!employeeFirst)
        {
            hired.FieldErrors("positionId").Should().Equal(["Должность неактивна"]);
        }
    }

    [Fact]
    public async Task Ac1_TwoRequestsThatCreateOnePositionName_LeaveOnePosition_AndTheLoserSeesTheDuplicateMessage()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var name = Scenarios.Unique("Дубль");
        var create = () => org.Admin.PostAsync("/api/v1/positions", new { name });

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, create);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, create);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.Created);
        var loser = await second;
        loser.Status.Should().Be(HttpStatusCode.Conflict, loser.Body);
        loser.FieldErrors("name").Should().Equal(["Должность с таким названием уже есть"]);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM positions WHERE name = '{name}'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac8_TwoAssignmentsOfAnHrToOneUnit_LeaveOneRow_TheLaterOneWinning_AndNeitherFails()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Root.Id);
        var first = await org.Admin.HrCandidateAsync(fixture.Host, unit.Id);
        var second = await org.Admin.HrCandidateAsync(fixture.Host, unit.Id);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var a = await RowLock.StartWaitingAsync(fixture.Host, 1, () => org.Admin.AssignHrAsync(unit.Id, first.EmployeeId));
        var b = await RowLock.StartWaitingAsync(fixture.Host, 2, () => org.Admin.AssignHrAsync(unit.Id, second.EmployeeId));
        await gate.ReleaseAsync();

        (await a).Status.Should().Be(HttpStatusCode.NoContent);
        (await b).Status.Should().Be(HttpStatusCode.NoContent);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM org_unit_hr WHERE org_unit_id = '{unit.Id}'")).Should().Be(1);
        (await org.Admin.GetAsync($"/api/v1/career/org-units/{unit.Id}/hr")).Json!["assigned"]!["employeeId"]!.GetValue<Guid>().Should().Be(second.Employee);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.NoContent)]
    [InlineData(false, HttpStatusCode.BadRequest)]
    public async Task Ac8_AnAssignmentRacingTheBlockingOfTheAccount_IsDecidedByWhichRequestIsFirst(bool assignmentFirst, HttpStatusCode assignmentStatus)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var unit = await org.Admin.CreateUnitAsync(org.Root.Id);
        var account = await org.Admin.HrCandidateAsync(fixture.Host, unit.Id);
        var assign = () => org.Admin.AssignHrAsync(unit.Id, account.EmployeeId);
        var block = () => org.Admin.PostAsync($"/api/v1/users/{account.Id}/block", ifMatch: account.User.ETag);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, assignmentFirst ? assign : block);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, assignmentFirst ? block : assign);
        await gate.ReleaseAsync();
        var (assigned, blocked) = assignmentFirst ? (await first, await second) : (await second, await first);

        assigned.Status.Should().Be(assignmentStatus, assigned.Body);
        blocked.Status.Should().Be(HttpStatusCode.OK, blocked.Body);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Created)]
    [InlineData(false, HttpStatusCode.Forbidden)]
    public async Task Ac14_AMatrixCreatedWhileTheHeadIsChanged_IsDecidedByWhichRequestIsFirst(bool creationFirst, HttpStatusCode creationStatus)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var successor = await org.Admin.CreateEmployeeAsync(org.Team.Id);
        var create = () => org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, directionId = direction.Id, trackId = track.Id });
        var change = async () => await org.Admin.SetHeadAsync(org.Team, successor.Id);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, creationFirst ? create : change);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, creationFirst ? change : create);
        await gate.ReleaseAsync();
        var (created, changed) = creationFirst ? (await first, await second) : (await second, await first);

        created.Status.Should().Be(creationStatus, created.Body);
        changed.Status.Should().Be(HttpStatusCode.OK, changed.Body);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.Conflict)]
    public async Task Ac16_ASubmissionRacingTheRemovalOfTheHr_IsDecidedByWhichRequestIsFirst(bool submissionFirst, HttpStatusCode submissionStatus)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var etag = await kit.ETagAsync();
        var submit = () => kit.TransitAsync(org.TeamHead.Client, "submit", etag: etag);
        var remove = () => org.Admin.DeleteAsync($"/api/v1/career/org-units/{org.Root.Id}/hr");

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.TreeKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, submissionFirst ? submit : remove);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, submissionFirst ? remove : submit);
        await gate.ReleaseAsync();
        var (submitted, removed) = submissionFirst ? (await first, await second) : (await second, await first);

        submitted.Status.Should().Be(submissionStatus, submitted.Body);
        removed.Status.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Ac9_TwoCreationsOfOneEntryName_LeaveOneEntry_AndTheLoserSeesTheDuplicateMessage()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var name = Scenarios.Unique("Дубль");
        var create = () => org.Admin.PostAsync("/api/v1/career/directions", new { name });

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, create);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, create);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.Created);
        var loser = await second;
        loser.Status.Should().Be(HttpStatusCode.Conflict, loser.Body);
        loser.FieldErrors("name").Should().Equal(["Запись с таким названием уже есть"]);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM career_directions WHERE name = '{name}'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac14_TwoCreationsOfAMatrixForOneUnitAndTrack_LeaveOneMatrix_AndTheLoserSeesTheDuplicateMessage()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        var create = () => org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, directionId = direction.Id, trackId = track.Id });

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, create);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, create);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.Created);
        var loser = await second;
        loser.Status.Should().Be(HttpStatusCode.Conflict, loser.Body);
        loser.FieldErrors("trackId").Should().Equal(["У подразделения уже есть матрица для этого трека"]);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM competency_matrices WHERE track_id = '{track.Id}'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac22_TwoRequestsThatMakeDifferentScalesTheDefault_LeaveExactlyOneDefault_TheLaterOne()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var a = await org.Admin.MakeScaleAsync();
        var b = await org.Admin.MakeScaleAsync();

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, () => org.Admin.PostAsync($"/api/v1/career/scales/{a.Id}/make-default", ifMatch: a.ETag));
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, () => org.Admin.PostAsync($"/api/v1/career/scales/{b.Id}/make-default", ifMatch: b.ETag));
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.OK);
        (await second).Status.Should().Be(HttpStatusCode.OK);
        var defaults = (await org.Admin.GetAsync("/api/v1/career/scales?isDefault=true")).Expect(HttpStatusCode.OK).ItemIds();
        await RestoreDefaultAsync(org);
        defaults.Should().Equal(b.Id.ToString());
    }

    [Fact]
    public async Task Ac11_ALevelAddedWhileTheLevelsAreOrdered_LeavesDistinctPlacesInTheTrack()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var direction = await org.Admin.MakeDirectionAsync();
        var track = await org.Admin.MakeTrackAsync(org.Team.Id, direction.Id);
        await org.Admin.MakeLevelAsync(track.Id);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, () => org.Admin.PostAsync("/api/v1/career/levels", new { name = Scenarios.Unique("Новый"), trackId = track.Id }));
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, () => org.Admin.PostAsync("/api/v1/career/levels", new { name = Scenarios.Unique("Ещё"), trackId = track.Id }));
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.Created);
        (await second).Status.Should().Be(HttpStatusCode.Created);
        (await fixture.Host.ScalarAsync<string>($"SELECT string_agg(position::text, ',' ORDER BY position) FROM career_levels WHERE track_id = '{track.Id}'")).Should().Be("1,2,3");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ac25_ALevelDeletedWhileAMatrixEditAddsIt_IsDecidedByWhichRequestIsFirst_AndNoReferenceIsLeftDangling(bool editFirst)
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var spare = await org.Admin.MakeLevelAsync(kit.Track.Id, Scenarios.Unique("Запасной"));
        var content = kit.Content();
        content["levels"]!.AsArray().Add(new System.Text.Json.Nodes.JsonObject { ["levelId"] = spare.Id, ["minOverallPercent"] = 80 });
        var etag = await kit.ETagAsync();
        var edit = () => kit.SaveAsync(org.TeamHead.Client, content, etag);
        var delete = () => org.Admin.DeleteAsync($"/api/v1/career/levels/{spare.Id}", spare.ETag);

        await using var gate = await RowLock.HoldAdvisoryAsync(fixture.Host, RowLock.CatalogueKey);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, editFirst ? edit : delete);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, editFirst ? delete : edit);
        await gate.ReleaseAsync();
        var (edited, deleted) = editFirst ? (await first, await second) : (await second, await first);

        if (editFirst)
        {
            edited.Status.Should().Be(HttpStatusCode.OK, edited.Body);
            deleted.Status.Should().Be(HttpStatusCode.Conflict, "the level is in the document now");
            (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM career_levels WHERE id = '{spare.Id}'")).Should().Be(1);
        }
        else
        {
            deleted.Status.Should().Be(HttpStatusCode.NoContent, deleted.Body);
            edited.Status.Should().Be(HttpStatusCode.BadRequest, "the level is gone, so the document cannot name it");
            (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE id = '{kit.VersionId}' AND content::text LIKE '%{spare.Id}%'")).Should().Be(0);
        }
    }

    [Fact]
    public async Task Ac15_TwoApprovalsOfOneVersion_PublishItOnce_AndTheSecondIsRefused()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        (await kit.TransitAsync(org.TeamHead.Client, "submit")).Expect(HttpStatusCode.OK);
        (await kit.TransitAsync(org.DepartmentHead.Client, "approve")).Expect(HttpStatusCode.OK);
        var etag = await kit.ETagAsync();
        var approve = () => kit.TransitAsync(org.Hr.Client, "approve", etag: etag);

        await using var gate = await RowLock.HoldMatrixAsync(fixture.Host, kit.MatrixId);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, approve);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, approve);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.OK);
        var loser = await second;
        loser.Status.Should().Be(HttpStatusCode.Conflict, loser.Body);
        (await kit.DecisionsAsync()).Should().Equal("Submitted", "Approved@Superior", "Approved@Hr", "Published");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE matrix_id = '{kit.MatrixId}' AND status = 'Published'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac15_ATransitionWaitingWhileTheMatrixIsDeleted_FindsItsVersionGone_AndIsAnsweredNotFound()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        var etag = await kit.ETagAsync();
        var delete = () => org.TeamHead.Client.DeleteAsync($"/api/v1/career/matrices/{kit.MatrixId}", etag);
        var submit = () => kit.TransitAsync(org.TeamHead.Client, "submit", etag: etag);

        await using var gate = await RowLock.HoldMatrixAsync(fixture.Host, kit.MatrixId);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, delete);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, submit);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.NoContent);
        var late = await second;
        late.Status.Should().Be(HttpStatusCode.NotFound, late.Body);
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE id = '{kit.VersionId}'")).Should().Be(0);
    }

    [Fact]
    public async Task Ac15_TwoRequestsForANewVersion_LeaveOneVersionInWork_AndTheSecondIsRefused()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        var create = () => org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions");

        await using var gate = await RowLock.HoldMatrixAsync(fixture.Host, kit.MatrixId);
        var first = await RowLock.StartWaitingAsync(fixture.Host, 1, create);
        var second = await RowLock.StartWaitingAsync(fixture.Host, 2, create);
        await gate.ReleaseAsync();

        (await first).Status.Should().Be(HttpStatusCode.Created);
        var loser = await second;
        loser.Status.Should().Be(HttpStatusCode.Conflict, loser.Body);
        loser.Json!["detail"]!.GetValue<string>().Should().Be("У матрицы уже есть версия в работе");
        (await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM matrix_versions WHERE matrix_id = '{kit.MatrixId}' AND status = 'Draft'")).Should().Be(1);
    }

    [Fact]
    public async Task Ac17_TheDatabase_RefusesASecondPublishedOrInWorkVersionOfAMatrix_ASecondMatrixOfAUnitAndTrack_ASecondHrOfAUnit_ASecondDefaultScale_AndTwoLevelsInOnePlace()
    {
        var org = await CareerOrg.CreateAsync(fixture.Host);
        var kit = await (await MatrixKit.CreateAsync(org)).PrepareAsync();
        await kit.PublishAsync();
        var next = (await org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions")).Expect(HttpStatusCode.Created).Json!["id"]!.GetValue<Guid>();
        var scale = await org.Admin.MakeScaleAsync();

        var violations = new Dictionary<string, bool>
        {
            ["a second published version"] = await ViolatesUniqueAsync($"UPDATE matrix_versions SET status = 'Published' WHERE id = '{next}'"),
            ["a second version in work"] = await ViolatesUniqueAsync($"INSERT INTO matrix_versions (id, version, created_at, matrix_id, number, status, author_id, content) SELECT gen_random_uuid(), 0, now(), matrix_id, 99, 'OnReview', author_id, content FROM matrix_versions WHERE id = '{next}'"),
            ["a second matrix of a unit and track"] = await ViolatesUniqueAsync($"INSERT INTO competency_matrices (id, version, created_at, org_unit_id, direction_id, track_id) SELECT gen_random_uuid(), 0, now(), org_unit_id, direction_id, track_id FROM competency_matrices WHERE id = '{kit.MatrixId}'"),
            ["a second HR of a unit"] = await ViolatesUniqueAsync($"INSERT INTO org_unit_hr (org_unit_id, employee_id) VALUES ('{org.Root.Id}', '{org.Hr.Employee}')"),
            ["a second default scale"] = await ViolatesUniqueAsync($"UPDATE competency_scales SET is_default = true WHERE id = '{scale.Id}'"),
            ["two levels in one place"] = await ViolatesUniqueAsync($"UPDATE career_levels SET position = 1 WHERE track_id = '{kit.Track.Id}' AND position = 2"),
        };

        violations.Where(violation => !violation.Value).Select(violation => violation.Key).Should().BeEmpty("the database backs up the locks with unique indexes");
    }

    private async Task RestoreDefaultAsync(CareerOrg org)
    {
        var baseScale = (await org.Admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK);
        (await org.Admin.PostAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}/make-default", ifMatch: baseScale.ETag)).Expect(HttpStatusCode.OK);
    }

    private async Task<bool> ViolatesUniqueAsync(string sql)
    {
        try
        {
            await fixture.Host.ExecuteAsync(sql);
            return false;
        }
        catch (Npgsql.PostgresException exception)
        {
            return exception.SqlState == "23505";
        }
    }

    private async Task<Func<Task<ApiResponse>>> PrepareAsync(string operation, CareerOrg org) => (await PrepareWithMatrixAsync(operation, org)).Send;

    private async Task<(Func<Task<ApiResponse>> Send, Guid MatrixId)> PrepareWithMatrixAsync(string operation, CareerOrg org)
    {
        var matrixId = Guid.Empty;
        var send = await PrepareCoreAsync(operation, org, id => matrixId = id);
        return (send, matrixId);
    }

    private async Task<Func<Task<ApiResponse>>> PrepareCoreAsync(string operation, CareerOrg org, Action<Guid> matrix)
    {
        var admin = org.Admin;
        switch (operation)
        {
            case "position-create":
                return () => admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") });
            case "position-rename":
            {
                var position = (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
                return () => admin.PutAsync($"/api/v1/positions/{position.Id}", new { name = Scenarios.Unique("Другая") }, position.ETag);
            }

            case "position-deactivate":
            {
                var position = (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
                return () => admin.PostAsync($"/api/v1/positions/{position.Id}/deactivate", ifMatch: position.ETag);
            }

            case "unit-position-add":
            {
                var position = (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
                return () => admin.PostAsync($"/api/v1/org-units/{org.Team.Id}/positions", new { positionId = position.Id });
            }

            case "unit-position-remove":
            {
                var positionId = await admin.PositionAsync(org.Team.Id, Scenarios.Unique("Должность"));
                return () => admin.DeleteAsync($"/api/v1/org-units/{org.Team.Id}/positions/{positionId}");
            }

            case "quick-add":
                return () => admin.PostAsync($"/api/v1/org-units/{org.Team.Id}/positions/quick-add", new { name = Scenarios.Unique("Должность") });
            case "employee-create":
            {
                var positionId = await admin.PositionAsync(org.Team.Id, Scenarios.Unique("Должность"));
                return () => admin.PostAsync("/api/v1/employees", new { lastName = Scenarios.Unique("Иванов"), firstName = "Иван", middleName = (string?)null, positionId, orgUnitId = org.Team.Id });
            }

            case "hr-assign":
            {
                var candidate = await admin.HrCandidateAsync(fixture.Host, org.Team.Id);
                return () => admin.AssignHrAsync(org.Team.Id, candidate.EmployeeId);
            }

            case "hr-remove":
                return () => admin.DeleteAsync($"/api/v1/career/org-units/{org.Sibling.Id}/hr");
            case "direction-create":
                return () => org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Направление") });
            case "direction-rename":
            {
                var direction = await admin.MakeDirectionAsync();
                return () => admin.PutAsync($"/api/v1/career/directions/{direction.Id}", new { name = Scenarios.Unique("Другое") }, direction.ETag);
            }

            case "direction-approve":
            {
                var draft = (await org.TeamHead.Client.PostAsync("/api/v1/career/directions", new { name = Scenarios.Unique("Черновик") })).Expect(HttpStatusCode.Created);
                return () => org.Hr.Client.PostAsync($"/api/v1/career/directions/{draft.Id}/approve", ifMatch: draft.ETag);
            }

            case "direction-deactivate":
            {
                var direction = await admin.MakeDirectionAsync();
                return () => admin.PostAsync($"/api/v1/career/directions/{direction.Id}/deactivate", ifMatch: direction.ETag);
            }

            case "direction-delete":
            {
                var direction = await admin.MakeDirectionAsync();
                return () => admin.DeleteAsync($"/api/v1/career/directions/{direction.Id}", direction.ETag);
            }

            case "scale-make-default":
            {
                var scale = await admin.MakeScaleAsync();
                return () => admin.PostAsync($"/api/v1/career/scales/{scale.Id}/make-default", ifMatch: scale.ETag);
            }

            case "level-order":
            {
                var direction = await admin.MakeDirectionAsync();
                var track = await admin.MakeTrackAsync(org.Team.Id, direction.Id);
                var level = await admin.MakeLevelAsync(track.Id);
                return () => admin.PutAsync($"/api/v1/career/tracks/{track.Id}/level-order", new { levelIds = new[] { level.Id } }, track.ETag);
            }

            case "level-create":
            {
                var direction = await admin.MakeDirectionAsync();
                var track = await admin.MakeTrackAsync(org.Team.Id, direction.Id);
                return () => admin.PostAsync("/api/v1/career/levels", new { name = Scenarios.Unique("Уровень"), trackId = track.Id });
            }
        }

        var kit = await MatrixKit.CreateAsync(org);
        matrix(kit.MatrixId);
        switch (operation)
        {
            case "matrix-create":
            {
                var direction = await admin.MakeDirectionAsync();
                var track = await admin.MakeTrackAsync(org.Team.Id, direction.Id);
                return () => org.TeamHead.Client.PostAsync("/api/v1/career/matrices", new { orgUnitId = org.Team.Id, directionId = direction.Id, trackId = track.Id });
            }

            case "matrix-submit":
            {
                await kit.PrepareAsync();
                var etag = await kit.ETagAsync();
                return () => kit.TransitAsync(org.TeamHead.Client, "submit", etag: etag);
            }

            case "matrix-delete":
            {
                var etag = await kit.ETagAsync();
                return () => org.TeamHead.Client.DeleteAsync($"/api/v1/career/matrices/{kit.MatrixId}", etag);
            }

            case "matrix-version-create":
            {
                await kit.PrepareAsync();
                await kit.PublishAsync();
                return () => org.TeamHead.Client.PostAsync($"/api/v1/career/matrices/{kit.MatrixId}/versions");
            }

            default:
            {
                var etag = await kit.ETagAsync();
                var content = kit.Content();
                return () => kit.SaveAsync(org.TeamHead.Client, content, etag);
            }
        }
    }
}
