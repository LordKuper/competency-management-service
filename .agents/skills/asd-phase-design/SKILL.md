---
# ASD generated. Edit .asd/skills/asd-phase-design/SKILL.md. source_digest=sha256:a2411b0a300cb97766cea98e7b855ef78096a13362d2d3570e5ed8b0950d5d3e content_digest=sha256:7d1d77f0009d9e30ce321216dec018f5f0c9ca761aeabfb8c00db55c7a14b831 asd_version=13.3.0 schema=1
name: asd-phase-design
description: "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): dispatches creators sequentially, one per document independently enabled via documents.* (asd-ba for prd.html, a design-system gate only if ux_spec enabled, asd-ux for ux-spec.html, asd-architect for adr.html and c4-full/) — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design.md`.
