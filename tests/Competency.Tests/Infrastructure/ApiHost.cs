using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace Competency.Tests.Infrastructure;

/// <summary>
/// The real API in a child process on a loopback port, started from the build output of the API project against one PostgreSQL database:
/// real Kestrel, real middleware, migrations and first administrator applied at start, exactly as in deployment.
/// A test run killed before its hosts are disposed leaves them running: stop them by their listening port.
/// </summary>
public sealed class ApiHost : IAsyncDisposable
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin-Test-12345!";

    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan LogTimeout = TimeSpan.FromSeconds(10);

    private readonly Process process;
    private readonly StringBuilder logs = new();
    private readonly string keysPath;
    private readonly Lazy<Task<ApiClient>> admin;

    private ApiHost(Process process, string keysPath, Uri baseAddress, string connectionString)
    {
        this.process = process;
        this.keysPath = keysPath;
        BaseAddress = baseAddress;
        ConnectionString = connectionString;
        admin = new Lazy<Task<ApiClient>>(() => LoginAsync(AdminEmail, AdminPassword));
    }

    public Uri BaseAddress { get; }

    /// <summary>
    /// The Npgsql connection string of the host's database, for tests that must reach it directly.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Everything the host wrote to its standard output and error so far.
    /// </summary>
    public string Logs
    {
        get
        {
            lock (logs)
            {
                return logs.ToString();
            }
        }
    }

    /// <summary>
    /// Waits until the host has logged the fragment and returns everything it has logged by then. The console logger writes
    /// asynchronously, so a record can reach the output after the response of the request that caused it.
    /// </summary>
    /// <param name="fragment">The text a record must contain.</param>
    /// <returns>Everything the host wrote to its standard output and error.</returns>
    public async Task<string> LogsAfterAsync(string fragment)
    {
        var waited = Stopwatch.StartNew();
        while (!Logs.Contains(fragment, StringComparison.Ordinal))
        {
            if (waited.Elapsed > LogTimeout)
            {
                throw new TimeoutException($"The API host did not log '{fragment}' in {LogTimeout.TotalSeconds} s.{Environment.NewLine}{Logs}");
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        return Logs;
    }

    /// <summary>
    /// Starts the host and waits until its readiness probe answers.
    /// </summary>
    /// <param name="connectionString">The Npgsql connection string of an empty database.</param>
    /// <param name="settings">Configuration overrides, as environment variable names with <c>__</c> separators.</param>
    /// <returns>The running host.</returns>
    public static async Task<ApiHost> StartAsync(string connectionString, IReadOnlyDictionary<string, string>? settings = null)
    {
        var port = FreePort();
        var keysPath = Path.Combine(Path.GetTempPath(), $"competency-tests-{Guid.NewGuid():N}");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("Competency.Api.dll");
        startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        startInfo.Environment["ConnectionStrings__Default"] = connectionString;
        startInfo.Environment["Bootstrap__AdminEmail"] = AdminEmail;
        startInfo.Environment["Bootstrap__AdminPassword"] = AdminPassword;
        startInfo.Environment["DataProtection__KeysPath"] = keysPath;
        startInfo.Environment["RateLimiting__Login__PermitLimit"] = "100000";
        foreach (var (name, value) in settings ?? new Dictionary<string, string>())
        {
            startInfo.Environment[name] = value;
        }

        var process = new Process { StartInfo = startInfo };
        var host = new ApiHost(process, keysPath, new Uri($"http://127.0.0.1:{port}"), connectionString);
        process.OutputDataReceived += (_, line) => host.Append(line.Data);
        process.ErrorDataReceived += (_, line) => host.Append(line.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await host.WaitUntilReadyAsync();
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }

        return host;
    }

    /// <summary>
    /// A client without a session.
    /// </summary>
    /// <returns>The anonymous client.</returns>
    public ApiClient Anonymous() => new(BaseAddress);

    /// <summary>
    /// The client of the first administrator created from the environment, signed in once and shared by the tests of this host.
    /// Tests must not change or block that account.
    /// </summary>
    /// <returns>The administrator's client.</returns>
    public Task<ApiClient> AdminAsync() => admin.Value;

    /// <summary>
    /// Signs in and returns a client holding the session.
    /// </summary>
    /// <param name="email">The account's e-mail.</param>
    /// <param name="password">The account's password.</param>
    /// <returns>The signed-in client.</returns>
    public async Task<ApiClient> LoginAsync(string email, string password)
    {
        var client = Anonymous();
        (await client.PostAsync("/api/v1/auth/login", new { email, password })).Expect(HttpStatusCode.OK);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (admin.IsValueCreated && admin.Value.IsCompletedSuccessfully)
        {
            admin.Value.Result.Dispose();
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        process.Dispose();
        if (Directory.Exists(keysPath))
        {
            Directory.Delete(keysPath, recursive: true);
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (logs)
        {
            logs.AppendLine(line);
        }
    }

    private async Task WaitUntilReadyAsync()
    {
        using var client = Anonymous();
        var waited = Stopwatch.StartNew();
        while (waited.Elapsed < StartTimeout)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The API host exited with code {process.ExitCode} before it was ready.{Environment.NewLine}{Logs}");
            }

            try
            {
                if ((await client.GetAsync("/healthz/ready")).Status == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"The API host was not ready in {StartTimeout.TotalSeconds} s.{Environment.NewLine}{Logs}");
    }
}
