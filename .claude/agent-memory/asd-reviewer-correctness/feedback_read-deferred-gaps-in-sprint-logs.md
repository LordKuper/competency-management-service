---
name: read-deferred-gaps-in-sprint-logs
description: Before the verdict, grep the sprint's decisions-log/friction-log for gaps the orchestrator explicitly "left for impl-review" - they are pre-found findings a diff-only read misses
metadata:
  type: feedback
---

Before emitting an impl-review verdict, grep `<sprint>/decisions-log.md` and `<sprint>/friction-log.md` for "left for impl-review", "flagged", and open friction rows touching the rules in the diff. Then check whether the current canon closes each one.

**Why:** sprint 019. The orchestrator logged "who flips a memory-fix `D-N` Status to fixed" as left for impl-review (friction F-2). iter-01 missed it because the review only read the diff. iter-02 caught it only by reading the logs. The gap was real: a memory `D-N` stays `pending` and gets re-collected every test-fix round.

**How to apply:** do this in every impl-review iteration, after the diff pass. A logged gap that the current diff's rules still leave open is in-surface when the diff touches its rule. Pairs with [[review-method-no-shell]].
