---
# ASD generated. Edit .asd/skills/asd-phase-pr/SKILL.md. source_digest=sha256:e82f127971031309e4d5d59621222a8c759e7b22b96b3231e57fe8acb32a1d2e content_digest=sha256:dfd4f5e67440dee67195ffc5f490e718608f4fd3642e247a10ea0a95e960a508 asd_version=13.4.0 schema=1
name: asd-phase-pr
description: "Runs the final ASD pr phase: the phase orchestrator verifies DoD, opens the sprint PR and later merges it, publishing the self-hosting release after the merge; it never archives or marks the sprint done, which the next sprint's scope does. Use when asd-sprint dispatches the pr phase, or when the user explicitly asks to run or re-run the pr phase for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-pr.md`.
