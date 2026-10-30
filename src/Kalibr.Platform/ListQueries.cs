using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Competency.Platform;

/// <summary>
/// The query steps the module lists share.
/// </summary>
public static class ListQueries
{
    /// <summary>
    /// Applies a filter only when the request asks for it.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="source">The query to filter.</param>
    /// <param name="condition">Whether to apply the filter.</param>
    /// <param name="predicate">The filter.</param>
    /// <returns>The filtered query, or the same query when the condition is false.</returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate) =>
        condition ? source.Where(predicate) : source;

    /// <summary>
    /// Reads one page of an already ordered query together with the number of rows across all pages.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="ordered">The ordered query.</param>
    /// <param name="page">The page to read, counting from 1.</param>
    /// <param name="pageSize">The number of rows per page.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The page.</returns>
    public static async Task<PageResponse<T>> ToPageAsync<T>(this IQueryable<T> ordered, int page, int pageSize, CancellationToken cancellationToken)
    {
        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PageResponse<T>(items, total, page, pageSize);
    }
}
