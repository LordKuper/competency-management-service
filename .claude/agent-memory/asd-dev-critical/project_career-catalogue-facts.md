---
name: career-catalogue-facts
description: Sprint 003 Task 5 career catalogues - why points are a converted jsonb string and next levels a uuid[], audit blind spot of complex types, Russian build output, untested ILIKE equality
metadata:
  type: project
---

Task 5 of sprint 003 (2026-10-09), EF Core 10.0.12 / Npgsql 10.0.3. None of it was run against PostgreSQL:

- `AuditSaveChangesInterceptor` reads `entry.Properties` only. EF complex types (`ComplexProperty(...).ToJson()`) are not in that list, so a change to them would bump `Version` but write no journal event. That is why `CompetencyScale.Points` is a `List<ScalePoint>` with a JSON value converter to `jsonb`, plus a `SequenceEqual` `ValueComparer`.
- A child link table does not bump the parent's `Version`, so `If-Match` would miss a change to the links alone. That is why `CareerLevel.NextLevelIds` is a primitive `List<Guid>` (`uuid[]`, no FK; levels are never deleted).
- An unmapped abstract base (`CatalogEntry : EntityBase`) gives each concrete type its own table. The Platform conventions (concurrency token, Guid generator) still apply, because each concrete root has `BaseType == null`. `[Audited]` on base-class properties is honoured. Never call `Set<CatalogEntry>()`.
- The case-insensitive name check is `ILIKE` with `ListRequest.ExactPattern`. It relies on PG lowering both sides on a UTF-8 database. This has not been verified against a real database (Ё/ё).
- `dotnet build` prints in Russian on this machine (`Ошибок: 0`, `Предупреждений: 0`). Grepping for `error|warning` alone matches nothing.
- Seed rows go in through `migrationBuilder.Sql` inside the migration, not through `HasData`, so the model snapshot holds no data. They use fixed GUIDs, and author null means seeded.

**Why:** these choices decide whether Task 7 matrix code keeps the journal and `If-Match` correct.
**How to apply:** when adding JSON or collection columns to audited, versioned entities (Task 7 matrix `content jsonb`, its snapshots), or when grepping build output. See also [[position-catalogue-facts]] and [[aspnet-platform-pipeline-gotchas]].
