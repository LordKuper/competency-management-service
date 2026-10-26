---
{
  "name": "asd-phase-audit",
  "description": "Runs the ASD audit phase for the active sprint: the main orchestrator scans code and documentation itself and resolves product/domain ambiguity its sources can settle, then gates audit.md. Use when asd-sprint dispatches the audit phase, or when the user explicitly asks to run or re-run audit for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-audit.md`.
