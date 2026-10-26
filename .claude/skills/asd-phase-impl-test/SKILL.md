---
# ASD generated. Edit .asd/skills/asd-phase-impl-test/SKILL.md. source_digest=sha256:4398e7b5033df617a984f8adc30b3aafc565930436ee986506f10a036a4301f3 content_digest=sha256:800a1376fa3ffac0de76c08e57b6afac9c94c507c6972ed56325459fa642832d asd_version=14.0.0 schema=1
name: asd-phase-impl-test
description: "Runs the ASD impl-test phase: dispatches asd-tester to run the existing impacted tests first, pick the test approach for the whole change scope after the code exists, prune redundant tests, author missing ones, and run the impacted set (never the full suite, which runs once at the end of impl-review) as its suite gate. Green routes to impl-review; code defects are recorded in test-plan.md and route back to impl test-fix mode. Use when asd-sprint dispatches the impl-test phase, or when the user explicitly asks to run or re-run impl-test for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl-test.md`.
