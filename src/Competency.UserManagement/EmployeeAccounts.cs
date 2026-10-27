using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.UserManagement;

/// <summary>
/// Blocks and detaches the account of an employee who leaves, on the request's database context, so the change is saved
/// and journaled together with the employee's own, in one transaction, and tells whether the account is the last active administrator.
/// </summary>
internal sealed class EmployeeAccounts(AppDbContext context) : IEmployeeAccounts
{
    public async Task BlockAsync(Guid employeeId, bool unbind, CancellationToken cancellationToken = default)
    {
        var user = await context.Set<AppUser>().FirstOrDefaultAsync(candidate => candidate.EmployeeId == employeeId, cancellationToken);
        if (user is null)
        {
            return;
        }

        if (!user.IsBlocked)
        {
            user.IsBlocked = true;
            user.EndSessions();
        }

        if (unbind)
        {
            user.EmployeeId = null;
        }
    }

    public Task LockAdministratorsAsync(CancellationToken cancellationToken = default) => context.LockAsync(cancellationToken);

    public async Task<bool> IsLastActiveAdministratorAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var administratorId = await context.Set<AppUser>()
            .Where(candidate => candidate.EmployeeId == employeeId)
            .Where(ActiveAdministrators.IsActive)
            .Select(candidate => (Guid?)candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return administratorId is { } id && !await context.HasOtherAsync(id, cancellationToken);
    }

    public async Task<bool> LeavesNoActiveAdministratorAsync(IReadOnlyCollection<Guid> employeeIds, CancellationToken cancellationToken = default)
    {
        var leaving = employeeIds.ToArray();
        var administrators = context.Set<AppUser>().Where(ActiveAdministrators.IsActive);
        return await administrators.AnyAsync(user => user.EmployeeId != null && leaving.Contains(user.EmployeeId.Value), cancellationToken)
            && !await administrators.AnyAsync(user => user.EmployeeId == null || !leaving.Contains(user.EmployeeId.Value), cancellationToken);
    }
}
