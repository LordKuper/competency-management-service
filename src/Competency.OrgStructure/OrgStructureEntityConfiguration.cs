using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Competency.OrgStructure;

/// <summary>
/// Contributes the <see cref="OrgUnit"/> and <see cref="Employee"/> tables to the shared model: snake_case names,
/// foreign keys that never cascade, and the Russian full-text and trigram indexes that serve search.
/// It also maps the keyless <see cref="EmployeeAccount"/> query over the accounts table, which has no table of its own.
/// </summary>
internal sealed class OrgStructureEntityConfiguration : IEntityConfigurationContributor
{
    private const string TrigramExtension = "pg_trgm";
    private const string EmployeeAccountsSql = "SELECT employee_id, email FROM users WHERE employee_id IS NOT NULL";

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension(TrigramExtension);

        modelBuilder.Entity<OrgUnit>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("org_units");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(OrgUnit.NameMaxLength);
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.HeadEmployeeId).HasColumnName("head_employee_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

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
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(Employee.PositionMaxLength);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");

            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);

            entity.HasGeneratedTsVectorColumn(e => e.SearchVector, TextSearch.FullTextConfig, e => new { e.FullName, e.Position });
            entity.Property(e => e.SearchVector).HasColumnName("search_vector");
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");
            entity.HasIndex(e => e.FullName, "IX_employees_full_name_trgm").HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
        });

        modelBuilder.Entity<EmployeeAccount>(entity =>
        {
            entity.HasNoKey();
            entity.ToSqlQuery(EmployeeAccountsSql);
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Email).HasColumnName("email");
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
