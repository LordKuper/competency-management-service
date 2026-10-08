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

- There is no separate full manual verification cycle before impl-test. Verification is done by impl-test with automated tests in the repository: API integration tests on real PostgreSQL through the existing test host, web tests on vitest with a mocked API.
- Impl (initial mode, every wave and every change request at the impl assessment gate) and impl fix modes (review-fix, test-fix) use minimal checks only: `dotnet build Competency.slnx --tl:off -c Release` with 0 warnings and 0 errors, `dotnet ef migrations has-pending-model-changes` clean, `npm --prefix web run lint`, `build` and `check:api` clean. No scratch database, no real-backend API or UI smoke, no screenshots. The dev report states what was not verified.
- Performance is measured only for a Task with a declared perf risk, on a reusable seed script for 10 000+ employees. The first Task with a declared perf risk creates the script; later Tasks reuse it.
- Playwright and e2e are not used (user decision).

## Multi-row invariant locking

- Every rule that guards an invariant spanning several rows (last active administrator, wrong-password counter, one account per employee, dismissal cascade) declares its lock in plan, including its place in the global lock order, and gets a deterministic race test in impl-test.

## Shared helper types

- Helper types shared across modules (paged list response, ProblemDetails rejections, LIKE escaping, page limits) live in `Competency.Platform`; never copy them per module.
