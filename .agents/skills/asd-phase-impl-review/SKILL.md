---
# ASD generated. Edit .asd/skills/asd-phase-impl-review/SKILL.md. source_digest=sha256:adf1550463ceb2f5db4f80c4078aba072304545d1aab72a9f53d2e50f4d2c480 content_digest=sha256:81b1795d13f126777e7eef94af39738afe3a556ca1124d478f0c7b94f40d6e83 asd_version=13.3.0 schema=1
name: asd-phase-impl-review
description: "Runs the ASD impl-review phase iteratively until DoD met: always dispatches the frozen workflow's impl roster in parallel (standard: correctness, efficiency, testing, documentation; lite: one combined reviewer; plus asd-external-review when enabled) against the sprint's code and tests, degrading a diff-derived rubric section to n/a inside correctness/efficiency rather than skipping either agent, reviews a large scope as up to 3 sequential review waves with a counter each, aggregates verdicts, fixes low-severity test-only findings in place via asd-tester, and on other unresolved findings sets state.json.review_fixes_pending and routes back to impl review-fix mode. Once the last wave's reviewers are all APPROVE/latched, dispatches asd-tester for the cycle's one full-suite run; green completes to retro (standard) or design-promote (lite), red exits to impl (test-fix mode) and clears every APPROVE latch. Use when asd-sprint dispatches impl-review, or when the user explicitly asks to run or re-run impl-review for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl-review.md`.
