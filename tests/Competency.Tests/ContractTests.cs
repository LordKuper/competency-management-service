using System.Net;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// The published API description and the rules every list endpoint shares. The description is the file the build generates from the running
/// application, so what it says is what clients are generated from.
/// </summary>
public sealed class ContractTests(TestEnvironment environment)
{
    private const string SolutionFile = "Competency.slnx";
    private const string ContractPath = "openapi/openapi.json";
    private const string RemovedPasswordMethod = "/api/v1/users/{id}/reset-password";
    private const int LongestPage = 200;
    private const int LastPage = int.MaxValue / LongestPage;

    private static readonly string[] AnonymousLinkMethods =
    [
        "/api/v1/auth/forgot-password",
        "/api/v1/auth/accept-invitation",
        "/api/v1/auth/reset-password",
    ];

    [Fact]
    public void Ac9_Contract_IsVersion2_WithoutTheAdministratorsPasswordMethod_AndWithTheLinkMethods()
    {
        var contract = ReadContract();
        var paths = contract["paths"]!.AsObject();

        contract["info"]!["version"]!.GetValue<string>().Should().Be("2.0.0");
        paths.ContainsKey(RemovedPasswordMethod).Should().BeFalse("an administrator no longer sets another account's password");
        paths.Select(path => path.Key).Should().OnlyContain(path => path.StartsWith("/api/v1/") || path.StartsWith("/healthz/"), "the path prefix did not change");
        foreach (var added in new[] { "/api/v1/users/{id}/resend-invitation", "/api/v1/users/{id}/send-password-reset" }.Concat(AnonymousLinkMethods))
        {
            paths.ContainsKey(added).Should().BeTrue(added);
        }

        var createUser = contract["components"]!["schemas"]!["CreateUserRequest"]!["properties"]!.AsObject();
        createUser.ContainsKey("password").Should().BeFalse("accounts are created without a password");
    }

    [Fact]
    public void Ac16_Contract_KeepsTheSchemaNamesOfTheSharedPagedResponse()
    {
        var schemas = ReadContract()["components"]!["schemas"]!.AsObject();

        foreach (var name in new[] { "PageResponseOfUserResponse", "PageResponseOfOrgUnitResponse", "PageResponseOfEmployeeResponse", "AuditPageResponse" })
        {
            schemas.ContainsKey(name).Should().BeTrue($"moving the paged response to the platform must not rename the schema '{name}' that clients are generated from");
        }
    }

    [Fact]
    public void Ac5_AnonymousLinkMethods_DeclareNo401_BecauseTheApplicationTakesAnyOfItForAnEndedSession()
    {
        var paths = ReadContract()["paths"]!.AsObject();

        foreach (var path in AnonymousLinkMethods)
        {
            var responses = paths[path]!["post"]!["responses"]!.AsObject();
            responses.ContainsKey("401").Should().BeFalse(path);
        }
    }

    [Fact]
    public void Ac19_PasswordPolicy_IsPublished_AsAnAnonymousGet_WithoutA401Or429_AndWithTheMinimumLength()
    {
        var contract = ReadContract();
        var responses = contract["paths"]!["/api/v1/auth/password-policy"]!["get"]!["responses"]!.AsObject();

        responses.ContainsKey("200").Should().BeTrue();
        responses.ContainsKey("401").Should().BeFalse("the method is open to an anonymous caller");
        responses.ContainsKey("429").Should().BeFalse("the method is not rate limited");
        contract["components"]!["schemas"]!["PasswordPolicyResponse"]!["properties"]!.AsObject().ContainsKey("minLength").Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/org-units")]
    [InlineData("/api/v1/employees")]
    [InlineData("/api/v1/audit")]
    public async Task Ac16_EveryList_RefusesPagingBeyondTheSharedLimits_WithTheMessagesItAlwaysGave(string path)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var belowFirst = await admin.GetAsync($"{path}?page=0");
        var aboveLast = await admin.GetAsync($"{path}?page={LastPage + 1}");
        var tooLong = await admin.GetAsync($"{path}?pageSize={LongestPage + 1}");
        var empty = await admin.GetAsync($"{path}?pageSize=0");
        var longest = await admin.GetAsync($"{path}?pageSize={LongestPage}");

        foreach (var refused in new[] { belowFirst, aboveLast })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest);
            refused.FieldErrors("page").Should().Equal($"page must be between 1 and {LastPage}.");
        }

        foreach (var refused in new[] { tooLong, empty })
        {
            refused.Status.Should().Be(HttpStatusCode.BadRequest);
            refused.FieldErrors("pageSize").Should().Equal($"pageSize must be between 1 and {LongestPage}.");
        }

        longest.Status.Should().Be(HttpStatusCode.OK, "the largest page the limit allows");
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/org-units")]
    [InlineData("/api/v1/employees")]
    public async Task Ac16_EverySearchableList_RefusesSearchTextBeyondTheSharedLimit_WithTheMessageItAlwaysGave(string path)
    {
        var admin = await (await environment.SharedHostAsync()).AdminAsync();

        var tooLong = await admin.GetAsync($"{path}?q={new string('я', LongestPage + 1)}");
        var longest = await admin.GetAsync($"{path}?q={new string('я', LongestPage)}");

        tooLong.Status.Should().Be(HttpStatusCode.BadRequest);
        tooLong.FieldErrors("q").Should().Equal($"q must not be longer than {LongestPage} characters.");
        longest.Status.Should().Be(HttpStatusCode.OK);
    }

    private static JsonNode ReadContract()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles(SolutionFile).Any())
        {
            directory = directory.Parent;
        }

        return JsonNode.Parse(File.ReadAllText(Path.Combine(directory?.FullName ?? throw new FileNotFoundException(SolutionFile), ContractPath)))!;
    }
}
