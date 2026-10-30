using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace Kalibr.Tests.Infrastructure;

/// <summary>
/// The published API description read from the repository, so a test can take the set of operations from the contract instead of listing them.
/// </summary>
public static partial class ApiContract
{
    private const string SolutionFile = "Kalibr.slnx";
    private const string ContractPath = "openapi/openapi.json";
    private static readonly string[] HttpMethods = ["get", "post", "put", "delete"];

    /// <summary>
    /// Reads the contract file next to the solution file.
    /// </summary>
    /// <returns>The OpenAPI document.</returns>
    public static JsonNode Read()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !directory.EnumerateFiles(SolutionFile).Any())
        {
            directory = directory.Parent;
        }

        return JsonNode.Parse(File.ReadAllText(Path.Combine(directory?.FullName ?? throw new FileNotFoundException(SolutionFile), ContractPath)))!;
    }

    /// <summary>
    /// Every operation of the contract with its upper-case method and its path with each placeholder written <c>{id}</c>.
    /// </summary>
    /// <returns>The operations.</returns>
    public static IReadOnlyList<(string Method, string Path, JsonNode Operation)> Operations() =>
    [
        .. Read()["paths"]!.AsObject().SelectMany(path => HttpMethods
            .Where(method => path.Value![method] is not null)
            .Select(method => (method.ToUpperInvariant(), Normalize(path.Key), path.Value![method]!))),
    ];

    /// <summary>
    /// Writes each placeholder of a path as <c>{id}</c>, and a literal number after <c>variants</c> too, so a hand-written path compares with the contract's.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    public static string Normalize(string path) => NumberSegment().Replace(Placeholder().Replace(path, "{id}"), "/variants/{id}");

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"/variants/\d+")]
    private static partial Regex NumberSegment();
}
