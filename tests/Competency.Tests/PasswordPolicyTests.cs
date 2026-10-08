using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A host whose deployment raises the shortest password above the shipped default.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class LongPasswordHost(TestEnvironment environment)
    : HostFixture(environment, new Dictionary<string, string> { ["Identity__Password__RequiredLength"] = LongPasswordHost.MinLength.ToString() })
{
    public const int MinLength = 17;
}

/// <summary>
/// The interface learns the shortest accepted password from the server, without a session, and the number follows the deployment's setting.
/// </summary>
public sealed class PasswordPolicyTests(LongPasswordHost fixture, TestEnvironment environment) : IClassFixture<LongPasswordHost>
{
    private const string Path = "/api/v1/auth/password-policy";
    private const int ShippedDefault = 10;

    [Fact]
    public async Task Ac19_PasswordPolicy_ReportsTheLengthThatDeploymentConfigured_WithoutASession()
    {
        var policy = (await fixture.Host.Anonymous().GetAsync(Path)).Expect(HttpStatusCode.OK);

        policy.Json!["minLength"]!.GetValue<int>().Should().Be(LongPasswordHost.MinLength);
    }

    [Fact]
    public async Task Ac19_PasswordPolicy_ReportsTheShippedDefault_WhenNothingIsOverridden()
    {
        var host = await environment.SharedHostAsync();

        var policy = (await host.Anonymous().GetAsync(Path)).Expect(HttpStatusCode.OK);

        policy.Json!["minLength"]!.GetValue<int>().Should().Be(ShippedDefault);
    }
}
