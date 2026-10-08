---
name: wave-review-probes
description: cheap probes that found real defects (or ruled out false ones) in a many-file domain-backend plus tests wave - decisions-log Open lines, audit.md design choices, unused test helpers, per-row lookup loops, in-body comments, memory bullets contradicting a new one
metadata:
  type: feedback
---

On a domain-backend plus API-test wave, these probes paid off and cost one Grep each:

- Grep `decisions-log.md` for `Open:` / the task's acceptance line before judging an invariant. An "Open:" note there is an
  unresolved item the orchestrator parked for review, not a user decision: raise it as a finding with a `question:` escalation
  (the last-administrator rule vs an unblocked admin bound to a dismissed employee was one).
- Before flagging a surprising side effect of a design choice (e.g. a role change voiding an invitation link through the
  security-stamp binding), Grep `audit.md` for it: an audit proposal the user accepted is authority, not a finding (sprint 002).
- For every helper, fixture class and extension in `tests/**/Infrastructure`, Grep the name over `tests`; a declaration with no
  other hit is dead code, which the over-engineering checklist makes critical.
- In a list endpoint, look for `foreach ... await directory.Find...` over page rows: a per-row lookup through a cross-module
  interface is the n+1 to flag (the org side joins, the user side looped).
- Scan the diff for added body comments and AC ids with `^\+\s+//[^/]|^\+.*[;{}]\s+//[^/]|AC-\d+` (code-style §7 bans body
  comments; §8 lets an AC id live only in a test's name or path, so `/// AC-N:` in class summaries counts once the floor is low,
  as does prose like "the acceptance ids are those of the sprint").
- When a wave adds an anonymous endpoint, re-read the "Permission matrix" summary of its endpoint class: it goes stale silently.
- When a review fix appends a memory bullet that replaces a technique, read the whole memory file: an older general bullet
  often still prescribes the rejected technique (the tester's gate-start race bullet outlived the held-lock rewrite, sprint 002).

**Why:** the wave diff was ~9.6k lines; the whole-diff read found the logic, these greps found the rule violations in minutes.

**How to apply:** run them after the diff read, before building the ledger.
