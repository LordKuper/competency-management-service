---
# ASD generated. Edit .asd/skills/asd-phase-design-promote/SKILL.md. source_digest=sha256:b4ec2db3e65e6dbf27b9d50877eb91b011c641933c8d91517acd75e34025101e content_digest=sha256:f5b7cb90eede8706ae210b082bb5185180dab6766bc8ab5147ebae936b0ee7ef asd_version=13.3.0 schema=1
name: asd-phase-design-promote
description: "Runs the ASD design-promote phase: the phase orchestrator handles decomposition and gates, then in-scope domain creators promote persistent docs — from approved drafts after design-review (standard workflow), or from the accepted implementation after impl-review with no draft and no review (lite workflow), where it runs before retro. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-promote.md`.
