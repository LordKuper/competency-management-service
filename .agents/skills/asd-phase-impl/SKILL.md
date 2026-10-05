---
# ASD generated. Edit .asd/skills/asd-phase-impl/SKILL.md. source_digest=sha256:428b6fa0737105a6827ed363137757348bef21b247e3f20ac674326d28365a86 content_digest=sha256:da3c047bed9cab80557c5bc313b3b71b892e72c6a621ba15f04a4acd34d7cc59 asd_version=13.5.0 schema=1
name: asd-phase-impl
description: "Runs the ASD impl phase in one of three modes detected from state.json: initial mode dispatches plan.md Task blocks to devs, review-fix mode resolves impl-review findings, test-fix mode resolves code defects found by impl-test. Devs write production code only (no tests; a plan-declared Test-only Task goes to asd-tester instead), run build/lint, and commit; the phase enforces a build+lint completion gate before COMPLETED and always routes to impl-test. Use when asd-sprint dispatches the impl phase, or when the user explicitly asks to run or re-run impl for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-impl.md`.
