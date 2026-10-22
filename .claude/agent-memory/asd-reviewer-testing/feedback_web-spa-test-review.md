---
name: web-spa-test-review
description: Vitest + fakeApi SPA test review - verify "add" rows by grepping the production symbols tests import, absence asserts synced only on the fake call count are vacuous, page-level role seams, typed Record tables over hand it.each lists
metadata:
  type: feedback
---

**1. A test-plan "add" row naming a module is a claim to grep.** Rows like "texts of reasons, per kind and reason" can describe tests that do not exist. **How to apply:** list the exported functions of the production module, then Grep `*.test.*` for each name; an export no test file mentions is uncovered whatever the row says. The mutation row of the same test file shows the real reach (it names what was mutated).

**2. fakeApi records a call when `fetch` is entered, not when the answer is delivered** (`web/src/test/fakeApi.ts`). `waitFor(() => calls.length === 1)` then asserting an element is absent proves nothing: react-query notifies subscribers on a later scheduler step. A sound absence check follows a positive transition (show a number, answer zero, wait for the number to go) or waits on the rendered state. **How to apply:** for every "shows nothing" test, ask what makes the assert run after the answer is rendered; the sibling test usually shows the right sync.

**3. Seams between a page and its pure component.** Unit tests of a presentational component take roles or props ready-made; the page maps API answers (units headed, units as HR, employee binding, account roles) into them. If every page test answers the role endpoints with empty lists, the mapping is unchecked. **How to apply:** grep the page test for the non-empty fixtures of each source the page reads.

**4. Names wider than asserts.** A test title promising "and the count" or "without hiding X" is checked against its expects: two equal numbers in a fixture, or a role whose cards are hidden anyway, make the claim unfalsifiable. Ask which mutation of the named behaviour would survive.

**5. Hand-enumerated kind lists.** `it.each` over a union's members written by hand misses the members nobody remembered; a table typed `Record<Union, ...>` makes tsc demand every one (the derive-from-source rule of the tests section of code-style).

**6. Mutating-flow rule on lists.** The project's mutating-UI rule (reread after a 412, no automatic retry) is easy to test on one screen and forgotten on its twin; compare the refusal tests of sibling hooks (desk resend vs users-list mail).

See [[review-isolation-and-log-window-tests]], [[removed-flag-vacuity]].
