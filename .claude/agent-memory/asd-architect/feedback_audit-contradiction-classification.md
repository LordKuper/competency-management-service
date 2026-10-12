---
name: audit-contradiction-classification
description: How this project's audits classify superseded canonical wording, Постановка.md conflicts and intra-scope AC conflicts
metadata:
  type: feedback
---

Canonical persistent-doc wording that a user-accepted scope directly supersedes (old non-goals, "position is free text", "two roles only") is NOT a Contradictions entry: list it as a doc-to-update at design-promote (Gaps). `docs/Постановка.md` is non-canonical (outside the artifact-layout path map); when it conflicts with sprint.md and the user already decided at scope, record winner=sprint.md citing the decisions-log entry instead of re-opening a gate. Only AC-vs-AC conflicts with no canonical side (or undeliverable ACs) become hard user decisions, each with Problem/Options/Recommended/Consequences.

**Why:** prior audits in this repo followed this split and the user accepted it; re-gating already-decided scope wastes a hard gate.

**How to apply:** at every audit, sort conflicts into (a) superseded canonical wording -> Gaps, (b) Постановка vs decided scope -> resolved with decisions-log citation, (c) AC conflicts -> Contradictions for the user. Lite workflow: plan reads audit directly, so give a recommended option for each.
