---
name: publication-cascade-facts
description: Sprint 004 Task 5 cascade publication - plan mutates tracked statuses and relies on no save, 412 metadata override, tag always required for version publishing, HR viewer admin flag, test fallout
metadata:
  type: project
---

Sprint 004 Task 5 (2026-10-13), commits 0453509, 2969c5f. Not run against PostgreSQL:

- `CatalogPublication.PlanAsync` sets `Status = Active` on the tracked root and drafts before the checks, so name checks see the whole batch through `ChangeTracker`. A refusal is safe only because no `SaveChanges` runs before `plan.IsRefused` is checked. The preview endpoints mutate tracked entities the same way and never save.
- `.Produces<PublicationChanged>(412, ...)` after the `IfMatch` parameter wins in the generated OpenAPI: the 412 schema becomes `PublicationChanged` with an optional `publication`. No duplicate-status error was raised.
- A publishing version transition needs `?cascade` even when nothing but the version is published. Records need it only when the closure is non-empty. Every existing test that publishes a version (HR approve, auto-approval) or approves a track with draft levels or a competency with draft references now gets 412 until it calls the preview first.
- Name-taken on approve is now a `NameTaken` blocker, not `errors.name`. NotDraft and draft-activation 409s are `PublicationRefusal` with empty `blockers`.
- `UserStatus` has no role, so the responsible HR's `CatalogViewer` is admin only when the HR is the signed-in actor and is an admin.

**Why:** Tasks 6, 9, 11 and 12 and the impl-test repair of tests build on these choices.
**How to apply:** extend `CatalogPublication`, and never add a save between planning and the refusal check. Pass the preview tag in every test that publishes. See [[level-position-links-facts]].
