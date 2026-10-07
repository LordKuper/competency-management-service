using System.Net;
using AwesomeAssertions;
using Competency.Tests.Infrastructure;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// A state-changing request a browser reports as cross-site is refused before it reaches any endpoint, even with a valid session.
/// </summary>
public sealed class CsrfTests(TestEnvironment environment)
{
    [Theory]
    [InlineData("Origin", "http://evil.example")]
    [InlineData("Origin", "http://127.0.0.1:1")]
    [InlineData("Sec-Fetch-Site", "cross-site")]
    [InlineData("Sec-Fetch-Site", "same-site")]
    public async Task Ac6_CrossSiteMutation_IsRefusedWith403_AndChangesNothing(string header, string value)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var name = Scenarios.Unique("Отдел-csrf");

        var refused = await admin.PostAsync("/api/v1/org-units", new { name }, configure: request => request.Headers.Add(header, value));

        refused.Status.Should().Be(HttpStatusCode.Forbidden, refused.Body);
        (await admin.GetAsync($"/api/v1/org-units?q={name}")).Json!["total"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task Ac6_CrossSiteSignIn_IsRefusedWith403()
    {
        var host = await environment.SharedHostAsync();

        var refused = await host.Anonymous().PostAsync(
            "/api/v1/auth/login",
            new { email = ApiHost.AdminEmail, password = ApiHost.AdminPassword },
            configure: request => request.Headers.Add("Origin", "http://evil.example"));

        refused.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Sec-Fetch-Site", "same-origin")]
    [InlineData("Sec-Fetch-Site", "none")]
    [InlineData("Origin", "own")]
    public async Task Ac6_SameOriginAndNonBrowserMutations_Pass(string? header, string? value)
    {
        var host = await environment.SharedHostAsync();
        var admin = await host.AdminAsync();
        var sent = value == "own" ? host.BaseAddress.GetLeftPart(UriPartial.Authority) : value;

        var created = await admin.PostAsync(
            "/api/v1/org-units",
            new { name = Scenarios.Unique("Отдел-csrf-ok") },
            configure: request =>
            {
                if (header is not null)
                {
                    request.Headers.Add(header, sent);
                }
            });

        created.Status.Should().Be(HttpStatusCode.Created, created.Body);
    }

    [Fact]
    public async Task Ac6_CrossSiteRead_IsNotAMutationAndPasses()
    {
        var host = await environment.SharedHostAsync();

        var read = await (await host.AdminAsync()).GetAsync("/api/v1/org-units", request => request.Headers.Add("Origin", "http://evil.example"));

        read.Status.Should().Be(HttpStatusCode.OK);
    }
}
