---
{
  "name": "asd-phase-impl",
  "description": "Runs the ASD impl phase in one of three modes detected from state.json: initial mode dispatches plan.md Task blocks to devs, review-fix mode resolves impl-review findings, test-fix mode resolves code defects found by impl-test. Devs write production code only (no tests; a plan-declared Test-only Task goes to asd-tester instead), run build/lint, and commit; the phase enforces a build+lint completion gate before COMPLETED and always routes to impl-test. Use when asd-sprint dispatches the impl phase, or when the user explicitly asks to run or re-run impl for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-impl.md`.
