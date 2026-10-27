using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// An employee's position is a reference to the catalogue: a new position, and any position after a transfer, must be active and allowed in the
/// unit or a unit above it; a save that changes neither the position nor the unit keeps an earlier position whatever became of it.
/// </summary>
public sealed class EmployeePositionTests(TestEnvironment environment)
{
    private const string NotAllowed = "Должность недоступна в выбранном подразделении";
    private const string Inactive = "Должность неактивна";

    [Theory]
    [InlineData("own", true, null)]
    [InlineData("ancestor", true, null)]
    [InlineData("descendant", false, NotAllowed)]
    [InlineData("sibling", false, NotAllowed)]
    [InlineData("unlisted", false, NotAllowed)]
    [InlineData("inactive", false, Inactive)]
    [InlineData("unknown", false, NotAllowed)]
    public async Task Ac3_Create_AcceptsAnActivePositionAllowedInTheUnitOrAUnitAbove_AndRefusesAllElse(string where, bool accepted, string? message)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var parent = await admin.CreateUnitAsync();
        var unit = await admin.CreateUnitAsync(parent.Id);
        var child = await admin.CreateUnitAsync(unit.Id);
        var sibling = await admin.CreateUnitAsync(parent.Id);
        var position = await CreatePositionAsync(admin);
        var listed = where switch
        {
            "own" => unit,
            "ancestor" => parent,
            "descendant" => child,
            "sibling" => sibling,
            "unlisted" => null,
            _ => unit,
        };
        if (listed is not null)
        {
            (await admin.PostAsync($"/api/v1/org-units/{listed.Id}/positions", new { positionId = position.Id })).Expect(HttpStatusCode.NoContent);
        }

        if (where == "inactive")
        {
            (await admin.PostAsync($"/api/v1/positions/{position.Id}/deactivate", ifMatch: position.ETag)).Expect(HttpStatusCode.OK);
        }

        var response = await admin.PostAsync("/api/v1/employees", Body(where == "unknown" ? Guid.NewGuid() : position.Id, unit.Id));

        response.Status.Should().Be(accepted ? HttpStatusCode.Created : HttpStatusCode.BadRequest, response.Body);
        if (accepted)
        {
            response.Json!["positionId"]!.GetValue<Guid>().Should().Be(position.Id);
            response.Json["position"]!.GetValue<string>().Should().Be(position.Json!["name"]!.GetValue<string>());
        }
        else
        {
            response.FieldErrors("positionId").Should().Equal(message!);
        }
    }

    [Fact]
    public async Task Ac3_Update_ChecksThePositionOnlyWhenThePositionOrTheUnitChanges_SoAnEarlierPositionSurvivesUnrelatedEdits()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var elsewhere = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: Scenarios.Unique("Редкая"));
        var heldId = employee.Json!["positionId"]!.GetValue<Guid>();
        (await admin.DeleteAsync($"/api/v1/org-units/{unit.Id}/positions/{heldId}")).Expect(HttpStatusCode.NoContent);
        var held = (await admin.GetAsync($"/api/v1/positions/{heldId}")).Expect(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/v1/positions/{heldId}/deactivate", ifMatch: held.ETag)).Expect(HttpStatusCode.OK);
        var other = await CreatePositionAsync(admin);

        var renamed = await admin.PutAsync($"/api/v1/employees/{employee.Id}", Body(heldId, unit.Id, "Переименованный"), employee.ETag);
        var toAnother = await admin.PutAsync($"/api/v1/employees/{employee.Id}", Body(other.Id, unit.Id), renamed.ETag);
        var transferred = await admin.PutAsync($"/api/v1/employees/{employee.Id}", Body(heldId, elsewhere.Id), renamed.ETag);

        renamed.Status.Should().Be(HttpStatusCode.OK, "the position is neither active nor listed, and it stays as it was");
        renamed.Json!["positionId"]!.GetValue<Guid>().Should().Be(heldId);
        toAnother.Status.Should().Be(HttpStatusCode.BadRequest, "a new position must be allowed in the unit");
        toAnother.FieldErrors("positionId").Should().Equal(NotAllowed);
        transferred.Status.Should().Be(HttpStatusCode.BadRequest, "a transfer checks the position again");
        transferred.FieldErrors("positionId").Should().Equal(Inactive);
    }

    [Fact]
    public async Task Ac3_Update_AfterATransfer_AcceptsAPositionTheNewUnitAllows()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var target = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var positionId = employee.Json!["positionId"]!.GetValue<Guid>();
        (await admin.PostAsync($"/api/v1/org-units/{target.Id}/positions", new { positionId })).Expect(HttpStatusCode.NoContent);

        var moved = await admin.PutAsync($"/api/v1/employees/{employee.Id}", Body(positionId, target.Id), employee.ETag);

        moved.Status.Should().Be(HttpStatusCode.OK, moved.Body);
        moved.Json!["orgUnitId"]!.GetValue<Guid>().Should().Be(target.Id);
    }

    [Fact]
    public async Task Ac3_Employee_ShowsThePositionIdentifierAndName_InBothProjections_AndFollowsARenameOfThePosition()
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var user = (await admin.PlainMemberAsync(host)).Client;
        var unit = await admin.CreateUnitAsync();
        var name = Scenarios.Unique("Прораб");
        var employee = await admin.CreateEmployeeAsync(unit.Id, position: name);
        var positionId = employee.Json!["positionId"]!.GetValue<Guid>();
        var position = (await admin.GetAsync($"/api/v1/positions/{positionId}")).Expect(HttpStatusCode.OK);

        var renamed = (await admin.PutAsync($"/api/v1/positions/{positionId}", new { name = $"{name} старший" }, position.ETag)).Expect(HttpStatusCode.OK);

        foreach (var reader in new[] { admin, user })
        {
            var seen = (await reader.GetAsync($"/api/v1/employees/{employee.Id}")).Expect(HttpStatusCode.OK);
            seen.Json!["positionId"]!.GetValue<Guid>().Should().Be(positionId);
            seen.Json["position"]!.GetValue<string>().Should().Be(renamed.Json!["name"]!.GetValue<string>());
        }

        var listed = (await admin.GetAsync($"/api/v1/employees?orgUnitId={unit.Id}")).Expect(HttpStatusCode.OK);
        listed.Json!["items"]![0]!["position"]!.GetValue<string>().Should().Be($"{name} старший");
    }

    [Fact]
    public async Task Ac6_Create_WithoutAPositionIdentifier_OrWithTheOldTextField_IsRefused()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();

        var missing = await admin.PostAsync("/api/v1/employees", new { lastName = "Иванов", firstName = "Иван", middleName = (string?)null, orgUnitId = unit.Id, legalEntityIds = Array.Empty<Guid>(), assignments = Array.Empty<object>() });
        var text = await admin.PostAsync("/api/v1/employees", new { lastName = "Иванов", firstName = "Иван", middleName = (string?)null, position = "Инженер", orgUnitId = unit.Id, legalEntityIds = Array.Empty<Guid>(), assignments = Array.Empty<object>() });

        missing.Status.Should().Be(HttpStatusCode.BadRequest);
        text.Status.Should().Be(HttpStatusCode.BadRequest, "the free-text position is no longer an input");
    }

    [Fact]
    public async Task Ac3_PositionAndUnitChecks_AreJournaledWithTheEmployeeChange()
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();
        var unit = await admin.CreateUnitAsync();
        var employee = await admin.CreateEmployeeAsync(unit.Id);
        var other = await admin.PositionAsync(unit.Id, Scenarios.Unique("Другая"));

        var changed = (await admin.PutAsync($"/api/v1/employees/{employee.Id}", Body(other, unit.Id), employee.ETag)).Expect(HttpStatusCode.OK);

        var row = (await admin.AuditOfRequestAsync(changed.RequestId!)).Single()!;
        row["action"]!.GetValue<string>().Should().Be("Employee.Updated");
        row["oldValue"]!["positionId"]!.GetValue<Guid>().Should().Be(employee.Json!["positionId"]!.GetValue<Guid>());
        row["newValue"]!["positionId"]!.GetValue<Guid>().Should().Be(other);
    }

    private static object Body(Guid positionId, Guid unitId, string lastName = "Иванов") =>
        Scenarios.EmployeeBody(lastName, "Иван", positionId, unitId);

    private static async Task<ApiResponse> CreatePositionAsync(ApiClient admin) =>
        (await admin.PostAsync("/api/v1/positions", new { name = Scenarios.Unique("Должность") })).Expect(HttpStatusCode.Created);
}
