---
name: trace-ac-to-motivating-case
description: For a retro-remediation AC, re-check the fix-round narrowing against the retro row's own evidenced case - a validation fix can make the one case the AC exists for inexpressible; also catch fix rounds that widen an AC's stated bound without the hard AC gate
metadata:
  type: feedback
---

When an AC cites a retrospective row (`(011 P3)`, `(010 F-5)`), open that row in the archived `retrospective.html` / `friction-log.md` and check that the case it cites still gets through the rule as written at HEAD. Review-fix rounds tighten rules (validation, ordering) to close a finding, and the tightening can quietly remove the motivating case.

**Why:** sprint 012 impl-review iter 2. The fix for iter-01's COR-1-3 made `asd-init` sprint-mediated mode reject keys missing from `t_config.yaml`, and the grammar added "never one a same-sprint task adds". Together with "alone in wave 1, applied before wave 1", that left 011 P3's only evidenced case (a sprint that ships a setting and enables it, 011 MS-1) still needing a manual step. A decisions-log entry from earlier in the same sprint had rejected key validation for exactly that reason, and the later acceptance never recorded the reversal.

**How to apply:** at every iteration of a retro-remediation sprint, for each AC touched by the fix round, (1) read the retro row, (2) walk its concrete case through the current rule and its acting-site order, (3) grep `decisions-log.md` for an earlier entry whose rationale the fix contradicts. Pairs with [[review-method-no-shell]]. Raise it as an AC coverage finding, not as a flip-flop on the earlier finding.

The mirror case: a fix round that WIDENS a bound the AC text states verbatim (sprint 013 iter 2: AC-19 "limited to release-mandated key renames and removals" widened in canon to value mappings/insertions/comment rewrites, `sprint.md` unchanged, routed as autofix). `checkpoints.md` makes a changed AC hard, so check `decisions-log.md` for user authority on the AC wording before accepting it.
