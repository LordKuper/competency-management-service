using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <summary>
    /// Adds the links of career levels to positions. Only the schema: existing tracks get no links, so their gaps stay until someone links positions.
    /// </summary>
    public partial class CareerLevelPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "career_level_positions",
                columns: table => new
                {
                    level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    track_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_career_level_positions", x => new { x.level_id, x.position_id });
                    table.ForeignKey(
                        name: "FK_career_level_positions_career_levels_level_id",
                        column: x => x.level_id,
                        principalTable: "career_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_level_positions_career_tracks_track_id",
                        column: x => x.track_id,
                        principalTable: "career_tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_career_level_positions_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_career_level_positions_position_id",
                table: "career_level_positions",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_career_level_positions_track_id_position_id",
                table: "career_level_positions",
                columns: new[] { "track_id", "position_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "career_level_positions");
        }
    }
}
