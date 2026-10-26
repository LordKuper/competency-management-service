---
# ASD generated. Edit .asd/skills/asd-sync/SKILL.md. source_digest=sha256:60b453795d0dd9f21fe200fa44a5c372e225f28ba82f800e6b01fb2efb42a1a2 content_digest=sha256:cc5d24ecf35547a132586f11190816f16ce9aed5560920114fa45de29976be4e asd_version=13.9.0 schema=1
name: asd-sync
description: "Reconciles this project's generated provider views (.claude/, .codex/, .agents/skills/) with the canonical ASD sources (.asd/agents, .asd/skills, .asd/hooks) via .asd/sync.js's check/apply flow, asking per-file whether to overwrite, keep, or diff before writing anything. Use when the user runs $asd-sync or asks to sync, regenerate, or reconcile the Claude/Codex agent and skill files after editing canonical ASD sources."
---

Operation mapping: see `.asd/rules/providers.md`.

# asd-sync

## Purpose

Reconcile generated provider views with canonical `.asd/` sources.

## Preconditions

```json
{"requires": [".asd/sync.js in this git repo"]}
```

## Steps

0. **Resolve the script path once**, self-locating so it works from any repo directory: `SYNC="$(git rev-parse --show-toplevel)/.asd/sync.js"`. Every command below is `node "$SYNC" ...`.
1. Run command `node "$SYNC" --check`. Parse its JSON report (`items[]`: `target`, `status` — `current | missing | stale | modified-foreign`).
2. If every item is `current`, report that and stop — nothing to do.
3. For each non-`current` item, request user decision: **overwrite** regenerates this target from canon (queued for apply); **keep as-is** skips this target this run; **show diff** reads the target and the canon source, presents the difference, then re-asks overwrite/keep for the same item. `modified-foreign` items (hand-edited outside ASD's ownership markers) get the same three options, but flag the risk explicitly: overwrite discards the hand edit.
4. Apply exactly the targets marked overwrite in step 3, split by their step-1 status:
   - `missing`/`stale`: `node "$SYNC" --apply <file...>` (no `--force`).
   - `modified-foreign`: `node "$SYNC" --apply <file...> --force` — without `--force` the CLI silently refuses and the file stays untouched.
   Skip either call if its group is empty.
5. Report per-file outcome (applied / kept / still stale) from the apply result(s) plus a final `node "$SYNC" --check` summary.

## Rules

- never: overwrite a file without a per-file decision.

## Delegates

```json
{"agents": [], "skills": []}
```

## Outputs

```json
["generated views marked overwrite: .claude/, .codex/, .agents/skills/"]
```

## Return

```
SKILL: asd-sync | STATUS: <complete|aborted> | NEXT: none
```

## References
- `.asd/rules/providers.md` (canonical path -> per-provider path, ownership markers)
