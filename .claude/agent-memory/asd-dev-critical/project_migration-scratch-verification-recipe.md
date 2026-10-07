---
name: migration-scratch-verification-recipe
description: Verifying an EF migration (Up/Down/Up, data survival) against a scratch Postgres on this box - ef --connection with the unused-connection factory, running the built dll off the user's DB, staged-deletion commit pitfall
metadata:
  type: project
---

Task 13 (RemoveOrgUnitValidity), 2026-10-06. Recipe and facts that are not in the code:

- **`dotnet ef database update [<migration>] --no-build --connection "<scratch conn>"` works** although `AppDbContextFactory` hard-codes an unused connection string: the tool overrides it. Apply to `UserManagement`, seed rows by SQL (`docker exec -i <c> psql ... < file.sql`), apply the new migration, then `update <previous>` for Down and `update` for Up again. `has-pending-model-changes` and `migrations script --idempotent <from>` need no DB.
- **EF emits `DropCheckConstraint` on its own** when a `HasCheckConstraint` leaves the model, and it comes before the column drops; Down re-adds columns and the CHECK with the original SQL. Down is lossy for data (restored columns are NULL) - say so in reports.
- **Run the API on a scratch DB without the launch profile**: `launchSettings.json` env vars would point it at the user's `localhost:15432`. Run `dotnet bin/Debug/net10.0/Competency.Api.dll` from `src/Competency.Api` with `ASPNETCORE_URLS`, `ConnectionStrings__Default`, `Bootstrap__*`, `DataProtection__KeysPath` and `RateLimiting__Login__PermitLimit=1000` set in a script (background, `exec bash`), kill by the listener PID.
- **System.Text.Json ignores unknown members**: a client still sending removed `validFrom`/`validTo` gets 200/201 and the fields are dropped silently (an inverted range included); no special handling is needed or added.
- Audit rows (`audit_events.old_value/new_value`) hold only allow-listed changed properties, so a removed property simply stops appearing; old rows keep their JSON.
- `git add -- <path>` of a file already staged as deleted (`git rm --cached`) fails with "pathspec did not match"; add the others, and name the deleted path only in `git commit --only -m ... -- <paths>`.
- A contract change that removes fields from request types cannot be split into "regenerate contract" and "UI" commits and stay green (`tsc` fails on the old UI, `check:api` fails on the old schema): commit openapi + schema + UI together.

**Why:** each cost a decision or a failed command during Task 13.
**How to apply:** any later migration verification, or a field removal that crosses backend, contract and UI (see also [[docker-spike-recipe]], [[scratch-process-control]], [[dotnet-build-time-host-gotchas]]).
