---
name: adr-authoring-review-traps
description: ADR drafting patterns design-review flagged in sprint 004 — fallback branches of hard gates, duplicated subsystem purpose in c4-full, count numerals, unsourced external norms, paging limits
metadata:
  type: feedback
---

Write ADRs so design-review has nothing to strip:

- No fallback branch for a pending hard gate ("если не одобрят — вариант В2", "решает пользователь"). Once the user accepts, reviewers flag it as critical dead code. Keep the loser only as a rejected alternative and write "принято пользователем <date>".
- The new subsystem's purpose and key paths live only in the ADR block. c4-full/subsystems.md holds just the registry rows and the diagram (its `excludes` names purpose/key paths). Point to the diagram with a link; do not restate its Rel lines.
- No count numerals that restate a list ("девять методов"). They drift from the list.
- An external norm (OWASP and the like) needs a URL to the exact document plus a "проверено <date>" check, or drop the attribution.
- Before you reuse `PageResponse`/`ListRequest` for a list (max 200 rows), check that the documented volumes fit under the limit.

**Why:** in sprint 004 design-review iter-01 (2026-10-13), efficiency raised E-1 (critical) and documentation raised F-1, F-4 and F-5 for exactly these patterns. The paging point is E-2: `/desk/items` used `ListRequest` paging, so rows past 200 could never be reached.

**How to apply:** check this list before returning COMPLETED on any adr.html or c4-full draft, and again on every autofix pass.
