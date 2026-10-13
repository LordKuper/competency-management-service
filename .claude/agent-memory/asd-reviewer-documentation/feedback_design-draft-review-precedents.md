---
name: design-draft-review-precedents
description: Documentation reviewer in a consumer project's design-review - which sprint-draft deviations are accepted house practice (do not raise) and which defect shapes recur in UX-spec, ADR and c4-full drafts (raise)
metadata:
  type: feedback
---

Design-review drafts here (prd.html, ux-spec.html, adr.html, c4-full/subsystems.md, design-md-delta.yaml) are large (UX-spec 2000+ lines). Read the non-mockup sections whole (flows, decisions, text catalog, coverage, component usage, notes), grep the mockups for duplicated strings, and verify counts and cross-doc ids with Grep instead of rereading.

**Accepted house practice - do not raise:**
- `<div class="ds"><style>` token block inside the ux-spec fragment: mockup-preview CSS, the same pattern sits in `docs/ux/*.html` (promoted from earlier drafts); it is not shell chrome.
- Sprint-specific `owns:` in the fragment comment and a paraphrased `meta responsibility`: same in the persistent docs.
- ADR `SUBSYSTEM=project` when ADRs span subsystems: `asd-architect.md` Output format sets it, while `artifact-layout.md` says `sprint` for drafts. The conflict is in canon, not in the draft.
- Doc `STATUS`: draft at iteration 1; the orchestrator flips it to `in-review` (and the ADR `status-chip` to `accepted` after the user's design-gate accept) in the iteration-1 autofix commit - recorded in the decisions-log. Shell CSS defines `.status-in-review`.
- Extra UX-spec sections (UX decisions, text catalog, coverage) and the ADR "Reversibility / Revisit" line: the first are accepted UX practice, the second is mandated by `design-principles.md` 9.
- `design-md-delta.yaml` created by the UX autofix as a documentation-only delta (no token change) with an adaptive decisions-log line: sanctioned, not a scope defect.
- A stale `updated` date in PRD meta/STATS/footer after a later autofix: low, drop at floor medium (raised at iteration 1 only).

**Defect shapes that paid off:**
- c4-full delta carrying a `desk.md` Purpose/Key paths proposal: the registry template excludes purpose and key paths, and ADR-1's hard-gate block already holds them (copies drift). Fixed by pointing to the ADR-1 block.
- UX text catalog re-listing static mockup strings verbatim, against its own lede; route lists in both a UX decision and an ADR "final routes" table. After the trim, check the remaining catalog strings against the mockups word for word (iteration 2: "Вместе с версией" in the catalog vs "Вместе с ней" in two modals).
- DESIGN.md edits promised "at design-promote" in prose while no `design-md-delta.yaml` exists; once it exists, read it against `t_design-md-delta.yaml`: `new_value` must be the real token value (a sentinel like `"unchanged"` on a composite token breaks the patch) and `reason` is "why", not the table row to add.
- Resolved hard-gate branches left in accepted ADRs (fallback variant, "if not approved"), and counts restating the list below them. A leftover can also be the cross-reference that outlives its own fix (ADR-6 "routes provisional in U-22" after U-22 was rewritten to delegate to ADR-6).
- A sprint draft linking a sibling draft (`ux-spec.html` -> `adr.html#adr-6`): ADRs are never promoted standalone, so the link dangles in `docs/ux/`. Plain text or the fold-target doc instead.
- One enumerated rule (here the four `publishingActions` cases, who-is-checked) restated across PRD ACs, UX notes and ADR: grep a distinctive phrase across all three drafts, then compare the copies - one has usually drifted.
- External-norm claims ("recommended by OWASP") without a source, against `design-principles.md` 11 (iteration 2 added source and date: accepted).

**Why:** the finding rows are limited to one `f` per rubric row, so hang each finding on the row it fits best and merge same-kind drift; the decisions-log segments (`decisions-log.00N.md`) are the evidence for dates and approvals. A new file in the iteration (the delta) is absent from the `.diff` - read it whole.

**How to apply:** at design-review iteration 1 read each draft whole (no `.diff`); in later iterations read the hand-off `.diff` first, then whole files only for cross-checks.
