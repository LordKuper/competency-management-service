---
name: large-diff-read-budget
description: how to finish a combined impl-review of a many-file, 200 KB-plus diff inside the turn cap - chunk size for the Read tool, cheap cross-file probes, write order
metadata:
  type: feedback
---

The Read tool refuses a result over about 25k tokens, so a 200+ KB diff cannot go in one call: read it in chunks of
300-330 lines (about 7 calls for a 1700-line diff), several chunks in one message when they are independent.

**Why:** an earlier attempt on a 16-file, 227 KB wave ran out of turns reading in small pieces and returned no verdict;
reading the whole diff first and probing afterwards finished in under 20 turns.

**How to apply:**
- Read the manifest, the acceptance list and the diff first; read a whole source file only when a hunk cannot show the
  fact (a caller, a helper's signature, a constant's current line number).
- Verify a cross-file claim with one `Grep` using `-o -n` and a narrow pattern, not a file read. A brace list in the
  `glob` parameter silently returns no match; use `path` per file or a single glob.
- Scope a repo-wide `Grep` to the canon subdirectories (rules, workflows, agents, skills, templates), one call each. A
  `glob` negation on the whole `.asd` tree still scans the archived sprint folders and floods the result with their
  copies of old text.
- A one-line paragraph can exceed the Grep line display ("Omitted long matching line"): `Read` with `offset` and
  `limit: 1` returns it whole.
- The coverage ledger has one row per manifest id and is the cost-free part: build it from the manifest ids as soon as the
  findings are known, and write the return file before the final message so a cap stop still leaves a verdict.
- A threshold or calibration observation that the data now contradicts is a finding with a `question:` escalation, not a
  silent pass.
