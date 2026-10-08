---
# ASD generated. Edit .asd/skills/asd-phase-audit/SKILL.md. source_digest=sha256:3032b107b725ec235c031866831fe45c81a70562fbdf1af77ae50bf5dda4086c content_digest=sha256:bc68cc59a35210556af3fa0b3dca1ddbc03e5e3d6f1526bd61bcd44082e61c70 asd_version=13.8.0 schema=1
name: asd-phase-audit
description: "Runs the ASD audit phase for the active sprint: Architect scans code and documentation; BA joins only for material product/domain ambiguity. The phase orchestrator merges and gates audit.md. Use when asd-sprint dispatches the audit phase, or when the user explicitly asks to run or re-run audit for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-audit.md`.
