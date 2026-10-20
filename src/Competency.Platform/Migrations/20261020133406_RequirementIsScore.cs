using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Competency.Platform.Migrations
{
    /// <inheritdoc />
    public partial class RequirementIsScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                DECLARE
                    competency record;
                    text_row record;
                    block_target record;
                    merged text;
                    candidate text;
                    dropped integer;
                    unit_name text;
                    suffix integer;
                BEGIN
                    ALTER TABLE competencies ADD COLUMN IF NOT EXISTS evidence_examples character varying(4000) NULL;
                    ALTER TABLE competency_versions ADD COLUMN IF NOT EXISTS evidence_examples character varying(4000) NULL;

                    FOR competency IN
                        SELECT DISTINCT (r ->> 'competencyId')::uuid AS id
                        FROM matrix_versions v, jsonb_array_elements(COALESCE(v.content -> 'requirements', '[]'::jsonb)) AS r
                        WHERE btrim(COALESCE(r ->> 'evidenceExamples', '')) <> ''
                    LOOP
                        merged := NULL;
                        dropped := 0;
                        FOR text_row IN
                            SELECT texts.example
                            FROM (
                                SELECT btrim(r.item ->> 'evidenceExamples') AS example, v.created_at, v.id AS version_id, r.position,
                                    row_number() OVER (
                                        PARTITION BY btrim(r.item ->> 'evidenceExamples')
                                        ORDER BY v.created_at DESC, v.id, r.position) AS occurrence
                                FROM matrix_versions v,
                                    jsonb_array_elements(COALESCE(v.content -> 'requirements', '[]'::jsonb)) WITH ORDINALITY AS r(item, position)
                                WHERE r.item ->> 'competencyId' = competency.id::text
                                    AND btrim(COALESCE(r.item ->> 'evidenceExamples', '')) <> '') AS texts
                            WHERE texts.occurrence = 1
                            ORDER BY texts.created_at DESC, texts.version_id, texts.position
                        LOOP
                            candidate := CASE WHEN merged IS NULL THEN text_row.example ELSE merged || E'\n\n' || text_row.example END;
                            IF dropped = 0 AND char_length(candidate) <= 4000 THEN
                                merged := candidate;
                            ELSE
                                dropped := dropped + 1;
                            END IF;
                        END LOOP;

                        IF merged IS NOT NULL THEN
                            UPDATE competencies SET evidence_examples = merged WHERE id = competency.id;
                            UPDATE competency_versions SET evidence_examples = merged
                            WHERE competency_id = competency.id
                                AND number = (SELECT max(history.number) FROM competency_versions history WHERE history.competency_id = competency.id);
                        END IF;

                        IF dropped > 0 THEN
                            RAISE WARNING 'RequirementIsScore: competency % kept no room for % evidence example text(s) over 4000 characters',
                                competency.id, dropped;
                        END IF;
                    END LOOP;

                    CREATE TEMP TABLE migration_own_blocks ON COMMIT DROP AS
                    SELECT v.id AS version_id, m.org_unit_id AS unit_id, v.created_at, v.author_id, v.status,
                        b.item ->> 'id' AS own_block_id, btrim(b.item ->> 'name') AS name, b.position,
                        COALESCE((
                            SELECT jsonb_agg(DISTINCT c ->> 'competencyId' ORDER BY c ->> 'competencyId')
                            FROM jsonb_array_elements(COALESCE(v.content -> 'competencies', '[]'::jsonb)) AS c
                            WHERE c ->> 'blockId' = b.item ->> 'id'), '[]'::jsonb) AS competency_ids
                    FROM matrix_versions v
                    JOIN competency_matrices m ON m.id = v.matrix_id
                    CROSS JOIN LATERAL jsonb_array_elements(COALESCE(v.content -> 'blocks', '[]'::jsonb)) WITH ORDINALITY AS b(item, position);

                    CREATE TEMP TABLE migration_block_targets ON COMMIT DROP AS
                    SELECT unit_id, name, competency_ids,
                        min(created_at) AS first_at,
                        (array_agg(author_id ORDER BY created_at, version_id))[1] AS author_id,
                        bool_or(status IN ('Published', 'Archived')) AS is_public,
                        gen_random_uuid() AS block_id,
                        NULL::text AS block_name
                    FROM migration_own_blocks
                    GROUP BY unit_id, name, competency_ids;

                    CREATE FUNCTION pg_temp.migration_block_name(name text, unit text, suffix text) RETURNS text
                    LANGUAGE plpgsql AS $fit$
                    DECLARE
                        room integer;
                    BEGIN
                        IF unit IS NULL THEN
                            room := 200 - char_length(suffix);
                            RETURN CASE WHEN char_length(name) <= room THEN name ELSE left(name, room - 1) || '…' END || suffix;
                        END IF;

                        room := 200 - char_length(name) - 3 - char_length(suffix);
                        IF char_length(unit) > room THEN
                            unit := CASE WHEN room >= 2 THEN left(unit, room - 1) || '…' ELSE '…' END;
                        END IF;

                        room := 200 - 3 - char_length(unit) - char_length(suffix);
                        IF char_length(name) > room THEN
                            name := left(name, room - 1) || '…';
                        END IF;

                        RETURN name || ' (' || unit || ')' || suffix;
                    END
                    $fit$;

                    FOR block_target IN
                        SELECT * FROM migration_block_targets ORDER BY first_at, unit_id, name, competency_ids::text
                    LOOP
                        unit_name := NULL;
                        IF EXISTS (
                                SELECT 1 FROM migration_block_targets other
                                WHERE other.block_id <> block_target.block_id AND lower(other.name) = lower(block_target.name))
                            OR EXISTS (SELECT 1 FROM competency_blocks existing WHERE lower(existing.name) = lower(block_target.name)) THEN
                            SELECT u.name INTO unit_name FROM org_units u WHERE u.id = block_target.unit_id;
                        END IF;

                        suffix := 1;
                        candidate := pg_temp.migration_block_name(block_target.name, unit_name, '');
                        WHILE EXISTS (SELECT 1 FROM competency_blocks existing WHERE lower(existing.name) = lower(candidate))
                            OR EXISTS (
                                SELECT 1 FROM migration_block_targets other
                                WHERE other.block_id <> block_target.block_id
                                    AND (lower(other.block_name) = lower(candidate) OR lower(other.name) = lower(candidate)))
                        LOOP
                            suffix := suffix + 1;
                            candidate := pg_temp.migration_block_name(block_target.name, unit_name, ' ' || suffix);
                        END LOOP;

                        UPDATE migration_block_targets SET block_name = candidate WHERE block_id = block_target.block_id;
                    END LOOP;

                    INSERT INTO competency_blocks (id, version, created_at, name, status, author_id, scope_unit_id, competency_ids)
                    SELECT block_id, 0, now(), block_name, CASE WHEN is_public THEN 'Active' ELSE 'Draft' END, author_id, unit_id, competency_ids
                    FROM migration_block_targets;

                    CREATE TEMP TABLE migration_block_map ON COMMIT DROP AS
                    SELECT own.version_id, own.own_block_id, own.name, own.position, own.competency_ids, block.block_id
                    FROM migration_own_blocks own
                    JOIN migration_block_targets block
                        ON block.unit_id = own.unit_id AND block.name = own.name AND block.competency_ids = own.competency_ids;

                    CREATE FUNCTION pg_temp.migration_without_own_blocks(document jsonb, mapping jsonb) RETURNS jsonb
                    LANGUAGE sql AS $rewrite$
                        WITH own AS (
                            SELECT key AS own_id, value AS target FROM jsonb_each(COALESCE(mapping, '{}'::jsonb))
                        ),
                        items AS (
                            SELECT c.item, c.position,
                                (SELECT o.target ->> 'blockId' FROM own o
                                 WHERE o.own_id = c.item ->> 'blockId'
                                     AND jsonb_typeof(o.target) = 'object'
                                     AND (o.target -> 'competencyIds') @> jsonb_build_array(c.item -> 'competencyId')) AS shared_id
                            FROM jsonb_array_elements(COALESCE(document -> 'competencies', '[]'::jsonb)) WITH ORDINALITY AS c(item, position)
                        ),
                        refs AS (
                            SELECT o.target ->> 'blockId' AS block_id, min((o.target ->> 'position')::bigint) AS first_position
                            FROM own o
                            WHERE jsonb_typeof(o.target) = 'object'
                            GROUP BY o.target ->> 'blockId'
                        ),
                        weights AS (
                            SELECT w.item ->> 'levelId' AS level_id,
                                CASE
                                    WHEN jsonb_typeof(w.item -> 'blockId') IS DISTINCT FROM 'string' THEN NULL
                                    WHEN NOT jsonb_exists(COALESCE(mapping, '{}'::jsonb), w.item ->> 'blockId') THEN w.item ->> 'blockId'
                                    ELSE (SELECT o.target ->> 'blockId' FROM own o WHERE o.own_id = w.item ->> 'blockId')
                                END AS group_id,
                                (w.item ->> 'percent')::numeric AS percent,
                                w.position
                            FROM jsonb_array_elements(COALESCE(document -> 'blockWeights', '[]'::jsonb)) WITH ORDINALITY AS w(item, position)
                        )
                        SELECT (document - 'blocks')
                            || jsonb_build_object('competencies', COALESCE((
                                SELECT jsonb_agg(jsonb_set(i.item, '{blockId}', 'null'::jsonb) ORDER BY i.position)
                                FROM items i WHERE i.shared_id IS NULL), '[]'::jsonb))
                            || jsonb_build_object('sharedBlocks', COALESCE(document -> 'sharedBlocks', '[]'::jsonb) || COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'blockId', r.block_id,
                                    'competencyTypes', COALESCE((
                                        SELECT jsonb_agg(jsonb_build_object(
                                            'competencyId', i.item ->> 'competencyId',
                                            'type', COALESCE(i.item ->> 'type', 'Profile')) ORDER BY i.position)
                                        FROM items i WHERE i.shared_id = r.block_id), '[]'::jsonb)) ORDER BY r.first_position)
                                FROM refs r), '[]'::jsonb))
                            || jsonb_build_object('blockWeights', COALESCE((
                                SELECT jsonb_agg(jsonb_build_object('levelId', g.level_id, 'blockId', g.group_id, 'percent', g.percent) ORDER BY g.first_position)
                                FROM (
                                    SELECT level_id, group_id, sum(percent) AS percent, min(position) AS first_position
                                    FROM weights
                                    GROUP BY level_id, group_id) AS g), '[]'::jsonb));
                    $rewrite$;

                    UPDATE matrix_versions v
                    SET content = pg_temp.migration_without_own_blocks(v.content, (
                        SELECT jsonb_object_agg(map.own_block_id, jsonb_build_object(
                            'blockId', map.block_id, 'competencyIds', map.competency_ids, 'position', map.position))
                        FROM migration_block_map map
                        WHERE map.version_id = v.id))
                    WHERE jsonb_typeof(v.content -> 'blocks') = 'array' AND jsonb_array_length(v.content -> 'blocks') > 0;

                    UPDATE matrix_version_variants variant
                    SET content = pg_temp.migration_without_own_blocks(variant.content, (
                        SELECT jsonb_object_agg(b.item ->> 'id', (
                            SELECT jsonb_build_object('blockId', map.block_id, 'competencyIds', map.competency_ids, 'position', b.position)
                            FROM migration_block_map map
                            WHERE map.version_id = variant.version_id AND map.name = btrim(b.item ->> 'name')
                            ORDER BY map.position
                            LIMIT 1))
                        FROM jsonb_array_elements(variant.content -> 'blocks') WITH ORDINALITY AS b(item, position)))
                    WHERE jsonb_typeof(variant.content -> 'blocks') = 'array' AND jsonb_array_length(variant.content -> 'blocks') > 0;

                    UPDATE matrix_versions v
                    SET snapshot = jsonb_set(v.snapshot, '{sharedBlocks}', COALESCE(v.snapshot -> 'sharedBlocks', '[]'::jsonb) || (
                        SELECT jsonb_agg(jsonb_build_object(
                            'id', added.block_id, 'name', added.name, 'status', 'Active', 'competencyIds', added.competency_ids) ORDER BY added.position)
                        FROM (
                            SELECT DISTINCT ON (map.block_id) map.block_id, map.name, map.competency_ids, map.position
                            FROM migration_block_map map
                            WHERE map.version_id = v.id
                            ORDER BY map.block_id, map.position) AS added))
                    WHERE v.snapshot IS NOT NULL
                        AND v.status IN ('Published', 'Archived')
                        AND EXISTS (SELECT 1 FROM migration_block_map map WHERE map.version_id = v.id);

                    CREATE FUNCTION pg_temp.migration_scores_only(document jsonb) RETURNS jsonb
                    LANGUAGE sql AS $strip$
                        SELECT (document - 'blocks')
                            || jsonb_build_object('sharedBlocks', COALESCE((
                                SELECT jsonb_agg((s.item - 'isMandatory') ORDER BY s.position)
                                FROM jsonb_array_elements(COALESCE(document -> 'sharedBlocks', '[]'::jsonb)) WITH ORDINALITY AS s(item, position)), '[]'::jsonb))
                            || jsonb_build_object('blockWeights', COALESCE((
                                SELECT jsonb_agg((w.item - 'minPercent') ORDER BY w.position)
                                FROM jsonb_array_elements(COALESCE(document -> 'blockWeights', '[]'::jsonb)) WITH ORDINALITY AS w(item, position)), '[]'::jsonb))
                            || jsonb_build_object('requirements', COALESCE((
                                SELECT jsonb_agg(jsonb_build_object(
                                    'levelId', r.item -> 'levelId',
                                    'competencyId', r.item -> 'competencyId',
                                    'requiredScore', r.item -> 'requiredScore') ORDER BY r.position)
                                FROM jsonb_array_elements(COALESCE(document -> 'requirements', '[]'::jsonb)) WITH ORDINALITY AS r(item, position)
                                WHERE jsonb_typeof(r.item -> 'requiredScore') = 'number'), '[]'::jsonb));
                    $strip$;

                    UPDATE matrix_versions
                    SET content = pg_temp.migration_scores_only(content)
                    WHERE jsonb_path_exists(content, '$.blocks[*]')
                        OR jsonb_path_exists(content, '$.sharedBlocks[*] ? (exists(@.isMandatory))')
                        OR jsonb_path_exists(content, '$.blockWeights[*] ? (@.minPercent != null)')
                        OR jsonb_path_exists(content, '$.requirements[*] ? (@.requiredScore == null || @.weight != null || @.isMandatory != null
                            || @.expectedBehavior != null || @.evidenceExamples != null || @.validFrom != null || @.type != null)');

                    UPDATE matrix_version_variants
                    SET content = pg_temp.migration_scores_only(content)
                    WHERE jsonb_path_exists(content, '$.blocks[*]')
                        OR jsonb_path_exists(content, '$.sharedBlocks[*] ? (exists(@.isMandatory))')
                        OR jsonb_path_exists(content, '$.blockWeights[*] ? (@.minPercent != null)')
                        OR jsonb_path_exists(content, '$.requirements[*] ? (@.requiredScore == null || @.weight != null || @.isMandatory != null
                            || @.expectedBehavior != null || @.evidenceExamples != null || @.validFrom != null || @.type != null)');

                    DROP FUNCTION pg_temp.migration_without_own_blocks(jsonb, jsonb);
                    DROP FUNCTION pg_temp.migration_scores_only(jsonb);
                    DROP FUNCTION pg_temp.migration_block_name(text, text, text);
                END
                $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "evidence_examples",
                table: "competency_versions");

            migrationBuilder.DropColumn(
                name: "evidence_examples",
                table: "competencies");

            migrationBuilder.Sql("""
                UPDATE matrix_versions SET content = jsonb_set(content, '{blocks}', '[]'::jsonb) WHERE NOT jsonb_exists(content, 'blocks');
                UPDATE matrix_version_variants SET content = jsonb_set(content, '{blocks}', '[]'::jsonb) WHERE NOT jsonb_exists(content, 'blocks');
                """);
        }
    }
}
