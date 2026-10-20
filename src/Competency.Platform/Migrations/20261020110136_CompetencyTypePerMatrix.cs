using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class CompetencyTypePerMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE FUNCTION migration_fallback_type(competency_id text, snapshot_competencies jsonb) RETURNS text
                LANGUAGE sql AS $$
                    SELECT COALESCE(
                        (SELECT s ->> 'type' FROM jsonb_array_elements(COALESCE(snapshot_competencies, '[]'::jsonb)) AS s
                         WHERE s ->> 'id' = competency_id LIMIT 1),
                        (SELECT c.type FROM competencies c WHERE c.id::text = competency_id),
                        'Profile');
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION migration_competency_type(document jsonb, competency_id text, snapshot_competencies jsonb) RETURNS text
                LANGUAGE sql AS $$
                    SELECT CASE
                        WHEN bool_or(r ->> 'type' = 'Critical') THEN 'Critical'
                        WHEN count(DISTINCT r ->> 'type') = 1 THEN min(r ->> 'type')
                        WHEN count(r ->> 'type') > 0 THEN 'Profile'
                        ELSE migration_fallback_type(competency_id, snapshot_competencies) END
                    FROM jsonb_array_elements(COALESCE(document -> 'requirements', '[]'::jsonb)) AS r
                    WHERE r ->> 'competencyId' = competency_id;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION migration_typed_content(document jsonb, compositions jsonb, snapshot_competencies jsonb) RETURNS jsonb
                LANGUAGE sql AS $$
                    SELECT document
                        || jsonb_build_object('competencies', COALESCE((
                            SELECT jsonb_agg(c.item || jsonb_build_object(
                                'type', migration_competency_type(document, c.item ->> 'competencyId', snapshot_competencies)) ORDER BY c.position)
                            FROM jsonb_array_elements(COALESCE(document -> 'competencies', '[]'::jsonb)) WITH ORDINALITY AS c(item, position)), '[]'::jsonb))
                        || jsonb_build_object('sharedBlocks', COALESCE((
                            SELECT jsonb_agg(b.item || jsonb_build_object('competencyTypes', COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'competencyId', m.id,
                                    'type', migration_competency_type(document, m.id, snapshot_competencies)) ORDER BY m.position)
                                FROM jsonb_array_elements_text(COALESCE(compositions -> (b.item ->> 'blockId'), '[]'::jsonb)) WITH ORDINALITY AS m(id, position)),
                                '[]'::jsonb)) ORDER BY b.position)
                            FROM jsonb_array_elements(COALESCE(document -> 'sharedBlocks', '[]'::jsonb)) WITH ORDINALITY AS b(item, position)), '[]'::jsonb))
                        || jsonb_build_object('requirements', COALESCE((
                            SELECT jsonb_agg((r.item - 'type') ORDER BY r.position)
                            FROM jsonb_array_elements(COALESCE(document -> 'requirements', '[]'::jsonb)) WITH ORDINALITY AS r(item, position)), '[]'::jsonb));
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION migration_compositions(snapshot jsonb) RETURNS jsonb
                LANGUAGE sql AS $$
                    SELECT CASE
                        WHEN snapshot IS NULL THEN
                            (SELECT COALESCE(jsonb_object_agg(b.id::text, b.competency_ids), '{}'::jsonb) FROM competency_blocks b)
                        ELSE
                            (SELECT COALESCE(jsonb_object_agg(sb ->> 'id', sb -> 'competencyIds'), '{}'::jsonb)
                             FROM jsonb_array_elements(COALESCE(snapshot -> 'sharedBlocks', '[]'::jsonb)) AS sb)
                        END;
                $$;
                """);

            migrationBuilder.Sql("""
                UPDATE matrix_version_variants variant
                SET content = migration_typed_content(variant.content, migration_compositions(v.snapshot), v.snapshot -> 'competencies')
                FROM matrix_versions v
                WHERE v.id = variant.version_id;
                """);

            migrationBuilder.Sql("""
                UPDATE matrix_versions v
                SET content = migration_typed_content(v.content, migration_compositions(v.snapshot), v.snapshot -> 'competencies');
                """);

            migrationBuilder.Sql("""
                UPDATE matrix_versions v
                SET snapshot = jsonb_set(v.snapshot, '{competencies}', COALESCE((
                    SELECT jsonb_agg(s.item || jsonb_build_object('type', COALESCE(
                        (SELECT c ->> 'type' FROM jsonb_array_elements(v.content -> 'competencies') AS c
                         WHERE c ->> 'competencyId' = s.item ->> 'id' LIMIT 1),
                        (SELECT t ->> 'type'
                         FROM jsonb_array_elements(v.content -> 'sharedBlocks') AS b, jsonb_array_elements(b -> 'competencyTypes') AS t
                         WHERE t ->> 'competencyId' = s.item ->> 'id' LIMIT 1),
                        s.item ->> 'type',
                        'Profile')) ORDER BY s.position)
                    FROM jsonb_array_elements(v.snapshot -> 'competencies') WITH ORDINALITY AS s(item, position)), '[]'::jsonb))
                WHERE v.snapshot IS NOT NULL AND jsonb_typeof(v.snapshot -> 'competencies') = 'array';
                """);

            migrationBuilder.Sql("""
                DROP FUNCTION migration_compositions(jsonb);
                DROP FUNCTION migration_typed_content(jsonb, jsonb, jsonb);
                DROP FUNCTION migration_competency_type(jsonb, text, jsonb);
                DROP FUNCTION migration_fallback_type(text, jsonb);
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_competencies_type",
                table: "competencies");

            migrationBuilder.DropColumn(
                name: "type",
                table: "competency_versions");

            migrationBuilder.DropColumn(
                name: "type",
                table: "competencies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "competency_versions", "competencies" })
            {
                migrationBuilder.AddColumn<string>(
                    name: "type",
                    table: table,
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false,
                    defaultValue: "Profile");
                migrationBuilder.AlterColumn<string>(
                    name: "type",
                    table: table,
                    type: "character varying(16)",
                    maxLength: 16,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(16)",
                    oldMaxLength: 16,
                    oldDefaultValue: "Profile");
            }

            migrationBuilder.AddCheckConstraint(
                name: "ck_competencies_type",
                table: "competencies",
                sql: "type IN ('Critical', 'Profile', 'Additional')");

            migrationBuilder.Sql("""
                CREATE FUNCTION migration_requirement_types(document jsonb) RETURNS jsonb
                LANGUAGE sql AS $$
                    SELECT jsonb_set(document, '{requirements}', COALESCE((
                        SELECT jsonb_agg(r.item || jsonb_build_object('type', COALESCE(
                            (SELECT c ->> 'type' FROM jsonb_array_elements(COALESCE(document -> 'competencies', '[]'::jsonb)) AS c
                             WHERE c ->> 'competencyId' = r.item ->> 'competencyId' LIMIT 1),
                            (SELECT t ->> 'type'
                             FROM jsonb_array_elements(COALESCE(document -> 'sharedBlocks', '[]'::jsonb)) AS b,
                                 jsonb_array_elements(COALESCE(b -> 'competencyTypes', '[]'::jsonb)) AS t
                             WHERE t ->> 'competencyId' = r.item ->> 'competencyId' LIMIT 1),
                            'Profile')) ORDER BY r.position)
                        FROM jsonb_array_elements(COALESCE(document -> 'requirements', '[]'::jsonb)) WITH ORDINALITY AS r(item, position)), '[]'::jsonb));
                $$;
                """);

            migrationBuilder.Sql("""
                UPDATE matrix_versions SET content = migration_requirement_types(content);
                UPDATE matrix_version_variants SET content = migration_requirement_types(content);
                DROP FUNCTION migration_requirement_types(jsonb);
                """);
        }
    }
}
