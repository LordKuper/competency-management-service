---
name: level-position-links-facts
description: Sprint 004 Task 4 level-position links - readiness via org snapshot, 409 union on approve/activate, refusal before If-Match, runtime test fallout of draft-only POST /tracks
metadata:
  type: project
---

Sprint 004 Task 4 (2026-10-13), commits c938c16, 2b35ff7, 8d4ac3f. None of it was run against PostgreSQL:

- `CatalogStore.ReadinessAsync(trackId)` reads positions only through `IEmployeeDirectory.FindOrgSnapshotAsync` (docs forbid other reads of positions). The snapshot is forgotten when `BeginWriteAsync`/`BeginSharedAsync` take the tree lock, so writers see a locked one. `MatrixCatalog` now takes `CatalogStore` (no DI cycle).
- `PublicationRefusal` was created minimal: kinds `Track`, `Level`, reasons `NoLevels`, `NoActivePosition`. Task 5 adds the rest. `approve`/`activate` of every kind declare 409 as `PublicationRefusal`, but NotDraft, default-scale and name-taken 409s still send plain ProblemDetails without `blockers`, so the declared schema is a union in practice.
- Unlink checks "last active position" before `IfMatch.ApplyTo`. `ApplyTo` throws 412 immediately, so with the usual order two racing removals would give 412, not the 409 the race test expects.
- `POST /tracks` is always draft now, and completeness requires links. Every test scenario that builds a matrix on a fresh track (`Infrastructure/CareerScenarios.cs`) fails at runtime until it adds levels, links and approval. It still compiles.

**Why:** Tasks 5, 6 and 9 build on these choices. impl-test will hit the scenario fallout.
**How to apply:** extend `PublicationRefusal`/`TrackReadiness` rather than adding copies. Keep the refusal-before-If-Match order for invariant refusals that a race test exercises. See [[career-record-deletion-facts]], [[typed-problem-extension-facts]].
