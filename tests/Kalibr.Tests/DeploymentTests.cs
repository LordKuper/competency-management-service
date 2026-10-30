using AwesomeAssertions;
using Xunit;

namespace Kalibr.Tests;

/// <summary>
/// What the image build and the operators read must follow the code: every source project is restored before the sources are copied in, or the
/// image build fails, and the deployment guide tells how to move an existing deployment to the squashed migration line.
/// </summary>
public sealed class DeploymentTests
{
    [Fact]
    public void Ac8_TheImageBuild_RestoresEverySourceProject_WithItsLockFile_BeforeCopyingTheSources()
    {
        var root = RepositoryRoot();
        var dockerfile = File.ReadAllText(Path.Combine(root, "Dockerfile"));
        var restore = dockerfile.IndexOf("RUN dotnet restore", StringComparison.Ordinal);
        var projects = Directory.GetDirectories(Path.Combine(root, "src"))
            .Select(Path.GetFileName)
            .Where(name => File.Exists(Path.Combine(root, "src", name!, $"{name}.csproj")))
            .ToList();

        projects.Should().Contain("Kalibr.CareerFramework", "the project list is read from the source tree");
        foreach (var project in projects)
        {
            var copy = $"COPY src/{project}/{project}.csproj src/{project}/packages.lock.json src/{project}/";
            var at = dockerfile.IndexOf(copy, StringComparison.Ordinal);
            at.Should().BeGreaterThanOrEqualTo(0, $"{project} must be copied with its lock file");
            at.Should().BeLessThan(restore, $"{project} must be copied before the restore");
        }
    }

    [Fact]
    public void Ac6_TheDeploymentGuide_DescribesRecreatingTheDatabaseOfAnOlderMigrationLine()
    {
        var guide = File.ReadAllText(Path.Combine(RepositoryRoot(), "deploy", "README.md"));

        guide.Should().Contain("InitialCreate").And.Contain("1.0.0").And.Contain("__EFMigrationsHistory");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("Kalibr.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new FileNotFoundException("Kalibr.slnx");
    }
}
