---
# ASD generated. Edit .asd/skills/asd-phase-design/SKILL.md. source_digest=sha256:90f721e0a22e6e82c8d34b9196d5efa23fa93fd59ab8c214e49955d5ea689d1e content_digest=sha256:2b0a2df6020bacb4b7828d425846cddbdaf380cf1ba979c2c7e377bdad99252a asd_version=14.0.0 schema=1
name: asd-phase-design
description: "Runs the ASD design phase for the active sprint (standard workflow only; lite has no design phase): the main orchestrator drafts inline, section by section with lock-in, each document independently enabled via documents.* (prd.html, ux-spec.html if ux_spec enabled, adr.html, c4-full/) and runs the design-system gate only if design_system is enabled — when every document is disabled, one deterministic check collapses design/design-review/design-promote into a single no-op write (phase=design-promote, NEXT=plan), and neither of the other two phases is dispatched separately. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design.md`.
