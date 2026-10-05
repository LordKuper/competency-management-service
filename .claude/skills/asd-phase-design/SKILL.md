---
# ASD generated. Edit .asd/skills/asd-phase-design/SKILL.md. source_digest=sha256:a2411b0a300cb97766cea98e7b855ef78096a13362d2d3570e5ed8b0950d5d3e content_digest=sha256:f6b034ff48530731ddecf1fa067538a05c1ce51bd9ced29916604acb862eccbf asd_version=13.3.0 schema=1
name: asd-phase-design
description: "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): dispatches creators sequentially, one per document independently enabled via documents.* (asd-ba for prd.html, a design-system gate only if ux_spec enabled, asd-ux for ux-spec.html, asd-architect for adr.html and c4-full/) — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint."
allowed-tools: "Read Write Edit Glob AskUserQuestion Task Skill"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design.md`.
