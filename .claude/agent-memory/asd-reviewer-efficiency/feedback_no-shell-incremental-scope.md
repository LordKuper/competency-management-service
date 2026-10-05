---
name: no-shell-incremental-scope
description: Efficiency review never runs git; scope = manifest file list, change content = the runtime-written fingerprint .diff; whole files are context only
metadata:
  type: feedback
---

Scope hand-off (review-policy.md "Scope hand-off"): the manifest's file list is the only normative scope (ledger rows + valid finding locations); the runtime-written, fingerprint-named `.diff` for exactly that list is the change content — impl-review every iteration, over the wave's range (`iteration_heads["iter-(NN-1)"]` of wave K, sprint-lifecycle.md "Review iteration counters"); design-review from iteration 2 only (iter 1 drafts are wholly new, read them directly). Whole files are context only; an unlisted path stays out of scope. No reviewer runs git. No split parts exist (no `.part-N`, no 25-file split, no out-of-part n/a). Never open another iteration's review files, any wave's — only the current `reviews/<phase>/[wave-<K>/]iter-NN/`. `test-plan.entry-NN.md` "none" rows often pre-flag dead/unreachable runtime code for impl-review — check them.

**Why:** a finding must sit on the change surface (`review-policy.md` "Change-surface rule").

**How to apply:** raise a finding only on a changed line. Agent-memory growth is outside "Documentation economy" reach — don't raise it. Test-only micro-costs (tens of ms in one-shot sweeps) fail the complexity-vs-value bar. See [[runtime-js-single-file]].
