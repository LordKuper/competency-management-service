using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.CareerFramework;

/// <summary>
/// Contributes the <see cref="OrgUnitHr"/> table to the shared model: one assignment per unit, keyed by the unit,
/// with foreign keys to the unit and the account that never cascade.
/// </summary>
internal sealed class CareerFrameworkEntityConfiguration : IEntityConfigurationContributor
{
    /// <summary>
    /// The unit and account entities are named rather than typed because their modules keep them internal; both must be configured before this contributor runs.
    /// </summary>
    private const string OrgUnitEntityName = "Competency.OrgStructure.OrgUnit";

    private const string UserEntityName = "Competency.UserManagement.AppUser";

    public void Configure(ModelBuilder modelBuilder) => modelBuilder.Entity<OrgUnitHr>(entity =>
    {
        entity.ToTable("org_unit_hr");
        entity.HasKey(e => e.OrgUnitId);
        entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
        entity.Property(e => e.UserId).HasColumnName("user_id");

        entity.HasOne(OrgUnitEntityName, navigationName: null).WithOne().HasForeignKey(typeof(OrgUnitHr), nameof(OrgUnitHr.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(OrgUnitHr.UserId)).OnDelete(DeleteBehavior.Restrict);
    });
}
