---
{
  "name": "asd-phase-audit",
  "description": "Runs the ASD audit phase for the active sprint: Architect scans code and documentation; BA joins only for material product/domain ambiguity. The phase orchestrator merges and gates audit.md. Use when asd-sprint dispatches the audit phase, or when the user explicitly asks to run or re-run audit for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-audit.md`.
