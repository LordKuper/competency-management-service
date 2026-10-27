---
# ASD generated. Edit .asd/skills/asd-phase-impl-review/SKILL.md. source_digest=sha256:4e80200649594fcc7731ec6bd1bea794003546ac9e1418ccecf580f723786bed content_digest=sha256:ea6347fecfd749607e79c11a2c7d03d0d933fd9cf3ef53a3764c5eaa10d0ff59 asd_version=14.1.0 schema=1
name: asd-phase-impl-review
description: "Runs the ASD impl-review phase iteratively until DoD met: always dispatches the frozen workflow's impl roster in parallel (standard: quality, efficiency; lite: one combined reviewer; plus asd-external-review when enabled) against the sprint's code and tests, degrading a diff-derived rubric section to n/a rather than skipping an agent, reviews a large scope as up to 3 sequential review waves with a counter each (lite: one wave), aggregates verdicts, fixes minor test-only findings in place via asd-tester, and on other unresolved findings sets state.json.review_fixes_pending and routes back to impl review-fix mode. Once the last wave's reviewers are all APPROVE/latched, dispatches asd-tester for the cycle's one full-suite run; green completes to cleanup (standard) or design-promote (lite), red exits to impl (test-fix mode) and clears every APPROVE latch. Use when asd-sprint dispatches impl-review, or when the user explicitly asks to run or re-run impl-review for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl-review.md`.
