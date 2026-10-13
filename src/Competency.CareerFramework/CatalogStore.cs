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
    /// Those are not worked out for global administrators and HR, who read every draft a matrix version can use: the drafts of no unit
    /// anyway, and draft levels belong to draft tracks, which no matrix uses.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The user's rights.</returns>
    public async Task<CatalogViewer> ViewerAsync(CancellationToken cancellationToken)
    {
        var viewer = await CatalogViewer.LoadAsync(actor, roles, cancellationToken);
        return viewer.IsAdmin || viewer.IsHr ? viewer : viewer with { RouteEntryIds = await route.RouteEntryIdsAsync(viewer, cancellationToken) };
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
    /// Entries changed in the same save count as they will be saved, so drafts of different authors approved together, such as the levels
    /// of an approved track, cannot become active under one name.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <param name="entry">The entry about to be saved.</param>
    /// <param name="sameScope">The entries whose names must differ, such as the tracks of one direction; null for the whole catalogue.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the name is taken.</returns>
    public async Task<bool> IsNameTakenAsync<T>(T entry, Expression<Func<T, bool>>? sameScope, CancellationToken cancellationToken)
        where T : CatalogEntry
    {
        var id = entry.Id;
        var authorId = entry.AuthorId;
        var pending = context.ChangeTracker.Entries<T>()
            .Where(other => other.State is EntityState.Added or EntityState.Modified)
            .Select(other => other.Entity)
            .Where(other => other.Id != id
                && string.Equals(other.Name, entry.Name, StringComparison.OrdinalIgnoreCase)
                && (other.Status != CatalogStatus.Draft || other.AuthorId == authorId))
            .ToList();
        if (pending.Count > 0 && (sameScope is null || pending.Any(sameScope.Compile())))
        {
            return true;
        }

        var pattern = ListRequest.ExactPattern(entry.Name);
        return await context.Set<T>()
            .WhereIf(sameScope is not null, sameScope!)
            .AnyAsync(
                other => other.Id != id
                    && EF.Functions.ILike(other.Name, pattern, ListRequest.LikeEscape)
                    && (other.Status != CatalogStatus.Draft || other.AuthorId == authorId),
                cancellationToken);
    }

    /// <summary>
    /// Whether an entry may refer to the entry: an active one, or a draft of the given author.
    /// </summary>
    /// <typeparam name="T">The referenced entry type.</typeparam>
    /// <param name="id">The referenced entry.</param>
    /// <param name="draftAuthorId">The author whose drafts may be referred to; null when only an active entry may, as for a referring entry that is active.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the reference is allowed; false also when the entry does not exist.</returns>
    public Task<bool> IsUsableAsync<T>(Guid id, Guid? draftAuthorId, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        context.Set<T>().AnyAsync(
            entry => entry.Id == id
                && (entry.Status == CatalogStatus.Active
                    || (draftAuthorId != null && entry.Status == CatalogStatus.Draft && entry.AuthorId == draftAuthorId)),
            cancellationToken);

    /// <summary>
    /// Whether the entry is a draft, to tell a draft reference from a missing or inactive one.
    /// </summary>
    /// <typeparam name="T">The entry type.</typeparam>
    /// <param name="id">The entry.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the entry exists and is a draft.</returns>
    public Task<bool> IsDraftAsync<T>(Guid id, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        context.Set<T>().AnyAsync(entry => entry.Id == id && entry.Status == CatalogStatus.Draft, cancellationToken);

    /// <summary>
    /// Works out whether the track may be active, the one place that decides it. Positions come from the organization snapshot, which
    /// the tree lock of a writer keeps current until it commits.
    /// </summary>
    /// <param name="trackId">The track.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The track's readiness.</returns>
    public async Task<TrackReadiness> ReadinessAsync(Guid trackId, CancellationToken cancellationToken)
    {
        var levels = await context.Set<CareerLevel>()
            .AsNoTracking()
            .Where(level => level.TrackId == trackId && level.Status != CatalogStatus.Inactive)
            .OrderBy(level => level.Position)
            .ToListAsync(cancellationToken);
        var links = (await context.Set<CareerLevelPosition>()
                .AsNoTracking()
                .Where(link => link.TrackId == trackId)
                .ToListAsync(cancellationToken))
            .ToLookup(link => link.LevelId, link => link.PositionId);
        return TrackReadiness.Of(levels, links, (await employees.FindOrgSnapshotAsync(cancellationToken)).Positions);
    }

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
    /// The matrices that list a competency of the category, as <see cref="MatricesUsing"/> finds the matrices of one competency; a
    /// published snapshot counts by the category the competency had then.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The matrices, a query to compose with others.</returns>
    public IQueryable<CompetencyMatrix> MatricesUsingCategory(CompetencyCategory category)
    {
        var id = category.Id;
        var matrixIds = context.Database.SqlQuery<Guid>($"""
            SELECT v.matrix_id AS "Value" FROM matrix_versions v
            WHERE v.snapshot -> 'competencies' @> jsonb_build_array(jsonb_build_object('categoryId', {id}::text))
                OR EXISTS (
                    SELECT 1 FROM competencies c
                    WHERE c.category_id = {id}
                        AND (v.content -> 'competencies' @> jsonb_build_array(jsonb_build_object('competencyId', c.id::text))
                            OR EXISTS (
                                SELECT 1 FROM matrix_version_variants variant
                                WHERE variant.version_id = v.id
                                    AND variant.content -> 'competencies' @> jsonb_build_array(jsonb_build_object('competencyId', c.id::text)))))
            """);
        return context.Set<CompetencyMatrix>().Where(matrix => matrixIds.Contains(matrix.Id));
    }

    /// <summary>
    /// The active entries nothing uses, the one definition every list of them shares: competencies no matrix version, variant or
    /// published snapshot lists; scales other than the default that no competency and no version is on; categories no competency is of
    /// and no published snapshot names; directions without tracks.
    /// </summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The competencies, then the scales, categories and directions, each in name order.</returns>
    public async Task<List<CatalogEntry>> UnusedEntriesAsync(CancellationToken cancellationToken)
    {
        var usedCompetencyIds = context.Database.SqlQuery<Guid>($"""
            SELECT (item ->> 'competencyId')::uuid AS "Value" FROM matrix_versions, jsonb_array_elements(content -> 'competencies') AS item
            UNION SELECT (item ->> 'competencyId')::uuid FROM matrix_version_variants, jsonb_array_elements(content -> 'competencies') AS item
            UNION SELECT (item ->> 'id')::uuid FROM matrix_versions, jsonb_array_elements(snapshot -> 'competencies') AS item
            """);
        var snapshotCategoryIds = context.Database.SqlQuery<Guid>($"""
            SELECT (item ->> 'categoryId')::uuid AS "Value" FROM matrix_versions, jsonb_array_elements(snapshot -> 'competencies') AS item
            """);
        var competencies = context.Set<CareerCompetency>();
        var versions = context.Set<MatrixVersion>();
        var tracks = context.Set<CareerTrack>();
        return
        [
            .. await ActiveByName(competencies.Where(competency => !usedCompetencyIds.Contains(competency.Id)), cancellationToken),
            .. await ActiveByName(
                context.Set<CompetencyScale>().Where(scale => !scale.IsDefault
                    && !competencies.Any(competency => competency.ScaleId == scale.Id)
                    && !versions.Any(version => version.ScaleId == scale.Id)),
                cancellationToken),
            .. await ActiveByName(
                context.Set<CompetencyCategory>().Where(category => !competencies.Any(competency => competency.CategoryId == category.Id)
                    && !snapshotCategoryIds.Contains(category.Id)),
                cancellationToken),
            .. await ActiveByName(context.Set<CareerDirection>().Where(direction => !tracks.Any(track => track.DirectionId == direction.Id)), cancellationToken),
        ];
    }

    /// <summary>
    /// Numbers the levels of one track from 1 in the given order. The levels that move are first set aside under negative numbers, so the
    /// unique number of a level within its track holds after every single update. Each moved level is then saved on its own: EF orders the
    /// updates of one save by the unique index it knows, from the numbers it read, and refuses two levels that trade places as a cycle,
    /// although the database no longer holds those numbers. A round trip per moved level is affordable, because a track holds a handful.
    /// </summary>
    /// <param name="levels">All levels of the track, tracked, in their new order.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>A task that completes when every moved level is saved under its new number, inside the caller's transaction.</returns>
    public async Task MoveLevelsAsync(IReadOnlyList<CareerLevel> levels, CancellationToken cancellationToken)
    {
        var moving = levels.Select((level, index) => (Level: level, Position: index + 1)).Where(move => move.Level.Position != move.Position).ToList();
        var movingIds = moving.Select(move => move.Level.Id).ToArray();
        await context.Database.ExecuteSqlAsync(
            $"UPDATE career_levels SET position = -position WHERE id = ANY({movingIds})",
            cancellationToken);

        foreach (var (level, position) in moving)
        {
            level.Position = position;
            await context.SaveChangesAsync(cancellationToken);
        }
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

    private static Task<List<T>> ActiveByName<T>(IQueryable<T> entries, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        entries.AsNoTracking()
            .Where(entry => entry.Status == CatalogStatus.Active)
            .OrderBy(entry => entry.Name)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The names of units, for showing which unit keeps a track or a matrix.
    /// </summary>
    /// <param name="unitIds">The units; nulls are skipped.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The name of each unit that exists, by identifier.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> UnitNamesAsync(IEnumerable<Guid?> unitIds, CancellationToken cancellationToken)
    {
        var units = await employees.FindUnitsAsync([.. unitIds.OfType<Guid>().Distinct()], cancellationToken);
        return units.ToDictionary(unit => unit.Key, unit => unit.Value.Name);
    }

    /// <summary>
    /// The names of categories, directions and scales, for showing which ones a competency refers to; one query for all of them.
    /// </summary>
    /// <param name="ids">The entries; nulls are skipped.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The name of each entry that exists, by identifier.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> EntryNamesAsync(IEnumerable<Guid?> ids, CancellationToken cancellationToken)
    {
        var wanted = ids.OfType<Guid>().Distinct().ToList();
        return await context.Set<CompetencyCategory>().Where(entry => wanted.Contains(entry.Id)).Select(entry => new { entry.Id, entry.Name })
            .Concat(context.Set<CareerDirection>().Where(entry => wanted.Contains(entry.Id)).Select(entry => new { entry.Id, entry.Name }))
            .Concat(context.Set<CompetencyScale>().Where(entry => wanted.Contains(entry.Id)).Select(entry => new { entry.Id, entry.Name }))
            .ToDictionaryAsync(entry => entry.Id, entry => entry.Name, cancellationToken);
    }

    /// <summary>
    /// The names of accounts: the full name of the bound employee, or else the e-mail.
    /// </summary>
    /// <param name="userIds">The accounts; nulls are skipped.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The name of each account that exists, by identifier.</returns>
    public async Task<IReadOnlyDictionary<Guid, string>> UserNamesAsync(IEnumerable<Guid?> userIds, CancellationToken cancellationToken)
    {
        var accounts = (await users.FindManyAsync([.. userIds.OfType<Guid>().Distinct()], cancellationToken)).Values;
        var employeeIds = accounts.Select(account => account.EmployeeId).OfType<Guid>().ToList();
        var employeesById = await employees.FindManyAsync(employeeIds, cancellationToken);
        return accounts.ToDictionary(
            account => account.Id,
            account => account.EmployeeId is { } employeeId && employeesById.TryGetValue(employeeId, out var employee) ? employee.FullName : account.Email);
    }
}
