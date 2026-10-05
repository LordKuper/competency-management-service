---
# ASD generated. Edit .asd/skills/asd-sync/SKILL.md. source_digest=sha256:f198477e9f35ba73c0b4088a690373e504ea0dd731d5fb501af54e477f3d7baa content_digest=sha256:a7953bc3e1b4afff4e541a774015a12d571d4916b672998df5d97f0115121bad asd_version=7.1.0 schema=1
name: asd-sync
description: "Reconciles this project's generated provider views (.claude/, .codex/, .agents/skills/) with the canonical ASD sources (.asd/agents, .asd/skills, .asd/hooks) via .asd/sync.js's check/apply flow, asking per-file whether to overwrite, keep, or diff before writing anything. Use when the user runs $asd-sync or asks to sync, regenerate, or reconcile the Claude/Codex agent and skill files after editing canonical ASD sources."
---

Operation mapping: see `.asd/rules/providers.md`.

# asd-sync

Reconcile generated provider views with canonical `.asd/` sources. Never overwrites a file without a per-file decision.

## Run

0. **Resolve the script path once, so this works from any directory in the repo** — `node .asd/sync.js` only resolves at a bare relative path from the repo root; a self-locating path removes that constraint entirely instead of relying on cwd discipline: `SYNC="$(git rev-parse --show-toplevel)/.asd/sync.js"`. Every command below is `node "$SYNC" ...`.
1. Run command `node "$SYNC" --check`. Parse its JSON report (`items[]`: `target`, `status` — `current | missing | stale | modified-foreign`).
2. If every item is `current`, report that and stop — nothing to do.
3. For each non-`current` item, request user decision, offering:
   - **overwrite** — regenerate this target from canon (queued for apply).
   - **keep as-is** — skip this target this run.
   - **show diff** — read the target and the canon source, present the difference, then re-ask overwrite/keep for the same item.
   - `modified-foreign` items (hand-edited outside ASD's ownership markers) get the same three options, but flag the risk explicitly: overwrite discards the hand edit.
4. Apply exactly the targets marked overwrite in step 3, split by their step-1 status — the CLI only honors an overwrite of `modified-foreign` when `--force` is explicitly passed, precisely because that status means "hand-edited outside ownership markers":
   - `missing`/`stale` targets marked overwrite: `node "$SYNC" --apply <file...>` (no `--force` — nothing to override, sync would write these anyway).
   - `modified-foreign` targets marked overwrite: `node "$SYNC" --apply <file...> --force` (same command form, `--force` appended — this is the ONLY thing that actually makes a confirmed `modified-foreign` overwrite take effect; without it the CLI silently refuses and the file stays untouched).
   Skip either call if its group is empty.
5. Report per-file outcome (applied / kept / still stale) from the apply result(s) plus a final `node "$SYNC" --check` summary.
