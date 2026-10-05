using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Competency.OrgStructure;

/// <summary>
/// Contributes the <see cref="OrgUnit"/> and <see cref="Employee"/> tables to the shared model: snake_case names,
/// foreign keys that never cascade, case-insensitive unique e-mail, and the Russian full-text and trigram indexes that serve search.
/// </summary>
internal sealed class OrgStructureEntityConfiguration : IEntityConfigurationContributor
{
    private const string TrigramExtension = "pg_trgm";

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension(TrigramExtension);

        modelBuilder.Entity<OrgUnit>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("org_units", table => table.HasCheckConstraint(
                "ck_org_units_valid_period",
                "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from"));
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(OrgUnit.NameMaxLength);
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.HeadEmployeeId).HasColumnName("head_employee_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");

            entity.HasOne<OrgUnit>().WithMany().HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(e => e.HeadEmployeeId).OnDelete(DeleteBehavior.Restrict);

            entity.HasGeneratedTsVectorColumn(e => e.SearchVector, TextSearch.FullTextConfig, e => e.Name);
            entity.Property(e => e.SearchVector).HasColumnName("search_vector");
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");
            entity.HasIndex(e => e.Name).HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("employees");
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(Employee.FullNameMaxLength);
            entity.Property(e => e.PersonnelNumber).HasColumnName("personnel_number").HasMaxLength(Employee.PersonnelNumberMaxLength);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(Employee.EmailMaxLength);
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(Employee.PositionMaxLength);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasComputedColumnSql("lower(email)", stored: true);

            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PersonnelNumber).IsUnique();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();

            entity.HasGeneratedTsVectorColumn(e => e.SearchVector, TextSearch.FullTextConfig, e => new { e.FullName, e.Position });
            entity.Property(e => e.SearchVector).HasColumnName("search_vector");
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");
            entity.HasIndex(e => e.FullName, "IX_employees_full_name_trgm").HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
            entity.HasIndex(e => e.Email, "IX_employees_email_trgm").HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
            entity.HasIndex(e => e.PersonnelNumber, "IX_employees_personnel_number_trgm").HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
        });
    }

    private static void ConfigureBase<T>(EntityTypeBuilder<T> entity)
        where T : EntityBase
    {
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
    }
}
