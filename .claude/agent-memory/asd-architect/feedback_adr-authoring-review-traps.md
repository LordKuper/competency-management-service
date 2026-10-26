---
name: adr-authoring-review-traps
description: ADR drafting patterns design-review flags — hard-gate fallbacks, c4-full purpose dupes, count numerals, unsourced norms, paging limits, optional client gates, POST-only rules
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
- When a server gate becomes mandatory (a required tag or param), grep every existing client caller of the gated methods, not only the new screens. Name in the ADR which screens send it and what happens to the old callers. A "single client is our SPA" argument for a minor version only holds if no SPA path breaks.
- A rule that closes a state for `POST` must also cover `PUT` of the same fields. A walk that goes "only through drafts" must not skip active nodes that still carry draft references from old data.

**Why:** design-review flagged each of these patterns, one as critical. `/desk/items` used `ListRequest` paging, so rows past 200 could never be reached; a 2 000 cap was flagged as well. The copies of the publishing-action list drifted apart and missed accept-changes followed by the HR auto-step. An optional `cascade` tag let a step-1 approve publish after the responsible HR changed. An HR `PUT` gave an active competency a draft reference. The old `MatrixPage` still called the transitions without a tag, so it always got 412; the user removed those buttons from the page.

**How to apply:** check this list before returning COMPLETED on any adr.html or c4-full draft, and again on every autofix pass.
