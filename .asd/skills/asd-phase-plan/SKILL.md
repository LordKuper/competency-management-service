---
{
  "name": "asd-phase-plan",
  "description": "Runs the ASD plan phase: the phase orchestrator authors plan.md from the promoted sprint design docs (standard workflow) or from sprint.md plus audit.md (lite workflow), decomposing work into Task N sections with checkbox subtasks traced to acceptance criteria. Always runs, never no-op. Use when asd-sprint dispatches the plan phase, or when the user explicitly asks to run or re-run plan for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-plan.md`.
