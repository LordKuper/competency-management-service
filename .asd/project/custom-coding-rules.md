---
responsibility:
  owns: project-owner custom rules read during impl and impl-review phases
  excludes: universal rules, design-only rules
  delegates_to: custom-common-rules.md (all phases), custom-design-rules.md (design/design-review)
---

# Custom Coding Rules

Project rules applying only to code and tests. Read by `asd-dev`, `asd-tester`, impl-review reviewers.

Put here: forbidden libraries/APIs, perf budgets (latency, memory, throughput, regression tolerances), security policy, test coverage thresholds, code-style constraints beyond `.asd/rules/code-style.md`. ASD never overwrites this file.

## Verification depth in impl

- Impl (initial mode, every wave and every change request at the impl assessment gate) is done as fast as possible with minimal checks: `dotnet build Competency.slnx --tl:off -c Release` with 0 warnings and 0 errors, `dotnet ef migrations has-pending-model-changes` clean, `npm --prefix web run lint`, `build` and `check:api` clean. No scratch database, no real-backend API or UI smoke, no screenshots, no performance runs. The dev report states what was not verified.
- The full verification cycle (scratch PostgreSQL with migrations applied through the app, real-backend API smoke, UI smoke, performance checks for AC-11) runs once, when the user first approves the transition from impl to impl-test, before impl-test starts.
- Impl fix modes (review-fix, test-fix) use the minimal checks only; they never repeat the full cycle. impl-test and impl-review keep their own gates unchanged.
