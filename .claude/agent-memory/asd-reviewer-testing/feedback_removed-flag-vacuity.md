---
name: removed-flag-vacuity
description: A "removed CLI flag no longer does X" test in tests/run.js can pass at the arg parser, not at the removed branch — confirm the recorded mutation restored the flag's parsing too; and a fix-round entry row listing only test-bearing finding ids is not itself a finding
metadata:
  type: feedback
---

`runtime.js` `parseFlagArgs` fails any non-boolean flag with no following value (`flags require values`, exit 2). So a test asserting "`manifest-digest --write` leaves the file untouched" (sprint 012 iter-02, EFF-1-1) passes before `main` ever sees the flag. It is only non-vacuous if the recorded mutation restored both the branch and the flag's boolean-list entry. The transcript's first-firing message proves that: a branch-only restore would have stayed green.

**Why:** a negative assertion on removed behaviour is the easiest one to satisfy by accident, and a green suite makes it look settled.

**How to apply:** for any "flag/subcommand no longer does X" assert, find where the parser rejects it. Check that the recorded mutation got past that point. Replay its claimed first FAIL (see [[no-shell-review-method]]).

A second pattern, also from sprint 012 iter-02: the Entry log row 2 named only 4 of the 12 dev-chain findings (the ones with test consequences). The other 8 were prose, grant or return-contract wording, and existing class rows already cover that kind of change: `none` for agent-runtime audit procedure, `none` for descriptive prose, and the AC-13 relation test. Treat that as below floor unless one of the unlisted changes adds a literal a workflow parses. In that case a missing row is a real gap.
