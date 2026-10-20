using System.Linq.Expressions;

namespace Competency.CareerFramework;

/// <summary>
/// What the shared catalogue endpoints need to know about one entry type.
/// </summary>
/// <typeparam name="TEntry">The entry type.</typeparam>
/// <typeparam name="TResponse">The entry as the API returns it.</typeparam>
/// <param name="Name">The entry type's name in endpoint names, such as <c>CareerTrack</c>.</param>
/// <param name="Describe">Builds the response from the entry, the names of its unit and of the entries it <paramref name="References"/> by
/// identifier, and the signed-in user's rights.</param>
/// <param name="SameScope">The entries whose names must differ from the given entry's; null when names are unique across the catalogue.</param>
/// <param name="References">The directions, scales and competencies whose names the response carries; null when it carries none.</param>
internal sealed record CatalogKind<TEntry, TResponse>(
    string Name,
    Func<TEntry, IReadOnlyDictionary<Guid, string>, CatalogViewer, TResponse> Describe,
    Func<TEntry, Expression<Func<TEntry, bool>>>? SameScope = null,
    Func<TEntry, IEnumerable<Guid?>>? References = null)
    where TEntry : CatalogEntry;
