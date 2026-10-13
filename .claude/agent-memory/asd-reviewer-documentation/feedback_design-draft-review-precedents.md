---
name: design-draft-review-precedents
description: Documentation reviewer in a consumer project's design-review - which sprint-draft deviations are accepted house practice (do not raise) and which defect shapes recur in UX-spec, ADR and c4-full drafts (raise)
metadata:
  type: feedback
---

Design-review drafts here (prd.html, ux-spec.html, adr.html, c4-full/subsystems.md) are large (UX-spec 2000+ lines). Read the non-mockup sections whole (flows, decisions, text catalog, coverage, component usage, notes), grep the mockups for duplicated strings, and verify counts and cross-doc ids with Grep instead of rereading.

**Accepted house practice - do not raise:**
- `<div class="ds"><style>` token block inside the ux-spec fragment: mockup-preview CSS, the same pattern sits in `docs/ux/*.html` (promoted from earlier drafts); it is not shell chrome.
- Sprint-specific `owns:` in the fragment comment and a paraphrased `meta responsibility`: same in the persistent docs.
- ADR `SUBSYSTEM=project` when ADRs span subsystems: `asd-architect.md` Output format sets it, while `artifact-layout.md` says `sprint` for drafts. The conflict is in canon, not in the draft.
- Doc `STATUS=draft` throughout design-review: no workflow flips it to `in-review`, and nothing flips the per-ADR `status-chip` after the user's accept.
- Extra UX-spec sections (UX decisions, text catalog, coverage) and the ADR "Reversibility / Revisit" line: the first are accepted UX practice, the second is mandated by `design-principles.md` 9.

**Defect shapes that paid off:**
- c4-full delta carrying a `desk.md` Purpose/Key paths proposal: the registry template excludes purpose and key paths, and ADR-1's hard-gate block already holds them (copies drift).
- UX text catalog re-listing static mockup strings verbatim, against its own lede; route lists in both a UX decision and an ADR "final routes" table.
- DESIGN.md edits promised "at design-promote" in prose while no `design-md-delta.yaml` exists: standard promote patches DESIGN.md only from the delta (`asd-ux.md` Outputs), and `design-system.md` section 10 regenerates design-system.html only when DESIGN.md was touched.
- Resolved hard-gate branches left in accepted ADRs (fallback variant, "if not approved"), and counts restating the list below them.
- `updated` date in shell meta/STATS/footer older than later edits logged in the decisions-log segments.
- External-norm claims ("recommended by OWASP") without a source, against `design-principles.md` 11.

**Why:** the finding rows are limited to one `f` per rubric row, so hang each finding on the row it fits best and merge same-kind drift; the decisions-log segments (`decisions-log.00N.md`) are the evidence for dates and approvals.

**How to apply:** at design-review iteration 1 read each draft whole (no `.diff`); in later iterations read the hand-off `.diff` first.
