---
# ASD generated. Edit .asd/skills/asd-phase-scope/SKILL.md. source_digest=sha256:ece9fa38eb3c48b734a26ce3e78cdac88743da90f712857edd0756fc4ffc5c93 content_digest=sha256:d27dc1d5e9cf3534df6ed7a8aeef83a13f627957864326586ff6f2dbd87edee7 asd_version=13.10.0 schema=1
name: asd-phase-scope
description: "Runs the ASD scope phase of a sprint: the phase orchestrator asks the user to choose the sprint workflow (standard or lite, frozen into state.json), creates the sprint folder, state.json and branch, runs staged solution design (requirements interview, BA best-practice research, approach choice into research.md), and refines scope under the active gate policy. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint."
allowed-tools: "Read Glob Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-scope.md`.
