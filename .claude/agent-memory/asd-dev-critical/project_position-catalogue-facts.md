---
name: position-catalogue-facts
description: Sprint 003 Task 2 (position catalogue) - expression unique index outside the EF model, .NET Trim parity in SQL, case-insensitive lookup without .NET lowering, which tests break on the 3.0.0 contract
metadata:
  type: project
---

Task 2 of sprint 003 (2026-10-09), EF Core 10.0.12 / Npgsql 10.0.3, not run against PostgreSQL:

- The unique index on `lower(name)` of `positions` cannot be expressed in the EF model; it lives only in the migration's `Sql("CREATE UNIQUE INDEX ...")`. `has-pending-model-changes` stays clean, and `DropTable` in Down drops it.
- SQL `btrim(s)` strips spaces only; to match .NET `Trim()` the migration passes the `Char.IsWhiteSpace` set as `U&' \0009\000A\000B\000C\000D\0085\00A0\1680\2000-\200A\2028\2029\202F\205F\3000'` (each code point listed). Put it in a C# verbatim `@"..."` constant and interpolate into the `$"""` SQL.
- `p.Name.ToLower() == name.ToLower()` would lower the parameter in .NET (funcletized). The lookup is `FromSql($"SELECT * FROM positions WHERE lower(name) = lower({name})")`, so both sides use PG `lower()` like the index.
- `dotnet build Competency.slnx` still compiles the test project after `position` -> `positionId` (backend tests post JSON); the web typecheck breaks in `EmployeeCards.test.tsx` and `orgTree.test.ts` (typed `Employee` fixtures without `positionId`).
- `int` response fields are plain `number` in `schema.d.ts` now (no `number | string`).

**Why:** each decided how the migration and the duplicate-name check stay consistent with the database (audit R-2).
**How to apply:** later catalogue-like tables with case-insensitive names (career framework records, Task 5) and any migration that normalizes text written by .NET (see also [[employee-name-split-facts]], [[ef-raw-sql-and-openapi-contract-gotchas]]).
