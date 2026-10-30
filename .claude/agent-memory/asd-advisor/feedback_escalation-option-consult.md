---
name: escalation-option-consult
description: How the orchestrator frames a pre-escalation consult on a reviewer's question with options (a)/(b)/(c) — the "Weigh" list it expects answered and the closing line it wants
metadata:
  type: feedback
---

For an escalation consult on a reviewer question with lettered options, answer the orchestrator's "Weigh" list in order: (1) which option the *current* AC text requires (quote the AC, check whether a retired AC motivated the current code), (2) whether an earlier decisions-log acceptance of the behaviour still stands after later AC amendments (date-order the log lines), (3) per-option size and risk split as code / migration-or-none / tests; close with one option id and a one-line reason. Name which options are HARD-gate matters (new scope, waiver) per `checkpoints.md` "Gate policy".

**Why:** The 2026-10-30 F1 consult (sprint 006, chairless commission hold) was dispatched exactly in this shape; the decisive facts were in log ordering (acceptance under retired AC-26 at decisions-log:50/62 predated AC-66 at :75) and in a state check (no migration needed because `Status`+`ProcessedStage` already encode the held state).

**How to apply:** Before costing an option, grep the consumers of the status/flag it touches (e.g. `UnfinishedStatuses`, letter queries) — a stranded status is the hidden cost. Related: [[review-consult-format]].
