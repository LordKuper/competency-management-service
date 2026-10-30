using System.Text.Json;
using Kalibr.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kalibr.CareerFramework;

/// <summary>
/// Contributes the CareerFramework tables to the shared model: the <see cref="OrgUnitHr"/> assignments, the career catalogues with the shared blocks, the links of levels to positions and the matrices,
/// with snake_case names, statuses and types stored as their names, and foreign keys that never cascade.
/// </summary>
internal sealed class CareerFrameworkEntityConfiguration : IEntityConfigurationContributor
{
    /// <summary>
    /// The unit, position, employee and account entities are named rather than typed because their modules keep them internal; all must be configured before this contributor runs.
    /// </summary>
    private const string OrgUnitEntityName = "Kalibr.OrgStructure.OrgUnit";

    private const string PositionEntityName = "Kalibr.OrgStructure.Position";
    private const string EmployeeEntityName = "Kalibr.OrgStructure.Employee";
    private const string UserEntityName = "Kalibr.UserManagement.AppUser";
    private const int EnumMaxLength = 16;

    /// <summary>
    /// The longest decision kind, <c>AcceptedAndRevising</c>, has 19 letters, more than the other enumerations need.
    /// </summary>
    private const int DecisionKindMaxLength = 32;

    private static readonly string StatusCheckSql = EnumCheckSql<CatalogStatus>("status");
    private static readonly JsonSerializerOptions PointsJson = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions DocumentJson = new(JsonSerializerDefaults.Web);

    private static readonly ValueConverter<MatrixContent, string> ContentConverter = new(
        content => content.ToJson(),
        json => MatrixContent.FromJson(json));

    private static readonly ValueComparer<MatrixContent> ContentComparer = new(
        (left, right) => left!.SameAs(right!),
        content => content.ToJson().GetHashCode(StringComparison.Ordinal),
        content => MatrixContent.FromJson(content.ToJson()));

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgUnitHr>(entity =>
        {
            entity.ToTable("org_unit_hr");
            entity.HasKey(e => e.OrgUnitId);
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");

            entity.HasOne(OrgUnitEntityName, navigationName: null).WithOne().HasForeignKey(typeof(OrgUnitHr), nameof(OrgUnitHr.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(EmployeeEntityName, navigationName: null).WithMany().HasForeignKey(nameof(OrgUnitHr.EmployeeId)).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureEntry<CareerDirection>(modelBuilder, "career_directions", _ => { });

        ConfigureEntry<CareerTrack>(modelBuilder, "career_tracks", entity =>
        {
            entity.Property(e => e.OwnerOrgUnitId).HasColumnName("owner_org_unit_id");
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CareerTrack.OwnerOrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareerDirection>().WithMany().HasForeignKey(e => e.DirectionId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureEntry<CareerLevel>(modelBuilder, "career_levels", ConfigureLevel);

        modelBuilder.Entity<CareerLevelPosition>(entity =>
        {
            entity.ToTable("career_level_positions");
            entity.HasKey(e => new { e.LevelId, e.PositionId });
            entity.Property(e => e.LevelId).HasColumnName("level_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.HasIndex(e => new { e.TrackId, e.PositionId }).IsUnique();
            entity.HasIndex(e => e.PositionId);

            entity.HasOne<CareerLevel>().WithMany().HasForeignKey(e => e.LevelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareerTrack>().WithMany().HasForeignKey(e => e.TrackId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(PositionEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CareerLevelPosition.PositionId)).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureEntry<CompetencyScale>(modelBuilder, "competency_scales", ConfigureScale);

        ConfigureEntry<CompetencyBlock>(modelBuilder, "competency_blocks", ConfigureBlock);

        ConfigureEntry<CareerCompetency>(modelBuilder, "competencies", entity =>
        {
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(CareerCompetency.DescriptionMaxLength);
            entity.Property(e => e.EvidenceExamples).HasColumnName("evidence_examples").HasMaxLength(CareerCompetency.DescriptionMaxLength);
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.ScaleId).HasColumnName("scale_id");

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
            entity.Property(e => e.EvidenceExamples).HasColumnName("evidence_examples").HasMaxLength(CareerCompetency.DescriptionMaxLength);
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.ScaleId).HasColumnName("scale_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.ChangedById).HasColumnName("changed_by_id");
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at");

            entity.HasOne<CareerCompetency>().WithMany().HasForeignKey(e => e.CompetencyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CompetencyVersion.ChangedById)).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureMatrices(modelBuilder);
    }

    /// <summary>
    /// Maps matrices, their versions and the versions' history. Partial unique indexes back up the transition locks: at most one
    /// published and one in-work version per matrix.
    /// </summary>
    private static void ConfigureMatrices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompetencyMatrix>(entity =>
        {
            entity.ToTable("competency_matrices", table => table.HasCheckConstraint(
                "ck_competency_matrices_kind",
                "(direction_id IS NOT NULL AND track_id IS NOT NULL AND position_id IS NULL) OR (direction_id IS NULL AND track_id IS NULL AND position_id IS NOT NULL)"));
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.DirectionId).HasColumnName("direction_id");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.PositionId).HasColumnName("position_id");
            entity.HasIndex(e => new { e.OrgUnitId, e.TrackId }).IsUnique();
            entity.HasIndex(e => new { e.OrgUnitId, e.PositionId }).IsUnique();

            entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CompetencyMatrix.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareerDirection>().WithMany().HasForeignKey(e => e.DirectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CareerTrack>().WithMany().HasForeignKey(e => e.TrackId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(PositionEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CompetencyMatrix.PositionId)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatrixVersion>(entity =>
        {
            entity.ToTable("matrix_versions", table =>
            {
                table.HasCheckConstraint("ck_matrix_versions_status", EnumCheckSql<MatrixVersionStatus>("status"));
                table.HasCheckConstraint("ck_matrix_versions_step", EnumCheckSql<ApprovalStep>("step"));
            });
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MatrixId).HasColumnName("matrix_id");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.Step).HasColumnName("step").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("jsonb").HasConversion(ContentConverter, ContentComparer);
            entity.Property(e => e.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb").HasConversion(
                snapshot => JsonSerializer.Serialize(snapshot, DocumentJson),
                json => JsonSerializer.Deserialize<MatrixEntries>(json, DocumentJson));
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.BuilderStep).HasColumnName("builder_step");
            entity.HasIndex(e => new { e.MatrixId, e.Number }).IsUnique();
            entity.HasIndex(e => e.MatrixId, "ux_matrix_versions_published").IsUnique().HasFilter("status = 'Published'");
            entity.HasIndex(e => e.MatrixId, "ux_matrix_versions_in_work").IsUnique().HasFilter("status IN ('Draft', 'OnReview', 'Returned')");

            entity.HasOne(e => e.Matrix).WithMany().HasForeignKey(e => e.MatrixId).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(e => e.Matrix).AutoInclude();
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(MatrixVersion.AuthorId)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatrixVariant>(entity =>
        {
            entity.ToTable("matrix_version_variants", table => table.HasCheckConstraint("ck_matrix_version_variants_kind", EnumCheckSql<MatrixVariantKind>("kind")));
            entity.HasKey(e => new { e.VersionId, e.Number });
            entity.Property(e => e.VersionId).HasColumnName("version_id");
            entity.Property(e => e.Number).HasColumnName("number");
            entity.Property(e => e.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("jsonb").HasConversion(ContentConverter, ContentComparer);
            entity.Property(e => e.CreatedById).HasColumnName("created_by_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne<MatrixVersion>().WithMany().HasForeignKey(e => e.VersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(MatrixVariant.CreatedById)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatrixDecision>(entity =>
        {
            entity.ToTable("matrix_version_decisions", table =>
            {
                table.HasCheckConstraint("ck_matrix_version_decisions_kind", EnumCheckSql<MatrixDecisionKind>("kind"));
                table.HasCheckConstraint("ck_matrix_version_decisions_step", EnumCheckSql<ApprovalStep>("step"));
            });
            entity.HasKey(e => new { e.VersionId, e.Sequence });
            entity.Property(e => e.VersionId).HasColumnName("version_id");
            entity.Property(e => e.Sequence).HasColumnName("sequence");
            entity.Property(e => e.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(DecisionKindMaxLength);
            entity.Property(e => e.Step).HasColumnName("step").HasConversion<string>().HasMaxLength(EnumMaxLength);
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.At).HasColumnName("at");
            entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(MatrixDecision.CommentMaxLength);
            entity.Property(e => e.VariantNumber).HasColumnName("variant_number");
            entity.HasIndex(e => e.ActorId);

            entity.HasOne<MatrixVersion>().WithMany().HasForeignKey(e => e.VersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(MatrixDecision.ActorId)).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLevel(EntityTypeBuilder<CareerLevel> entity)
    {
        entity.Property(e => e.TrackId).HasColumnName("track_id");
        entity.Property(e => e.Position).HasColumnName("position");
        entity.HasIndex(e => new { e.TrackId, e.Position }, "ux_career_levels_track_position").IsUnique();

        entity.HasOne(e => e.Track).WithMany().HasForeignKey(e => e.TrackId).OnDelete(DeleteBehavior.Restrict);
        entity.Navigation(e => e.Track).AutoInclude();
    }

    /// <summary>
    /// Maps a scale's points and its default flag; a partial unique index keeps the flag on one scale at most.
    /// </summary>
    private static void ConfigureScale(EntityTypeBuilder<CompetencyScale> entity)
    {
        entity.Property(e => e.Points)
            .HasColumnName("points")
            .HasColumnType("jsonb")
            .HasConversion(
                points => JsonSerializer.Serialize(points, PointsJson),
                json => JsonSerializer.Deserialize<List<ScalePoint>>(json, PointsJson) ?? new List<ScalePoint>(),
                new ValueComparer<List<ScalePoint>>(
                    (left, right) => left!.SequenceEqual(right!),
                    points => points.Aggregate(0, (hash, point) => HashCode.Combine(hash, point)),
                    points => points.ToList()));
        entity.Property(e => e.IsDefault).HasColumnName("is_default");
        entity.HasIndex(e => e.IsDefault, "ux_competency_scales_default").IsUnique().HasFilter("is_default");
    }

    /// <summary>
    /// Maps the unit a shared block applies to and its composition, a jsonb array of competency identifiers on the block's row,
    /// so a change of the composition is one change of the block and one journal event.
    /// </summary>
    private static void ConfigureBlock(EntityTypeBuilder<CompetencyBlock> entity)
    {
        entity.Property(e => e.ScopeUnitId).HasColumnName("scope_unit_id");
        entity.Property(e => e.CompetencyIds)
            .HasColumnName("competency_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, DocumentJson),
                json => JsonSerializer.Deserialize<List<Guid>>(json, DocumentJson) ?? new List<Guid>(),
                new ValueComparer<List<Guid>>(
                    (left, right) => left!.SequenceEqual(right!),
                    ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
                    ids => ids.ToList()));

        entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CompetencyBlock.ScopeUnitId)).OnDelete(DeleteBehavior.Restrict);
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
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(CatalogEntry.NameMaxLength).UseCollation(TextCollation.Russian);
        entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(EnumMaxLength);
        entity.Property(e => e.AuthorId).HasColumnName("author_id");

        entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(CatalogEntry.AuthorId)).OnDelete(DeleteBehavior.Restrict);

        configure(entity);
    });

    private static string EnumCheckSql<TEnum>(string column)
        where TEnum : struct, Enum => $"{column} IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"))})";
}
