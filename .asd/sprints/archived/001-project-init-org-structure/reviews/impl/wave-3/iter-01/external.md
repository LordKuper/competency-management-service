[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-3/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | .claude/agent-memory/asd-dev-critical/project_scratch-process-control.md:13 | codex F1 (critical). The scratch recipe runs `docker compose -p cmp-scratch ... down -v` against `deploy/dev/docker-compose.yml`. That file names the data volume `competency-dev-pgdata`, so `-p` does not isolate it; `down -v` can delete the user's local dev DB data. Line 13 says the volume has a fixed name and that `-v` is what frees it. | Give the scratch run its own volume (Compose override or env var) and remove only that volume; never `down -v` on the shared volume. |
| 2 | high | web/src/features/users/UserModal.tsx:120 | codex F2 (major). A background refetch that brings a new `version` remounts the form and silently wipes unsaved edits (reproduced, no conflict message). Employee and unit forms use the same mechanism. | Capture data and version when editing starts; replace the draft only after an explicit re-read following a conflict. |
| 3 | high | web/src/api/client.ts:13 | codex F3 (major). The SPA auth flow has no tests: route guard, login, logout, 401 handling with cache clearing (AC-6/13). | Add a behavioural test with a mocked API covering login and session end, checking the resulting route and cache clearing. |
| 4 | medium | web/src/app/theme.ts:135 | codex F4 (minor). `motion=false` does not stop the shimmer of an `active` Skeleton in the installed antd; no reduced-motion override. | Turn off Skeleton animation under `prefers-reduced-motion`, or drop `active` conditionally. |
| 5 | medium | web/src/app/theme.ts:129 | codex F5 (minor). Inputs keep the default focus shadow `rgba(5,92,230,0.08)` (contrast 1.13:1) with `outline=0`; DESIGN.md calls for a 2 px primary ring with a 2 px offset. | Apply the DESIGN.md focus ring to fields and compound controls. |
| 6 | medium | web/src/features/org-structure/useUnitExpansion.ts:48 | codex F6 (minor). Expanding a unit while a search is running: the late response adds it to `pathIds` and inverts the user's toggle (reproduced). | Keep explicit expand state separate from auto-expanded paths. |
| 7 | medium | web/src/features/org-structure/useEmployeeSearch.ts:39 | codex F7 (minor). A one-character query is not sent; with no unit match the page says «Ничего не найдено» even when employees would match. | Show the two-character minimum instead of «nothing found». |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none

## Verdict
FAIL: 7

## Next action
Fix F1–F3 first, then F4–F7. F1 is agent memory, not code — severity may be recalibrated by the orchestrator. F3 is missing tests; the cited line is an anchor.

- user decision (2026-10-07): all 7 findings accepted for fix; #3 (missing SPA auth tests) is test work and goes to impl-test entry 7
