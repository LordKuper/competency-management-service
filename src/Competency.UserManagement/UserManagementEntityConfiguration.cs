using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Competency.UserManagement;

/// <summary>
/// Contributes the <see cref="AppUser"/> table to the shared model: snake_case names, a client-generated Guid v4 key,
/// a unique case-insensitive e-mail that doubles as the internal Identity user name, one account per employee,
/// a role limited to the known values, and a foreign key to the employee that never cascades.
/// Only the Identity columns this application uses are mapped; roles, claims, logins, tokens, phone and two-factor are not.
/// </summary>
internal sealed class UserManagementEntityConfiguration : IEntityConfigurationContributor
{
    private const int RoleMaxLength = 32;
    private const string RoleCheckName = "ck_users_role";

    private static readonly string RoleCheckSql = $"role IN ({string.Join(", ", Enum.GetNames<UserRole>().Select(role => $"'{role}'"))})";

    /// <summary>
    /// The employee entity is named rather than typed because OrgStructure keeps it internal; it must be configured before this contributor runs.
    /// </summary>
    private const string EmployeeEntityName = "Competency.OrgStructure.Employee";

    public void Configure(ModelBuilder modelBuilder) => modelBuilder.Entity<AppUser>(entity =>
    {
        entity.ToTable("users", table => table.HasCheckConstraint(RoleCheckName, RoleCheckSql));

        entity.Property(e => e.Id).HasColumnName("id").HasValueGenerator<GuidValueGenerator>();
        entity.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(AppUser.EmailMaxLength).IsRequired();
        entity.Property(e => e.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(AppUser.EmailMaxLength).IsRequired();
        entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(AppUser.EmailMaxLength).IsRequired();
        entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(AppUser.EmailMaxLength).IsRequired();
        entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
        entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp");
        entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();
        entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
        entity.Property(e => e.LockoutEnabled).HasColumnName("lockout_enabled");
        entity.Property(e => e.AccessFailedCount).HasColumnName("access_failed_count");
        entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(RoleMaxLength);
        entity.Property(e => e.IsBlocked).HasColumnName("is_blocked");
        entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
        entity.Property(e => e.Version).HasColumnName("version");

        entity.Ignore(e => e.EmailConfirmed);
        entity.Ignore(e => e.PhoneNumber);
        entity.Ignore(e => e.PhoneNumberConfirmed);
        entity.Ignore(e => e.TwoFactorEnabled);

        entity.HasIndex(e => e.NormalizedUserName).IsUnique();
        entity.HasIndex(e => e.NormalizedEmail).IsUnique();
        entity.HasIndex(e => e.EmployeeId).IsUnique().HasFilter("employee_id IS NOT NULL");

        entity.HasOne(EmployeeEntityName, navigationName: null).WithMany().HasForeignKey(nameof(AppUser.EmployeeId)).OnDelete(DeleteBehavior.Restrict);
    });
}
