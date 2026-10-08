using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// One message the mail server received, as far as the tests read it.
/// </summary>
/// <param name="Subject">The subject line.</param>
/// <param name="Text">The plain-text body.</param>
public sealed partial record CapturedMail(string Subject, string Text)
{
    private const string TokenKey = "#token=";

    /// <summary>
    /// The link in the body.
    /// </summary>
    public Uri Link => new(LinkPattern().Match(Text).Value);

    /// <summary>
    /// The path of the link without its slashes, which names the screen the link opens.
    /// </summary>
    public string Screen => Link.AbsolutePath.Trim('/');

    /// <summary>
    /// The token in the fragment of the link.
    /// </summary>
    public string Token => Link.Fragment[TokenKey.Length..];

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkPattern();
}

/// <summary>
/// The mail server of the test run: one Mailpit container that every API host sends its mail to, read back through its REST API.
/// A test finds its mail by the unique address of the recipient, never by position, because the hosts of other tests send to the same server.
/// A server can be made to require the given sign-in, for the tests of the authenticated branch.
/// The server keeps every message and does not look up the sender's host name: with the lookup, each SMTP session waits about ten seconds
/// for the greeting under Docker Desktop, which turns every sending request into a slow one.
/// </summary>
public sealed class MailCatcher : IAsyncDisposable
{
    private const string Image = "axllent/mailpit:v1.31.4";
    private const int SmtpPort = 1025;
    private const int ApiPort = 8025;
    private const int MessagesPerSearch = 200;

    private const string AuthFile = "/smtp-auth.txt";

    private readonly IContainer container;
    private readonly HttpClient api = new();
    private readonly string? userName;
    private readonly string? password;

    /// <summary>
    /// Creates the server, not started yet.
    /// </summary>
    /// <param name="userName">The sign-in the server requires, or null for a server that takes mail from anyone.</param>
    /// <param name="password">The password for <paramref name="userName"/>.</param>
    public MailCatcher(string? userName = null, string? password = null)
    {
        this.userName = userName;
        this.password = password;
        var builder = new ContainerBuilder(Image)
            .WithPortBinding(SmtpPort, true)
            .WithPortBinding(ApiPort, true)
            .WithEnvironment("MP_MAX_MESSAGES", "0")
            .WithEnvironment("MP_SMTP_DISABLE_RDNS", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/readyz")));
        if (userName is not null)
        {
            builder = builder
                .WithEnvironment("MP_SMTP_AUTH_FILE", AuthFile)
                .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
                .WithResourceMapping(Encoding.UTF8.GetBytes($"{userName}:{password}\n"), AuthFile);
        }

        container = builder.Build();
    }

    /// <summary>
    /// The configuration that points a host at this server, as environment variable names with <c>__</c> separators.
    /// </summary>
    public IReadOnlyDictionary<string, string> HostSettings
    {
        get
        {
            var settings = new Dictionary<string, string>
            {
                ["Smtp__Host"] = container.Hostname,
                ["Smtp__Port"] = container.GetMappedPublicPort(SmtpPort).ToString(),
                ["Smtp__SecureSocketOptions"] = "None",
                ["Smtp__From"] = "noreply@calibr.test.local",
            };
            if (userName is not null)
            {
                settings["Smtp__UserName"] = userName;
                settings["Smtp__Password"] = password!;
            }

            return settings;
        }
    }

    public async Task StartAsync()
    {
        await container.StartAsync();
        api.BaseAddress = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(ApiPort)}/");
    }

    /// <summary>
    /// Waits until the recipient has received at least the given number of messages.
    /// </summary>
    /// <param name="recipient">The recipient's address.</param>
    /// <param name="count">How many messages must have arrived.</param>
    /// <returns>Every message the recipient has received, oldest first.</returns>
    public async Task<CapturedMail[]> WaitForAsync(string recipient, int count = 1)
    {
        CapturedMail[] found = [];
        await Waiting.UntilAsync(async () => (found = await FindAsync(recipient)).Length >= count, $"{count} message(s) reached {recipient}");
        return found;
    }

    /// <summary>
    /// Reads what the recipient has received so far.
    /// </summary>
    /// <param name="recipient">The recipient's address.</param>
    /// <returns>Every message the recipient has received, oldest first.</returns>
    public async Task<CapturedMail[]> FindAsync(string recipient)
    {
        var search = await GetAsync($"api/v1/search?query={Uri.EscapeDataString($"to:{recipient}")}&limit={MessagesPerSearch}");
        var ids = search["messages"]!.AsArray()
            .Where(message => message!["To"]!.AsArray().Any(to => string.Equals(to!["Address"]!.GetValue<string>(), recipient, StringComparison.OrdinalIgnoreCase)))
            .Select(message => message!["ID"]!.GetValue<string>())
            .Reverse();
        var mails = new List<CapturedMail>();
        foreach (var id in ids)
        {
            var message = await GetAsync($"api/v1/message/{id}");
            mails.Add(new CapturedMail(message["Subject"]!.GetValue<string>(), message["Text"]!.GetValue<string>()));
        }

        return [.. mails];
    }

    public async ValueTask DisposeAsync()
    {
        api.Dispose();
        await container.DisposeAsync();
    }

    private async Task<JsonNode> GetAsync(string path)
    {
        using var response = await api.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
