---
# ASD generated. Edit .asd/skills/asd-phase-retro/SKILL.md. source_digest=sha256:e8e8b03349b6b05b6170fb2d81cf092237e0b9f4f067a739b80d2f57491d2bfc content_digest=sha256:60a95206bbbee6598267a65dc16345316ef33b3d19135257beea2c876eec7fd7 asd_version=14.1.0 schema=1
name: asd-phase-retro
description: "Runs the ASD retro phase: the phase orchestrator reads the sprint friction log, derives remediation traced to F-N entries plus systemic proposals from how the sprint actually ran, merges findings sharing one root cause, drops any an existing rule in its home already covers, gives each survivor a one-line Guardrail and its Home split into consumer-project and ASD-framework actions (proposed, never applied), writes retrospective.html, in a consumer project publishes its upstream-ASD rows as GitHub issues, and posts a short chat summary. An absent or entry-free log takes the empty-log branch — remediation skipped, systemic proposals still produced — and still completes to pr. Use when asd-sprint dispatches the retro phase, or when the user explicitly asks to run or re-run retro for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash(node .asd/runtime.js:*) Bash(gh auth status:*) Bash(gh issue create:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-retro.md`.
