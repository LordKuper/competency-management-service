using System.Net;
using AwesomeAssertions;
using Kalibr.Tests.Infrastructure;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// The scale the deployment creates: its points with their texts are the contract of the initial data, and the creation leaves no audit record.
/// The class has a host of its own, so no other test has touched the scale.
/// </summary>
public sealed class BaseScaleSeedTests(CareerHost fixture) : IClassFixture<CareerHost>
{
    private static readonly (int Score, string Label)[] BasePoints =
    [
        (0, "Компетенция не подтверждена"),
        (1, "Выполняет работу преимущественно с поддержкой"),
        (2, "Самостоятельно решает типовые задачи"),
        (3, "Самостоятельно решает сложные и неопределённые задачи, способен помогать другим"),
        (4, "Формирует подходы и стандарты, решает наиболее сложные задачи и развивает других"),
    ];

    [Fact]
    public async Task Ac25_TheBaseScale_IsActiveAndTheDefault_WithTheFivePointsOfTheSource()
    {
        var admin = await fixture.Host.AdminAsync();

        var scale = (await admin.GetAsync($"/api/v1/career/scales/{CareerScenarios.BaseScaleId}")).Expect(HttpStatusCode.OK).Json!;

        scale["isDefault"]!.GetValue<bool>().Should().BeTrue();
        scale["status"]!.GetValue<string>().Should().Be("Active");
        scale["points"]!.AsArray().Select(point => (point!["score"]!.GetValue<int>(), point["label"]!.GetValue<string>())).Should().Equal(BasePoints);
    }

    [Fact]
    public async Task Ac25_TheCreationOfTheBaseScale_LeavesNoAuditRecord()
    {
        var recorded = await fixture.Host.ScalarAsync<long>($"SELECT count(*) FROM audit_events WHERE entity_id = '{CareerScenarios.BaseScaleId}'");

        recorded.Should().Be(0);
    }
}
