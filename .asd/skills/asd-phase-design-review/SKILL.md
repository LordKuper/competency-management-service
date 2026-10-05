---
{
  "name": "asd-phase-design-review",
  "description": "Runs the ASD design-review phase iteratively until DoD met, scoped to whichever drafts documents.* actually enabled this sprint: always dispatches asd-reviewer-documentation + asd-reviewer-efficiency + asd-reviewer-correctness for any non-empty draft set (and asd-external-review when enabled) — correctness's UI section is n/a unless a ux-spec/design-system draft is in scope, never an agent-level skip — aggregates verdicts, and routes CONCERNS to creator autofix or FAIL to user escalation. Never dispatched after a design-block collapse (every design documents.* disabled, effective c4 included); this phase's own no-op path is only a defensive fallback for a direct/explicit re-dispatch. Use when asd-sprint dispatches design-review, or when the user explicitly asks to run or re-run design-review for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-design-review.md`.
