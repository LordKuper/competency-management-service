---
name: asd-mirror-ownership
description: In ASD's review-fix waves, respect the exact file-ownership allowlist even when a stale cross-reference sits in an unowned file
metadata:
  type: feedback
---

ASD's own repo (see [[asd-self-hosting]]) enforces heavy cross-file mirroring (README mirrors config schema/agent roster, `core.md` glossary mirrors usage across rule docs, `providers.md` mirrors model tiers, etc — see `AGENTS.md` "Cross-file consistency"). Multi-wave review-fix dispatch gives each dev-agent instance a strict, named file-ownership list ("write ONLY these... Do NOT touch: ...").

**Why:** other waves/tasks own the excluded files and may be editing them concurrently; touching them causes merge conflicts or duplicated fixes. A dispatch brief that says "check whether X's citations are already fixed and report, do not edit them" means exactly that — verify via read-only grep, then report status in the final summary instead of fixing it.

**How to apply:** before editing any file, re-check it's on the explicit ownership list for that dispatch. When a fix legitimately needs to touch multiple mentions of the same stale term (e.g., renaming "phase orchestrator" to a new canonical glossary term across a dozen files), still filter the edit to only the owned files — grep broadly to see the reference's full blast radius, but `sed`/edit only inside the allowlist, and name the still-stale non-owned locations in the completion report for the next agent/tester to pick up.
