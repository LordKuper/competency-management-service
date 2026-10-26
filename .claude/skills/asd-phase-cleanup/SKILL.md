---
# ASD generated. Edit .asd/skills/asd-phase-cleanup/SKILL.md. source_digest=sha256:67d91216fcb5ed04e6ef7bc5b4276c057d36628a8185516b38dd4e2c7a88e365 content_digest=sha256:04b860fd508dcb77268ac96209a744c4ba78c2360c03773df2dfcc81aa883d90 asd_version=14.0.0 schema=1
name: asd-phase-cleanup
description: "Runs the ASD cleanup phase: the phase orchestrator removes dead code, debug output, scratch files and unused fixtures the sprint left behind on clear evidence only, in checkpointed commits with no review and no user gate, verifies build, lint and the impacted tests (reverting a removal that breaks them), records one decisions-log line and returns to retro. A frozen cleanup setting of false is a no-op skip. Use when asd-sprint dispatches the cleanup phase, or when the user explicitly asks to run or re-run cleanup for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-cleanup.md`.
