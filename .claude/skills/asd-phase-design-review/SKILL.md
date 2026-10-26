---
# ASD generated. Edit .asd/skills/asd-phase-design-review/SKILL.md. source_digest=sha256:8dea38d633c07f1c467a80a2f3a55a7c63c8dc878c64425f55852ac6e97efdda content_digest=sha256:d9ad1dc36f0f6b9a39e422b89fac675d61fe082cd7c1841953b5d853bbf0ef98 asd_version=14.0.0 schema=1
name: asd-phase-design-review
description: "Runs the ASD design-review phase iteratively until DoD met, scoped to whichever drafts documents.* actually enabled this sprint: always dispatches asd-reviewer-quality + asd-reviewer-efficiency for any non-empty draft set (and asd-external-review when enabled) — quality's UI section is n/a unless a ux-spec/design-system draft is in scope, never an agent-level skip — aggregates verdicts, fixes CONCERNS inline, and escalates FAIL to the user after an advisor consult. Never dispatched after a design-block collapse (every design documents.* disabled, effective c4 included); this phase's own no-op path is only a defensive fallback for a direct/explicit re-dispatch. Use when asd-sprint dispatches design-review, or when the user explicitly asks to run or re-run design-review for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-review.md`.
