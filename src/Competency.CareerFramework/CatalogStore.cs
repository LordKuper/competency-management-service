using System.Linq.Expressions;
using Competency.OrgStructure;
using Competency.Platform;
using Competency.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.CareerFramework;

/// <summary>
/// The database side of the career catalogues: the write lock, the rule checks that read other entries, saving, and the names of the
/// units and accounts an entry refers to. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class CatalogStore(
    AppDbContext context,
    IEmployeeDirectory employees,
    IUserDirectory users,
    ProcessRoles roles,
    MatrixRoute route,
    ICurrentActor actor,
    IAuditWriter audit,
    TimeProvider time)
{
    private const long LockKey = 5_003_001;
    private const string DeletedAction = "Deleted";

    /// <summary>
    /// Starts a transaction for a catalogue change: first the tree lock in shared mode, because the writer's rights come from the tree,
    /// then the catalogue lock, so that name checks, which see drafts other writers cannot, run one at a time.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public async Task<IDbContextTransaction> BeginWriteAsync(CancellationToken cancellationToken)
    {
        var transaction = await employees.BeginSharedAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({LockKey})", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Starts a transaction for a change that only refers to catalogue entries: the tree lock and then the catalogue lock, both in shared
    /// mode, so entries it checks cannot be deleted before it commits while such changes still run side by side.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait for the locks.</param>
    /// <returns>The open transaction; the caller commits it, and disposing it releases the locks.</returns>
    public async Task<IDbContextTransaction> BeginSharedAsync(CancellationToken cancellationToken)
    {
        var transaction = await employees.BeginSharedAsync(cancellationToken);
        await context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock_shared({LockKey})", cancellationToken);
        return transaction;
    }

    /// <summary>
    /// Works out the signed-in user's rights in the catalogues, including the drafts they read through the matrix versions they take part in.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The user's rights.</returns>
    public async Task<CatalogViewer> ViewerAsync(CancellationToken cancellationToken)
    {
        var viewer = await CatalogViewer.LoadAsync(actor, roles, cancellationToken);
        return viewer with { RouteEntryIds = await route.RouteEntryIdsAsync(viewer, cancellationToken) };
    }

    /// <summary>
    /// Finds an entry the user can read, tracked for a change.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <param name="id">The entry identifier.</param>
    /// <param name="viewer">The user's rights.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The entry, or <see langword="null"/> when it does not exist or the user cannot read it.</returns>
    public Task<T?> FindVisibleAsync<T>(Guid id, CatalogViewer viewer, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        context.Set<T>().Where(viewer.Sees<T>()).FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    /// <summary>
    /// Whether another entry in the same scope has the entry's name regardless of letter case. An entry competes with the entries that are
    /// not drafts and with the drafts of its own author, so a draft another author keeps to themselves never blocks a name, and is checked again when approved.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <param name="entry">The entry about to be saved.</param>
    /// <param name="sameScope">The entries whose names must differ, such as the tracks of one direction; null for the whole catalogue.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the name is taken.</returns>
    public Task<bool> IsNameTakenAsync<T>(T entry, Expression<Func<T, bool>>? sameScope, CancellationToken cancellationToken)
        where T : CatalogEntry
    {
        var id = entry.Id;
        var authorId = entry.AuthorId;
        var pattern = ListRequest.ExactPattern(entry.Name);
        return context.Set<T>()
            .WhereIf(sameScope is not null, sameScope!)
            .AnyAsync(
                other => other.Id != id
                    && EF.Functions.ILike(other.Name, pattern, ListRequest.LikeEscape)
                    && (other.Status != CatalogStatus.Draft || other.AuthorId == authorId),
                cancellationToken);
    }

    /// <summary>
    /// Whether an entry of the given author may refer to the entry: an active one, or a draft of the same author.
    /// </summary>
    /// <typeparam name="T">The referenced entry type.</typeparam>
    /// <param name="id">The referenced entry.</param>
    /// <param name="authorId">The author of the referring entry.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the reference is allowed; false also when the entry does not exist.</returns>
    public Task<bool> IsUsableAsync<T>(Guid id, Guid? authorId, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        context.Set<T>().AnyAsync(
            entry => entry.Id == id
                && (entry.Status == CatalogStatus.Active || (entry.Status == CatalogStatus.Draft && entry.AuthorId == authorId)),
            cancellationToken);

    /// <summary>
    /// The matrices one of whose versions, a variant of one or the snapshot of a published one lists the level or competency. The
    /// documents are jsonb, so the check is a containment query rather than a foreign key.
    /// </summary>
    /// <param name="entry">A level or a competency.</param>
    /// <returns>The matrices, a query to compose with others.</returns>
    public IQueryable<CompetencyMatrix> MatricesUsing(CatalogEntry entry)
    {
        var (list, field) = entry is CareerLevel ? ("levels", "levelId") : ("competencies", "competencyId");
        var id = entry.Id;
        var matrixIds = context.Database.SqlQuery<Guid>($"""
            SELECT matrix_id AS "Value" FROM matrix_versions
            WHERE content -> {list} @> jsonb_build_array(jsonb_build_object({field}, {id}::text))
                OR snapshot -> {list} @> jsonb_build_array(jsonb_build_object('id', {id}::text))
                OR id IN (
                    SELECT version_id FROM matrix_version_variants
                    WHERE content -> {list} @> jsonb_build_array(jsonb_build_object({field}, {id}::text)))
            """);
        return context.Set<CompetencyMatrix>().Where(matrix => matrixIds.Contains(matrix.Id));
    }

    /// <summary>
    /// The levels outside the given ones that name one of them as their previous or a next level.
    /// </summary>
    /// <param name="levelIds">The levels about to be deleted.</param>
    /// <returns>The levels, a query to compose with others.</returns>
    public IQueryable<CareerLevel> LevelsReferencing(Guid[] levelIds)
    {
        var referencingIds = context.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM career_levels
            WHERE NOT (id = ANY({levelIds})) AND (previous_level_id = ANY({levelIds}) OR next_level_ids && {levelIds})
            """);
        return context.Set<CareerLevel>().Where(level => referencingIds.Contains(level.Id));
    }

    /// <summary>
    /// Marks a history row for removal with the entry it belongs to and journals its deletion, which the journal does not record on its
    /// own for rows that are not audited.
    /// </summary>
    /// <param name="row">The tracked row.</param>
    /// <param name="entityId">The row's key as the journal writes keys: its parts joined by commas.</param>
    public void RemoveHistoryRow(object row, string entityId)
    {
        context.Remove(row);
        var entityType = row.GetType().Name;
        audit.Stage(new AuditEntry($"{entityType}.{DeletedAction}", entityType, entityId), context);
    }

    /// <summary>
    /// Saves the change and commits. A changed competency also gets its next version row, numbered after the version the save gave it.
    /// </summary>
    /// <param name="entry">The entry that was created or changed.</param>
    /// <param name="transaction">The transaction to commit.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    public async Task SaveAsync(CatalogEntry entry, IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        var isChanged = context.Entry(entry).State is EntityState.Added or EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
        if (isChanged && entry is CareerCompetency competency)
        {
            context.Add(CompetencyVersion.Of(competency, actor.UserId, time.GetUtcNow()));
            await context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// The names of units, for showing which unit keeps a track or a matrix.
    /// </summary>
    /// <param name="unitIds">The units; nulls are skipped.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The name of each unit that exists, by identifier.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> UnitNamesAsync(IEnumerable<Guid?> unitIds, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var unitId in unitIds.OfType<Guid>().Distinct())
        {
            if (await employees.FindUnitAsync(unitId, cancellationToken) is { } unit)
            {
                names[unitId] = unit.Name;
            }
        }

        return names;
    }

    /// <summary>
    /// The names of accounts: the full name of the bound employee, or else the e-mail.
    /// </summary>
    /// <param name="userIds">The accounts; nulls are skipped.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The name of each account that exists, by identifier.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> UserNamesAsync(IEnumerable<Guid?> userIds, CancellationToken cancellationToken)
    {
        var accounts = new List<UserStatus>();
        foreach (var userId in userIds.OfType<Guid>().Distinct())
        {
            if (await users.FindAsync(userId, cancellationToken) is { } account)
            {
                accounts.Add(account);
            }
        }

        var employeeIds = accounts.Select(account => account.EmployeeId).OfType<Guid>().ToList();
        var employeesById = await employees.FindManyAsync(employeeIds, cancellationToken);
        return accounts.ToDictionary(
            account => account.Id,
            account => account.EmployeeId is { } employeeId && employeesById.TryGetValue(employeeId, out var employee) ? employee.FullName : account.Email);
    }
}
