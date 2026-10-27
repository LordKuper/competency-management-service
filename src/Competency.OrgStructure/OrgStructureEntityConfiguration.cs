using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Competency.OrgStructure;

/// <summary>
/// Contributes the <see cref="OrgUnit"/>, <see cref="Employee"/>, <see cref="Position"/>, <see cref="OrgUnitPosition"/>, <see cref="LegalEntity"/>,
/// <see cref="EmployeeAssignment"/> and legal entity link tables to the shared model: snake_case names, foreign keys that never cascade
/// except from an employee to its own assignments and links, and the Russian full-text and trigram indexes that serve search.
/// It also maps the keyless <see cref="EmployeeAccount"/> query over the account and role tables and the keyless <see cref="EmployeePlacement"/> query
/// over the employee and assignment tables; they have no table of their own.
/// The unique indexes on <c>lower(name)</c> of positions and legal entities are created by the migrations' own SQL, because the model cannot express an index on an expression.
/// </summary>
internal sealed class OrgStructureEntityConfiguration : IEntityConfigurationContributor
{
    private const string TrigramExtension = "pg_trgm";
    private const string FullNameSql = "last_name || ' ' || first_name || coalesce(' ' || middle_name, '')";
    private const string EmployeePlacementsSql = "SELECT id AS employee_id, org_unit_id, position_id, true AS is_main FROM employees UNION ALL SELECT employee_id, org_unit_id, position_id, false AS is_main FROM employee_assignments";
    private const string EmployeeAccountsSql = "SELECT u.employee_id, u.email, u.is_blocked, NOT u.is_blocked AND u.password_hash IS NOT NULL AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'Hr') AS can_be_hr FROM users u WHERE u.employee_id IS NOT NULL";

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension(TrigramExtension);

        modelBuilder.Entity<OrgUnit>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("org_units");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(OrgUnit.NameMaxLength).UseCollation(TextCollation.Russian);
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
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(Employee.NameMaxLength);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(Employee.NameMaxLength);
            entity.Property(e => e.MiddleName).HasColumnName("middle_name").HasMaxLength(Employee.NameMaxLength);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasComputedColumnSql(FullNameSql, stored: true).UseCollation(TextCollation.Russian);
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");

            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);

            entity.HasGeneratedTsVectorColumn(e => e.SearchVector, TextSearch.FullTextConfig, e => new { e.LastName, e.FirstName, e.MiddleName });
            entity.Property(e => e.SearchVector).HasColumnName("search_vector");
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");
            entity.HasIndex(e => e.FullName, "IX_employees_full_name_trgm").HasMethod("gin").HasOperators(TextSearch.TrigramOperators);
        });

        modelBuilder.Entity<Position>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("positions");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(Position.NameMaxLength).UseCollation(TextCollation.Russian);
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.HasGeneratedTsVectorColumn(e => e.SearchVector, TextSearch.FullTextConfig, e => e.Name);
            entity.Property(e => e.SearchVector).HasColumnName("search_vector");
            entity.HasIndex(e => e.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<OrgUnitPosition>(entity =>
        {
            entity.ToTable("org_unit_positions");
            entity.HasKey(e => new { e.OrgUnitId, e.PositionId });
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");

            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LegalEntity>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("legal_entities");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(LegalEntity.NameMaxLength).UseCollation(TextCollation.Russian);
        });

        modelBuilder.Entity<EmployeeAssignment>(entity =>
        {
            ConfigureBase(entity);
            entity.ToTable("employee_assignments");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.HasIndex(e => new { e.EmployeeId, e.OrgUnitId, e.PositionId }).IsUnique();

            entity.HasOne<Employee>().WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.OrgUnit).WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeLegalEntity>(entity =>
        {
            entity.ToTable("employee_legal_entities");
            entity.HasKey(e => new { e.EmployeeId, e.LegalEntityId });
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.LegalEntityId).HasColumnName("legal_entity_id");

            entity.HasOne<Employee>().WithMany().HasForeignKey(e => e.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LegalEntity).WithMany().HasForeignKey(e => e.LegalEntityId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssignmentLegalEntity>(entity =>
        {
            entity.ToTable("employee_assignment_legal_entities");
            entity.HasKey(e => new { e.AssignmentId, e.LegalEntityId });
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.LegalEntityId).HasColumnName("legal_entity_id");

            entity.HasOne(e => e.Assignment).WithMany(e => e.LegalEntities).HasForeignKey(e => e.AssignmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LegalEntity).WithMany().HasForeignKey(e => e.LegalEntityId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeePlacement>(entity =>
        {
            entity.HasNoKey();
            entity.ToSqlQuery(EmployeePlacementsSql);
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.IsMain).HasColumnName("is_main");
        });

        modelBuilder.Entity<EmployeeAccount>(entity =>
        {
            entity.HasNoKey();
            entity.ToSqlQuery(EmployeeAccountsSql);
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsBlocked).HasColumnName("is_blocked");
            entity.Property(e => e.CanBeHr).HasColumnName("can_be_hr");
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
