---
name: wave-review-probes
description: cheap probes that found real defects in a many-file domain-backend plus tests wave - decisions-log Open lines, unused test helpers, per-row lookup loops, in-body comments
metadata:
  type: feedback
---

On a domain-backend plus API-test wave, these probes paid off and cost one Grep each:

- Grep `decisions-log.md` for `Open:` / the task's acceptance line before judging an invariant. An "Open:" note there is an
  unresolved item the orchestrator parked for review, not a user decision: raise it as a finding with a `question:` escalation
  (the last-administrator rule vs an unblocked admin bound to a dismissed employee was one).
- For every helper, fixture class and extension in `tests/**/Infrastructure`, Grep the name over `tests`; a declaration with no
  other hit is dead code, which the over-engineering checklist makes critical.
- In a list endpoint, look for `foreach ... await directory.Find...` over page rows: a per-row lookup through a cross-module
  interface is the n+1 to flag (the org side joins, the user side looped).
- Scan for body comments with `^\s+// |[;{}]\s+//[^/]` per source folder (code-style §7 bans them); a review-fix commit can add one.
- code-style §8 lets an AC id live only in a test's name or path, so `/// AC-N:` in class summaries counts once the floor is low.

**Why:** the wave diff was ~9.6k lines; the whole-diff read found the logic, these greps found the rule violations in minutes.

**How to apply:** run them after the diff read, before building the ledger.
