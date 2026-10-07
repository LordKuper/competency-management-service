---
name: delta-review-decisions-log
description: on a wave re-review (iter-02+) check decisions-log and the shared-host test traps before flagging persistent-doc drift or a log-wait helper
metadata:
  type: feedback
---

On a re-review the diff is only the review-fix and impl-test delta: read `decisions-log.md` (Grep `review-fix|For design-promote`)
before raising persistent-doc drift. The review-fix entry names the docs it deliberately leaves for design-promote.

**Why:** iter-02 of sprint 001 found `docs/architecture/tech-reference/postgres-image-18.6-trixie.md` still saying the superuser
bypasses the audit trigger; the log already routed that update to design-promote and the file carries a "Last verified" date, so
raising it would have been noise.

**How to apply:**
- A tech-reference file with a "Last verified: <date>" header is a dated record, not live doc: drift against later migrations is
  not a finding unless the decisions-log leaves it unrouted.
- `ApiHost.LogsAfterAsync(fragment)` waits for a fragment, which is vacuous on the shared host when another test already logged
  it (412 records from the optimistic-concurrency tests); only a dedicated host makes the wait real. A pre-existing weak guard
  that the fix does not worsen is below a medium floor.
- Agent-memory files in the scope list are checked only for stale claims; a stale "not yet run" line is low.
