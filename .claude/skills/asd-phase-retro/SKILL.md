---
# ASD generated. Edit .asd/skills/asd-phase-retro/SKILL.md. source_digest=sha256:5508b3169ccd79d6dc65720264e2c4154305a5a335d1ab39a96d18f427dc092c content_digest=sha256:2cd648fbafa58e9993652c20b7f06bb1e5c7a59dbacbc0f62bece9ea1c38ee5f asd_version=13.8.0 schema=1
name: asd-phase-retro
description: "Runs the ASD retro phase: the phase orchestrator reads the sprint friction log, derives remediation traced to F-N entries plus systemic proposals from how the sprint actually ran, merges findings sharing one root cause, drops any an existing rule in its home already covers, gives each survivor a one-line Guardrail and its Home split into consumer-project and ASD-framework actions (proposed, never applied), writes retrospective.html and posts a short chat summary. An absent or entry-free log takes the empty-log branch — remediation skipped, systemic proposals still produced — and still completes to pr. Use when asd-sprint dispatches the retro phase, or when the user explicitly asks to run or re-run retro for the active sprint."
allowed-tools: "Read Write Edit Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-retro.md`.
