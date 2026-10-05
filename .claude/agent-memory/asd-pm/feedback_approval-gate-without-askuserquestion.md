---
name: approval-gate-without-askuserquestion
description: When dispatched without a discrete user-decision tool, present the draft in text and let the dispatcher run the gate; do not write artefacts on relayed free text alone without recording provenance.
metadata:
  type: feedback
---

When dispatched as a subagent whose toolset has no discrete user-decision tool, do not silently downgrade a HARD gate to free-text approval, and do not deadlock either. Present the refined artefact in the text response, emit `QUESTION`/blocked, and let the dispatching skill run the decision call on your behalf. When it relays explicit decisions back, proceed — but record in the sprint `decisions-log.md` that the gate was executed by the orchestrating skill rather than observed directly.

**Why:** `checkpoints.md` + `language-policy.md` require an auditable discrete-option decision at every gate, and agent messages are not by themselves user consent. The user validated this workaround on sprint 002-lean-workflow (2026-09-01): they ran the decision call themselves and expected the agent to continue rather than block indefinitely.

**How to apply:** any phase gate (scope approval, plan sections, impl assessment, PR confirmation) reached without the decision tool. Proceed only when the relayed decision is explicit and unambiguous, the presented draft is unchanged, and provenance is written to the log. See [[escalate-structural-git-conflicts]] for the halt-instead case.
