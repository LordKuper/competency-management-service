---
name: ef-save-batch-ordering-facts
description: EF Core 10 save-batch facts verified on PostgreSQL here - unique-index swap cycle despite a raw set-aside, FK error before the concurrency count, name checks blind to tracked changes, enum varchar ceilings, trailer paragraphs
metadata:
  type: project
---

Verified against PostgreSQL (testcontainers) on EF Core 10.0.12 / Npgsql 10.0.3:

- EF topologically sorts the updates of one `SaveChanges` by every unique index in the model, using the values it read. Two rows trading places on a unique index throw `Multigraph.ThrowCycle` (500, message not logged) even when a raw `UPDATE ... SET position = -position` already freed the values in the database. Working fix without a model change: one `SaveChanges` per moved row inside the transaction (one journal event and version bump each).
- A stale concurrency token on a dependant's `DELETE` affects 0 rows, but a later `DELETE` of the principal in the same batch raises the FK error (`23001` RESTRICT) before EF counts rows, so the client sees 500, not 412. Compare `If-Match` with the rows read under the lock before staging deletes.
- A name-uniqueness query sees only the database. Entities activated in memory in the same request (several drafts approved together) all pass it; also check `ChangeTracker.Entries<T>()` in Added/Modified state (it runs DetectChanges).
- `HasConversion<string>().HasMaxLength(n)` with one shared ceiling overflows silently until a long member is written (`22001`); the check constraint lists the name, so it passes. Measure member names when adding enum values.
- Several `-m "ASD-Task: X"` arguments make separate paragraphs; git reads only the last as trailers. Put all trailers in one `-m` with embedded newlines.

**Why:** each of these was a 500 that unit-free review and a clean build missed.
**How to apply:** reorders under unique indexes, cascading deletes guarded by `If-Match`, batch approvals, new enum members, multi-id commits. See also [[aspnet-platform-pipeline-gotchas]], [[pg-behaviour-facts]].
