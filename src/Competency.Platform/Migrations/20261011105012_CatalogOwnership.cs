using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CatalogOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_career_directions_org_units_owner_org_unit_id",
                table: "career_directions");

            migrationBuilder.DropForeignKey(
                name: "FK_career_levels_org_units_owner_org_unit_id",
                table: "career_levels");

            migrationBuilder.DropForeignKey(
                name: "FK_competencies_org_units_owner_org_unit_id",
                table: "competencies");

            migrationBuilder.DropForeignKey(
                name: "FK_competency_categories_org_units_owner_org_unit_id",
                table: "competency_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_competency_scales_org_units_owner_org_unit_id",
                table: "competency_scales");

            migrationBuilder.DropIndex(
                name: "IX_competency_scales_owner_org_unit_id",
                table: "competency_scales");

            migrationBuilder.DropIndex(
                name: "IX_competency_categories_owner_org_unit_id",
                table: "competency_categories");

            migrationBuilder.DropIndex(
                name: "IX_competencies_owner_org_unit_id",
                table: "competencies");

            migrationBuilder.DropIndex(
                name: "IX_career_levels_owner_org_unit_id",
                table: "career_levels");

            migrationBuilder.DropIndex(
                name: "IX_career_directions_owner_org_unit_id",
                table: "career_directions");

            migrationBuilder.DropColumn(
                name: "owner_org_unit_id",
                table: "competency_scales");

            migrationBuilder.DropColumn(
                name: "owner_org_unit_id",
                table: "competency_categories");

            migrationBuilder.DropColumn(
                name: "owner_org_unit_id",
                table: "competencies");

            migrationBuilder.DropColumn(
                name: "owner_org_unit_id",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "owner_org_unit_id",
                table: "career_directions");

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "competency_scales",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE competency_scales SET is_default = true WHERE id = '3004a734-475a-4e4c-b6a6-18275f5a1d5c';");

            migrationBuilder.Sql("""
                UPDATE career_tracks
                SET owner_org_unit_id = (SELECT id FROM org_units WHERE parent_id IS NULL ORDER BY created_at, id LIMIT 1)
                WHERE owner_org_unit_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_org_unit_id",
                table: "career_tracks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_competency_scales_default",
                table: "competency_scales",
                column: "is_default",
                unique: true,
                filter: "is_default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_competency_scales_default",
                table: "competency_scales");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "competency_scales");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_org_unit_id",
                table: "competency_scales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_org_unit_id",
                table: "competency_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_org_unit_id",
                table: "competencies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_org_unit_id",
                table: "career_tracks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_org_unit_id",
                table: "career_levels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "owner_org_unit_id",
                table: "career_directions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_scales_owner_org_unit_id",
                table: "competency_scales",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_categories_owner_org_unit_id",
                table: "competency_categories",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_competencies_owner_org_unit_id",
                table: "competencies",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_owner_org_unit_id",
                table: "career_levels",
                column: "owner_org_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_directions_owner_org_unit_id",
                table: "career_directions",
                column: "owner_org_unit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_career_directions_org_units_owner_org_unit_id",
                table: "career_directions",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_career_levels_org_units_owner_org_unit_id",
                table: "career_levels",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competencies_org_units_owner_org_unit_id",
                table: "competencies",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_categories_org_units_owner_org_unit_id",
                table: "competency_categories",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_competency_scales_org_units_owner_org_unit_id",
                table: "competency_scales",
                column: "owner_org_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
