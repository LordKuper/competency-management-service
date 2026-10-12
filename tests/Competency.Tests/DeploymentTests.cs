using AwesomeAssertions;
using Xunit;

namespace Competency.Tests;

/// <summary>
/// What the image build and the operators read must follow the code: every source project is restored before the sources are copied in, or the
/// image build fails, and the deployment guide describes the move to contract 3.0.0 and the migration of the positions.
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

        projects.Should().Contain("Competency.CareerFramework", "the project list is read from the source tree");
        foreach (var project in projects)
        {
            var copy = $"COPY src/{project}/{project}.csproj src/{project}/packages.lock.json src/{project}/";
            var at = dockerfile.IndexOf(copy, StringComparison.Ordinal);
            at.Should().BeGreaterThanOrEqualTo(0, $"{project} must be copied with its lock file");
            at.Should().BeLessThan(restore, $"{project} must be copied before the restore");
        }
    }

    [Fact]
    public void Ac6_TheDeploymentGuide_DescribesTheMoveToContract3AndTheMigrationOfThePositions()
    {
        var guide = File.ReadAllText(Path.Combine(RepositoryRoot(), "deploy", "README.md"));

        guide.Should().Contain("3.0.0").And.Contain("positionId").And.Contain("PositionCatalogue");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles("Competency.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new FileNotFoundException("Competency.slnx");
    }
}
