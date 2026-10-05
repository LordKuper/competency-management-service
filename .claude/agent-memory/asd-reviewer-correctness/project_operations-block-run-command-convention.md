---
name: operations-block-run-command-convention
description: A workflow's `## Operations used` run-command line declares only git + commands.yaml operations; `node .asd/runtime.js` invocations are never declared there - check uniformity across sibling workflows before raising an undeclared-operation finding
metadata:
  type: project
---

Before raising "step N runs a command the `## Operations used` block never declared", grep every sibling `asd-phase-*.md` for the same omission. If it is uniform, it is convention, not a defect.

**Why:** sprint 010 impl-review iter 2. `asd-phase-design-review.md` has no `run command` operation at all, yet steps 3a/8/8a run `node .asd/runtime.js external-preflight|validate-ledger|manifest-digest`; `asd-phase-impl-review.md` declares one but enumerates only `git diff`/`git show` + `commands.yaml`. Looked like a clean twin of the impl.md gap the same sprint had just fixed - until the repo-wide grep showed that `asd-phase-impl.md` (step 5a) and `asd-phase-impl-test.md` (step 1a) also run `route-task` undeclared, including in impl.md's *newly added* grant line. Five for five: runtime.js is treated as orchestrator-internal tooling, outside the block's scope. Raising it would have shipped a false finding on a fix round.

**How to apply:** the block IS grant-bearing here (sprint 002 quality finding #2 added a run-command op for exactly this reason), so the finding class is real - but it only lands for git or `commands.yaml` operations, and only where the sibling workflows do declare theirs. Same shape as the "rule with no acting-site binding" hunt in [[review-method-no-shell]]: confirm the acting-site convention before calling a gap a gap.
