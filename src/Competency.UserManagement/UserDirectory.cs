using System.Linq.Expressions;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.UserManagement;

/// <summary>
/// Answers <see cref="IUserDirectory"/> lookups straight from the account table.
/// </summary>
internal sealed class UserDirectory(AppDbContext context) : IUserDirectory
{
    private static readonly Expression<Func<AppUser, UserStatus>> StatusProjection = user => new UserStatus(
        user.Id, user.Email!, user.EmployeeId, user.IsBlocked, user.PasswordHash != null);

    public Task<UserStatus?> FindAsync(Guid userId, CancellationToken cancellationToken = default) => context
        .Set<AppUser>()
        .AsNoTracking()
        .Where(user => user.Id == userId)
        .Select(StatusProjection)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, UserStatus>> FindManyAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToArray();
        return await context
            .Set<AppUser>()
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(StatusProjection)
            .ToDictionaryAsync(status => status.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, UserStatus>> FindByEmployeeIdsAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToArray();
        return await context
            .Set<AppUser>()
            .AsNoTracking()
            .Where(user => user.EmployeeId != null && ids.Contains(user.EmployeeId.Value))
            .Select(StatusProjection)
            .ToDictionaryAsync(status => status.EmployeeId!.Value, cancellationToken);
    }
}
