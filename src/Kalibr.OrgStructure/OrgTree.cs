using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kalibr.OrgStructure;

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
    /// Starts a transaction and takes the tree lock in shared mode until it ends, so that what the caller reads about units, their heads
    /// and employees stays as committed while it acts on it: shared holders run side by side and only wait for, and hold off, the exclusive
    /// holders that change the tree. Taking the exclusive lock later in the same transaction would wait for the other shared holders while
    /// they may wait for it, so a caller that may change the tree takes <see cref="BeginExclusiveAsync"/> instead.
    /// </summary>
    /// <param name="context">The context the changes will be saved through.</param>
    /// <param name="cancellationToken">Cancels the wait for the lock.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the lock.</returns>
    public static async Task<IDbContextTransaction> BeginSharedAsync(this AppDbContext context, CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock_shared({LockKey})", cancellationToken);
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

    /// <summary>
    /// A unit and all units above it up to its root, nearest first. The depth that orders them makes every row distinct, so a cycle
    /// in the parent links is cut by the cycle check rather than by set union.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="unitId">The bottom of the chain.</param>
    /// <returns>A query to run as it is, since composing it would drop the order; empty when the unit does not exist.</returns>
    public static IQueryable<OrgUnitStatus> Ancestry(this AppDbContext context, Guid unitId) => context.Database.SqlQuery<OrgUnitStatus>($"""
        WITH RECURSIVE ancestry(id, name, parent_id, head_employee_id, is_active, depth) AS (
            SELECT id, name, parent_id, head_employee_id, is_active, 0 FROM org_units WHERE id = {unitId}
            UNION ALL
            SELECT parent.id, parent.name, parent.parent_id, parent.head_employee_id, parent.is_active, ancestry.depth + 1
            FROM org_units AS parent JOIN ancestry ON parent.id = ancestry.parent_id)
            CYCLE id SET is_cycle USING path
        SELECT id AS "Id", name AS "Name", parent_id AS "ParentId", head_employee_id AS "HeadEmployeeId", is_active AS "IsActive"
        FROM ancestry
        WHERE NOT is_cycle
        ORDER BY depth
        """);
}
