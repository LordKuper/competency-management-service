namespace Competency.Tests.Infrastructure;

/// <summary>
/// A host with a database of its own for a class of career tests. The tests of one class run one after another, so the locks a test holds and
/// the lock waiters it counts are its own, and the settings every test of the class shares (the default scale, the set of units) are the class's.
/// </summary>
/// <param name="environment">The shared environment that supplies the database.</param>
public sealed class CareerHost(TestEnvironment environment) : HostFixture(environment, null)
{
    private Task<CareerOrg>? org;

    /// <summary>
    /// A tree of units and people built once for the class, for tests that only read the roles in it. A test that changes a role, blocks an
    /// account or moves a unit builds a tree of its own with <see cref="CareerOrg.CreateAsync"/>.
    /// </summary>
    /// <returns>The class's tree.</returns>
    public Task<CareerOrg> OrgAsync() => org ??= CareerOrg.CreateAsync(Host);
}
