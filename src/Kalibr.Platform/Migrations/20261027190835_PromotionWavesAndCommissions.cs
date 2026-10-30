using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionWavesAndCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_commissions",
                columns: table => new
                {
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chair_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_commissions", x => x.org_unit_id);
                    table.ForeignKey(
                        name: "FK_promotion_commissions_employees_chair_employee_id",
                        column: x => x.chair_employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_commissions_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    min_interval_months = table.Column<int>(type: "integer", nullable: false),
                    stage_durations_days = table.Column<int[]>(type: "integer[]", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_settings", x => x.id);
                    table.CheckConstraint("ck_promotion_settings_durations", "cardinality(stage_durations_days) = 7");
                    table.CheckConstraint("ck_promotion_settings_interval", "min_interval_months BETWEEN 0 AND 120");
                    table.CheckConstraint("ck_promotion_settings_singleton", "id = 1");
                });

            migrationBuilder.CreateTable(
                name: "promotion_waves",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ends_on = table.Column<DateOnly[]>(type: "date[]", nullable: false),
                    processed_stage = table.Column<int>(type: "integer", nullable: false),
                    rater_reminder_sent = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_waves", x => x.id);
                    table.CheckConstraint("ck_promotion_waves_ends_on", "cardinality(ends_on) = 7");
                    table.CheckConstraint("ck_promotion_waves_processed_stage", "processed_stage BETWEEN 0 AND 7");
                    table.ForeignKey(
                        name: "FK_promotion_waves_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_commission_members",
                columns: table => new
                {
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_commission_members", x => new { x.org_unit_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_promotion_commission_members_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_commission_members_promotion_commissions_org_unit~",
                        column: x => x.org_unit_id,
                        principalTable: "promotion_commissions",
                        principalColumn: "org_unit_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_wave_matrices",
                columns: table => new
                {
                    wave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_wave_matrices", x => new { x.wave_id, x.matrix_id });
                    table.ForeignKey(
                        name: "FK_promotion_wave_matrices_career_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_wave_matrices_competency_matrices_matrix_id",
                        column: x => x.matrix_id,
                        principalTable: "competency_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_wave_matrices_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_wave_matrices_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_wave_matrices_promotion_waves_wave_id",
                        column: x => x.wave_id,
                        principalTable: "promotion_waves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_wave_units",
                columns: table => new
                {
                    wave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_wave_units", x => new { x.wave_id, x.org_unit_id });
                    table.ForeignKey(
                        name: "FK_promotion_wave_units_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_wave_units_promotion_waves_wave_id",
                        column: x => x.wave_id,
                        principalTable: "promotion_waves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "promotion_settings",
                columns: new[] { "id", "min_interval_months", "stage_durations_days", "version" },
                values: new object[] { 1, 12, new[] { 14, 7, 14, 7, 14, 3, 14 }, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_commission_members_employee_id",
                table: "promotion_commission_members",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_commissions_chair_employee_id",
                table: "promotion_commissions",
                column: "chair_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_wave_matrices_matrix_id",
                table: "promotion_wave_matrices",
                column: "matrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_wave_matrices_org_unit_id",
                table: "promotion_wave_matrices",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_wave_matrices_track_id",
                table: "promotion_wave_matrices",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_wave_matrices_version_id",
                table: "promotion_wave_matrices",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_wave_units_org_unit_id",
                table: "promotion_wave_units",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_waves_author_id",
                table: "promotion_waves",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_waves_processed_stage",
                table: "promotion_waves",
                column: "processed_stage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_commission_members");

            migrationBuilder.DropTable(
                name: "promotion_settings");

            migrationBuilder.DropTable(
                name: "promotion_wave_matrices");

            migrationBuilder.DropTable(
                name: "promotion_wave_units");

            migrationBuilder.DropTable(
                name: "promotion_commissions");

            migrationBuilder.DropTable(
                name: "promotion_waves");
        }
    }
}
