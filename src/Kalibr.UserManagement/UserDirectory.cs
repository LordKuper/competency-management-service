using System.Linq.Expressions;
using Competency.OrgStructure;
using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Competency.UserManagement;

/// <summary>
/// Answers <see cref="IUserDirectory"/> lookups straight from the account table, and creates the accounts other modules invite employees with.
/// </summary>
internal sealed class UserDirectory(
    AppDbContext context,
    UserManager<AppUser> users,
    IEmployeeDirectory employees,
    AccountMail mail,
    IOptions<AccountLinkOptions> links,
    TimeProvider time) : IUserDirectory
{
    private const string EmailField = "email";
    private const string EmailTakenMessage = "E-mail: уже используется другой учётной записью.";

    private static readonly Guid HrRoleId = UserRoleIds.Of(UserRole.Hr);

    private static readonly Guid GlobalAdminRoleId = UserRoleIds.Of(UserRole.GlobalAdmin);

    private static readonly Expression<Func<AppUser, UserStatus>> StatusProjection = user => new UserStatus(
        user.Id,
        user.Email!,
        user.EmployeeId,
        user.IsBlocked,
        user.PasswordHash != null,
        user.UserRoles.Any(role => role.RoleId == HrRoleId),
        user.UserRoles.Any(role => role.RoleId == GlobalAdminRoleId),
        user.Version);

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

    public Task<bool> HasHrCandidateAsync(CancellationToken cancellationToken = default) => context
        .Set<AppUser>()
        .AnyAsync(
            user => user.UserRoles.Any(role => role.RoleId == HrRoleId) && !user.IsBlocked && user.PasswordHash != null && user.EmployeeId != null,
            cancellationToken);

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

    public async Task<IReadOnlyList<Guid>> FindActiveAdministratorIdsAsync(CancellationToken cancellationToken = default) =>
        await context.Set<AppUser>().Where(ActiveAdministrators.IsActive).Select(user => user.Id).ToListAsync(cancellationToken);

    public async Task<string?> EmailProblemAsync(string email, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string[]> errors = [];
        CredentialChecks.CheckEmail(errors, EmailField, email);
        if (errors.Count > 0)
        {
            return errors[EmailField][0];
        }

        var normalized = users.NormalizeEmail(email.Trim());
        return await context.Set<AppUser>().AnyAsync(user => user.NormalizedEmail == normalized, cancellationToken) ? EmailTakenMessage : null;
    }

    public async Task<EmployeeInvitation> InviteEmployeeAsync(Guid employeeId, string email, CancellationToken cancellationToken = default)
    {
        var address = email.Trim();
        var user = new AppUser { EmployeeId = employeeId };
        await using var transaction = await employees.BeginExclusiveAsync(cancellationToken);
        if (await employees.ProblemAsync(employeeId, cancellationToken) is not null
            || await context.IsBoundElsewhereAsync(employeeId, Guid.Empty, cancellationToken)
            || await EmailProblemAsync(address, cancellationToken) is not null)
        {
            return EmployeeInvitation.Refused;
        }

        user.SetEmail(address);
        var (result, mailSent) = await NewAccount.InviteAsync(user, transaction, null, users, context, mail, links.Value, time, cancellationToken);
        return !result.Succeeded ? EmployeeInvitation.Refused : mailSent ? EmployeeInvitation.Sent : EmployeeInvitation.NotSent;
    }
}
