---
{
  "name": "asd-phase-design-promote",
  "description": "Runs the ASD design-promote phase: the phase orchestrator handles decomposition and gates, then in-scope domain creators promote persistent docs — from approved drafts after design-review (standard workflow), or from the accepted implementation after impl-review with no draft and no review (lite workflow), where it runs before retro. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-design-promote.md`.
