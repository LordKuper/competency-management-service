---
name: design-vs-docs-disambiguation
description: How to decide whether a bare "design/" reference in ASD canon means the sprint-local draft folder or the persistent docs root
metadata:
  type: feedback
---

When a canonical file (template frontmatter `delegates_to`, config comment, workflow prose) carries a bare `design/` reference, do NOT resolve it by picking one vocabulary globally. Resolve per site with the corroboration test: does the phase/workflow that consumes or writes that file corroborate a persistent-root reading? If the file is authored **after** design-promote and its workflow names "persistent docs", it means the root → reword to `persistent docs`. If it is authored **before** design-promote and all sibling `delegates_to` items are sprint-local, it means the draft folder → keep `design/`.

**Why:** sprint 001 renamed `design/` → `docs/` and produced two referents sharing one word. A blanket "unify the vocabulary" fix over-renamed sprint-draft references (iter-01 findings), and the corrective pass then over-reverted `t_test-plan.md:5` — which is written during `impl-test`, after design-promote, where `asd-phase-impl-test.md:11` confirms the root reading. Both errors came from applying one rule uniformly instead of testing each site.

When such a per-site exception is approved at an escalation, immediately write it into the sprint's own AC text (the in-scope rule + named exception, and the AC-7-style grep exclusion set), not only into `decisions-log.md` — the `pr` phase re-runs the AC grep literally and will otherwise report a false failure. Amending AC text to record an already-granted decision is bookkeeping and needs no fresh user gate.

**How to apply:** when routing review findings that propose vocabulary unification across sibling files, check each site's authoring phase before accepting the reviewer's suggested fix; Documentation-reviewer site-specific reasoning outranks Simplification's blanket-consistency suggestion where they conflict.
