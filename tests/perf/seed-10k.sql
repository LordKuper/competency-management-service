-- Performance seed: an organization of 10 000 working employees for measuring the desk and later per-request computations.
--
-- Run once against a migrated database that has no organization units yet, after the API has started against it once,
-- so the bootstrap administrator exists:
--     psql "<connection string>" -v ON_ERROR_STOP=1 -f tests/perf/seed-10k.sql
-- Every seeded account that can sign in shares the bootstrap administrator's password.
--
-- Accounts to measure with:
--     global administrator                  the bootstrap administrator
--     HR assigned to the root unit          hr.root@seed.local
--     head of the top unit                  head.1@seed.local
--     employee without process roles        employee@seed.local
--
-- Contents: 1 000 units up to 7 levels deep, 400 positions with unit lists, 10 000 working and 200 dismissed employees,
-- about 350 accounts (some invited with expired or expiring links, some blocked), HR assigned to the root and to 50 units,
-- 60 tracks of 0 to 6 levels with positions linked in part, 500 matrices with published and in-work versions and their
-- decisions, 1 500 competencies with their history, and drafts of every catalogue. Identifiers are md5 hashes of a kind and a
-- number, so two runs give the same data.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM users u
        JOIN user_roles ur ON ur.user_id = u.id
        JOIN roles r ON r.id = ur.role_id
        WHERE r.name = 'GlobalAdmin' AND NOT u.is_blocked AND u.password_hash IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'No active administrator: start the API against this database once before seeding.';
    END IF;

    IF EXISTS (SELECT 1 FROM org_units) THEN
        RAISE EXCEPTION 'The database already has organization units: the seed expects an empty organization.';
    END IF;
END $$;

CREATE FUNCTION pg_temp.sid(kind text, n integer) RETURNS uuid
    LANGUAGE sql IMMUTABLE AS $$ SELECT md5(kind || ':' || n)::uuid $$;

CREATE FUNCTION pg_temp.parent_of(n integer) RETURNS integer
    LANGUAGE sql IMMUTABLE AS $$ SELECT CASE WHEN n > 1 THEN (n - 2) / 3 + 1 END $$;

-- Positions: every 50th is inactive.
INSERT INTO positions (id, version, created_at, name, is_active)
SELECT pg_temp.sid('position', n), 0, now() - interval '400 days', 'Должность ' || n, n % 50 <> 0
FROM generate_series(1, 400) AS n;

-- Units: a ternary tree, 7 levels deep; a few of the empty leaves are inactive.
INSERT INTO org_units (id, version, created_at, name, parent_id, head_employee_id, is_active)
SELECT pg_temp.sid('unit', n), 0, now() - interval '400 days', 'Подразделение ' || n, pg_temp.sid('unit', pg_temp.parent_of(n)), NULL,
       NOT (n > 950 AND n % 7 = 0)
FROM generate_series(1, 1000) AS n
ORDER BY n;

-- Position lists: every unit but each tenth lists one or two positions; the root lists five more.
INSERT INTO org_unit_positions (org_unit_id, position_id)
SELECT pg_temp.sid('unit', u), pg_temp.sid('position', p)
FROM generate_series(1, 1000) AS u
CROSS JOIN LATERAL (VALUES (u * 7 % 400 + 1), (CASE WHEN u % 3 = 0 THEN u * 13 % 400 + 1 END)) AS list(p)
WHERE p IS NOT NULL AND u % 10 <> 0
UNION
SELECT pg_temp.sid('unit', 1), pg_temp.sid('position', p)
FROM generate_series(1, 5) AS p;

-- Employees: units 951 to 1 000 stay empty; a position comes from the unit's list, else its parent's, else the root's;
-- every 211th employee holds a position outside the lists; employees above 10 000 are dismissed.
INSERT INTO employees (id, version, created_at, last_name, first_name, middle_name, is_active, org_unit_id, position_id)
SELECT pg_temp.sid('employee', e), 0, now() - interval '300 days', 'Сотрудников', 'Сотрудник' || e, 'Сидорович', e <= 10000,
       pg_temp.sid('unit', place.u),
       pg_temp.sid('position', CASE
           WHEN e % 211 = 0 THEN e % 400 + 1
           WHEN place.u % 10 <> 0 THEN place.u * 7 % 400 + 1
           WHEN pg_temp.parent_of(place.u) % 10 <> 0 THEN pg_temp.parent_of(place.u) * 7 % 400 + 1
           ELSE 1
       END)
FROM generate_series(1, 10200) AS e
CROSS JOIN LATERAL (SELECT (e - 1) % 950 + 1 AS u) AS place;

-- Heads: employee n heads unit n; every 41st unit and the empty units have no head.
UPDATE org_units
SET head_employee_id = heads.employee_id
FROM (
    SELECT pg_temp.sid('unit', n) AS unit_id, pg_temp.sid('employee', n) AS employee_id
    FROM generate_series(1, 950) AS n
    WHERE n % 41 <> 0
) AS heads
WHERE org_units.id = heads.unit_id;

-- Accounts.
CREATE TEMP TABLE seed_accounts (n integer PRIMARY KEY, email text NOT NULL, employee_no integer, state text NOT NULL, link_expires_at timestamptz);

INSERT INTO seed_accounts VALUES
    (1, 'hr.root@seed.local', 9999, 'active', NULL),
    (2, 'employee@seed.local', 9998, 'active', NULL);

INSERT INTO seed_accounts
SELECT 100 + n, 'head.' || n || '@seed.local', n,
       CASE WHEN n % 23 = 0 THEN 'blocked' WHEN n % 17 = 0 THEN 'invited' ELSE 'active' END,
       CASE WHEN n % 23 <> 0 AND n % 17 = 0 THEN now() - interval '1 day' END
FROM generate_series(1, 250) AS n;

INSERT INTO seed_accounts
SELECT 400 + k, 'hr.' || k || '@seed.local', 9000 + k,
       CASE WHEN k % 10 = 0 THEN 'blocked' ELSE 'active' END, NULL
FROM generate_series(1, 50) AS k;

INSERT INTO seed_accounts
SELECT 500 + e - 5000, 'user.' || e || '@seed.local', e,
       CASE WHEN e % 7 = 0 THEN 'invited' ELSE 'active' END,
       CASE WHEN e % 7 = 0 THEN now() + interval '1 day' END
FROM generate_series(5001, 5040) AS e;

INSERT INTO seed_accounts
SELECT 600 + n, 'orphan.' || n || '@seed.local', NULL, 'active', NULL
FROM generate_series(1, 5) AS n;

INSERT INTO users (
    id, version, email, normalized_email, user_name, normalized_user_name, employee_id, is_blocked, password_hash,
    security_stamp, concurrency_stamp, lockout_enabled, access_failed_count,
    link_token_hash, link_security_stamp, link_issued_at, link_expires_at)
SELECT pg_temp.sid('account', a.n), 0, a.email, upper(a.email), a.email, upper(a.email),
       CASE WHEN a.employee_no IS NOT NULL THEN pg_temp.sid('employee', a.employee_no) END,
       a.state = 'blocked',
       CASE WHEN a.state <> 'invited' THEN admin.password_hash END,
       md5('stamp:' || a.email), md5('concurrency:' || a.email), true, 0,
       CASE WHEN a.state = 'invited' THEN decode(md5('link:' || a.email), 'hex') END,
       CASE WHEN a.state = 'invited' THEN md5('stamp:' || a.email) END,
       a.link_expires_at - interval '7 days',
       a.link_expires_at
FROM seed_accounts AS a
CROSS JOIN (
    SELECT u.password_hash FROM users u
    JOIN user_roles ur ON ur.user_id = u.id
    JOIN roles r ON r.id = ur.role_id
    WHERE r.name = 'GlobalAdmin' AND NOT u.is_blocked AND u.password_hash IS NOT NULL
    ORDER BY u.email
    LIMIT 1
) AS admin;

-- The HR role: the accounts of the employees assigned below carry it.
INSERT INTO user_roles (user_id, role_id)
SELECT pg_temp.sid('account', a.n), r.id
FROM seed_accounts AS a
CROSS JOIN roles AS r
WHERE r.name = 'Hr' AND (a.n = 1 OR a.n BETWEEN 401 AND 450);

-- HR assignments by employee: the root and 50 units below it, every tenth of the accounts of those employees blocked.
INSERT INTO org_unit_hr (org_unit_id, employee_id)
SELECT pg_temp.sid('unit', 1), pg_temp.sid('employee', 9999)
UNION ALL
SELECT pg_temp.sid('unit', 3 * k + 2), pg_temp.sid('employee', 9000 + k)
FROM generate_series(1, 50) AS k;

-- Directions: ten active (the tenth without tracks), one inactive, two drafts of unit heads.
INSERT INTO career_directions (id, version, created_at, name, status, author_id)
SELECT pg_temp.sid('direction', n), 0, now() - interval '200 days', 'Направление ' || n,
       CASE WHEN n <= 10 THEN 'Active' WHEN n = 11 THEN 'Inactive' ELSE 'Draft' END,
       CASE WHEN n > 11 THEN pg_temp.sid('account', 100 + n) END
FROM generate_series(1, 13) AS n;

-- Tracks: track t belongs to unit t; every fifteenth is a draft of its unit head, track 59 is inactive.
INSERT INTO career_tracks (id, version, created_at, name, owner_org_unit_id, direction_id, status, author_id)
SELECT pg_temp.sid('track', t), 0, now() - interval '150 days' + t * interval '1 hour', 'Трек ' || t, pg_temp.sid('unit', t),
       pg_temp.sid('direction', t % 9 + 1),
       CASE WHEN t % 15 = 0 THEN 'Draft' WHEN t = 59 THEN 'Inactive' ELSE 'Active' END,
       CASE WHEN t % 15 = 0 THEN pg_temp.sid('account', 100 + t) END
FROM generate_series(1, 60) AS t;

-- Levels: 3 to 6 per track; tracks 20 and 40 have one level, tracks 45 and 50 none.
CREATE TEMP TABLE seed_levels AS
SELECT t AS track_no, k
FROM generate_series(1, 60) AS t
CROSS JOIN LATERAL generate_series(1, CASE WHEN t IN (45, 50) THEN 0 WHEN t IN (20, 40) THEN 1 ELSE 3 + t % 4 END) AS k;

INSERT INTO career_levels (id, version, created_at, name, position, status, track_id, author_id)
SELECT pg_temp.sid('level', track_no * 10 + k), 0, now() - interval '150 days', 'Уровень ' || k, k,
       CASE WHEN track_no % 15 = 0 THEN 'Draft' ELSE 'Active' END,
       pg_temp.sid('track', track_no),
       CASE WHEN track_no % 15 = 0 THEN pg_temp.sid('account', 100 + track_no) END
FROM seed_levels;

-- Links: most levels stand for one position, every fourth for none; the first level also stands for the first position of the track's unit.
INSERT INTO career_level_positions (level_id, position_id, track_id)
SELECT pg_temp.sid('level', links.track_no * 10 + links.k), pg_temp.sid('position', links.p), pg_temp.sid('track', links.track_no)
FROM (
    SELECT track_no, k, (track_no * 11 + k * 17) % 400 + 1 AS p FROM seed_levels WHERE (track_no + k) % 4 <> 0
    UNION ALL
    SELECT track_no, k, track_no * 7 % 400 + 1 FROM seed_levels WHERE k = 1
) AS links
ON CONFLICT DO NOTHING;

-- Scales beyond the initial one: an unused active scale and a draft.
INSERT INTO competency_scales (id, version, created_at, name, points, is_default, status, author_id)
VALUES
    (pg_temp.sid('scale', 1), 0, now() - interval '100 days', 'Шкала 1–5',
     '[{"score":1,"label":"Начальный"},{"score":3,"label":"Средний"},{"score":5,"label":"Высокий"}]', false, 'Active', NULL),
    (pg_temp.sid('scale', 2), 0, now() - interval '3 days', 'Черновая шкала',
     '[{"score":0,"label":"Нет"},{"score":1,"label":"Да"}]', false, 'Draft', pg_temp.sid('account', 104));

-- Competencies: every third is general, the rest belong to a direction; every hundredth is inactive, ten are drafts of unit
-- heads, and every 37th has been changed once, so matrices published with its first version see it changed.
INSERT INTO competencies (id, version, created_at, name, description, direction_id, scale_id, status, author_id)
SELECT pg_temp.sid('competency', n), CASE WHEN n % 37 = 0 THEN 1 ELSE 0 END, now() - interval '120 days',
       'Компетенция ' || n, 'Описание компетенции ' || n,
       CASE WHEN n % 3 <> 0 THEN pg_temp.sid('direction', n % 9 + 1) END,
       initial.scale_id,
       CASE WHEN n % 150 = 7 THEN 'Draft' WHEN n % 100 = 0 THEN 'Inactive' ELSE 'Active' END,
       CASE WHEN n % 150 = 7 THEN pg_temp.sid('account', 100 + n % 50 + 1) END
FROM generate_series(1, 1500) AS n
CROSS JOIN (
    SELECT (SELECT id FROM competency_scales WHERE is_default) AS scale_id
) AS initial;

INSERT INTO competency_versions (competency_id, number, name, description, direction_id, scale_id, status, changed_by_id, changed_at)
SELECT c.id, history.number, c.name, c.description, c.direction_id, c.scale_id, c.status, c.author_id,
       c.created_at + (history.number - 1) * interval '60 days'
FROM competencies AS c
CROSS JOIN LATERAL generate_series(1, c.version + 1) AS history(number);

-- Matrices: units 1 to 625 except every fifth, each on the track of its own unit or the nearest unit above with a usable track.
CREATE TEMP TABLE seed_matrices AS
WITH RECURSIVE chain(u, a) AS (
    SELECT n, n FROM generate_series(1, 625) AS n WHERE n % 5 <> 4
    UNION ALL
    SELECT chain.u, pg_temp.parent_of(chain.a)
    FROM chain
    WHERE chain.a > 1 AND NOT (chain.a <= 60 AND chain.a NOT IN (45, 50) AND (chain.a % 15 <> 0 OR chain.a = chain.u))
)
SELECT u, a AS t
FROM chain
WHERE a <= 60 AND a NOT IN (45, 50) AND (a % 15 <> 0 OR a = u);

INSERT INTO competency_matrices (id, version, created_at, org_unit_id, direction_id, track_id)
SELECT pg_temp.sid('matrix', u), 0, now() - interval '90 days', pg_temp.sid('unit', u), pg_temp.sid('direction', t % 9 + 1), pg_temp.sid('track', t)
FROM seed_matrices;

-- Versions: three matrices in five have a published version; one of those and one more have a version in work, as a draft,
-- on review at either step, or returned; matrices on a draft track have only a version in work.
CREATE TEMP TABLE seed_versions AS
SELECT pg_temp.sid('version', u * 10 + number) AS id, u, t, number, status, step
FROM (
    SELECT u, t, 1 AS number, 'Published' AS status, NULL::text AS step
    FROM seed_matrices
    WHERE u % 5 IN (0, 1, 2) AND t % 15 <> 0
    UNION ALL
    SELECT u, t, CASE WHEN u % 5 = 1 AND t % 15 <> 0 THEN 2 ELSE 1 END,
           (ARRAY['Draft', 'OnReview', 'OnReview', 'Returned'])[u / 5 % 4 + 1],
           (ARRAY[NULL, 'Superior', 'Hr', 'Hr']::text[])[u / 5 % 4 + 1]
    FROM seed_matrices
    WHERE u % 5 IN (1, 3) OR t % 15 = 0
) AS planned;

CREATE FUNCTION pg_temp.matrix_competency(u integer, j integer) RETURNS uuid
    LANGUAGE sql IMMUTABLE AS $$ SELECT pg_temp.sid('competency', 3 * ((u * 5 + j) % 500) + 3) $$;

CREATE FUNCTION pg_temp.matrix_type(j integer) RETURNS text
    LANGUAGE sql IMMUTABLE AS $$ SELECT (ARRAY['Critical', 'Profile', 'Additional'])[j % 3 + 1] $$;

-- Five competencies outside blocks, each typed in the version; one group of them weighs 100 % on every level.
CREATE FUNCTION pg_temp.matrix_content(u integer, t integer) RETURNS jsonb
    LANGUAGE sql STABLE AS $$
    SELECT jsonb_build_object(
        'levels', COALESCE((
            SELECT jsonb_agg(jsonb_build_object('levelId', pg_temp.sid('level', t * 10 + k), 'minOverallPercent', 60) ORDER BY k)
            FROM seed_levels WHERE track_no = t), '[]'::jsonb),
        'competencies', (
            SELECT jsonb_agg(jsonb_build_object('competencyId', pg_temp.matrix_competency(u, j), 'blockId', NULL, 'type', pg_temp.matrix_type(j)) ORDER BY j)
            FROM generate_series(0, 4) AS j),
        'blockWeights', COALESCE((
            SELECT jsonb_agg(jsonb_build_object('levelId', pg_temp.sid('level', t * 10 + k), 'blockId', NULL, 'percent', 100) ORDER BY k)
            FROM seed_levels WHERE track_no = t), '[]'::jsonb),
        'requirements', COALESCE((
            SELECT jsonb_agg(jsonb_build_object('levelId', pg_temp.sid('level', t * 10 + k), 'competencyId', pg_temp.matrix_competency(u, j), 'requiredScore', least(k, 4)) ORDER BY k, j)
            FROM seed_levels CROSS JOIN generate_series(0, 4) AS j WHERE track_no = t), '[]'::jsonb),
        'sharedBlocks', '[]'::jsonb)
$$;

-- The snapshot of a published version; on every seventh track it misses the last level, so the track looks changed since.
CREATE FUNCTION pg_temp.matrix_snapshot(u integer, t integer) RETURNS jsonb
    LANGUAGE sql STABLE AS $$
    SELECT jsonb_build_object(
        'direction', (SELECT jsonb_build_object('id', id, 'name', name, 'status', status) FROM career_directions WHERE id = pg_temp.sid('direction', t % 9 + 1)),
        'track', (SELECT jsonb_build_object('id', id, 'name', name, 'status', 'Active') FROM career_tracks WHERE id = pg_temp.sid('track', t)),
        'scales', (SELECT jsonb_agg(jsonb_build_object('id', id, 'name', name, 'points', points, 'status', status)) FROM competency_scales WHERE is_default),
        'levels', COALESCE((
            SELECT jsonb_agg(jsonb_build_object('id', pg_temp.sid('level', t * 10 + k), 'name', 'Уровень ' || k, 'status', 'Active') ORDER BY k)
            FROM seed_levels
            WHERE track_no = t AND NOT (t % 7 = 0 AND k = (SELECT max(k) FROM seed_levels WHERE track_no = t))), '[]'::jsonb),
        'competencies', (
            SELECT jsonb_agg(jsonb_build_object(
                'id', c.id, 'name', c.name, 'description', c.description, 'directionId', c.direction_id,
                'type', pg_temp.matrix_type(j), 'versionNumber', 1, 'status', 'Active', 'scaleId', c.scale_id) ORDER BY j)
            FROM generate_series(0, 4) AS j
            JOIN competencies AS c ON c.id = pg_temp.matrix_competency(u, j)),
        'sharedBlocks', '[]'::jsonb)
$$;

INSERT INTO matrix_versions (id, version, created_at, matrix_id, number, status, step, author_id, content, snapshot, published_at, builder_step)
SELECT v.id, 0, now() - interval '80 days' + v.number * interval '30 days', pg_temp.sid('matrix', v.u), v.number, v.status, v.step,
       CASE WHEN v.u <= 250 THEN pg_temp.sid('account', 100 + v.u) ELSE pg_temp.sid('account', 1) END,
       pg_temp.matrix_content(v.u, v.t),
       CASE WHEN v.status = 'Published' THEN pg_temp.matrix_snapshot(v.u, v.t) END,
       CASE WHEN v.status = 'Published' THEN now() - interval '60 days' END,
       CASE WHEN v.status = 'Draft' THEN 3 END
FROM seed_versions AS v;

-- History of the versions in work: submitted from 1 to 20 days ago, approved at the first step when waiting for HR,
-- returned at the HR step with a comment and, for even units, the approver's changes.
INSERT INTO matrix_version_variants (version_id, number, kind, content, created_at, created_by_id)
SELECT v.id, 1, 'Author', mv.content, now() - (v.u % 20 + 1) * interval '1 day', mv.author_id
FROM seed_versions AS v JOIN matrix_versions AS mv ON mv.id = v.id
WHERE v.status IN ('OnReview', 'Returned')
UNION ALL
SELECT v.id, 2, 'Approver', mv.content, now() - interval '12 hours', pg_temp.sid('account', 1)
FROM seed_versions AS v JOIN matrix_versions AS mv ON mv.id = v.id
WHERE v.status = 'Returned' AND v.u % 2 = 0;

INSERT INTO matrix_version_decisions (version_id, sequence, kind, step, actor_id, at, comment, variant_number)
SELECT v.id, 1, 'Submitted', 'Superior', mv.author_id, now() - (v.u % 20 + 1) * interval '1 day', NULL, 1
FROM seed_versions AS v JOIN matrix_versions AS mv ON mv.id = v.id
WHERE v.status IN ('OnReview', 'Returned')
UNION ALL
SELECT v.id, 2, 'Approved', 'Superior', pg_temp.sid('account', 1), now() - interval '1 day', NULL, NULL
FROM seed_versions AS v
WHERE (v.status = 'OnReview' AND v.step = 'Hr') OR v.status = 'Returned'
UNION ALL
SELECT v.id, 3, 'Returned', 'Hr', pg_temp.sid('account', 1), now() - interval '12 hours', 'Уточните требования к уровням',
       CASE WHEN v.u % 2 = 0 THEN 2 END
FROM seed_versions AS v
WHERE v.status = 'Returned';

ANALYZE;

COMMIT;
