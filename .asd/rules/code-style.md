# Code Style

Implementation-level rules for code-writing agents (Dev, Tester). Binding during `impl` and `impl-test`, verified during `impl-review`. Governs how code is written; architecture-level rules out of scope. All code in English.

## 1. Engineering Principles

- Follow SOLID, KISS, DRY, YAGNI.
- Small atomic functions, single clear responsibility.
- Readability over cleverness.
- No hidden coupling, global state, or action at a distance.
- Apply the over-engineering AND structure/cohesion checklists (`review-policy.md`) and both `artifact-layout.md` iron rules — SSoT and documentation economy — PROACTIVELY while authoring, not only at review — the same items impl-review judges against.

## 2. Naming

- Names reveal intent. Abbreviations only when domain-standard.
- Casing follows language convention. No Hungarian or type prefixes.
- Booleans read as predicates (`is`, `has`, `should`).
- One concept, one name across the codebase.

## 3. Scope Discipline

- Touch only what the task requires; every changed line traces to the task.
- Match surrounding code style.
- Do not refactor adjacent code the task did not ask for.
- Remove only the code and dependencies your change introduced.

## 4. Functions and Modules

- Guard clauses over deep nesting. Keep nesting shallow.
- Pass explicit parameters; no implicit globals or ambient state.
- A function needing a paragraph to explain itself is too big — split it.

## 5. Root Cause Over Patch

- Fix the underlying cause, not the symptom.
- No temporary workarounds, no masking of failures.

## 6. Error Handling

- No swallowed errors. Empty catch block forbidden.
- Validate input at system boundaries; reject bad state early.
- Errors carry context (what failed, with which inputs).
- Do not use errors/exceptions as control flow for expected cases.

## 7. Comments and Documentation

- No comments inside method/function bodies, ever. Meaning belongs in the name, the signature, or the member's doc comment — a body that needs narration is renamed, split, or rewritten instead of commented. The sole permitted in-body marker is `// TODO(sprint-<NNN-slug>): <reason>` (see below).
- Doc comments mandatory on every public/exported type and member. Internal code documents only what a clear name cannot carry. Doc comments explain WHY, not WHAT — this WHY allowance applies to doc comments only, never to in-body comments (which are banned above).
- Type-level doc: short, states the type's purpose ONLY — never duplicates or summarizes its members' docs. Member-level doc: short, states the member's purpose, never its implementation. Each member carries its own doc; state each fact once.
- Comments concise and clear — the economy rule for agent-facing text (`artifact-layout.md` "Documentation economy") governs comment text too.
- Inherit docs (`<inheritdoc/>`, `@inheritDoc`, etc.) wherever an override or implementation matches the base contract; do not restate inherited text.
- Use the language-native doc format (XML-doc C#, docstrings Python, JSDoc TypeScript, etc.).
- Update doc comments when code changes.
- No commented-out code, no dead code kept "in case".
- A `TODO` uses marker `// TODO(sprint-<NNN-slug>): <reason>` with a matching entry in the project stub registry.

## 8. References to Project Documents

- Code (comments, doc strings, string literals) must not reference project documents (rules, ADR, PRD, UX spec, decisions log, sprint files).
- Code is the SSoT for behavior; in-code document references rot once those documents move.
- Do not quote or paraphrase document text in code; replace with a brief standalone rationale (e.g. `not deterministic by design`).
- Exception: `TODO` markers may carry a reference, since they are tracked and removed.
- Exception: a test file's name or path may carry an `AC-N` id (the impacted-set AC-citation convention, `sprint-lifecycle.md` "Impacted test set") — an id only, never prose referencing a document.

## 9. Types and Contracts

- Explicit types at boundaries. Untyped escape hatches (`any` and equivalents) only with written justification.
- Make illegal states unrepresentable where the language makes it cheap.
- Honor declared API and interface contracts exactly.

## 10. State

- Prefer immutable, local data. No shared mutable state without explicit synchronization.
- No hidden side effects in functions that look pure.
- Keep variable scope and lifetime minimal.

## 11. Determinism

- Prefer logic producing the same outputs for the same inputs, unless the task requires otherwise.
- Avoid incidental time- or order-dependent behavior in core logic.
- When nondeterminism is intended, isolate it behind a small interface; test the deterministic part separately.

## 12. No Hardcoded Values

- Tuning/configuration values live in external config files, not code.
- UI visual values (color, typography, spacing, radii) bind to design tokens. No raw hex, px, or pt in UI code.

## 13. Dependencies

- A new third-party dependency follows `checkpoints.md` decision policy before use; retain trust-boundary and compatibility checks.
- Prefer the standard library. Pin dependency versions.

## 14. External API Verification

- Do not rely on training data for the API of a library, framework, or runtime.
- Verify signatures and behavior against the pinned version's documentation before use.

## 15. Security

- No secrets in code, logs, or commits.
- Validate and sanitize all external input.
- Use parameterized queries/APIs; never build SQL, shell, or other commands by string concatenation.
- Least privilege for every credential, token, access scope.

## 16. Logging

- Logs are structured and carry context.
- Never log secrets or PII.
- Meaningful log levels: error (failures), warn (recoverable anomalies), info (state changes), debug (detail).

## 17. Tests

Written and run in `impl-test`, never in `impl` — one carve-out: a plan-declared `Test-only` Task is dispatched to `asd-tester` (shape: `sprint-lifecycle.md` "Plan file format"). Selection happens **after** the implementation is accepted, against the real change surface.

- Risk-based and change-scoped: pick the cheapest reliable check per material risk — static/architecture check → focused unit or property test for logic → component or contract test at boundaries → only essential e2e journeys.
- Every acceptance criterion is covered by a check at some level; the level is chosen by risk, not by rule.
- Tests verify observable behavior, not implementation detail.
- **Hypothetical-risk criterion — single home, governs both authoring and pruning:** a test earns its place only by covering a real, material risk on the change surface, evidenced by actual behavior, an identified failure mode, or a stated requirement. A test verifying a hypothetical rather than a real risk — including one whose behavior an existing check already covers, or whose only value is a coverage number — is not authored in the first place, and is a removal candidate wherever it already exists. Authoring is never the default: absent a qualifying risk, "no new test needed" is a first-class outcome of the strategy pass, not a silent fallback — record the decision (`none`) and its reason in `test-plan.md`.
- Forbidden: trivial, implementation-coupled, mock-confirming, redundant, flaky tests, and any test failing the hypothetical-risk criterion above. Duplicates of an existing check are deleted, not kept "for safety".
- Every fixed defect leaves a regression test proven against the pre-fix behavior (fail-first run recorded) or an equivalent targeted mutation. The record names the exact command, its non-zero exit code and the runner-reported name of the failing test; a mutation proof records the same for the mutated run. A bare claim ("proved", "fails without the fix") is no proof. A mutation made to prove fail-first is restored before the agent's next tool call. A mutation left on disk is a defect regardless of what the suite reports.
- Coverage numbers locate untested code; they are never a quota or a gate.
- Deterministic: no `sleep`, wall-clock timing, random seeds, or execution-order reliance.
- Isolated: no real external APIs, databases, or file I/O; use dependency injection.
- No hardcoded test data: build fixtures from named constants or factories (exception: boundary-value tests where the literal is the point).
- A test or rule that names the members of a set derives that set from its source wherever a source exists, rather than enumerating members by hand. A hardcoded enumeration drifts silently as the source changes.
- A content-contract test pins a token that cannot be reworded without changing the contract — a heading, a command, a field name, a parsed literal — never the surrounding prose. Its fail-first proof is bounded per added test: one mutation per asserted relation plus one reword control (the same relation reworded must stay green); the `test-plan.md` "Added tests" row's `Regression proof` cell records the run count.
- Test files named `<system>_<feature>_test.<ext>`; test functions `test_<scenario>_<expected>`.
- A test mutating global/static state saves and restores it in setup/teardown.
- Structure each test as Arrange — Act — Assert.

## 18. Concurrency

- No data races. Document thread-safety of every shared component.
- Every blocking I/O call has a timeout.
- Keep main/UI threads free: offload CPU-heavy and I/O-bound work. No `sleep`, busy-wait, or synchronous locks on the main/UI thread.

## 19. Formatting

- The project formatter and linter decide style; no manual style debate.
- Style consistent within a file.
- Preserve a file's existing line endings. A scripted or regex edit anchors on the file's actual EOL, never an assumed `\n` — assuming the wrong one strips or orphans the `\r` on every line it rewrites. Symptom: a whole-file diff for a small edit. Prefer an editing tool that keeps EOL; verify the diff is the size of the change before staging.
- Build and lint must pass before any commit; the impacted test set (`sprint-lifecycle.md` "Impacted test set") gates `impl-test`, not each commit — the full suite runs once, at the end of `impl-review`.
- Run the pre-commit lint against staged content: `git diff --check` inspects unstaged changes only and exits 0 once the damage is staged, so a stage → lint → commit sequence passes blind. Use `git diff --cached --check`. `git diff --check HEAD` is a superset, not an equivalent — it compares worktree against HEAD, so it also flags unstaged content nobody is committing (in a shared worktree, a sibling's edits); acceptable only when the tree holds nothing but the staged change, and a project's configured `lint` command must be the staged form.

## 20. Per-Language Rules

Language- and stack-specific rules are added below as the project stack is fixed. Empty until then.
