---
name: level-position-links-facts
description: Level-position links - readiness via org snapshot, 409 union on approve/activate, refusal before If-Match, runtime test fallout of draft-only POST /tracks
metadata:
  type: project
---

As of 2026-10-13 none of it was run against PostgreSQL:

- `CatalogStore.ReadinessAsync(trackId)` reads positions only through `IEmployeeDirectory.FindOrgSnapshotAsync` (docs forbid other reads of positions). The snapshot is forgotten when `BeginWriteAsync`/`BeginSharedAsync` take the tree lock, so writers see a locked one. `MatrixCatalog` now takes `CatalogStore` (no DI cycle).
- Every 409 of `approve`/`activate` is a `PublicationRefusal` (its shape: [[publication-cascade-facts]]).
- Unlink checks "last active position" before `IfMatch.ApplyTo`. `ApplyTo` throws 412 immediately, so with the usual order two racing removals would give 412, not the 409 the race test expects.
- `POST /tracks` is always draft now, and completeness requires links. Every test scenario that builds a matrix on a fresh track (`Infrastructure/CareerScenarios.cs`) fails at runtime until it adds levels, links and approval. It still compiles.

**Why:** publication, scenario reads and the desk build on these choices; impl-test hits the scenario fallout.
**How to apply:** extend `PublicationRefusal`/`TrackReadiness` rather than adding copies. Keep the refusal-before-If-Match order for invariant refusals that a race test exercises. See [[career-record-deletion-facts]], [[typed-problem-extension-facts]].
