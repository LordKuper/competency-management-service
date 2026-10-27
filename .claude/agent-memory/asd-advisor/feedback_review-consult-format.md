---
name: review-consult-format
description: How the orchestrator wants impl-review escalation consults answered — per finding accept/override, minimal fix shape, requirement-change flag, one-line rationale
metadata:
  type: feedback
---

For an impl-review escalation consult (external/combined review findings), answer per finding: accept-for-fix or override, the minimal fix shape (file:line), a one-line rationale, and an explicit flag when the fix narrows or changes an AC, contract or concept (that part goes to the user as Complication Approval / changed-AC hard gate).

**Why:** The orchestrator asked exactly this ("keep it brief: one recommendation per finding, each with a one-line rationale… flag any finding that changes a requirement, contract or concept") and supplied its own leads to verify, not assume. Verifying leads against code found two that were incomplete (a "no user-visible limit" root fix that left a second quadratic path; a link fix that needed two sites, not one).

**How to apply:** Read every cited file plus the consumers of the touched symbol (grep) before agreeing with a lead; answer in the caller's order; put requirement/AC impact in its own line per finding so the orchestrator can lift it straight into the user-decision format.
