using System.Text.Json;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Competency.CareerFramework;

/// <summary>
/// Contributes the CareerFramework tables to the shared model: the <see cref="OrgUnitHr"/> assignments and the career catalogues,
/// with snake_case names, statuses and types stored as their names, and foreign keys that never cascade.
/// </summary>
internal sealed class CareerFrameworkEntityConfiguration : IEntityConfigurationContributor
{
    /// <summary>
    /// The unit and account entities are named rather than typed because their modules keep them internal; both must be configured before this contributor runs.
    /// </summary>
    private const string OrgUnitEntityName = "Competency.OrgStructure.OrgUnit";

    private const string UserEntityName = "Competency.UserManagement.AppUser";
    private const int EnumMaxLength = 16;
    private const int SalaryPrecision = 12;
    private const int SalaryScale = 2;

    private static readonly string StatusCheckSql = EnumCheckSql<CatalogStatus>("status");
    private static readonly string TypeCheckSql = EnumCheckSql<CompetencyType>("type");
    private static readonly JsonSerializerOptions PointsJson = new(JsonSerializerDefaults.Web);

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgUnitHr>(entity =>
        {
            entity.ToTable("org_unit_hr");
            entity.HasKey(e => e.OrgUnitId);
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(OrgUnitEntityName, navigationName: null).WithOne().HasForeignKey(typeof(OrgUnitHr), nameof(OrgUnitHr.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(OrgUnitHr.UserId)).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureEntry<CareerDirection>(modelBuilder, "career_directions", _ => { });

        ConfigureEntry<CareerTrack>(modelBuilder, "career_tracks", entity =>
        {
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.HasOne<CareerDirection>().WithMany().HasForeignKey(e => e.DirectionId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureEntry<CareerLevel>(modelBuilder, "career_levels", ConfigureLevel);

        ConfigureEntry<CompetencyCategory>(modelBuilder, "competency_categories", _ => { });

        ConfigureEntry<CompetencyScale>(modelBuilder, "competency_scales", entity => entity.Property(e => e.Points)
            .HasColumnName("points")
            .HasColumnType("jsonb")
            .HasConversion(
                points => JsonSerializer.Serialize(points, PointsJson),
                json => JsonSerializer.Deserialize<List<ScalePoint>>(json, PointsJson) ?? new List<ScalePoint>(),
                new ValueComparer<List<ScalePoint>>(
                    (left, right) => left!.SequenceEqual(right!),
                    points => points.Aggregate(0, (hash, point) => HashCode.Combine(hash, point)),
                    points => points.ToList())));

        ConfigureEntry<CareerCompetency>(modelBuilder, "competencies", entity =>
        {
            entity.ToTable(table => table.HasCheckConstraint("ck_competencies_type", TypeCheckSql));
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(CareerCompetency.DescriptionMaxLength);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.ScaleId).HasColumnName("scale_id");

            entity.HasOne<CompetencyCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareerDirection>().WithMany().HasForeignKey(e => e.DirectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompetencyScale>().WithMany().HasForeignKey(e => e.ScaleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompetencyVersion>(entity =>
        {
            entity.ToTable("competency_versions");
            entity.HasKey(e => new { e.CompetencyId, e.Number });
            entity.Property(e => e.CompetencyId).HasColumnName("competency_id");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(CatalogEntry.NameMaxLength);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(CareerCompetency.DescriptionMaxLength);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.ScaleId).HasColumnName("scale_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.ChangedById).HasColumnName("changed_by_id");
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at");

            entity.HasOne<CareerCompetency>().WithMany().HasForeignKey(e => e.CompetencyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CompetencyVersion.ChangedById)).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLevel(EntityTypeBuilder<CareerLevel> entity)
    {
        entity.Property(e => e.TrackId).HasColumnName("track_id");
        entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(CareerLevel.CodeMaxLength);
        entity.Property(e => e.IsManagerial).HasColumnName("is_managerial");
        entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
        entity.Property(e => e.ValidTo).HasColumnName("valid_to");
        entity.Property(e => e.RoleDescription).HasColumnName("role_description").HasMaxLength(CareerLevel.RoleDescriptionMaxLength);
        entity.Property(e => e.PreviousLevelId).HasColumnName("previous_level_id");
        entity.Property(e => e.NextLevelIds).HasColumnName("next_level_ids");
        entity.Property(e => e.SalaryFrom).HasColumnName("salary_from").HasPrecision(SalaryPrecision, SalaryScale);
        entity.Property(e => e.SalaryTo).HasColumnName("salary_to").HasPrecision(SalaryPrecision, SalaryScale);

        entity.HasOne(e => e.Track).WithMany().HasForeignKey(e => e.TrackId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(e => e.Track).AutoInclude();
        entity.HasOne<CareerLevel>().WithMany().HasForeignKey(e => e.PreviousLevelId).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Maps the columns every catalogue entry shares, then the entry type's own.
    /// </summary>
    private static void ConfigureEntry<T>(ModelBuilder modelBuilder, string table, Action<EntityTypeBuilder<T>> configure)
        where T : CatalogEntry => modelBuilder.Entity<T>(entity =>
    {
        entity.ToTable(table, builder => builder.HasCheckConstraint($"ck_{table}_status", StatusCheckSql));
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(CatalogEntry.NameMaxLength);
        entity.Property(e => e.OwnerOrgUnitId).HasColumnName("owner_org_unit_id");
        entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(EnumMaxLength);
        entity.Property(e => e.AuthorId).HasColumnName("author_id");

        entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CatalogEntry.OwnerOrgUnitId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CatalogEntry.AuthorId)).OnDelete(DeleteBehavior.Restrict);

        configure(entity);
    });

    private static string EnumCheckSql<TEnum>(string column)
        where TEnum : struct, Enum => $"{column} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"))})";
}
