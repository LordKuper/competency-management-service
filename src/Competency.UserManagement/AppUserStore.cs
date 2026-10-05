using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Competency.UserManagement;

/// <summary>
/// The Identity user store, saving only what changed on the tracked user instead of marking every property modified,
/// so the audit journal records a user update only when an audited property differs.
/// Users must be loaded through the same context, which Identity's own managers always do.
/// </summary>
internal sealed class AppUserStore(AppDbContext context, IdentityErrorDescriber describer)
    : UserOnlyStore<AppUser, AppDbContext, Guid>(context, describer)
{
    public override async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        try
        {
            await SaveChanges(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }

        return IdentityResult.Success;
    }
}
