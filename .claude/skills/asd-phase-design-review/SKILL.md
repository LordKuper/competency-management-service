---
# ASD generated. Edit .asd/skills/asd-phase-design-review/SKILL.md. source_digest=sha256:69c9fdad15b9a192b5eef8b1f77507eb3ea0209bf7ead243353bdbd9ea9732bc content_digest=sha256:7e230b7bccdf1dd735440000579d0730c9418286685415c7ac22cc49b907ecfe asd_version=13.8.0 schema=1
name: asd-phase-design-review
description: "Runs the ASD design-review phase iteratively until DoD met, scoped to whichever drafts documents.* actually enabled this sprint: always dispatches asd-reviewer-documentation + asd-reviewer-efficiency + asd-reviewer-correctness for any non-empty draft set (and asd-external-review when enabled) — correctness's UI section is n/a unless a ux-spec/design-system draft is in scope, never an agent-level skip — aggregates verdicts, and routes CONCERNS to creator autofix or FAIL to user escalation. Never dispatched after a design-block collapse (every design documents.* disabled, effective c4 included); this phase's own no-op path is only a defensive fallback for a direct/explicit re-dispatch. Use when asd-sprint dispatches design-review, or when the user explicitly asks to run or re-run design-review for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-review.md`.
