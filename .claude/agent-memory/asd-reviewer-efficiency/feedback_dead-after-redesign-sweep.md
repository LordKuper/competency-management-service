---
name: dead-after-redesign-sweep
description: After a screen redesign, grep every export, CSS class and CSS variable whose last caller the diff removed; orphans hide behind underscore names, contract tests and untouched stylesheets
metadata:
  type: feedback
---

A redesign that deletes a component leaves orphans in lines the diff does not touch. Grep each candidate across `web/src`, tests included, and check who still reads it:

- an API helper whose only remaining caller is a contract test (a test-only reader is not a caller);
- CSS rules for an element the diff removed, and a CSS variable whose reader line the diff deleted (stylesheets are outside the hunks, so read the whole file);
- a `_`-prefixed constant: it passes lint, so the linter never reports it;
- a wrapper component that only returns a lookup-table string, once the styling variant it carried is gone.

**Why:** these were the real dead-code hits in a large UI diff; none showed up as a changed line, and the lint and build gates were green.

**How to apply:**
- Also grep an export before calling it dead: a helper new in the diff is often read by screens in another review wave.
- Check `docs/ux` for the mock before calling a UI widget over-engineered; behaviour the spec draws is not a finding.
- A hook file that also exports shared UI text, or a component with a mode flag reused as a wizard step, is cohesive enough: a split adds files and import edits and buys no behaviour.
- Ledger: a rule row carries one `f` and a section row none, so merge same-kind findings onto the rubric row each fits (dead code onto the over-engineering row, a pointless wrapper onto complexity-vs-value) and list every location in the one finding. Related: [[large-diff-read-method]].
