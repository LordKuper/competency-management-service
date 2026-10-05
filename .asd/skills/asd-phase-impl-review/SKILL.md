---
{
  "name": "asd-phase-impl-review",
  "description": "Runs the ASD impl-review phase iteratively until DoD met: always dispatches the frozen workflow's impl roster in parallel (standard: correctness, efficiency, testing, documentation; lite: one combined reviewer; plus asd-external-review when enabled) against the sprint's code and tests, degrading a diff-derived rubric section to n/a inside correctness/efficiency rather than skipping either agent, reviews a large scope as up to 3 sequential review waves with a counter each, aggregates verdicts, fixes low-severity test-only findings in place via asd-tester, and on other unresolved findings sets state.json.review_fixes_pending and routes back to impl review-fix mode. Once the last wave's reviewers are all APPROVE/latched, dispatches asd-tester for the cycle's one full-suite run; green completes to retro (standard) or design-promote (lite), red exits to impl (test-fix mode) and clears every APPROVE latch. Use when asd-sprint dispatches impl-review, or when the user explicitly asks to run or re-run impl-review for the active sprint.",
  "claude": { "allowed-tools": "Read Write Edit Bash AskUserQuestion Task" },
  "codex": {}
}
---

Execute workflow `.asd/workflows/asd-phase-impl-review.md`.
