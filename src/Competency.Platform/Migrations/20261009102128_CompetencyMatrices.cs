using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CompetencyMatrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "competency_matrices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_matrices", x => x.id);
                    table.ForeignKey(
                        name: "FK_competency_matrices_career_directions_direction_id",
                        column: x => x.direction_id,
                        principalTable: "career_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_matrices_career_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_matrices_org_units_org_unit_id",
                        column: x => x.org_unit_id,
                        principalTable: "org_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    step = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_versions", x => x.id);
                    table.CheckConstraint("ck_matrix_versions_status", "status IN ('Draft', 'OnReview', 'Returned', 'Approved', 'Published', 'Archived')");
                    table.CheckConstraint("ck_matrix_versions_step", "step IN ('Superior', 'Hr')");
                    table.ForeignKey(
                        name: "FK_matrix_versions_competency_matrices_matrix_id",
                        column: x => x.matrix_id,
                        principalTable: "competency_matrices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_versions_competency_scales_scale_id",
                        column: x => x.scale_id,
                        principalTable: "competency_scales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_versions_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_version_decisions",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    step = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    variant_number = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_version_decisions", x => new { x.version_id, x.sequence });
                    table.CheckConstraint("ck_matrix_version_decisions_kind", "kind IN ('Submitted', 'Skipped', 'Approved', 'AutoApproved', 'Returned', 'AcceptedChanges', 'AcceptedAndRevising', 'RolledBack', 'Published')");
                    table.CheckConstraint("ck_matrix_version_decisions_step", "step IN ('Superior', 'Hr')");
                    table.ForeignKey(
                        name: "FK_matrix_version_decisions_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_version_decisions_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_version_variants",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matrix_version_variants", x => new { x.version_id, x.number });
                    table.CheckConstraint("ck_matrix_version_variants_kind", "kind IN ('Author', 'Approver')");
                    table.ForeignKey(
                        name: "FK_matrix_version_variants_matrix_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "matrix_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matrix_version_variants_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_direction_id",
                table: "competency_matrices",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_org_unit_id_track_id",
                table: "competency_matrices",
                columns: new[] { "org_unit_id", "track_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_track_id",
                table: "competency_matrices",
                column: "track_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_version_decisions_actor_id",
                table: "matrix_version_decisions",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_version_variants_created_by_id",
                table: "matrix_version_variants",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_author_id",
                table: "matrix_versions",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_matrix_id_number",
                table: "matrix_versions",
                columns: new[] { "matrix_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_scale_id",
                table: "matrix_versions",
                column: "scale_id");

            migrationBuilder.CreateIndex(
                name: "ux_matrix_versions_in_work",
                table: "matrix_versions",
                column: "matrix_id",
                unique: true,
                filter: "status IN ('Draft', 'OnReview', 'Returned')");

            migrationBuilder.CreateIndex(
                name: "ux_matrix_versions_published",
                table: "matrix_versions",
                column: "matrix_id",
                unique: true,
                filter: "status = 'Published'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matrix_version_decisions");

            migrationBuilder.DropTable(
                name: "matrix_version_variants");

            migrationBuilder.DropTable(
                name: "matrix_versions");

            migrationBuilder.DropTable(
                name: "competency_matrices");
        }
    }
}
