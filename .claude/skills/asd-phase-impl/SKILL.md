---
# ASD generated. Edit .asd/skills/asd-phase-impl/SKILL.md. source_digest=sha256:90f96c8ed3267ef92f709dcd6cceb255210bccbe4126d0963e576081f8f2d2f3 content_digest=sha256:4c40ae6482abd91f1596c23055cb2988f7239413e134b8cebbc14692fb756a64 asd_version=13.8.0 schema=1
name: asd-phase-impl
description: "Runs the ASD impl phase in one of three modes detected from state.json: initial mode dispatches plan.md Task blocks to devs, review-fix mode resolves impl-review findings, test-fix mode resolves code defects found by impl-test. Devs write production code only (no tests; a plan-declared Test-only Task goes to asd-tester instead), run build/lint, and commit; the phase enforces a build+lint completion gate before COMPLETED and always routes to impl-test. Use when asd-sprint dispatches the impl phase, or when the user explicitly asks to run or re-run impl for the active sprint."
allowed-tools: "Read Write Edit AskUserQuestion Task Bash(node .asd/runtime.js:*)"
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl.md`.
