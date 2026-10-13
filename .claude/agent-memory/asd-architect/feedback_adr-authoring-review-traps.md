---
name: adr-authoring-review-traps
description: ADR drafting patterns design-review flagged in sprint 004 — hard-gate fallbacks, c4-full purpose dupes, count numerals, unsourced norms, paging limits, optional client gates, POST-only rules
metadata:
  type: feedback
---

Write ADRs so design-review has nothing to strip:

- No fallback branch for a pending hard gate ("если не одобрят — вариант В2", "решает пользователь"). Once the user accepts, reviewers flag it as critical dead code. Keep the loser only as a rejected alternative and write "принято пользователем <date>".
- The new subsystem's purpose and key paths live only in the ADR block. c4-full/subsystems.md holds just the registry rows and the diagram (its `excludes` names purpose/key paths). Point to the diagram with a link; do not restate its Rel lines.
- No count numerals that restate a list ("девять методов"). They drift from the list.
- An external norm (OWASP and the like) needs a URL to the exact document plus a "проверено <date>" check, or drop the attribution.
- Before you reuse `PageResponse`/`ListRequest` for a list (max 200 rows), check that the documented volumes fit under the limit. A cap that has no way to fetch the rest must say it is a documented ceiling and where the rest is reachable.
- Write a list of cases (initiators, publishing actions) in one bullet only. The lede, "who has the right" and the confirmation bullets link to it. Check the list against the code paths, including auto-steps such as `ToHrAsync`.
- If the server decides under locks whether an action has a gated effect (publishing), enforce the gate at execution. An optional client parameter does not work, because the client cannot know that the route changed after the preview.
- A rule that closes a state for `POST` must also cover `PUT` of the same fields. A walk that goes "only through drafts" must not skip active nodes that still carry draft references from old data.

**Why:** in sprint 004 design-review iter-01 (2026-10-13), efficiency raised E-1 (critical) and documentation raised F-1, F-4 and F-5 for exactly these patterns. The paging point is E-2: `/desk/items` used `ListRequest` paging, so rows past 200 could never be reached. In iter-02, external #3 flagged the 2 000 cap. Correctness #1 and documentation #1 found that the copies of the publishing-action list had drifted apart and missed accept-changes followed by the HR auto-step. In iter-03, external #1 found that the optional `cascade` tag let a step-1 approve publish after the responsible HR changed. Correctness #1 found that an HR `PUT` gave an active competency a draft reference.

**How to apply:** check this list before returning COMPLETED on any adr.html or c4-full draft, and again on every autofix pass.
