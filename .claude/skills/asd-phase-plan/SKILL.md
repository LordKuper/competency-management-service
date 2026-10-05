---
# ASD generated. Edit .asd/skills/asd-phase-plan/SKILL.md. source_digest=sha256:a5856fbb79957301101dc65b7eb2f6269103d1e09a3b6053bc4f38d083494829 content_digest=sha256:0dafdbb3a87dec6c07d51d18d27838d4f7136556475fca95c3af3acc0c6add7c asd_version=13.3.0 schema=1
name: asd-phase-plan
description: "Runs the ASD plan phase: the phase orchestrator authors plan.md from the promoted sprint design docs (standard workflow) or from sprint.md plus audit.md (lite workflow), decomposing work into Task N sections with checkbox subtasks traced to acceptance criteria. Always runs, never no-op. Use when asd-sprint dispatches the plan phase, or when the user explicitly asks to run or re-run plan for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-plan.md`.
