---
responsibility:
  owns: project-global registry of CURRENTLY OPEN todo stubs across all sprints
  excludes: code review issues, plan tasks, design todos, resolved stubs (deleted on resolution)
  delegates_to: reviews/ (code issues), plan.md (tasks), <sprint>/decisions-log.md (audit trail of resolutions)
---

# Stubs

Only OPEN stubs. Resolved stubs deleted immediately. Migrated stubs deleted from prior sprint and re-registered under the new sprint. Accepted-debt entries kept; their Reason field MUST begin with `(accepted-debt)` so the pr-phase block exempts them.

Persists across sprint archival.

| Sprint | File:Line | Reason | Owner |
|---|---|---|---|
| {{NNN-slug}} | {{path:N}} | {{why; prefix with `(accepted-debt)` if known debt}} | {{agent}} |

<!-- when empty: -->
<!-- | — | — | no open stubs | — | -->
