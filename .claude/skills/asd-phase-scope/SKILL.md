---
# ASD generated. Edit .asd/skills/asd-phase-scope/SKILL.md. source_digest=sha256:dff87a5da486ba75f72008ced9587f71ad738e55b8df4ddef9b5091922ca2646 content_digest=sha256:2cc4250e98a3d3b3945c1cbdf14bd87b669e187f6a68499b79dd28b6cbf9e343 asd_version=14.1.0 schema=1
name: asd-phase-scope
description: "Runs the ASD scope phase of a sprint: the main orchestrator asks the user to choose the sprint workflow (standard or lite, frozen into state.json), creates the sprint folder, state.json and branch, runs staged solution design (requirements interview, its own best-practice research, approach choice into research.md), and refines scope under the active gate policy; under self-hosting it also triages open ASD retro issues at the retro intake. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash WebSearch WebFetch AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-scope.md`.
