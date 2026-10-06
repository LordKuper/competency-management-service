---
name: pg-behaviour-facts
description: PostgreSQL 18.6 behaviours confirmed on the real image that matter when changing queries, errors or migrations here - SQLSTATE of RESTRICT, ё/е in ILIKE vs FTS, stemming, superuser trigger bypass
metadata:
  type: project
---

Confirmed on `postgres:18.6-trixie` (en_US.utf8, libc provider) during the Task 10 spike; full detail in the postgres-image tech-reference note.

- `FOREIGN KEY ... ON DELETE RESTRICT` raises SQLSTATE **23001** (restrict_violation), the same code as the audit append-only trigger. Only the child-side check raises 23503. `ProblemExceptionHandler` maps 23505 and 23503 to 409 and nothing else, so a future delete/update-of-key endpoint would surface RESTRICT as 500 until 23001 is mapped; today there is no such endpoint.
- `ILIKE` folds Cyrillic case but treats `ё` and `е` as different letters; the `russian` FTS config maps `ё` to `е` and strips endings (`Петров`->`петр`, `Семёнов`->`семен`), and stems the query too. So `q=семенов` finds Семёнов only through FTS and `q=семен` finds nothing; any "search must find X" expectation has to be checked against both paths. `plainto_tsquery` is safe for arbitrary text.
- `ILIKE ... ESCAPE '\'` still uses the `gin_trgm_ops` index, also inside a BitmapOr with the tsvector index.
- The default cluster collation sorts Cyrillic correctly (`е` < `ё` < `ж`); no ICU column collation is needed. `SHOW lc_collate` is not available in 18 (use `pg_database`). `default_text_search_config` is `english`, so `'russian'` must always be passed.
- The cluster superuser (`POSTGRES_USER`, which the app uses in the manifests) bypasses the audit trigger with `SET session_replication_role = replica`; a non-superuser database owner cannot, and `pg_trgm` is trusted so such an owner can run the migrations.
- The DB accepts `org_units.parent_id = id` (no CHECK); acyclicity is enforced only by the app under `pg_advisory_xact_lock`.
- Unhandled startup exception exits the container with 139 on Docker Desktop/WSL2, not 134.

**Why:** these surprised or would surprise a later change; each was observed, not assumed.
**How to apply:** touching search, error mapping, delete/move endpoints, audit trigger or the DB role model.
