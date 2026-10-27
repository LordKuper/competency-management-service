---
# ASD generated. Edit .asd/skills/asd-phase-scope/SKILL.md. source_digest=sha256:dff87a5da486ba75f72008ced9587f71ad738e55b8df4ddef9b5091922ca2646 content_digest=sha256:aad453257ed4a645cfd68bba332ea66c14fb2e7cc24733dcc04c4d99098cb872 asd_version=14.1.0 schema=1
name: asd-phase-scope
description: "Runs the ASD scope phase of a sprint: the main orchestrator asks the user to choose the sprint workflow (standard or lite, frozen into state.json), creates the sprint folder, state.json and branch, runs staged solution design (requirements interview, its own best-practice research, approach choice into research.md), and refines scope under the active gate policy; under self-hosting it also triages open ASD retro issues at the retro intake. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-scope.md`.
