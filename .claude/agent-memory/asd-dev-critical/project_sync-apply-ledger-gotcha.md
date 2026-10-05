---
name: sync-apply-ledger-gotcha
description: release-manifest.json hash ledgers are recomputed repo-wide by the orchestrator's once-per-wave sync --apply, not by the dev; how to measure ledger staleness correctly - a raw unnormalized read fakes a repo-wide mismatch
metadata:
  type: project
---

Every `node .asd/sync.js --apply <target>` rewrites `.asd/release-manifest.json`'s `canon_hashes`/`upstream_hashes` for the **whole repo**, not just the requested target. Since sprint 021 the dev never runs `--apply`; the orchestrator runs it once per wave or fix round and commits views plus ledger (`sprint-lifecycle.md` "Self-hosting").

**Why:** the ledgers are pure functions of on-disk canon content. `tests/run.js` asserts every `upstream_hashes` entry matches the actual file, so between a dev's canon commit and the orchestrator's wave sync the ledger (and any affected view) is stale by design.

**How to apply:**
- Do not try to make each of your own commits ledger-consistent by running `--apply`; report the stale ledger/views as expected pending the wave sync.
- A new or `git rm`-ed file under a `managed_paths` tree (e.g. a new `t_*` template) gets its `upstream_hashes` entry inserted/dropped by that sync; `.asd/templates` is listed as a whole tree, so no `managed_paths` edit either.
- **Measure ledger staleness with `sync.sha256Hex(sync.readNormalized(p))`, never a raw `readFileSync` hash.** `normalizeText` strips the BOM and folds CRLF/CR to LF before hashing, so a raw read mismatches *every* CRLF file in `upstream_hashes`. Sprint 009 lost a task escalation to exactly this: "94 mismatching entries" equalled the count of CRLF worktree files; the real count was 14. Corollary: `.gitattributes` / `core.autocrlf` / `eol=lf` can never move a ledger entry.
- `--check` (the `build` command) reports view staleness only and exits 0 with a stale ledger and stale views (`ok: false` only on orphans), so a green build does not mean the ledger is current.
