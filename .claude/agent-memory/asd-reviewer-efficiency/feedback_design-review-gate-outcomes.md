---
name: design-review-gate-outcomes
description: In design-review, read the rotated decisions-log segments to learn which ADR gates the user already settled before judging fallbacks, Complication Approvals and hard-gate blocks
metadata:
  type: feedback
---

Before judging an ADR's Complication Approvals, "Hard gate" blocks or fallback variants, read `decisions-log.md` and every rotated `decisions-log.NNN.md` in the sprint folder. After rotation the live log can be empty while the accepted-gate entries sit in a rotated segment.

**Why:** a gate the user approved is not re-raised, and a fallback the approval made unreachable is dead design text — the over-engineering checklist item "dead code left in case we need it".

**How to apply:**
- Approved Complication Approvals (new interface, port method, queue, subsystem): assess as keep-as-is and do not raise them.
- Leftover "if not approved, variant X" sentences, pending-gate blocks and conditional fold targets in the ADR: raise as one critical `simplify` finding with the exact lines, fix is mechanical deletion, no escalation.
- Related: [[runtime-js-single-file]] for the same "user decision stands, judge only new code" stance.
