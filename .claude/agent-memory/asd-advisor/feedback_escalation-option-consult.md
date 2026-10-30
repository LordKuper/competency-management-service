---
name: escalation-option-consult
description: How the orchestrator frames a pre-escalation consult on a reviewer's question with options (a)/(b)/(c) — the "Weigh" list it expects answered and the closing line it wants
metadata:
  type: feedback
---

For an escalation consult on a reviewer question with lettered options, answer the orchestrator's "Weigh" list in order: (1) which option the *current* AC text requires (quote the AC, check whether a retired AC motivated the current code), (2) whether an earlier decisions-log acceptance of the behaviour still stands after later AC amendments (date-order the log lines), (3) per-option size and risk split as code / migration-or-none / tests; close with one option id and a one-line reason. Name which options are HARD-gate matters (new scope, waiver) per `checkpoints.md` "Gate policy".

**Why:** The decisive facts usually sit in the date order of the decisions-log entries (an earlier acceptance under a since-retired AC predating the AC that now applies) and in a state check showing no migration is needed because existing status fields already encode the state in question.

**How to apply:** Before costing an option, grep the consumers of the status/flag it touches (e.g. `UnfinishedStatuses`, letter queries) — a stranded status is the hidden cost. Related: [[review-consult-format]].
