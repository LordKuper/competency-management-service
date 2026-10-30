---
name: threshold-is-contract
description: A test threshold pinned in docs/architecture/ (SQL-command budget, limits) is a contract; a red run against it is a code defect, never a reason to move the threshold
metadata:
  type: feedback
---

A test threshold pinned in `docs/architecture/` (SQL-command budget, limits) is a contract. A red run against it is a code defect: append a `D-N` row to `test-plan.md` `Defects`, leave the threshold unchanged.

**Why:** raising the number to make the run green hides the regression the budget exists to catch.

**How to apply:** before touching an asserted budget or limit, find where `docs/architecture/` states it; a test may change it only together with that document by an approved decision, never to turn a run green. Related: [[backend-tests-scratch-build]] (counting SQL commands of a request).
