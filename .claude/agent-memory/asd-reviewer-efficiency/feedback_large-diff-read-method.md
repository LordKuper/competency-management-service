---
name: large-diff-read-method
description: How to cover a very large impl-review diff within the turn budget and how the persisted report/ledger are parsed
metadata:
  type: feedback
---

Map the diff first with a `^diff --git` grep (line numbers per file), then read hand-authored source in ~1000-line chunks — a single Read is capped near 25-29k tokens whatever `allow_large` says. Generated EF `*.Designer.cs`, deleted old migrations and `AppDbContextModelSnapshot.cs` need only a glance at the snapshot (indexes, FKs, check constraints); `packages.lock.json` needs none.

**Why:** a ~35k-line diff is mostly generated migrations; reading them whole burns the turn budget without finding anything.

**How to apply:**
- No shell is available, so the ledger's file rows are transcribed by hand from the manifest's `ledger.files`; copy ids verbatim and count them against the manifest.
- `persist-review` reads the first `|` table of the returned text by column position (id, severity, location), so no other table may come before the Findings table; ledger `findings` must equal that table's ids, and only a rule/section row with status `finding` may carry one `f`.
- Verify a perf claim in the working tree (file:line) before citing it; the diff's own line numbers are not file lines.
- Measured perf evidence lives in `test-plan.md` (Suite run), not in `custom-coding-rules.md`, when the project defines no budgets.
