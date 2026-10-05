---
{
  "name": "asd-phase-impl-test",
  "description": "Runs the ASD impl-test phase: dispatches asd-tester to run the existing impacted tests first, pick the test approach for the whole change scope after the code exists, prune redundant tests, author missing ones, and run the impacted set (never the full suite, which runs once at the end of impl-review) as its suite gate. Green routes to impl-review; code defects are recorded in test-plan.md and route back to impl test-fix mode. Use when asd-sprint dispatches the impl-test phase, or when the user explicitly asks to run or re-run impl-test for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Bash AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-impl-test.md`.
