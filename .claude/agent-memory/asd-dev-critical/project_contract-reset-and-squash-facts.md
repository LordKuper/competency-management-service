---
name: contract-reset-and-squash-facts
description: Sprint 004 Task 58 - contract 1.0.0 reset, required score fill, migration squash recipe, Pkcs pin, JsonRequired probe
metadata:
  type: project
---

Sprint 004 Task 58 (2026-10-21), commits 4ef9c9d, d3c3860, 451579c. Nothing was run against PostgreSQL.

- No API backward compatibility before 1.0: delete obsolete surface, never deprecate. Schema changes are incremental migrations over `20261021091246_InitialCreate`, with data transfer as hand SQL in `Up` (EF scaffolds drops first and a rename where a transfer is needed: reorder by hand, review with `ef migrations script <from> [<to>]`); the squash below runs only right before release.
- Squash recipe (release only) that worked offline next to the user's VS-locked Debug output: `git rm` every file in `src/Competency.Platform/Migrations`, then `dotnet ef migrations add InitialCreate ... --configuration Release`, then hand-append to `Up` the audit trigger function, both triggers with `ENABLE ALWAYS`, `IX_positions_name_lower` and the base-scale insert (copy the SQL from `git show HEAD~:<old file>`), and `DROP FUNCTION audit_events_reject_change();` at the end of `Down`. EF writes the new files with CRLF and a BOM; the Edit tool keeps that.
- `[property: JsonRequired] int` on a positional record parameter makes a missing field a JsonException (400), and `null` fails too. Checked with a file-based probe: `dotnet run probe.cs` needs `#:property PublishAot=false` and `#:property JsonSerializerIsReflectionEnabledByDefault=true`, or reflection serialization is off.
- Requirement fill: `MatrixContent.Filled(compositions, lowestScores)` is the one function. Its sources are `MatrixEntries.LowestScores()` (snapshot or live entries, used by `DescribeAsync` and publication) and `MatrixCatalog.FilledAsync` (the live query, used by edit, variant, `/changes` and diff). Fill first, then `Expanded`.
- Removing contract fields did not break the build of `tests/Competency.Tests`. The tests break when they run (JSON payloads, old migration names in `CareerMigrationTests`, `PositionMigrationTests`, `Infrastructure/MigrationDatabase.cs`).
- A Pkcs pin in Platform rewrites `tests/Competency.Tests/packages.lock.json` as well. That lock file sits outside the dev write scope, so flag it in the report.

**Why:** Task 59 (SPA) and impl-test build on these choices; the next schema change repeats the squash.
**How to apply:** any schema change before 1.0, any matrix document reader. See [[position-matrix-shared-block-facts]], [[matrix-workflow-facts]].
