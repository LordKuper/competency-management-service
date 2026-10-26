---
# ASD generated. Edit .asd/skills/asd-phase-impl-review/SKILL.md. source_digest=sha256:b2b00a12d2a5e6cbac2b1a3ee58f7a1e6e8462df00af6ef7299a750b11e9a5c4 content_digest=sha256:af55ec0c15a3f5a046533c935fd8a8a3b9fba765471783513217bd565e1d43b9 asd_version=14.0.0 schema=1
name: asd-phase-impl-review
description: "Runs the ASD impl-review phase iteratively until DoD met: always dispatches the frozen workflow's impl roster in parallel (standard: quality, efficiency; lite: one combined reviewer; plus asd-external-review when enabled) against the sprint's code and tests, degrading a diff-derived rubric section to n/a rather than skipping an agent, reviews a large scope as up to 3 sequential review waves with a counter each (lite: one wave), aggregates verdicts, fixes low-severity test-only findings in place via asd-tester, and on other unresolved findings sets state.json.review_fixes_pending and routes back to impl review-fix mode. Once the last wave's reviewers are all APPROVE/latched, dispatches asd-tester for the cycle's one full-suite run; green completes to cleanup (standard) or design-promote (lite), red exits to impl (test-fix mode) and clears every APPROVE latch. Use when asd-sprint dispatches impl-review, or when the user explicitly asks to run or re-run impl-review for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl-review.md`.
