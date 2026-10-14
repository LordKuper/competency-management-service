using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class PositionMatricesAndSharedBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "track_id",
                table: "competency_matrices",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "direction_id",
                table: "competency_matrices",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "position_id",
                table: "competency_matrices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "competency_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competency_ids = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_blocks", x => x.id);
                    table.CheckConstraint("ck_competency_blocks_status", "status IN ('Draft', 'Active', 'Inactive')");
                    table.ForeignKey(
                        name: "FK_competency_blocks_competency_scales_scale_id",
                        column: x => x.scale_id,
                        principalTable: "competency_scales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_competency_blocks_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_org_unit_id_position_id",
                table: "competency_matrices",
                columns: new[] { "org_unit_id", "position_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_matrices_position_id",
                table: "competency_matrices",
                column: "position_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_competency_matrices_kind",
                table: "competency_matrices",
                sql: "(direction_id IS NOT NULL AND track_id IS NOT NULL AND position_id IS NULL) OR (direction_id IS NULL AND track_id IS NULL AND position_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_author_id",
                table: "competency_blocks",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_scale_id",
                table: "competency_blocks",
                column: "scale_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competency_matrices_positions_position_id",
                table: "competency_matrices",
                column: "position_id",
                principalTable: "positions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competency_matrices_positions_position_id",
                table: "competency_matrices");

            migrationBuilder.DropTable(
                name: "competency_blocks");

            migrationBuilder.DropIndex(
                name: "IX_competency_matrices_org_unit_id_position_id",
                table: "competency_matrices");

            migrationBuilder.DropIndex(
                name: "IX_competency_matrices_position_id",
                table: "competency_matrices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_competency_matrices_kind",
                table: "competency_matrices");

            migrationBuilder.DropColumn(
                name: "position_id",
                table: "competency_matrices");

            migrationBuilder.AlterColumn<Guid>(
                name: "track_id",
                table: "competency_matrices",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "direction_id",
                table: "competency_matrices",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
