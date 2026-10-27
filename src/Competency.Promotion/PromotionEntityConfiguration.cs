using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.Promotion;

/// <summary>
/// Contributes the Promotion tables to the shared model, with snake_case names and foreign keys that never cascade.
/// </summary>
internal sealed class PromotionEntityConfiguration : IEntityConfigurationContributor
{
    /// <summary>
    /// The entities of the other modules are named rather than typed because their modules keep them internal; all must be configured before this contributor runs.
    /// </summary>
    private const string OrgUnitEntityName = "Competency.OrgStructure.OrgUnit";

    private const string EmployeeEntityName = "Competency.OrgStructure.Employee";
    private const string UserEntityName = "Competency.UserManagement.AppUser";
    private const string MatrixEntityName = "Competency.CareerFramework.CompetencyMatrix";
    private const string MatrixVersionEntityName = "Competency.CareerFramework.MatrixVersion";
    private const string TrackEntityName = "Competency.CareerFramework.CareerTrack";
    private const int NameMaxLength = 200;

    public void Configure(ModelBuilder modelBuilder)
    {
        ConfigureSettings(modelBuilder);
        ConfigureWaves(modelBuilder);
        ConfigureCommissions(modelBuilder);
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<PromotionSettings>(entity =>
        {
            entity.ToTable("promotion_settings", table =>
            {
                table.HasCheckConstraint("ck_promotion_settings_singleton", $"id = {PromotionSettings.SingletonId}");
                table.HasCheckConstraint("ck_promotion_settings_interval", $"min_interval_months BETWEEN 0 AND {PromotionSettings.MaxIntervalMonths}");
                table.HasCheckConstraint("ck_promotion_settings_durations", $"cardinality(stage_durations_days) = {PromotionWave.StageCount}");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.MinIntervalMonths).HasColumnName("min_interval_months");
            entity.Property(e => e.StageDurationsDays).HasColumnName("stage_durations_days");
            entity.HasData(new
            {
                Id = PromotionSettings.SingletonId,
                Version = 0,
                MinIntervalMonths = PromotionSettings.DefaultMinIntervalMonths,
                StageDurationsDays = PromotionSettings.DefaultStageDurationsDays.ToArray(),
            });
        });

    private static void ConfigureWaves(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromotionWave>(entity =>
        {
            entity.ToTable("promotion_waves", table =>
            {
                table.HasCheckConstraint("ck_promotion_waves_ends_on", $"cardinality(ends_on) = {PromotionWave.StageCount}");
                table.HasCheckConstraint("ck_promotion_waves_processed_stage", $"processed_stage BETWEEN 0 AND {PromotionWave.StageCount}");
            });
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.StartsOn).HasColumnName("starts_on");
            entity.Property(e => e.EndsOn).HasColumnName("ends_on");
            entity.Property(e => e.ProcessedStage).HasColumnName("processed_stage");
            entity.Property(e => e.RaterReminderSent).HasColumnName("rater_reminder_sent");
            entity.HasIndex(e => e.ProcessedStage);

            entity.HasOne(UserEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWave.AuthorId)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PromotionWaveUnit>(entity =>
        {
            entity.ToTable("promotion_wave_units");
            entity.HasKey(e => new { e.WaveId, e.OrgUnitId });
            entity.Property(e => e.WaveId).HasColumnName("wave_id");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.HasIndex(e => e.OrgUnitId);

            entity.HasOne<PromotionWave>().WithMany().HasForeignKey(e => e.WaveId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWaveUnit.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PromotionWaveMatrix>(entity =>
        {
            entity.ToTable("promotion_wave_matrices");
            entity.HasKey(e => new { e.WaveId, e.MatrixId });
            entity.Property(e => e.WaveId).HasColumnName("wave_id");
            entity.Property(e => e.MatrixId).HasColumnName("matrix_id");
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.VersionId).HasColumnName("version_id");
            entity.Property(e => e.TrackId).HasColumnName("track_id");
            entity.Property(e => e.TrackName).HasColumnName("track_name").HasMaxLength(NameMaxLength);
            entity.HasIndex(e => e.OrgUnitId);
            entity.HasIndex(e => e.VersionId);

            entity.HasOne<PromotionWave>().WithMany().HasForeignKey(e => e.WaveId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(OrgUnitEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWaveMatrix.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(MatrixEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWaveMatrix.MatrixId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(MatrixVersionEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWaveMatrix.VersionId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(TrackEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionWaveMatrix.TrackId)).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCommissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromotionCommission>(entity =>
        {
            entity.ToTable("promotion_commissions");
            entity.HasKey(e => e.OrgUnitId);
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.ChairEmployeeId).HasColumnName("chair_employee_id");
            entity.HasIndex(e => e.ChairEmployeeId);

            entity.HasOne(OrgUnitEntityName, navigationName: null).WithOne().HasForeignKey(typeof(PromotionCommission), nameof(PromotionCommission.OrgUnitId)).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(EmployeeEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionCommission.ChairEmployeeId)).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PromotionCommissionMember>(entity =>
        {
            entity.ToTable("promotion_commission_members");
            entity.HasKey(e => new { e.OrgUnitId, e.EmployeeId });
            entity.Property(e => e.OrgUnitId).HasColumnName("org_unit_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.HasIndex(e => e.EmployeeId);

            entity.HasOne<PromotionCommission>().WithMany().HasForeignKey(e => e.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(EmployeeEntityName, navigationName: null).WithMany().HasForeignKey(nameof(PromotionCommissionMember.EmployeeId)).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
