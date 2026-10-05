---
name: value-removal-sprints
description: Reviewing sprints that remove a config value/family (e.g. sprint 016 terra) — fixture swaps must keep hitting the same branch; hand-enumerated value sets in asserts are §17 set-derivation findings; impl may own the test edits so impl-test has no test commit
metadata:
  type: feedback
---

When a sprint removes an enum member (sprint 016: Codex `terra` family), check three things in the test edits:

1. **Branch preservation of swapped fixtures.** A fixture that used the removed value (`sol: 'gpt-5.6-terra'`) may still pass after the removal, but through a different disjunct of the guard (regex fail instead of `endsWith` suffix mismatch), because both disjuncts share one error message. Replay the new fixture against each disjunct and confirm it still reaches the intended one. Sprint 016 swapped it to `gpt-6-luna`, which is correct.
2. **Hand-enumerated sets on touched lines.** An assert regex listing the members (`(sol|terra|luna)` → `(sol|luna)`) had to be hand-edited because it names the set instead of deriving it from its source (`manifest.model_families.codex`). That is the drift `code-style.md` §17 warns about, so raise it as low.
3. **Suite-run HEAD.** When the plan puts the fixture edits in the impl task, impl-test adds no test commit. A Suite-run HEAD equal to the "enter impl-test" commit is then legitimate, not stale.

**Why:** a green suite after a removal proves nothing about which branch a swapped fixture exercises, and the hand enumeration is exactly what needed the manual edit.
**How to apply:** use this in any removal or rename sprint. Also check that every AC has a test-plan line, including an AC deferred to the pr phase (version/CHANGELOG), where the line should cite the existing CHANGELOG==asd_version pin. See [[no-shell-review-method]], [[removed-flag-vacuity]].

Replay tip (sprint 016 iter-02): a per-agent loop's first-firing message names the first file in `fs.readdirSync` order, which is alphabetical on NTFS (`asd-advisor`). The Glob tool lists by mtime, so don't use its order for the replay. Review-fix rows written before the next impl-test entry's rotation land inside the rotated segment legitimately. That is not a "segment edited" violation.
