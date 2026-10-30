using Kalibr.OrgStructure;
using Kalibr.Platform;
using Kalibr.UserManagement;

namespace Kalibr.Promotion;

/// <summary>
/// Works out who the signed-in user is to applications, from the account, the commissions of the units and the wave. A caller that acts on the
/// answer holds the promotion locks first. Scoped to a request, because it shares the request's database context.
/// </summary>
internal sealed class ApplicationAccess(CommissionReader commissions, IEmployeeDirectory employees, IUserDirectory users, ICurrentActor actor)
{
    /// <summary>
    /// Finds the employee the signed-in account is bound to.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The employee; null when the account is bound to none.</returns>
    public async Task<Guid?> EmployeeIdAsync(CancellationToken cancellationToken) =>
        actor.UserId is { } userId ? (await users.FindAsync(userId, cancellationToken))?.EmployeeId : null;

    /// <summary>
    /// Finds the employee the signed-in account is bound to, if they can file an application: they work and their account can act.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The employee; null when there is none or they cannot.</returns>
    public async Task<EmployeeStatus?> ActingCandidateAsync(CancellationToken cancellationToken)
    {
        if (actor.UserId is not { } userId || await users.FindAsync(userId, cancellationToken) is not { IsActive: true, EmployeeId: { } employeeId })
        {
            return null;
        }

        return await employees.FindAsync(employeeId, cancellationToken) is { IsActive: true } person ? person : null;
    }

    /// <summary>
    /// Works out what the signed-in user is to a wave and to the units of its targets.
    /// </summary>
    /// <param name="wave">The wave.</param>
    /// <param name="unitIds">The units.</param>
    /// <param name="cancellationToken">Cancels the lookups.</param>
    /// <returns>The viewer.</returns>
    public async Task<ApplicationViewer> ViewerAsync(PromotionWave wave, IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken)
    {
        var employeeId = await EmployeeIdAsync(cancellationToken);
        var waveRoles = (actor.IsGlobalAdmin() ? ApplicationRole.Administrator : ApplicationRole.None)
            | (actor.UserId is { } userId && wave.AuthorId == userId ? ApplicationRole.Author : ApplicationRole.None);
        var rosters = await commissions.ReadAsync(unitIds, cancellationToken);
        return new ApplicationViewer(employeeId, waveRoles, rosters.ToDictionary(roster => roster.Key, roster => RolesIn(roster.Value, employeeId)));
    }

    private ApplicationRole RolesIn(CommissionRoster roster, Guid? employeeId)
    {
        var roles = actor.UserId is not null && roster.ResponsibleHrUserId == actor.UserId ? ApplicationRole.ResponsibleHr : ApplicationRole.None;
        return employeeId is { } id && roster.Members.FirstOrDefault(member => member.EmployeeId == id && member.IsActing) is { } member
            ? roles | ApplicationRole.Member
                | (member.IsHead ? ApplicationRole.Head : ApplicationRole.None)
                | (roster.ChairEmployeeId == id ? ApplicationRole.Chair : ApplicationRole.None)
            : roles;
    }
}
