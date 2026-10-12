using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Reduces a career level to its name, status and place in its track. Existing levels are numbered within each track by the old chain
    /// of previous levels: a level after its previous one, levels the same number of steps from the start of a chain by name, and levels
    /// on a closed loop of previous levels last by name. Down restores the dropped columns empty.
    /// </summary>
    public partial class SimplifyCareerLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "position",
                table: "career_levels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                WITH RECURSIVE chain (id, track_id, depth) AS (
                    SELECT lvl.id, lvl.track_id, 0
                    FROM career_levels lvl
                    WHERE NOT EXISTS (
                        SELECT 1 FROM career_levels prev
                        WHERE prev.id = lvl.previous_level_id AND prev.track_id = lvl.track_id)
                    UNION ALL
                    SELECT lvl.id, lvl.track_id, chain.depth + 1
                    FROM career_levels lvl
                    JOIN chain ON lvl.previous_level_id = chain.id AND lvl.track_id = chain.track_id
                ),
                numbered AS (
                    SELECT lvl.id,
                        row_number() OVER (PARTITION BY lvl.track_id ORDER BY chain.depth NULLS LAST, lvl.name, lvl.id) AS position
                    FROM career_levels lvl
                    LEFT JOIN chain ON chain.id = lvl.id
                )
                UPDATE career_levels SET position = numbered.position
                FROM numbered
                WHERE career_levels.id = numbered.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_career_levels_career_levels_previous_level_id",
                table: "career_levels");

            migrationBuilder.DropIndex(
                name: "IX_career_levels_previous_level_id",
                table: "career_levels");

            migrationBuilder.DropIndex(
                name: "IX_career_levels_track_id",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "code",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "is_managerial",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "next_level_ids",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "previous_level_id",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "role_description",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "valid_from",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "valid_to",
                table: "career_levels");

            migrationBuilder.CreateIndex(
                name: "ux_career_levels_track_position",
                table: "career_levels",
                columns: new[] { "track_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_career_levels_track_position",
                table: "career_levels");

            migrationBuilder.DropColumn(
                name: "position",
                table: "career_levels");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "career_levels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_managerial",
                table: "career_levels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "next_level_ids",
                table: "career_levels",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<Guid>(
                name: "previous_level_id",
                table: "career_levels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role_description",
                table: "career_levels",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_from",
                table: "career_levels",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_to",
                table: "career_levels",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_previous_level_id",
                table: "career_levels",
                column: "previous_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_levels_track_id",
                table: "career_levels",
                column: "track_id");

            migrationBuilder.AddForeignKey(
                name: "FK_career_levels_career_levels_previous_level_id",
                table: "career_levels",
                column: "previous_level_id",
                principalTable: "career_levels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
