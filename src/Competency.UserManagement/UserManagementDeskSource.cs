using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.UserManagement;

/// <summary>
/// What the accounts expect of a global administrator, who alone manages them: invitations whose link no longer works or soon will not,
/// accounts bound to no employee that are expected to have one, and unit heads without an account that can act, with the prerequisite that an
/// account with the HR role can act. Other users get nothing from it.
/// </summary>
internal sealed class UserManagementDeskSource(ICurrentActor actor, AppDbContext context, IEmployeeDirectory employees, TimeProvider time) : IDeskSource
{
    /// <summary>
    /// How close to its expiry an invitation link counts as soon to stop working.
    /// </summary>
    private static readonly TimeSpan ExpiringSoonWithin = TimeSpan.FromDays(2);

    /// <inheritdoc />
    public async Task<DeskReport> FindAsync(CancellationToken cancellationToken)
    {
        if (!actor.IsGlobalAdmin())
        {
            return new DeskReport([], []);
        }

        var snapshot = await employees.FindOrgSnapshotAsync(cancellationToken);
        var headedUnits = snapshot.Units.Values.Where(unit => unit.IsActive && unit.HeadEmployeeId is not null).ToList();
        var headIds = headedUnits.Select(unit => unit.HeadEmployeeId!.Value).ToHashSet();
        var accounts = await ReadAccountsAsync(headIds, cancellationToken);
        var people = await employees.FindManyAsync(
            [.. headIds.Concat(accounts.Select(account => account.Status.EmployeeId).OfType<Guid>()).Distinct()],
            cancellationToken);
        var accountOfHead = accounts
            .Where(account => account.Status.EmployeeId is { } employeeId && headIds.Contains(employeeId))
            .ToDictionary(account => account.Status.EmployeeId!.Value);
        var now = time.GetUtcNow();
        return new DeskReport(
            [
                .. headedUnits.Select(unit => HeadFinding(snapshot, unit, accountOfHead.GetValueOrDefault(unit.HeadEmployeeId!.Value), people)).OfType<DeskFinding>(),
                .. accounts.Select(account => InvitationFinding(account, people, now)).OfType<DeskFinding>(),
                .. accounts.Where(IsExpectedToHaveEmployee)
                    .Select(account => new DeskFinding(
                        DeskKind.AccountWithoutEmployee,
                        AccountObject(account, people),
                        IsAction: true,
                        Details: account.Status.IsHr ? new DeskDetails(HasHrRole: true) : null)),
            ],
            accounts.Any(account => account.Status is { IsHr: true, IsActive: true }) ? [DeskPrerequisite.HrAccount] : []);
    }

    /// <summary>
    /// Reads, in one query, the invited accounts that are not blocked, the accounts bound to no employee that are expected to have one,
    /// the accounts with the HR role that can act and the accounts of the given heads.
    /// </summary>
    private async Task<List<AccountRow>> ReadAccountsAsync(HashSet<Guid> headIds, CancellationToken cancellationToken)
    {
        var heads = headIds.ToArray();
        var globalAdminRoleId = UserRoleIds.Of(UserRole.GlobalAdmin);
        var hrRoleId = UserRoleIds.Of(UserRole.Hr);
        return await context
            .Set<AppUser>()
            .AsNoTracking()
            .Select(user => new
            {
                User = user,
                IsGlobalAdmin = user.UserRoles.Any(role => role.RoleId == globalAdminRoleId),
                IsHr = user.UserRoles.Any(role => role.RoleId == hrRoleId),
            })
            .Where(row => (row.User.PasswordHash == null && !row.User.IsBlocked)
                || ((!row.IsGlobalAdmin || row.IsHr) && row.User.EmployeeId == null && !row.User.IsBlocked)
                || (row.IsHr && !row.User.IsBlocked && row.User.PasswordHash != null)
                || (row.User.EmployeeId != null && heads.Contains(row.User.EmployeeId.Value)))
            .Select(row => new AccountRow(
                new UserStatus(row.User.Id, row.User.Email!, row.User.EmployeeId, row.User.IsBlocked, row.User.PasswordHash != null, row.IsHr, row.IsGlobalAdmin, row.User.Version),
                row.IsGlobalAdmin,
                row.User.LinkTokenHash != null && row.User.LinkSecurityStamp == row.User.SecurityStamp,
                row.User.LinkExpiresAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The finding about a unit whose head has no account that can act, by the same rule as anywhere else an account must act;
    /// an invited account is its subject, so the finding about the invitation absorbs it.
    /// </summary>
    private static DeskFinding? HeadFinding(OrgSnapshot snapshot, OrgUnitStatus unit, AccountRow? account, IReadOnlyDictionary<Guid, EmployeeStatus> people)
    {
        DeskKind? kind = account switch
        {
            null => DeskKind.HeadWithoutAccount,
            { Status.IsActive: true } => null,
            { Status.IsBlocked: true } => DeskKind.HeadAccountBlocked,
            _ => DeskKind.HeadAccountInvited,
        };
        if (kind is null)
        {
            return null;
        }

        var head = people.GetValueOrDefault(unit.HeadEmployeeId!.Value);
        return new DeskFinding(
            kind.Value,
            snapshot.UnitDeskObject(unit),
            IsAction: true,
            Subject: account is null ? null : AccountObject(account, people),
            Details: new DeskDetails(Person: head is null ? null : new DeskObject(DeskObjectType.Employee, head.Id, head.FullName)));
    }

    /// <summary>
    /// The finding about an invited account that is not blocked and whose link no longer works or soon will not, with the invitation to send again.
    /// A voided link has no expiry worth showing, since it stopped working before it.
    /// </summary>
    private static DeskFinding? InvitationFinding(AccountRow account, IReadOnlyDictionary<Guid, EmployeeStatus> people, DateTimeOffset now)
    {
        if (account.Status.IsRegistered || account.Status.IsBlocked)
        {
            return null;
        }

        DeskLinkState? state = account switch
        {
            { IsLinkCurrent: false } or { LinkExpiresAt: null } => DeskLinkState.Revoked,
            _ when account.LinkExpiresAt <= now => DeskLinkState.Expired,
            _ when account.LinkExpiresAt - now < ExpiringSoonWithin => DeskLinkState.ExpiringSoon,
            _ => null,
        };
        if (state is null)
        {
            return null;
        }

        return new DeskFinding(
            DeskKind.InvitationNotAccepted,
            AccountObject(account, people),
            IsAction: true,
            OccurredAt: state == DeskLinkState.Revoked ? null : account.LinkExpiresAt,
            Details: new DeskDetails(LinkState: state),
            Invitation: new DeskInvitation(account.Status.Id, account.Status.Version));
    }

    /// <summary>
    /// Whether an account that is not blocked lacks the employee it is expected to have: any account but one with the global administrator
    /// role alone, which is meant to stay unbound; an HR account needs one to be assigned as a responsible HR.
    /// </summary>
    private static bool IsExpectedToHaveEmployee(AccountRow account) =>
        account.Status is { EmployeeId: null, IsBlocked: false } && (!account.IsGlobalAdmin || account.Status.IsHr);

    private static DeskObject AccountObject(AccountRow account, IReadOnlyDictionary<Guid, EmployeeStatus> people) => new(
        DeskObjectType.UserAccount,
        account.Status.Id,
        account.Status.Email,
        account.Status.EmployeeId is { } employeeId && people.TryGetValue(employeeId, out var employee) ? [employee.FullName] : null);

    /// <summary>
    /// The facts of an account the desk needs, beyond those other modules rely on.
    /// </summary>
    /// <param name="Status">The facts other modules rely on, with the one rule of an account that can act.</param>
    /// <param name="IsGlobalAdmin">Whether the account has the global administrator role.</param>
    /// <param name="IsLinkCurrent">Whether the account has a link issued under its current security stamp.</param>
    /// <param name="LinkExpiresAt">When the account's link expires; null when it has none.</param>
    private sealed record AccountRow(UserStatus Status, bool IsGlobalAdmin, bool IsLinkCurrent, DateTimeOffset? LinkExpiresAt);
}
