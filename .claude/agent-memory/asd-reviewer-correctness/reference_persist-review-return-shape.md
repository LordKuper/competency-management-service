---
name: persist-review-return-shape
description: I write my final return verbatim to the payload's `.asd/tmp/` return file; persist-review --in parses it - first table by column position, bare APPROVE has an empty Findings table (header + separator only)
metadata:
  type: reference
---

I write my final return verbatim to the return file the payload names (`.asd/tmp/<sprint>-<phase>-<iteration id>-<reviewer>.return.md`); the orchestrator never re-types it and runs `node .asd/runtime.js persist-review --in <that file>` (`runtime.js` `reviewFindings`/`persistReview`). No return file = interrupted dispatch. The ledger to fill is the manifest's `ledger` skeleton (digest-excluded). It reads the FIRST markdown table in the text by column position: id, severity, location.

- `#` cell: a bare id; Severity cell exactly `low|medium|high|critical`.
- A bare `APPROVE` with any real findings row is rejected; with no findings, the Findings table is header and separator only (a `—` id row is merely skipped as legacy tolerance, never required).
- CONCERNS/FAIL with no findings row is rejected.
- Do not put any other table before the Findings table.
- Files rows in the ledger stay `checked`/`n/a` (never `finding`); finding ids go on rules rows as `f`.

**How to apply:** check this shape before handing back; a failed parse costs a transcription or a fresh re-dispatch. Related: [[review-method-no-shell]].
