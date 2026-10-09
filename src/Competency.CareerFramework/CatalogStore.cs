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
    ICurrentActor actor,
    TimeProvider time)
{
    private const long LockKey = 5_003_001;

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

    public Task<CatalogViewer> ViewerAsync(CancellationToken cancellationToken) => CatalogViewer.LoadAsync(actor, roles, cancellationToken);

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
    /// not drafts and with the drafts of its own unit, so a draft another unit keeps to itself never blocks a name, and is checked again when approved.
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
        var ownerOrgUnitId = entry.OwnerOrgUnitId;
        var pattern = ListRequest.ExactPattern(entry.Name);
        return context.Set<T>()
            .WhereIf(sameScope is not null, sameScope!)
            .AnyAsync(
                other => other.Id != id
                    && EF.Functions.ILike(other.Name, pattern, ListRequest.LikeEscape)
                    && (other.Status != CatalogStatus.Draft || other.OwnerOrgUnitId == ownerOrgUnitId),
                cancellationToken);
    }

    /// <summary>
    /// Whether an entry of the given unit may refer to the entry: an active one, or a draft of the same unit.
    /// </summary>
    /// <typeparam name="T">The referenced entry type.</typeparam>
    /// <param name="id">The referenced entry.</param>
    /// <param name="ownerOrgUnitId">The unit of the referring entry; null for a shared one.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>True when the reference is allowed; false also when the entry does not exist.</returns>
    public Task<bool> IsUsableAsync<T>(Guid id, Guid? ownerOrgUnitId, CancellationToken cancellationToken)
        where T : CatalogEntry =>
        context.Set<T>().AnyAsync(
            entry => entry.Id == id
                && (entry.Status == CatalogStatus.Active || (entry.Status == CatalogStatus.Draft && entry.OwnerOrgUnitId == ownerOrgUnitId)),
            cancellationToken);

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
    /// The names of units, for showing who keeps an entry.
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
