---
name: ef-left-join-count-and-visibility-facts
description: Unit tree head name and employee count (Task 14) - EF left join over a GroupBy count needs (int?)x.Count ?? 0, correlated subqueries vs group join plans, role-visibility rule, seeded-DB parity method
metadata:
  type: project
---

Verified on EF Core 10.0.12 / Npgsql 10.0.3 / PostgreSQL 18.6 with 1000 units and 10 000 employees (scratch DB, removed):

- `GET /api/v1/org-units/tree` returns `OrgUnitTreeNodeResponse` (the six `OrgUnitResponse` fields plus `headName`, `employeeCount`); `OrgUnitResponse` stays for every other unit endpoint, so no join runs elsewhere. Rule: both extras use `employees.VisibleTo(actor)` - administrator sees every employee and any head, role User counts working employees and gets `headName` only for a working head (an active unit always has one, so null for a User means a state the app cannot create). `headEmployeeId` is still returned as before.
- LINQ shape that works: `employees.GroupBy(e => e.OrgUnitId).Select(g => new { OrgUnitId = g.Key, Count = g.Count() })`, query-syntax `join ... into x from y in x.DefaultIfEmpty()` twice (counts, head), `select new Dto(..., head.FullName, (int?)count.Count ?? 0)`. Writing `count == null ? 0 : count.Count` instead compiles and generates the same join but every request answers **500 InvalidOperationException** (ProblemExceptionHandler hides the text); `COALESCE(e0."Count", 0)` comes only from the `(int?)` form.
- Plans: correlated subqueries per unit (`Count(...)`/`FirstOrDefault()` in the projection) cost 7 ms admin / 15 ms user (user: bitmap heap scan per unit, 11k buffers); the group join costs 5.4 / 5.1 ms (one hash aggregate over employees, hash join for the head), baseline without extras 4.5 ms. HTTP p50/p95 for 60 calls were 9.9/11.5 ms (admin) and 11.0/12.2 ms (user) with the extras vs 10.1/11.9 and 10.7/12.4 without: not measurable.
- Parity method: seed with `generate_series` and `md5('u'||n)::uuid` ids (deterministic, joinable from SQL), an inactive-leaf rule so active units never have inactive ancestors, a few deliberately invalid active units with non-working heads to exercise the User filter, a User account created through `POST /api/v1/users` (role `User`, `employeeId`) and logins through `/auth/login` with `Sec-Fetch-Site: same-origin`; then a node script takes the `.competency.auth` cookie from the curl jar (`#HttpOnly_` prefix, tab-separated, fields 5/6) and compares `employeeCount` with `GET /employees?orgUnitId=..&includeDescendants=false&pageSize=1` `total` and `headName` with `GET /employees/{id}` for every unit.
- A positional record with `<param>` docs for only some parameters fails the build (CS1573 under TreatWarningsAsErrors); document such a record in its type summary only.

**Why:** each cost a failed run or a decision in Task 14.
**How to apply:** any aggregate added to a list endpoint, or a before/after check of role-dependent counts (see also [[npgsql-search-and-result-type-gotchas]], [[scratch-host-without-postgres]], [[vs-debug-build-lock-and-release-config]]).
