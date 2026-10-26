---
{
  "name": "asd-phase-cleanup",
  "description": "Runs the ASD cleanup phase: the phase orchestrator removes dead code, debug output, scratch files and unused fixtures the sprint left behind on clear evidence only, in checkpointed commits with no review and no user gate, verifies build, lint and the impacted tests (reverting a removal that breaks them), records one decisions-log line and returns to retro. A frozen cleanup setting of false is a no-op skip. Use when asd-sprint dispatches the cleanup phase, or when the user explicitly asks to run or re-run cleanup for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Glob Grep Bash Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-cleanup.md`.
