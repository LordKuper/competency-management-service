---
name: matrix-workflow-facts
description: Sprint 003 Task 7 matrix versions - same-column partial indexes collapse unless named, ef migrations remove needs a DB, long multi-heredoc bash calls break here
metadata:
  type: project
---

Task 7 of sprint 003 (2026-10-09), EF Core 10.0.12 / Npgsql 10.0.3. None of it was run against PostgreSQL:

- Two `HasIndex(e => e.MatrixId)` calls with different `HasFilter` give one index: EF keys an unnamed index by its property set, so the second call reconfigures the first and the migration silently lacks one partial index. Use the named overload `HasIndex(e => e.MatrixId, "ux_...")` for each. Always read the generated `CreateIndex` list.
- `dotnet ef migrations remove` connects to the database to check whether the migration was applied, and fails offline ("Этот хост неизвестен"). To redo a migration without a DB, delete its two files and `git checkout -- .../AppDbContextModelSnapshot.cs`, then `migrations add` again.
- A single Bash call that writes several files through consecutive `cat > f <<'EOF'` heredocs failed with "unexpected EOF while looking for matching `''" and wrote none of them. Write each C# file with the Write tool, and pass scripted multi-line edits as a `.js` file in the scratchpad.
- The matrix document (`MatrixContent`) and the snapshot (`MatrixEntries`) are jsonb through a JSON value converter, the content with a JSON-string `ValueComparer`, for the same audit reason as scale points ([[career-catalogue-facts]]). Content edits are journaled by an explicit `MatrixVersion.ContentUpdated` event, because the column is deliberately not `[Audited]`.

**Why:** the index collapse would have dropped the one-published-version guarantee with a clean `has-pending-model-changes`.
**How to apply:** any entity with several filtered unique indexes on the same columns; any offline migration redo. See also [[ef-raw-sql-and-openapi-contract-gotchas]], [[heredoc-backslash-collapse]].
