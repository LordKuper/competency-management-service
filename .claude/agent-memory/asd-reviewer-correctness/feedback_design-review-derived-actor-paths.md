---
name: design-review-derived-actor-paths
description: Product design-review in this app (competency service) - highest-yield checks are rights checks keyed on the acting user along indirect paths, canon-vs-ux contradictions, and mockup sample data showing forbidden states
metadata:
  type: feedback
---

In product design-review (PRD + ux-spec + ADR drafts), the defects that surfaced were not in what the drafts say, but in where they disagree:

- A rights check written as "the actor may approve" (e.g. `viewer.CanManage`) breaks on derived/indirect paths where the actor is not the role the rule assumed: the author accepting an approver's changes, a step-1 approver whose action triggers auto-approval of the next step. Walk every path listed as "publishing" against who actually clicks (sprint 004, ADR-4 cascade).
- ux-spec notes can contradict the subsystem canon in `docs/product/requirements/<subsystem>.html` (e.g. "action X does not depend on completeness" while canon runs completeness at X). Grep the canon for the rule's trigger list.
- Mockup sample data often shows states the PRD forbids (same object as two desk items, a version both draft and on approval, refusal examples that cannot occur under the rights rules). Those texts go to plan approval, so they count.

**Why:** sprint 004 iter-1 found all of its high/medium findings this way. The drafts were each internally plausible and only conflicted across files.

**How to apply:** for each ADR rule keyed on the current user, list every action that triggers it and who performs it. For each ux "independent of / always available" claim, check the canon trigger list. Scan mockup example rows for one-object-one-item and status-exclusivity violations. The token-mirror `.ds{--ds-*}` block and outlined-danger buttons are an established docs/ux convention, not findings. Related: [[review-method-no-shell]].
