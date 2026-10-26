---
name: redundant-gate-pair-keep
description: Two single-conjunct gates that are each sufficient alone (a query enabled flag plus an adoption condition) are judged keep-as-is, not an over-engineering hit
metadata:
  type: feedback
---

A small state machine can carry two gates that each suffice alone: a query `enabled` flag and the condition that adopts its result into state. Both are one `&&` operand, neither is an abstraction, and each states one half of one documented rule (fetch only after a stale refusal, adopt only inside that cycle). Judge it keep-as-is, rule row `pass`, no finding. Removing either one gains nothing but costs a full review-fix, impl-test and impl-review round.

**Why:** the over-engineering item is "defensive code for an impossible-by-contract case"; a guard whose job is a documented half of the rule, with no behaviour of its own to maintain, does not trip it. Flagging it is critical and undroppable, so a flip between iterations is pure churn.

**How to apply:** look first at what each gate buys (here the enabled gate also saves a GET per focus and online signal). Raise only when a gate guards a state the code can no longer reach AND carries real code (a branch, a state variable, a handler), or when the pair hides a behaviour bug. A hook that already holds the same state machine (`useEditBase`) is a reuse candidate only when the state shape fits it; a composite state needing a rewrite is not. Related: [[memory-fix-fallback-keep]], [[020-workflow-definition-keys]].
