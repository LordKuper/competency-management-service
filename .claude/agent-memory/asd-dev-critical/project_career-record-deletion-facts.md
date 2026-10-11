---
name: career-record-deletion-facts
description: Deletion of career catalogue records and matrices - jsonb containment check shape, unaudited history rows need explicit Deleted events, matrix If-Match target, content edits hold 5003001 shared
metadata:
  type: project
---

Career record deletion (2026-10-11), commit 034b8ec. None of it was run against PostgreSQL:

- Matrix documents are jsonb strings through a value converter, so LINQ cannot look inside. The reference check is raw `SqlQuery` with `AS "Value"` (now a `SqlQuery<Guid>` of matrix ids): `content -> 'levels' @> jsonb_build_array(jsonb_build_object('levelId', id::text))`, the same over `matrix_version_variants.content`, and `snapshot -> 'levels'` with key `id`. Key names follow STJ Web camelCase of `MatrixContent` / `MatrixEntries`.
- `CompetencyVersion`, `MatrixVariant` and `MatrixDecision` are not `[Audited]`, so the interceptor writes nothing when they are deleted. `CatalogStore.RemoveHistoryRow` stages an explicit `<Type>.Deleted` event with the composite key joined by commas, like the interceptor does.
- A matrix row never changes version. `DELETE /matrices/{id}` therefore applies If-Match to its only version, the one in work. A never-published matrix has exactly one version.
- Matrix content edits used to take no advisory lock (an earlier plan decision), so a delete could race an edit and leave a dangling id in `content`. The orchestrator revised the lock order, and commit 07304c0 fixed it. `MatrixWorkflow.EditAsync` now takes `CatalogStore.BeginSharedAsync` (tree shared, then 5003001 shared) before it loads the version and checks references. Deletes take 5003001 exclusively.

**Why:** any later change that deletes or deactivates rows that matrix documents refer to faces the same gaps.
**How to apply:** reuse `CatalogStore.MatricesUsing` / `LevelsReferencing` (composable queries that replaced the old bool checks) / `RemoveHistoryRow` / `BeginSharedAsync`; refusals list blockers through `DeletionBlockers`. A change that only refers to entries takes the shared lock; a change that removes them takes the exclusive one. See [[matrix-workflow-facts]].
