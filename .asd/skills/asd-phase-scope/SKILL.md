---
{
  "name": "asd-phase-scope",
  "description": "Runs the ASD scope phase of a sprint: the main orchestrator asks the user to choose the sprint workflow (standard or lite, frozen into state.json), creates the sprint folder, state.json and branch, runs staged solution design (requirements interview, its own best-practice research, approach choice into research.md), and refines scope under the active gate policy. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-scope.md`.
