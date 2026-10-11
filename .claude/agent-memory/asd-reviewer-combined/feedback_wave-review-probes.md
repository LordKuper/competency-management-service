---
name: wave-review-probes
description: cheap probes that found real defects (or ruled out false ones) in a many-file domain-backend plus tests wave - decisions-log Open lines and accepted trade-offs, audit.md design choices, unused test helpers and constants, per-row lookup loops, in-body comments, memory bullets contradicting a new one or the code
metadata:
  type: feedback
---

On a domain-backend plus API-test wave, these probes paid off and cost one Grep each:

- Grep `decisions-log.md` for `Open:` / the task's acceptance line before judging an invariant. An "Open:" note there is an
  unresolved item the orchestrator parked for review, not a user decision: raise it as a finding with a `question:` escalation
  (the last-administrator rule vs an unblocked admin bound to a dismissed employee was one). The same log also records accepted
  trade-offs (e.g. tsvector concatenation without a GIN index, accepted with a measured p95 in test-plan) - not a finding.
- Before flagging a surprising side effect of a design choice (e.g. a role change voiding an invitation link through the
  security-stamp binding), Grep `audit.md` for it: an audit proposal the user accepted is authority, not a finding (sprint 002).
- For every helper, fixture class, extension and public constant in `tests/**/Infrastructure`, Grep the name over `src` and
  `tests`; a declaration with no other hit is dead code, which the over-engineering checklist makes critical (e.g. an
  unused migration-id constant among used ones).
- In a list endpoint, look for `foreach ... await directory.Find...` over page rows: a per-row lookup through a cross-module
  interface is the n+1 to flag (the org side joins, the user side looped).
- Scan the diff for added body comments and AC ids with `^\+\s+//[^/]|^\+.*[;{}]\s+//[^/]|AC-\d+` (code-style §7 bans body
  comments; §8 lets an AC id live only in a test's name or path, so `/// AC-N:` in class summaries counts once the floor is low,
  as does prose like "the acceptance ids are those of the sprint").
- `AcN_` prefixes in test method names follow the subsystem docs' AC numbering (Platform `Ac5_`, `Ac16_`, Deployment `Ac8_`),
  not the current sprint.md: a mismatch with sprint ACs is not a finding.
- When a wave adds an anonymous endpoint, re-read the "Permission matrix" summary of its endpoint class: it goes stale silently.
- When a review fix appends a memory bullet that replaces a technique, read the whole memory file: an older general bullet
  often still prescribes the rejected technique (the tester's gate-start race bullet outlived the held-lock rewrite, sprint 002).
- A memory bullet that quotes a code literal in shorthand (a `\2000-\200A` range in a `btrim` character set, where `-` is a
  literal hyphen) is a copy hazard: compare it with the constant in code.

**Why:** the wave diff was ~9.6k lines; the whole-diff read found the logic, these greps found the rule violations in minutes.

**How to apply:** run them after the diff read, before building the ledger.
