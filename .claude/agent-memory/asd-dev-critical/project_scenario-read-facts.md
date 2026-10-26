---
name: scenario-read-facts
description: Career scenario reads - where the shared rules live for desk sources (nearest HR, unused entries, account state), choices behind setup/my-place/entry matrices, raw SQL never run on PostgreSQL
metadata:
  type: project
---

As of 2026-10-13 nothing was run against PostgreSQL:

- Shared rules for desk sources: `ProcessRoles.NearestAssigned(ancestry, assigneeOf)` (pure, works on snapshot ancestry) plus `ProcessRoles.AssigneesAsync(unitIds)` (one query); `ResponsibleHrAsync` is built from them. "Can the head's account act" and "superior approver" were NOT extracted as pure functions yet.
- `CatalogStore.UnusedEntriesAsync` is the one definition of unused entries. Competency usage counts content, variants and snapshots, as the deletion check does. Scale usage ignores competency history rows, so deleting an "unused" scale can still be refused.
- `UnitAccountResponse.Of(UserStatus?)`: None / Invited / Blocked / Active, and Blocked wins over Invited. `UserStatus` gained only `Version`; "invited" is still `!IsRegistered`.
- Setup "positions missing" = `snapshot.AvailablePositionIds(id)` is empty, active or not. The org-structure desk source must use the same rule.
- `/…/{id}/matrices` leaves out archived versions. Level, competency and shared block select matrices through `MatricesUsing`, then list all of their non-archived readable versions.
- Unverified raw SQL: the `jsonb_array_elements` unions in `UnusedEntriesAsync` and the nested EXISTS of the competency arm of `MatricesUsing`.

**Why:** desk sources must reuse these definitions, or the desk disagrees with the screens.
**How to apply:** call these functions from desk sources, never copy them. When impl-test is red on these endpoints, suspect the raw SQL first. See [[level-position-links-facts]], [[ef-raw-sql-and-openapi-contract-gotchas]].
