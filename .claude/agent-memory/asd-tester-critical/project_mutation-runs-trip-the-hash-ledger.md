---
name: mutation-runs-trip-the-hash-ledger
description: Mutating any managed_paths file to prove a test fails first also fails the upstream_hashes test — expected noise, not a second defect
metadata:
  type: project
---

When proving a fail-first record by mutating a canonical file (a rule doc, workflow, template, migration, hook), the run also fails `release-manifest.json: every upstream_hashes entry matches the actual file` — and mutating `session-start.js` additionally fails the `sync.js --check` test.

The converse also holds: `.claude/agent-memory/**` is not in `managed_paths`. Reverting a memory file to its pre-fix blob fails only the test you aimed at (sprint 019 entry 2, D-1/D-2 proofs: 237/238, no ledger lines). If you see extra FAIL lines there, something else changed.

**Why:** those files are tracked by `managed_paths`/`upstream_hashes`, so any byte change invalidates the recorded hash. It is an artefact of the mutation, not a finding.

**How to apply:** grep the FAIL output for your own test's name rather than reading the count; never restore with `sync.js --apply`, which rewrites the ledger repo-wide (restore procedure: [[testability-envelope]] "Mutate, run, restore — one bash call"), and confirm the suite returns to full green before moving on. Deleting a fenced block with `sed -n 'A,Bp'` to preview line ranges is worth doing first — an off-by-one that also removes the next statement silently confounds which assertion fired first.
