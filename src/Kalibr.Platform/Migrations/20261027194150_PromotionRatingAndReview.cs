using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kalibr.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionRatingAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "artifacts_missing",
                table: "promotion_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "decided_at",
                table: "promotion_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "decided_by",
                table: "promotion_applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "decision",
                table: "promotion_applications",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "feedback_received_at",
                table: "promotion_applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "meets_after_rating",
                table: "promotion_applications",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "promotion_artifacts",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    links = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_artifacts", x => new { x.application_id, x.competency_id });
                    table.ForeignKey(
                        name: "FK_promotion_artifacts_promotion_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "promotion_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_final_scores",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_final_scores", x => new { x.application_id, x.competency_id });
                    table.ForeignKey(
                        name: "FK_promotion_final_scores_promotion_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "promotion_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rater_scores",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_rater_scores", x => new { x.application_id, x.employee_id, x.competency_id });
                    table.ForeignKey(
                        name: "FK_promotion_rater_scores_promotion_raters_application_id_empl~",
                        columns: x => new { x.application_id, x.employee_id },
                        principalTable: "promotion_raters",
                        principalColumns: new[] { "application_id", "employee_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_applications_decided_by",
                table: "promotion_applications",
                column: "decided_by");

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_applications_users_decided_by",
                table: "promotion_applications",
                column: "decided_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_applications_users_decided_by",
                table: "promotion_applications");

            migrationBuilder.DropTable(
                name: "promotion_artifacts");

            migrationBuilder.DropTable(
                name: "promotion_final_scores");

            migrationBuilder.DropTable(
                name: "promotion_rater_scores");

            migrationBuilder.DropIndex(
                name: "IX_promotion_applications_decided_by",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "artifacts_missing",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "decided_at",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "decided_by",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "decision",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "feedback_received_at",
                table: "promotion_applications");

            migrationBuilder.DropColumn(
                name: "meets_after_rating",
                table: "promotion_applications");
        }
    }
}
