---
name: fix-the-class
description: When a review finding recurs across iterations, remove the structure that produces it (e.g. a blanket header qualifier) instead of patching the one branch reported
metadata:
  type: feedback
---

When a finding is the third of its kind, fix the shape that generates it, not the reported instance. Concretely for rule/workflow prose: a step header that states a blanket reach ("internal reviewers only") over a list of branches whose SSoT rules have differing reach will keep producing findings, one branch at a time. Strip the reach off the header and put it on each branch line instead.

**Why:** sprint 009 spent three of four review iterations on the reach of one step (7a/8a in both review workflows). Iteration 3 bought one branch (late duplicate return) out of the header; iteration 4 found the next branch (interrupted dispatch) still swallowed by it. The user asked explicitly for the fix that "ends the class, not the instance".

**How to apply:** when handed a review-fix finding, check whether the same section already took a fix last iteration. If yes, treat the enclosing structure as the defect. Also: verify a reviewer's premise against source before acting — here two reviewers disagreed (documentation high, correctness below-floor/pre-existing) and the rule files settled it. Pre-existing is not a reason to leave it; state the judgement and fix anyway. See [[sequential-fix-rounds]].
