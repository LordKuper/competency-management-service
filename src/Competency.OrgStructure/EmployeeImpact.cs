using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// The changes elsewhere that an employee stopping work or being deleted causes: the units they head lose their head,
/// and the account bound to them is blocked. It is computed once and both previewed and applied from the same value,
/// so the warning an administrator confirms is exactly what then happens.
/// </summary>
internal sealed record EmployeeImpact(IReadOnlyList<OrgUnit> HeadOfUnits, EmployeeImpactAccount? Account)
{
    /// <summary>
    /// Finds what the employee's departure touches. The units are tracked, so applying the impact changes them.
    /// </summary>
    /// <param name="context">The context to query through.</param>
    /// <param name="employeeId">The employee who leaves.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The units headed by the employee, in name order, and the bound account if there is one.</returns>
    public static async Task<EmployeeImpact> OfAsync(AppDbContext context, Guid employeeId, CancellationToken cancellationToken)
    {
        var units = await context.Set<OrgUnit>()
            .Where(unit => unit.HeadEmployeeId == employeeId)
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .ToListAsync(cancellationToken);
        var account = await context.Set<EmployeeAccount>()
            .Where(candidate => candidate.EmployeeId == employeeId)
            .Select(candidate => new EmployeeImpactAccount(candidate.Email, candidate.IsBlocked))
            .FirstOrDefaultAsync(cancellationToken);
        return new EmployeeImpact(units, account);
    }

    /// <summary>
    /// The impact as the API reports it.
    /// </summary>
    /// <returns>The units and the account, without any tracked state.</returns>
    public EmployeeImpactResponse ToResponse() => new([.. HeadOfUnits.Select(OrgUnitResponse.From)], Account);

    /// <summary>
    /// Takes the employee off every unit they head and blocks their account; the caller saves the changes in its transaction.
    /// </summary>
    /// <param name="employeeId">The employee who leaves, the one the impact was computed for.</param>
    /// <param name="accounts">Blocks the account, which another module owns.</param>
    /// <param name="unbindAccount">Whether the account is also detached from the employee, as deleting the employee requires.</param>
    /// <param name="cancellationToken">Cancels the account lookup.</param>
    /// <returns>A task that completes when every change is tracked.</returns>
    public async Task ApplyAsync(Guid employeeId, IEmployeeAccounts accounts, bool unbindAccount, CancellationToken cancellationToken)
    {
        foreach (var unit in HeadOfUnits)
        {
            unit.HeadEmployeeId = null;
        }

        if (Account is not null)
        {
            await accounts.BlockAsync(employeeId, unbindAccount, cancellationToken);
        }
    }
}

/// <summary>
/// The account an employee's departure touches, as the API reports it.
/// </summary>
/// <param name="Email">The e-mail of the account.</param>
/// <param name="IsBlocked">Whether the account is blocked already, so that blocking changes nothing.</param>
internal sealed record EmployeeImpactAccount(string Email, bool IsBlocked);

/// <summary>
/// What stopping work or deleting an employee changes elsewhere, for the administrator to confirm.
/// </summary>
/// <param name="HeadOfUnits">The units that lose their head.</param>
/// <param name="Account">The bound account, which is blocked and, when the employee is deleted, detached; null when there is none.</param>
internal sealed record EmployeeImpactResponse(IReadOnlyList<OrgUnitResponse> HeadOfUnits, EmployeeImpactAccount? Account);
