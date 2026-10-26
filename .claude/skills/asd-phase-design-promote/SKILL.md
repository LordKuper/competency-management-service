---
# ASD generated. Edit .asd/skills/asd-phase-design-promote/SKILL.md. source_digest=sha256:f8309017880561ad20a68357923342a9aa184fabd80a87ab0166e9931724eacf content_digest=sha256:3150aea3222d568348115585afbbf85a3c617f4fe1cf073be04c868a66f0d291 asd_version=14.0.0 schema=1
name: asd-phase-design-promote
description: "Runs the ASD design-promote phase: the main orchestrator handles decomposition and gates and promotes persistent docs itself — from approved drafts after design-review (standard workflow), or from the accepted implementation after impl-review with no draft and no review (lite workflow), where it runs before cleanup. Standard continues to plan. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-promote.md`.
