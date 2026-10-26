---
# ASD generated. Edit .asd/skills/asd-phase-impl/SKILL.md. source_digest=sha256:d3910cb0d81e097ab68f255bfb94b84c5ebfd1db3b3db3a695613737f93a4f4d content_digest=sha256:f73400499e73b1a960e0f45c5b92b400ae8104ee69d3843d04f050585d99e077 asd_version=14.0.0 schema=1
name: asd-phase-impl
description: "Runs the ASD impl phase in one of three modes detected from state.json: initial mode dispatches plan.md Task blocks to devs, review-fix mode resolves impl-review findings, test-fix mode resolves code defects found by impl-test. Devs write production code only (no tests; a plan-declared Test-only Task goes to asd-tester instead), run build/lint, and commit; the phase enforces a build+lint completion gate before COMPLETED and always routes to impl-test. Use when asd-sprint dispatches the impl phase, or when the user explicitly asks to run or re-run impl for the active sprint."
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion Task"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl.md`.
