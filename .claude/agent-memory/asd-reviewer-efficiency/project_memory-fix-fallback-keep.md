---
name: memory-fix-fallback-keep
description: review-policy.md memory-fix dispatch's MEMORY-FIX fallback (owner with no write tool) looks unreachable but is AC-14-mandated; judged keep-as-is in 019 iter-02
metadata:
  type: project
---

Since sprint 019 iter-01 answer (b), every canonical agent carries `memory: project` with no `Write` in `disallowedTools`, so on Claude every memory owner holds `Write`. The `MEMORY-FIX <path>` fallback branch ("an owner with no write tool at all") in `review-policy.md` "Autofix vs escalation" and `asd-phase-impl.md` step 3 therefore never fires for canon agents.

**Why:** not a dead branch. Sprint 019 AC-14 requires it by name ("when the host gives the owner no write tool..."), and a consumer's custom agent can still reach it if it has `memory: project` and lists `Write` in `disallowedTools`. Iter-02 judged it keep-as-is, so it is not an OE "impossible-by-contract" hit.

**How to apply:** don't raise that fallback as dead or defensive code unless an AC or the user removes the requirement. Related: [[no-shell-incremental-scope]].
