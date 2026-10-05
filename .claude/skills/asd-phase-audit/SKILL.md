---
# ASD generated. Edit .asd/skills/asd-phase-audit/SKILL.md. source_digest=sha256:8bbda9f2eb8b7925599c387f783d888ba92b12772f82942e8fc28c1e92383eb8 content_digest=sha256:55e357f8871854a29ea56418cb57e3ee913864d57bccb786f95a10d5f2a6c1e8 asd_version=7.1.0 schema=1
name: asd-phase-audit
description: "Runs the ASD audit phase for the active sprint: Architect scans code and documentation; BA joins only for material product/domain ambiguity. The phase orchestrator merges and gates audit.md. Use when asd-sprint dispatches the audit phase, or when the user explicitly asks to run or re-run audit for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-audit.md`.
