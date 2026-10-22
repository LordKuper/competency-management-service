---
name: publication-cascade-facts
description: Sprint 004 Task 5 cascade publication - checks flip statuses only while running, no save before refusal, 412 metadata override, tag always required for version publishing, HR viewer admin flag, test fallout
metadata:
  type: project
---

Sprint 004 Task 5 (2026-10-13), commits 0453509, 2969c5f. Not run against PostgreSQL:

- `CatalogPublication.CheckAsync` sets `Status = Active` on the tracked root and drafts only while the checks run (name checks see the whole batch through `ChangeTracker`), then restores the statuses as read: blocker visibility (`SeesLoaded`) must see drafts as drafts, or every draft blocker leaks through the active branch. `Apply` sets the drafts active; a catalogue root is set active by its caller. Still never save between planning and the refusal check.
- `.Produces<PublicationChanged>(412, ...)` after the `IfMatch` parameter wins in the generated OpenAPI: the 412 schema becomes `PublicationChanged` with an optional `publication`. No duplicate-status error was raised.
- A publishing version transition needs `?cascade` even when nothing but the version is published. Records need it only when the closure is non-empty. Every existing test that publishes a version (HR approve, auto-approval) or approves a track with draft levels or a competency with draft references now gets 412 until it calls the preview first.
- Name-taken on approve is now a `NameTaken` blocker, not `errors.name`. NotDraft and draft-activation 409s are `PublicationRefusal` with empty `blockers`.
- `UserStatus` has no role, so the responsible HR's `CatalogViewer` is admin only when the HR is the signed-in actor and is an admin.

**Why:** Tasks 6, 9, 11 and 12 and the impl-test repair of tests build on these choices.
**How to apply:** extend `CatalogPublication`, and never add a save between planning and the refusal check. Pass the preview tag in every test that publishes. See [[level-position-links-facts]].
