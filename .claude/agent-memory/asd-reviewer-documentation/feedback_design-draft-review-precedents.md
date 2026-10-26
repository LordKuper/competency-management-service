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
- Doc `STATUS`: draft in the first round; the orchestrator flips it to `in-review` (and the ADR `status-chip` to `accepted` after the user's design-gate accept) in the first-round autofix commit - recorded in the decisions-log. Shell CSS defines `.status-in-review`.
- Extra UX-spec sections (UX decisions, text catalog, coverage) and the ADR "Reversibility / Revisit" line: the first are accepted UX practice, the second is mandated by `design-principles.md` 9.
- `design-md-delta.yaml` created by the UX autofix as a documentation-only delta (no token change) with an adaptive decisions-log line: sanctioned, not a scope defect. Its `doc_patch` field (outside `t_design-md-delta.yaml`, carries the exact DESIGN.md table/row text while `reason` stays "why") is a logged flagged choice, confirmed at design-promote; no validator reads the delta (runtime.js/workflows grep empty). Verify its `new_value`s against `docs/ux/DESIGN.md` instead.
- A stale `updated` date in PRD meta/STATS/footer after a later autofix: low severity, raise only while the floor is low.
- Two homes of one enumerated list (here `publishingActions`: PRD AC-34.4 and ADR-4 preview) when the decisions-log "ответы пользователя" entry names both: user-approved, compare the copies for drift, do not raise the duplication.

**Defect shapes that paid off:**
- c4-full delta carrying a `desk.md` Purpose/Key paths proposal: the registry template excludes purpose and key paths, and ADR-1's hard-gate block already holds them (copies drift). Fixed by pointing to the ADR-1 block.
- UX text catalog re-listing static mockup strings verbatim, against its own lede; route lists in both a UX decision and an ADR "final routes" table. After the trim, check the remaining catalog strings against the mockups word for word (catalog "Вместе с версией" vs "Вместе с ней" in two modals). An autofix that adds a catalog row can reintroduce static buttons and a string already in another row («Не сейчас», «Утвердить вместе с {n} записями»): medium, so only raised while the floor is medium or lower.
- DESIGN.md edits promised "at design-promote" in prose while no `design-md-delta.yaml` exists; once it exists, read it against `t_design-md-delta.yaml`: `new_value` must be the real token value (a sentinel like `"unchanged"` on a composite token breaks the patch) and `reason` is "why", not the table row to add.
- Resolved hard-gate branches left in accepted ADRs (fallback variant, "if not approved"), and counts restating the list below them. A leftover can also be the cross-reference that outlives its own fix (ADR-6 "routes provisional in U-22" after U-22 was rewritten to delegate to ADR-6).
- A sprint draft linking a sibling draft (`ux-spec.html` -> `adr.html#adr-6`): ADRs are never promoted standalone, so the link dangles in `docs/ux/`. Plain text or the fold-target doc instead.
- One enumerated rule (here the four `publishingActions` cases, who-is-checked) restated across PRD ACs, UX notes and ADR: grep a distinctive phrase across all three drafts, then compare the copies - one has usually drifted.
- A new AC added by an autofix: grep its id across ADR/UX. The miss looks like ADR-4 "Context" left at "AC-34.1–34.9" and its decision item citing a neighbour id, and AC-34.7 rewritten to "an action not in this list publishes nothing" (reads wrongly for record approval). Both medium: raise only when the floor allows.
- External-norm claims ("recommended by OWASP") without a source, against `design-principles.md` 11 (adding source and date fixes it).

**Why:** the finding rows are limited to one `f` per rubric row, so hang each finding on the row it fits best and merge same-kind drift; the decisions-log segments (`decisions-log.00N.md`, live `decisions-log.md`) are the evidence for dates and approvals. A new file in the round (the delta) is absent from the `.diff` - read it whole.

**How to apply:** on the first design-review round read each draft whole (no `.diff`); on later rounds read the hand-off `.diff` first, then whole files only for cross-checks. Check the round's floor before deciding a shape is raisable (late rounds: high only).
