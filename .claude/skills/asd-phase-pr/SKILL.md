---
# ASD generated. Edit .asd/skills/asd-phase-pr/SKILL.md. source_digest=sha256:84bc3ecfb735abcfe74501642b2f30fa966539894507daef54631772c2a80639 content_digest=sha256:48c4432b1b1b5b7c037429b18b7e1b39d7e8024cddb5bc7ed83c0b713ce1b711 asd_version=13.8.0 schema=1
name: asd-phase-pr
description: "Runs the final ASD pr phase: the phase orchestrator verifies DoD, opens the sprint PR and later merges it, publishing the self-hosting release after the merge; it never archives or marks the sprint done, which the next sprint's scope does. Use when asd-sprint dispatches the pr phase, or when the user explicitly asks to run or re-run the pr phase for the active sprint."
allowed-tools: "Read Glob Grep AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-pr.md`.
