---
name: career-desk-source-facts
description: Career desk source query budget (auth costs 2 per request), rule functions over the snapshot, HrUnitsAsync now reads the snapshot, unverified EF projections, and how the perf seed must be run
metadata:
  type: project
---

Sprint 004 Task 9 (2026-10-13), commits 075cc8d…d39ff81. Nothing was run against PostgreSQL.

- Budget is 25 SQL commands per desk request, and cookie validation already spends 2 (account, then employee). The career source spends about 19 for an HR assigned to the root (4 of them `UnusedEntriesAsync`, 4 the snapshot). Before adding a query to any desk source, count all three sources plus auth.
- `CareerDeskSource` loads the data, and `CareerDeskScan` applies the rules in memory through static functions: `ProcessRoles.HeadedUnits/HrUnits/ResponsibleHr/ActingHeadAccount`, `MatrixRoute.SuperiorApprover/Turn/ReadsAnyVersion`, `TrackReadiness.Of`, `CareerLevelPositionEndpoints.AvailablePositions`. The async action paths call the same functions.
- `ProcessRoles.HrUnitsAsync` now reads the org snapshot (4 queries) when the account has any assignment. That reaches every HR catalogue request through `CatalogViewer.LoadAsync`.
- EF translations never run: `TracksAsync` puts two correlated `.ToList()` collections in a record constructor (levels auto-include `Track`), and `VersionsAsync` projects into a record. If impl-test is red on `/desk`, suspect these first.
- "Position linked to an active level of an active track" exists twice: as SQL in `MyPlaceEndpoints.TracksAsync` and in memory in the scan. Change both together.
- `tests/perf/seed-10k.sql` needs the API to have started once, so the bootstrap admin exists. Every seeded account copies that admin's password hash. Accounts to measure with: `hr.root@`, `head.1@`, `employee@seed.local`. Ids are `md5(kind:n)::uuid`.

**Why:** the next perf Task reuses the seed, and impl-test measures the budget.
**How to apply:** read before touching a desk source, `ProcessRoles` or the seed. See [[desk-merge-contract-facts]], [[scenario-read-facts]].
