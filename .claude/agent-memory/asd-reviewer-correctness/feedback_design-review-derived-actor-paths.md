---
name: design-review-derived-actor-paths
description: Product design-review in this app (competency service) - highest-yield checks are rights checks keyed on the acting user along indirect paths, canon-vs-ux contradictions, enumerations checked against the real code path, sibling write paths (PUT vs POST) of a new invariant, and mockup sample data showing forbidden states
metadata:
  type: feedback
---

In product design-review (PRD + ux-spec + ADR drafts), the defects that turned up were not in what each draft said on its own. They were in the places where the drafts disagreed with each other or with the code:

- A rights check written as "the actor may approve" (e.g. `viewer.CanManage`) breaks on derived or indirect paths, where the person acting is not the role the rule assumed. Examples: the author accepting an approver's changes; a step-1 approver whose action auto-approves the next step. Walk every path listed as "publishing" and check who actually clicks (sprint 004, ADR-4 cascade).
- When a fix adds an explicit enumeration ("publishes when: A, B, C"), check it against the existing code, not just the drafts. Sprint 004 iter-2: `MatrixWorkflow.AcceptChangesAsync` → `AdvanceToHrAsync` → `ToHrAsync` auto-approves and publishes after a step-1 return when the author is the responsible HR. ADR-4 `publishingActions` and PRD AC-9.4/34.4/34.7 listed `accept-changes` only "after an HR-step return".
- When a fix adds an invariant for one write path, look for the sibling write path. Sprint 004 iter-3: AC-34.10 made POST create a draft when the new record references drafts. PUT on an active competency (`ReferenceProblemAsync` → `IsUsableAsync(…, competency.AuthorId)` allows the author's drafts) still produced an active record that references a draft. Also look for a closure that stops at active nodes.
- ux-spec notes can contradict the subsystem canon in `docs/product/requirements/<subsystem>.html`, e.g. "action X does not depend on completeness" while the canon runs the completeness check at X. Grep the canon for the rule's trigger list.
- Mockup sample data often shows states the PRD forbids: the same object as two desk items, a version that is both a draft and on approval, refusal examples that cannot occur under the rights rules. Those texts go to plan approval, so they count. One shared state text (e.g. the truncation hint "do some actions") can be false for the reference-group variant that the ADR itself says behaves differently.
- `design-md-delta.yaml` follows `t_design-md-delta.yaml` (path/new_value/usage). A sentinel like `new_value: "unchanged"` on a component map would overwrite the map on promote.

**Why:** sprint 004 iter-1, iter-2 and iter-3 found all of their high and medium findings this way. Each draft was plausible on its own; the problems only showed up across files or against the code.

**How to apply:** For each ADR rule keyed on the current user, list every action that triggers it and who performs it, then confirm against the workflow code. For each new invariant, grep every endpoint that writes the guarded field. For each ux "independent of / always available" claim, check the canon trigger list. Scan mockup example rows for one-object-one-item and status-exclusivity violations. The token-mirror `.ds{--ds-*}` block and outlined-danger buttons are an established docs/ux convention, not findings. Related: [[review-method-no-shell]], [[no-prior-iteration-reports]].
