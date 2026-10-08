---
# ASD generated. Edit .asd/skills/asd-phase-design/SKILL.md. source_digest=sha256:d95a0e07166562e771c91cde78e1e72bdcadf454341a9dc1761e267f67ccfee2 content_digest=sha256:c24a7bc36b9aaa0b522fb74e0c28af2630d8c5fd3d06cdd2a6169efac8124c33 asd_version=13.8.0 schema=1
name: asd-phase-design
description: "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): dispatches creators sequentially, one per document independently enabled via documents.* (asd-ba for prd.html, a design-system gate only if ux_spec enabled, asd-ux for ux-spec.html, asd-architect for adr.html and c4-full/) — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint."
allowed-tools: "Read Write Edit Glob AskUserQuestion Task Skill Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design.md`.
