using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class SharedBlockWithoutScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competency_blocks_competency_scales_scale_id",
                table: "competency_blocks");

            migrationBuilder.DropIndex(
                name: "IX_competency_blocks_scale_id",
                table: "competency_blocks");

            migrationBuilder.DropColumn(
                name: "scale_id",
                table: "competency_blocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "scale_id",
                table: "competency_blocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE competency_blocks b
                SET scale_id = COALESCE(
                    (SELECT c.scale_id FROM competencies c WHERE c.id = (b.competency_ids ->> 0)::uuid),
                    (SELECT id FROM competency_scales WHERE is_default LIMIT 1));
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "scale_id",
                table: "competency_blocks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_scale_id",
                table: "competency_blocks",
                column: "scale_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competency_blocks_competency_scales_scale_id",
                table: "competency_blocks",
                column: "scale_id",
                principalTable: "competency_scales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
