---
{
  "name": "asd-phase-design-promote",
  "description": "Runs the ASD design-promote phase: the main orchestrator handles decomposition and gates and promotes persistent docs itself — from approved drafts after design-review (standard workflow), or from the accepted implementation after impl-review with no draft and no review (lite workflow), where it runs before cleanup. Standard continues to plan. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-design-promote.md`.
