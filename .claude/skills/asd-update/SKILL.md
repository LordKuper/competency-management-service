---
# ASD generated. Edit .asd/skills/asd-update/SKILL.md. source_digest=sha256:6cf765b826c48fa4db7766b78b6f82a1d29d56946f94ac9dd8a2e663c1fef592 content_digest=sha256:9e8a6362b4faffc3296e16d7c388b5bc31f2ac87b036ea02709e5c9924f9e7d9 asd_version=13.8.0 schema=1
name: asd-update
description: "Updates the ASD framework infrastructure (.asd/rules, .asd/templates, ASD agents/skills/hooks, .asd/migrations) in a consumer project to the latest version by fetching them from the configured ASD repo's main branch, replacing only framework-managed paths, running any pending `.asd/migrations/<version>.js` scripts in ascending order, and never touching consumer-owned config (beyond a migration's release-mandated key renames and removals, plus the value mappings, key insertions and shipped-comment rewrites that carry a renamed or removed key's or value's intent), sprints, persistent docs, or custom skills/agents/hooks. Use when the user runs /asd-update or asks to update, upgrade, or pull the latest ASD framework / workflow version."
---

Operation mapping: see `.asd/rules/providers.md`.

# asd-update

Pull latest ASD framework files into this consumer project. Overwrites **framework-managed** paths only; leaves consumer-owned files intact.

## Self-hosting guard

Read `self_hosting` from `.asd/project/config.yaml` first; missing, unreadable, or a duplicated key → treat as `disabled` (fail closed). If `enabled`: this command is for pulling framework files INTO a consumer project — this repo IS the framework. Print a one-line message ("asd-update is for consumer projects; this repo develops ASD directly — use a self-hosting sprint instead") and stop. No mutation, no fetch.

## What it touches

Managed set = SSoT in `.asd/release-manifest.json`'s `managed_paths` (canonical `.asd/` trees, `.asd/migrations`, and `sync.js` itself, walked recursively file-by-file) — replacing the old wholesale tree-delete approach with a per-file state machine (`add | update | delete | conflict | conflict-foreign | keep-local-modified | noop`, driven by `classifyUpdateItem` in `.asd/sync.js`). A file whose local hash still matches the last-fetched release is safe to update or delete; a file that diverged is a **conflict** and is never touched without explicit confirmation.

Never touched: `.asd/project/**` (except `config.yaml`, which a release migration may rewrite as a sanctioned settings writer, limited to release-mandated key renames and removals, plus the value mappings, key insertions and shipped-comment rewrites that carry a renamed or removed key's or value's intent), `.asd/sprints/**`, `docs/**`, `AGENTS.md`, `CLAUDE.md`, `.claude/settings.json`, `.codex/hooks.json`, any non-ASD skill/agent/hook, anything outside `managed_paths`.

## Run

1. Confirm with user (it overwrites framework files): show what will update, offer dry run. Skipped in sprint-mediated mode (below).
2. Run command `node "$(git rev-parse --show-toplevel)/.asd/skills/asd-update/update.js"` — self-locating, so this works from any directory in the repo (a bare relative path only resolves from the repo root).
   - Preview first: append `--dry-run` (reports the full classification — add/update/delete/conflict — mutates nothing).
3. Updater flow: fetch tarball from `repo`@`branch` → compute the full classification for every `managed_paths` entry (fail-closed on an unfamiliar `schema_version`, unsafe path, or case-collision) → show every conflict and planned action → only THEN write (add/update/delete) → run pending migrations → rewrite `.asd/release-manifest.json` (`asd_version` + `upstream_hashes`).
   - Fetch/validation failure, or any conflict left unresolved = that file is skipped, everything else proceeds.
   - A conflict the user explicitly confirms overwriting is re-run with `--force <relPath...>` (one or more of the reported conflict paths) — this is the only way a `conflict`/`conflict-foreign` file is ever written; never force without asking first.
4. Migrations (`.asd/migrations/<version>.js`): run AFTER the managed-path replacement above, in ascending version order, for every migration strictly newer than the consumer's current `release-manifest.json.asd_version` up to this release's target version. Each already-applied version is skipped (never re-run past its own target). The runner stops at the first failing migration and reports which one failed and what it had already done; the recorded `asd_version` afterward is the last migration that succeeded (never an unrecorded intermediate) — or the release's full target version if every pending migration succeeded, including when none were pending.
   - Script contract (also in each script's own header comment): filename (minus `.js`) is the exact target version it migrates a consumer TO, e.g. `.asd/migrations/3.2.0.js`; `module.exports = (ctx) => void | Promise<void>` with `ctx.repoRoot` = the consumer's project root; zero-dependency Node (may `require` `.asd/sync.js` from `ctx.repoRoot` for helpers); idempotent — re-running an already-applied migration is a no-op, never an error.
5. Automatically runs `node .asd/sync.js --check` afterward — canon changed upstream means the provider-views (`.claude/`, `.codex/`, `.agents/`) are now stale; report this, do not auto-apply.
6. Report version `old -> new` (or the version actually reached, if a migration failed) + counts from script output.

## Sprint-mediated mode

Run by `.asd/workflows/asd-phase-scope.md` step 1 when the user chose to update at `asd-sprint`'s new-sprint version check.

- That choice is the confirmation: skip Run step 1; Run steps 2-6 apply unchanged.
- Conflicts and `--force` still go to the user — never force without asking.
- No commit here: the orchestrator commits the result. Report a failed migration or a declined conflict as a partial update; the orchestrator owns what follows.
- Self-hosting guard unchanged: `asd-sprint` never runs the check when `self_hosting: enabled`.

Version check: `node "$(git rev-parse --show-toplevel)/.asd/skills/asd-update/update.js" --check-version` is read-only. It fetches only the remote `.asd/release-manifest.json` (raw GitHub URL from the local manifest's `repo`/`branch`), reads only its `asd_version` (must match `^\d+(\.\d+)*$`; every other field is untrusted and ignored), prints one JSON line on stdout (e.g. `{"local":"13.7.0","remote":"13.8.0","newer":true}`) and exits 0. Any network, parse or repo error, a body over 1 MiB, or the 5 s total timeout, prints one warning line on stderr and `remote: null, newer: false`.

## After

- Remind user: `.claude/settings.json` and `.codex/hooks.json` are **not** auto-updated by this step (they hold user permissions/hook registration; `node .asd/sync.js --apply <generated-view-path...>` (generated view paths only, per `.asd/rules/providers.md` "Canonical path -> per-provider path") handles their own owned entries separately). If update changed hook files or added skills, run sync next.
- Requires `tar` on PATH (ships with Win10 1803+/macOS/Linux) and Node >= 16.7.
