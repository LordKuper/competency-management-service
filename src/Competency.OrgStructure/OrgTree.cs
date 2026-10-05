using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// The tree-wide lock and the recursive queries over the unit hierarchy, which is stored as parent links.
/// The recursive queries use set union, so they terminate even if the links ever formed a cycle.
/// </summary>
internal static class OrgTree
{
    private const long LockKey = 5_001_001;

    /// <summary>
    /// Starts a transaction and takes the exclusive tree lock until it ends, so every change to units and employees runs one at a time
    /// and each invariant check sees the committed result of the previous change. One lock for the whole tree suits administrator-driven
    /// edit rates; per-subtree locking is the upgrade path if edits ever contend. The wait is bounded by the database command timeout,
    /// and the lock needs the default read committed isolation.
    /// </summary>
    /// <param name="context">The context the changes will be saved through.</param>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    public static async Task<IDbContextTransaction> BeginExclusiveAsync(this AppDbContext context, CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// The identifiers of a unit and all units below it.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="unitId">The top of the subtree.</param>
    /// <returns>A query to compose into larger queries; empty when the unit does not exist.</returns>
    public static IQueryable<Guid> DescendantIds(this AppDbContext context, Guid unitId) => context.Database.SqlQuery<Guid>($"""
        WITH RECURSIVE subtree(id) AS (
            SELECT id FROM org_units WHERE id = {unitId}
            UNION
            SELECT child.id FROM org_units AS child JOIN subtree ON child.parent_id = subtree.id)
        SELECT id AS "Value" FROM subtree
        """);

    /// <summary>
    /// The identifiers of a unit and all units above it up to its root.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="unitId">The bottom of the chain.</param>
    /// <returns>A query to compose into larger queries; empty when the unit does not exist.</returns>
    public static IQueryable<Guid> AncestorIds(this AppDbContext context, Guid unitId) => context.Database.SqlQuery<Guid>($"""
        WITH RECURSIVE ancestry(id, parent_id) AS (
            SELECT id, parent_id FROM org_units WHERE id = {unitId}
            UNION
            SELECT parent.id, parent.parent_id FROM org_units AS parent JOIN ancestry ON parent.id = ancestry.parent_id)
        SELECT id AS "Value" FROM ancestry
        """);
}
