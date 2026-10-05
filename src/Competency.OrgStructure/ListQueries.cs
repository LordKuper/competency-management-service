using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// The paging rules and query steps the unit and employee lists share.
/// </summary>
internal static class ListQueries
{
    public const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;
    private const int MaxPage = int.MaxValue / MaxPageSize;

    /// <summary>
    /// Checks the paging and search text of a list request.
    /// </summary>
    /// <param name="page">The requested page, counting from 1.</param>
    /// <param name="pageSize">The requested number of rows per page.</param>
    /// <param name="searchText">The requested search text.</param>
    /// <returns>The problems found by field name; empty when the request is valid.</returns>
    public static Dictionary<string, string[]> Validate(int page, int pageSize, string? searchText)
    {
        var errors = new Dictionary<string, string[]>();
        if (page is < 1 or > MaxPage)
        {
            errors["page"] = [$"page must be between 1 and {MaxPage}."];
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];
        }

        if (searchText?.Length > TextSearch.MaxLength)
        {
            errors["q"] = [$"q must not be longer than {TextSearch.MaxLength} characters."];
        }

        return errors;
    }

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
