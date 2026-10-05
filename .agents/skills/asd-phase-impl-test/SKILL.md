---
# ASD generated. Edit .asd/skills/asd-phase-impl-test/SKILL.md. source_digest=sha256:2ab69e3e919b66863b95c2097bba108cc2f49dfc04b2c2926a37bc96c38ec6c2 content_digest=sha256:1fcf6ec18fb0d022db8eb3be22fbcb871f0c58c7dc570c2debdb2ce93ebe7138 asd_version=7.1.0 schema=1
name: asd-phase-impl-test
description: "Runs the ASD impl-test phase: dispatches asd-tester to run the existing impacted tests first, pick the test approach for the whole change scope after the code exists, prune redundant tests, author missing ones, and run the impacted set (never the full suite, which runs once at the end of impl-review) as its suite gate. Green routes to impl-review; code defects are recorded in test-plan.md and route back to impl test-fix mode. Use when asd-sprint dispatches the impl-test phase, or when the user explicitly asks to run or re-run impl-test for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl-test.md`.
