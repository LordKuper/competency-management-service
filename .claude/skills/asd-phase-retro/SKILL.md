---
# ASD generated. Edit .asd/skills/asd-phase-retro/SKILL.md. source_digest=sha256:5c90a068c58d4b4984e1f99c03f8b730d578aa8a33eaaa56fba44f2d84ccc5fe content_digest=sha256:e98dde8c953a09fa123dac2823133cfb9548d2cb4a3793b328e3b7cc31011e48 asd_version=14.0.0 schema=1
name: asd-phase-retro
description: "Runs the ASD retro phase: the phase orchestrator reads the sprint friction log, derives remediation traced to F-N entries plus systemic proposals from how the sprint actually ran, merges findings sharing one root cause, drops any an existing rule in its home already covers, gives each survivor a one-line Guardrail and its Home split into consumer-project and ASD-framework actions (proposed, never applied), writes retrospective.html and posts a short chat summary. An absent or entry-free log takes the empty-log branch — remediation skipped, systemic proposals still produced — and still completes to pr. Use when asd-sprint dispatches the retro phase, or when the user explicitly asks to run or re-run retro for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-retro.md`.
