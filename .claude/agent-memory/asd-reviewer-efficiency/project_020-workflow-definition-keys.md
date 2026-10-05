---
name: 020-workflow-definition-keys
description: Sprint 020 workflow definitions — next/rollback_reset are derivable from phases but judged keep-as-is; persist-review parses Findings columns by position
metadata:
  type: project
---

`.asd/workflows/<name>.json` stores `next` and `rollback_reset` even though both are mostly derivable from `phases`. In 020 impl-review wave-1 iter-01 I judged both keep-as-is: they are plan-Overview-binding, the orchestrator (an LLM) reads them directly instead of re-deriving them, and `tests/run.js` asserts `rollback_reset` equals the derivation. Only the unused `dir` parameter and the hand-listed Documentation ids in `NA_TARGETS.docs` were raised; both were fixed by iter-02 (APPROVE).

**Why:** if a later iteration flips this call without new evidence, the result is churn and inconsistent verdicts.

**How to apply:** do not raise the stored-derivable keys unless the test guard is dropped or a new consumer appears. Since 020, `persist-review` (`runtime.js` `reviewFindings`) reads a review's first table by column position (id, severity, location). A Category column must go after Location, never before it. Related: [[runtime-js-single-file]].
