---
name: rereview-no-shell-checks
description: how to back a re-review verdict when the reviewer has no shell - which claims to derive from canon text and which to take from the tester's recorded run
metadata:
  type: feedback
---

The combined reviewer has file read, search and write tools but no shell, so a hash ledger, a suite result or a lint run
cannot be reproduced during a review.

**Why:** a verdict that silently assumed a green run, or an unverifiable hash, would be unsupported; the review basis has
to say which facts were derived and which were taken from a record.

**How to apply:**
- A hash-ledger edit in the diff: confirm the suite has a test that compares each ledger value to its file, read the test
  plan's recorded run for it, and check the hand-off's git status shows no canon file modified after that run. Say in the
  basis that the values were not recomputed.
- An id or ordinal scheme added to a rule (a suffix for a re-run): derive uniqueness from the counters' lifecycle in the
  lifecycle rule before accepting or rejecting it, and compare the id forms with keys that really sit in an archived
  state file (one Grep over the archived `state.json` files).
- A test pin that finds a sentence by code spans: read the helpers (`sectionOf`, `stepOf`, `spans`) once and confirm
  that the new lookup cannot be satisfied by a sibling sentence - the pin must go red on the mutation the test plan lists.
- A gap that predates the fix and only clamps a tier upward is below a medium floor; note it in the basis, do not count it.
- The return always carries the `## Findings` table of the review template, as a single `no findings` row when empty;
  prose in its place is rejected by the persistence step and costs a re-dispatch.
