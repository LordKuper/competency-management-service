---
# ASD generated. Edit .asd/skills/asd-phase-plan/SKILL.md. source_digest=sha256:40f01dc26b5454d7e34a3589b4c75aeb5d86451cef95b283262115a0e20a0d39 content_digest=sha256:49a6aed41fd777fe742c731734bbe9ae7ffa5d3e8c589c4c2ed66e6a20610aee asd_version=14.0.0 schema=1
name: asd-phase-plan
description: "Runs the ASD plan phase: the phase orchestrator authors plan.md from the promoted sprint design docs (standard workflow) or from sprint.md plus audit.md (lite workflow), decomposing work into Task N sections with checkbox subtasks traced to acceptance criteria. Always runs, never no-op. Use when asd-sprint dispatches the plan phase, or when the user explicitly asks to run or re-run plan for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-plan.md`.
