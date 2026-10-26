---
# ASD generated. Edit .asd/skills/asd-phase-pr/SKILL.md. source_digest=sha256:08ff0724ff4c42180f06ef58b402694d8aaeee282ee6831b4853f5125b7ac954 content_digest=sha256:7b1a165920ae559ff03201598eaeccefdaa82e58f6c4c1b873ec96f8a847f63a asd_version=14.0.0 schema=1
name: asd-phase-pr
description: "Runs the final ASD pr phase: the phase orchestrator verifies DoD, opens the sprint PR and later merges it, publishing the self-hosting release after the merge; it never archives or marks the sprint done, which the next sprint's scope does. Use when asd-sprint dispatches the pr phase, or when the user explicitly asks to run or re-run the pr phase for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-pr.md`.
