---
name: desk-merge-contract-facts
description: Desk contract and merge choices a new IDeskSource must follow (unit naming helper, subject absorption, one finding per kind per object, groups), plus the Dockerfile line every new src project needs
metadata:
  type: project
---

Sprint 004 Task 8 (2026-10-13), commits 6fbdfe1…96889fe. Nothing was run against PostgreSQL or a live API.

- Items merge by `(DeskObject.Type, Id)`; the first reason's object wins, so every source must name a unit through `OrgSnapshot.UnitDeskObject(unit)` (`OrgDeskObjects.cs`, context = ancestor names root-first).
- `DeskRules` dedups by `(Kind, object, subject)`, keeping the action. Emit one finding per kind per object: several levels without a position go into one `LevelWithoutPosition` with `Details.Entries`, not one finding each.
- Cross-object absorption works only through `Subject`: `PositionWithoutTrack` (object = position) absorbs `NoTrackForPosition` whose subject is that position; `InvitationNotAccepted` (object = account) absorbs `HeadAccountInvited` whose subject is the account.
- A kind's group is fixed in `DeskRules.GroupOf`; only the five track/position link kinds switch group by `IsAction`. 5.2/6.1 share `DraftAwaitsApproval`; 6.3/6.4 share `UnitWithoutHr` (cannot-act state in `Details.AccountState`).
- Not modelled yet (Task 9 adds additively to `DeskDetails`): return with changes/comment (4.1), level change kind (26.5).
- Revoked invitation links have `OccurredAt = null`: the revocation time is not stored.
- A new `src/` project needs its `COPY …csproj …packages.lock.json` line in the root `Dockerfile` before `dotnet restore`; `DeploymentTests.Ac8_…` enforces it. New project refs also change `tests/Competency.Tests/packages.lock.json`.

**Why:** a source that names objects differently or splits a kind per level silently breaks "one object, one item" and the counter.
**How to apply:** read before writing any `IDeskSource` or changing `DeskRules`. See [[scenario-read-facts]].
