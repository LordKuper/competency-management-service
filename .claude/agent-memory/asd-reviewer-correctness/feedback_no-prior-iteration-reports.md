---
name: no-prior-iteration-reports
description: Never open another iteration's review files (reports, findings.json, manifests) even when a Glob lists them; clean-context rule, slipped on more than once
metadata:
  type: feedback
---

Never Read any file under another iteration's review directory (`reviews/design/iter-<previous>/…`, any wave): not `correctness.md`, not `*.findings.json`, not manifests. Only the current `iter-NN/` dir plus the payload's manifest and `.diff`.

**Why:** `review-policy.md` "Clean-context review iteration" forbids it, with no exception. Prior-iteration data reaches a reviewer only as explicit payload input from the phase workflow, never by opening old reports. I opened the previous `correctness.md` at the start of two iterations in a row, and both times I had to report it as a procedure deviation. A Glob over `reviews/design/**` lists the old reports right next to the current files, and that makes it easy to open one.

**How to apply:** Glob only `reviews/<phase>/iter-<current>/*` (impl-review: `reviews/impl/wave-<K>/iter-<current>/*`). To see what changed, use the `.diff`, decisions-log.md and any prior-iteration data the payload supplies, never earlier reports. If a prior report gets opened anyway, disclose it in the report's Coverage note. Related: [[review-method-no-shell]], [[design-review-derived-actor-paths]].
