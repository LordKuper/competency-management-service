---
{
  "name": "asd-phase-scope",
  "description": "Runs the ASD scope phase of a sprint: the phase orchestrator asks the user to choose the sprint workflow (standard or lite, frozen into state.json), creates the sprint folder, state.json, branch, and refined scope under the active gate policy. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint.",
  "claude": { "allowed-tools": "Read Glob Bash AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-scope.md`.
