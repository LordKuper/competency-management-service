---
name: scratch-host-without-postgres
description: How to exercise module endpoints without PostgreSQL - InMemory with stubbed raw-SQL helpers for logic, Npgsql with a fake DbCommand for SQL capture including transactions
metadata:
  type: project
---

Two scratch hosts outside the repo (project reference to Platform, `<Compile Include>` of the module sources) covered a whole module:

- **Logic host (EF InMemory)**: replace the registered `AppDbContext` options (see [[aspnet-platform-pipeline-gotchas]]), exclude the file holding raw SQL helpers and compile a stub with the same signatures (no-op `IDbContextTransaction`, in-memory BFS that returns an EF queryable of ids so `AnyAsync`/`Contains` still compose). InMemory cannot map `NpgsqlTsVector` (ignore the property from a scratch `IEntityConfigurationContributor` registered after the module) and does not generate computed columns (a scratch `SaveChangesInterceptor` fills them). Real HTTP over Kestrel on port 0 with a header-driven test auth handler; ~120 checks covered 401/403/404/409/412/428, projections, audit rows and version bumps.
- **SQL host (Npgsql provider)**: `ConnectionOpening` suppressed; `NpgsqlTransaction` is sealed with an internal constructor and `NpgsqlCommand.Transaction` casts, so a fake `DbTransaction` fails. Return a custom `DbCommand` from `IDbCommandInterceptor.CommandCreating` (wrap a detached `NpgsqlCommand` for its parameter collection and `CreateParameter`), fake the transaction from `TransactionStarting`, and answer `ReaderExecuting` with `DataTable` readers built from the SELECT list (empty, a count row, an EXISTS row, or a fixture row, `RETURNING` lists answered with one row). Reads then answer 200 and every INSERT/UPDATE/CTE statement is printed; write requests end 500 at batch consumption, after the SQL is captured.

**Why:** with no Docker or PostgreSQL the only evidence for query translation and invariant logic is the generated SQL and these runs; building the harness took the longest part of the verification.
**How to apply:** any later task needing endpoint or SQL evidence before impl-test has a real database.
