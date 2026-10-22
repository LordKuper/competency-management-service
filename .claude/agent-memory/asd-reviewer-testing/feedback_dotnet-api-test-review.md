---
name: dotnet-api-test-review
description: Reviewing this repo's xUnit API-integration tests without a shell - checks that catch neutered tests, false "equivalent mutant" claims, untested moved validations and new fields, missing race tests, and record gaps; plus ledger/persist-review shape facts
metadata:
  type: feedback
---

Backend tests here are HTTP tests against a real API process and PostgreSQL (`tests/Competency.Tests`), one `IClassFixture` host per class, helpers in `Infrastructure/`. Review them from the manifest `.diff` plus whole-file reads; production code is context only.

**1. A mechanical fixture swap can turn a precedence test into a duplicate.** When a field or rule is removed, the dev rewrites the document the test saves (`Content("text")` to `Content(4)`). For a test named "X is refused before Y judges the input", re-derive whether the swapped document still triggers Y. If the document is now valid, the test cannot fail for its claim and equals a sibling. **How to apply:** grep the sibling tests in the same file for the same save-then-act sequence; compare how a neighbouring test builds an incomplete document.

**2. "Equivalent mutant" claims are falsifiable by reading the upstream filter.** Read the production predicate and the query that feeds it. A conjunct redundant for one account state (blocked, filtered out by SQL) can be the only guard for another (invited, in the result set). List the states the API can create (blocked, invited, unbound, dismissed, inactive, draft) and check each against the survivor. Do the same for C# restatements of a rule whose SQL twin is tested.

**3. A validation that moved with a field loses its test silently.** The case list of a data-driven "is refused when malformed" test shrinks when the field is removed; the new location of the same check (another JSON path) is rarely re-added. **How to apply:** take the production error message, grep `tests/` for it; zero hits means untested.

**4. New request fields: grep the field name in `tests/`.** Zero hits for a field with keep-when-absent / clear-when-empty semantics, trimming and a length cap is a coverage gap on a persisted contract.

**5. Rules guarding several rows need a race test (project rule "Multi-row invariant locking").** For each new "cannot remove X while Y refers to it" or "can assign only with Z" rule, look in `CareerLockTests`-style tests for both orders (`RowLock.HoldAdvisoryAsync` + `StartWaitingAsync`). The shape to copy is the existing assign-versus-block race. Sequential tests do not count.

**6. Deleted data-migration test classes may carry the only pin on seed data.** Read the removed test bodies for assertions about shipped seed rows (texts, ids, "not journaled") and grep the remaining tests for those literals. The seed now lives in the squashed initial migration.

**7. `none` decisions: split the reason by property.** A reason about wall-clock nondeterminism says nothing about deterministic parts of the same budget (SQL command count, constant query count). The API host is a child process whose console log is captured (`ApiHost.Logs`, `LogsAfterAsync`), so command counting is feasible.

**8. Corroborating `runs: N` in the Added-tests table without a shell.** `runs` can be a multi-class batch total. Sum test methods per class (facts, plus theory rows: InlineData lines, member-data rows, enum member count via `Grep -c` on the enum file) and check single-class rows exactly; batch rows should equal a sum of whole classes.

**9. Ledger facts for this reviewer.** `persist-review` reads the FIRST table in the returned text as the Findings table, so no table may precede it. File rows take only `checked`/`n/a` (no finding reference); only rule rows carry `f`, one finding id per row. The validator requires the ledger `findings` list to equal the table ids, not every finding to be referenced by a row. The manifest may authorize no `n/a` at all.

**Why:** each of these was a defect or a false record that a green suite and a plausible mutation table hid.
**How to apply:** at the start of each backend wave, run points 3, 4 and 5 as greps before reading bodies; run 1 and 2 while reading rewritten tests and survivor notes. See [[no-shell-review-method]], [[review-fix-defect-proof]].
