---
# ASD generated. Edit .asd/skills/asd-phase-design-promote/SKILL.md. source_digest=sha256:7850ecd38ceb9665f6bd8681f340a8519c43cc26ac28ee70173a15ae532293b4 content_digest=sha256:53eb649b7d73954d7b428488506ab6261ee3cf2970ee07d1d8feab8b2030c047 asd_version=13.8.0 schema=1
name: asd-phase-design-promote
description: "Runs the ASD design-promote phase: the phase orchestrator handles decomposition and gates, then in-scope domain creators promote persistent docs — from approved drafts after design-review (standard workflow), or from the accepted implementation after impl-review with no draft and no review (lite workflow), where it runs before retro. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-promote.md`.
