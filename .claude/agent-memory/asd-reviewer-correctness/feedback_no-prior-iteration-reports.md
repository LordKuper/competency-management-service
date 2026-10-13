---
name: no-prior-iteration-reports
description: Never open reviews/<phase>/iter-<other>/ files (reports, findings.json, manifests) even when a Glob lists them; clean-context rule, violated in sprint 004 iter-2 AND iter-3
metadata:
  type: feedback
---

Never Read any file under another iteration's review directory (`reviews/design/iter-0<N-1>/…`, any wave): not `correctness.md`, not `*.findings.json`, not manifests. Only the current `iter-NN/` dir plus the payload's manifest and `.diff`.

**Why:** `review-policy.md` "Clean-context review iteration" forbids it. In sprint 004 I opened the previous `correctness.md` at the start of both iter-2 and iter-3, and both times I had to report it as a procedure deviation. A Glob over `reviews/design/**` lists the old reports right next to the current files, and that makes it easy to open one.

**How to apply:** Glob only `reviews/<phase>/iter-<current>/*`. To see what changed, use the `.diff` and decisions-log.md, never earlier reports. Related: [[review-method-no-shell]], [[design-review-derived-actor-paths]].
