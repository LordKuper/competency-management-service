---
name: sequential-fix-rounds
description: Cross-file review-fix rounds in this repo are dispatched to ONE dev working sequentially, not parallel agents; re-read each file after touching it
metadata:
  type: feedback
---

When a review round's findings span adjacent rule docs / workflows / templates that mirror each other, the user dispatches a single dev to fix the whole set in order, and expects each touched file re-read after the edit.

**Why:** sprint 008 iterations 1 and 2 were both fixed by parallel dev dispatches, and each round closed its own findings while introducing a fresh cross-file contradiction (a workflow restating a rule doc it should only cite, with the restatement contradicting the SSoT). Agents editing adjacent files could not see each other's edits. The external reviewer flagged the pattern explicitly.

**How to apply:** when handed a multi-finding fix set, treat the mirror pairs (rule doc ↔ workflow ↔ template ↔ README) as one unit of work, fix the shared defect once at its SSoT and cite it everywhere else, and verify by grepping every mirror before reporting COMPLETED. Related: [[parallel-agent-commit-sweep]] for the staging hazard when siblings do run concurrently.
