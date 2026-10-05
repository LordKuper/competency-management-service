---
responsibility:
  owns: per-sprint registry of manual operational actions a human must perform for the sprint plan to complete
  excludes: code todo stubs (stubs.md), manual QA verification of behaviour (reviews testing.md), plan tasks (plan.md)
  delegates_to: stubs.md (code stubs), plan.md (tasks + BLOCKED markers), reviews/ (manual verification of behaviour)
---

# Manual Steps

Definition, boundary against `stubs.md` and manual verification, validation duty, status transition and lifecycle: `artifact-layout.md` "Manual steps" (sole SSoT, not restated here). Entries are append-only; entry content is `language.docs`.

## Summary

| ID | Title | Blocks | Performed by | Status |
|---|---|---|---|---|
| {{MS-N}} | {{title}} | {{Task N / subtask(s)}} | {{role}} | {{pending\|done}} |

<!-- when no manual action arose, this file is not created -->

## MS-{{N}} — {{title}}

- **Blocks**: {{Task N — subtask(s)}}
- **Why**: {{AC-N / reason the plan needs this}}
- **When**: {{before first run | before deploy | …}}
- **Prerequisites**: {{what must exist first}}
- **Performed by**: {{role — typically user}}
- **Status**: {{pending | done}}

### Steps

1. {{step}}
2. {{step}}

### Verification

{{how workflow confirms completion — a commands.yaml check command, observable state to inspect, or explicit user confirmation when no automated check exists}}
