using System.Linq.Expressions;

namespace Competency.CareerFramework;

/// <summary>
/// What the shared catalogue endpoints need to know about one entry type.
/// </summary>
/// <typeparam name="TEntry">The entry type.</typeparam>
/// <typeparam name="TResponse">The entry as the API returns it.</typeparam>
/// <param name="Name">The entry type's name in endpoint names, such as <c>CareerTrack</c>.</param>
/// <param name="Describe">Builds the response from the entry and the name of its unit, null for an entry of no unit.</param>
/// <param name="SameScope">The entries whose names must differ from the given entry's; null when names are unique across the catalogue.</param>
internal sealed record CatalogKind<TEntry, TResponse>(
    string Name,
    Func<TEntry, string?, TResponse> Describe,
    Func<TEntry, Expression<Func<TEntry, bool>>>? SameScope = null)
    where TEntry : CatalogEntry;
