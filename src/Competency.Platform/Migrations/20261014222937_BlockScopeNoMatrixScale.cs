using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class BlockScopeNoMatrixScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE matrix_versions v
                SET snapshot = jsonb_set(v.snapshot, '{competencies}', COALESCE((
                    SELECT jsonb_agg(c.item || jsonb_build_object('scaleId', COALESCE(
                        (SELECT b.block -> 'scale' -> 'id'
                         FROM jsonb_array_elements(COALESCE(v.snapshot -> 'sharedBlocks', '[]'::jsonb)) WITH ORDINALITY AS b(block, position)
                         WHERE b.block -> 'competencyIds' @> jsonb_build_array(c.item -> 'id')
                         ORDER BY b.position
                         LIMIT 1),
                        v.snapshot -> 'scale' -> 'id')) ORDER BY c.position)
                    FROM jsonb_array_elements(v.snapshot -> 'competencies') WITH ORDINALITY AS c(item, position)), '[]'::jsonb))
                WHERE v.snapshot IS NOT NULL AND jsonb_typeof(v.snapshot -> 'competencies') = 'array';
                """);

            migrationBuilder.Sql("""
                UPDATE matrix_versions v
                SET snapshot = v.snapshot || jsonb_build_object('scales', COALESCE((
                    SELECT jsonb_agg(candidate.scale ORDER BY candidate.position)
                    FROM (
                        SELECT DISTINCT ON (s.scale ->> 'id') s.scale, s.position
                        FROM (
                            SELECT v.snapshot -> 'scale' AS scale, 0::bigint AS position
                            WHERE EXISTS (
                                SELECT 1 FROM jsonb_array_elements(v.snapshot -> 'competencies') AS c(item)
                                WHERE c.item -> 'scaleId' = v.snapshot -> 'scale' -> 'id')
                            UNION ALL
                            SELECT b.block -> 'scale', b.position
                            FROM jsonb_array_elements(COALESCE(v.snapshot -> 'sharedBlocks', '[]'::jsonb)) WITH ORDINALITY AS b(block, position)
                        ) AS s
                        WHERE s.scale IS NOT NULL AND jsonb_typeof(s.scale) = 'object'
                        ORDER BY s.scale ->> 'id', s.position) AS candidate), '[]'::jsonb))
                WHERE v.snapshot IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_matrix_versions_competency_scales_scale_id",
                table: "matrix_versions");

            migrationBuilder.DropIndex(
                name: "IX_matrix_versions_scale_id",
                table: "matrix_versions");

            migrationBuilder.DropColumn(
                name: "scale_id",
                table: "matrix_versions");

            migrationBuilder.AddColumn<Guid>(
                name: "scope_unit_id",
                table: "competency_blocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_blocks_scope_unit_id",
                table: "competency_blocks",
                column: "scope_unit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_competency_blocks_org_units_scope_unit_id",
                table: "competency_blocks",
                column: "scope_unit_id",
                principalTable: "org_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_competency_blocks_org_units_scope_unit_id",
                table: "competency_blocks");

            migrationBuilder.DropIndex(
                name: "IX_competency_blocks_scope_unit_id",
                table: "competency_blocks");

            migrationBuilder.DropColumn(
                name: "scope_unit_id",
                table: "competency_blocks");

            migrationBuilder.AddColumn<Guid>(
                name: "scale_id",
                table: "matrix_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE matrix_versions
                SET scale_id = COALESCE(
                    (snapshot -> 'scale' ->> 'id')::uuid,
                    (SELECT id FROM competency_scales WHERE is_default LIMIT 1));
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "scale_id",
                table: "matrix_versions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_matrix_versions_scale_id",
                table: "matrix_versions",
                column: "scale_id");

            migrationBuilder.AddForeignKey(
                name: "FK_matrix_versions_competency_scales_scale_id",
                table: "matrix_versions",
                column: "scale_id",
                principalTable: "competency_scales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
