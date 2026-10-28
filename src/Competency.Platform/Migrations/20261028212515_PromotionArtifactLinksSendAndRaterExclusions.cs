using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PromotionArtifactLinksSendAndRaterExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE promotion_artifacts SET links = COALESCE((SELECT jsonb_agg(jsonb_build_object('Description', text, 'Url', link)) FROM jsonb_array_elements_text(links) AS link), '[]'::jsonb)");
            migrationBuilder.Sql("DELETE FROM promotion_artifacts WHERE links = '[]'::jsonb");

            migrationBuilder.DropColumn(
                name: "text",
                table: "promotion_artifacts");

            migrationBuilder.AddColumn<bool>(
                name: "is_sent_to_review",
                table: "promotion_applications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "promotion_rater_exclusions",
                columns: table => new
                {
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_rater_exclusions", x => new { x.kind, x.subject_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_rater_exclusions");

            migrationBuilder.DropColumn(
                name: "is_sent_to_review",
                table: "promotion_applications");

            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "promotion_artifacts",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");
        }
    }
}
