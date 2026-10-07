---
name: ledger-finding-mapping
description: how findings attach to ledger rows so persist-review accepts the return - one f per rule row, no f on file or section rows, first table must be the Findings table
metadata:
  type: feedback
---

Plan the finding-to-rubric mapping before writing the return: a rule row carries one `f`, file and section rows cannot
carry one (their vocabularies have no `finding` status), so give each finding its own rule row (merge themes when rubric
rows run out, e.g. a code-comment reference to a project doc folds into SSoT).

**Why:** `.asd/runtime.js` `rowsById` and `reviewFindings` reject a malformed return; the findings table is read from the
first `|` line after the verdict token, so no other table may precede it, and a literal `|` in any cell breaks parsing.

**How to apply:**
- Findings table first (columns `# | Severity | Location | Description | Suggested fix`), ids as plain strings (`F1`).
- Put the over-engineering category (`simplify`/`keep-as-is`/`escalate`) in the Description cell; the table has no category column.
- One fenced JSON ledger block only; ids copied verbatim from the manifest (backticks and em dashes included).
- Verify "unused member" claims with `Grep -o -n` over both `src` and `tests` for every receiver name before calling dead code.
