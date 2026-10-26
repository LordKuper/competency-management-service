---
name: position-catalogue-facts
description: Position catalogue - expression unique index outside the EF model, .NET Trim parity in SQL, case-insensitive lookup without .NET lowering, which tests broke on the position -> positionId rename
metadata:
  type: project
---

As of 2026-10-09, EF Core 10.0.12 / Npgsql 10.0.3, not run against PostgreSQL:

- The unique index on `lower(name)` of `positions` cannot be expressed in the EF model; it lives only in a migration's `Sql("CREATE UNIQUE INDEX ...")` (now `IX_positions_name_lower` in `InitialCreate`). `has-pending-model-changes` stays clean, and `DropTable` in Down drops it.
- SQL `btrim(s)` strips spaces only; to match .NET `Trim()` a data-transfer migration passes the `Char.IsWhiteSpace` set as a `U&'...'` literal (the original `PositionCatalogue` migration did; it was squashed into `InitialCreate`, so `git log` holds the copy). List every code point (U+2000 to U+200A one by one): `btrim`'s second argument is a plain character set, not a regex, so `\2000-\200A` would trim `-` and keep U+2001 to U+2009.
- `p.Name.ToLower() == name.ToLower()` would lower the parameter in .NET (funcletized). The lookup is `FromSql($"SELECT * FROM positions WHERE lower(name) = lower({name})")`, so both sides use PG `lower()` like the index.
- `dotnet build Competency.slnx` still compiles the test project after `position` -> `positionId` (backend tests post JSON); the web typecheck breaks in `EmployeeCards.test.tsx` and `orgTree.test.ts` (typed `Employee` fixtures without `positionId`).
- `int` response fields are plain `number` in `schema.d.ts` now (no `number | string`).

**Why:** each decided how the migration and the duplicate-name check stay consistent with the database (audit R-2).
**How to apply:** later catalogue-like tables with case-insensitive names (career framework records) and any migration that normalizes text written by .NET (see also [[employee-name-split-facts]], [[ef-raw-sql-and-openapi-contract-gotchas]]).
