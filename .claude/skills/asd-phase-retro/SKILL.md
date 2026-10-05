---
# ASD generated. Edit .asd/skills/asd-phase-retro/SKILL.md. source_digest=sha256:d40dea69c3415c91b8043f4fff9bbda9dbf5750f76ebd2ff762942ab56fb4bef content_digest=sha256:7bc98cbcbc6ba3b928190987d2fe4889723c0ab929fc73433db8bc53187d4a29 asd_version=8.0.0 schema=1
name: asd-phase-retro
description: "Runs the ASD retro phase: the phase orchestrator reads the sprint friction log, derives remediation traced to F-N entries plus systemic proposals from how the sprint actually ran, merges findings sharing one root cause, drops any an existing rule in its home already covers, gives each survivor a one-line Guardrail and its Home split into consumer-project and ASD-framework actions (proposed, never applied), writes retrospective.html and posts a short chat summary. An absent or entry-free log takes the empty-log branch — remediation skipped, systemic proposals still produced — and still completes to pr. Use when asd-sprint dispatches the retro phase, or when the user explicitly asks to run or re-run retro for the active sprint."
allowed-tools: "Read Write Edit"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-retro.md`.
