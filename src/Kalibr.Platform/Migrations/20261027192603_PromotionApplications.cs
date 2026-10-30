using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    education = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    hired_or_promoted_on = table.Column<DateOnly>(type: "date", nullable: true),
                    ignore_interval = table.Column<bool>(type: "boolean", nullable: false),
                    ignore_interval_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ignore_interval_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    formal_failures = table.Column<string[]>(type: "character varying(30)[]", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_promotion_applications_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_applications_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_applications_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_applications_promotion_wave_matrices_wave_id_matr~",
                        columns: x => new { x.wave_id, x.matrix_id },
                        principalTable: "promotion_wave_matrices",
                        principalColumns: new[] { "wave_id", "matrix_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_applications_users_ignore_interval_by",
                        column: x => x.ignore_interval_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_raters",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_raters", x => new { x.application_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_promotion_raters_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_raters_promotion_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "promotion_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_self_scores",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_self_scores", x => new { x.application_id, x.competency_id });
                    table.ForeignKey(
                        name: "FK_promotion_self_scores_promotion_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "promotion_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_employee_id",
                table: "promotion_applications",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_ignore_interval_by",
                table: "promotion_applications",
                column: "ignore_interval_by");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_org_unit_id",
                table: "promotion_applications",
                column: "org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_version_id",
                table: "promotion_applications",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_wave_id_matrix_id",
                table: "promotion_applications",
                columns: new[] { "wave_id", "matrix_id" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_wave_id_org_unit_id",
                table: "promotion_applications",
                columns: new[] { "wave_id", "org_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ux_promotion_applications_unfinished",
                table: "promotion_applications",
                columns: new[] { "wave_id", "employee_id" },
                unique: true,
                filter: "status IN ('Submitted', 'Admitted', 'UnderReview')");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_raters_employee_id",
                table: "promotion_raters",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_raters");

            migrationBuilder.DropTable(
                name: "promotion_self_scores");

            migrationBuilder.DropTable(
                name: "promotion_applications");
        }
    }
}
