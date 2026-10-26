using Competency.Platform;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Competency.UserManagement;

/// <summary>
/// Contributes the <see cref="AppUser"/> table and the Identity role tables to the shared model: snake_case names, a client-generated Guid v4 key,
/// a unique case-insensitive e-mail that doubles as the internal Identity user name, one account per employee, a link looked up by its token hash,
/// a role row per <see cref="UserRole"/> member, and foreign keys that never cascade.
/// Only the Identity columns and tables this application uses are mapped; Identity's claims, logins, tokens, passkeys, phone and two-factor are not.
/// </summary>
internal sealed class UserManagementEntityConfiguration : IEntityConfigurationContributor
{
    /// <summary>
    /// The employee entity is named rather than typed because OrgStructure keeps it internal; it must be configured before this contributor runs.
    /// </summary>
    private const string EmployeeEntityName = "Competency.OrgStructure.Employee";

    private const int RoleNameMaxLength = 256;

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");

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
            entity.Property(e => e.IsBlocked).HasColumnName("is_blocked");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.LinkTokenHash).HasColumnName("link_token_hash");
            entity.Property(e => e.LinkExpiresAt).HasColumnName("link_expires_at");
            entity.Property(e => e.LinkIssuedAt).HasColumnName("link_issued_at");
            entity.Property(e => e.LinkSecurityStamp).HasColumnName("link_security_stamp");

            entity.Ignore(e => e.EmailConfirmed);
            entity.Ignore(e => e.PhoneNumber);
            entity.Ignore(e => e.PhoneNumberConfirmed);
            entity.Ignore(e => e.TwoFactorEnabled);

            entity.HasIndex(e => e.NormalizedUserName).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
            entity.HasIndex(e => e.EmployeeId).IsUnique().HasFilter("employee_id IS NOT NULL");
            entity.HasIndex(e => e.LinkTokenHash).IsUnique().HasFilter("link_token_hash IS NOT NULL");

            entity.HasOne(EmployeeEntityName, navigationName: null).WithMany().HasForeignKey(nameof(AppUser.EmployeeId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.UserRoles).WithOne().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("roles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(RoleNameMaxLength);
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name").HasMaxLength(RoleNameMaxLength);
            entity.Property(e => e.ConcurrencyStamp).HasColumnName("concurrency_stamp").IsConcurrencyToken();

            entity.HasIndex(e => e.NormalizedName).IsUnique();

            entity.HasData(Enum.GetValues<UserRole>().Select(RoleRow));
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne<IdentityRole<Guid>>().WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// The row of a role; its concurrency stamp is fixed, because Identity's random default would make every new migration update the rows.
    /// </summary>
    private static IdentityRole<Guid> RoleRow(UserRole role) => new()
    {
        Id = UserRoleIds.Of(role),
        Name = role.ToString(),
        NormalizedName = role.ToString().ToUpperInvariant(),
        ConcurrencyStamp = UserRoleIds.Of(role).ToString(),
    };
}
