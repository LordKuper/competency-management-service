---
# ASD generated. Edit .asd/skills/asd-phase-plan/SKILL.md. source_digest=sha256:5a47823a92d59a85b5b8747f5c6ade2eb9e1ca7c56b662f3d98434d7783a0b6b content_digest=sha256:b8cac549a6b89db4f42a276328d4a9dc0fc9e113f5619bb5ae7011d38e7133ae asd_version=13.8.0 schema=1
name: asd-phase-plan
description: "Runs the ASD plan phase: the phase orchestrator authors plan.md from the promoted sprint design docs (standard workflow) or from sprint.md plus audit.md (lite workflow), decomposing work into Task N sections with checkbox subtasks traced to acceptance criteria. Always runs, never no-op. Use when asd-sprint dispatches the plan phase, or when the user explicitly asks to run or re-run plan for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-plan.md`.
