---
# ASD generated. Edit .asd/skills/asd-init/SKILL.md. source_digest=sha256:79413ccde0b404e63f8e1bedcf3e467311a3dd83021d7a5d50385b91c0f7d4e0 content_digest=sha256:e359589dd09cb66c467a94e6a7721f9f5fe36e388bb28a1d9874c1a60a6743e1 asd_version=13.4.0 schema=1
name: asd-init
description: "Initializes the ASD (Agentic Software Development) workflow in a project, or edits existing ASD settings in diff mode, or applies a plan-declared settings change for the active sprint's impl phase. Auto-detects build commands and external tools, collects config via request user decision, generates .asd/project/config.yaml and seeds infrastructure-only persistent docs; concept, stack, and design system are owned by dedicated skills. Use when the user runs $asd-init or asks to set up, initialize, configure, or change ASD workflow settings."
---

Operation mapping: see `.asd/rules/providers.md`.

# ASD Init

## Preconditions
- Repo at project root
- Infra present: `.asd/rules/`, `.asd/templates/`, `.claude/`

## Modes
- **Fresh**: no `.asd/project/config.yaml` → full setup
- **Re-init**: config exists → diff editor
- **Sprint-mediated**: invoked by `asd-phase-impl.md` step 6 with an accepted plan's `Settings change:` pairs (`sprint-lifecycle.md` "Plan file format") → applies those pairs only

## Always first (fresh and re-init)

0. **Determine self-hosting mode** (`self_hosting` field in `.asd/project/config.yaml`; missing, unreadable, or duplicated key → `disabled`, fail closed).
0a. **Sync `AGENTS.md`/`CLAUDE.md` managed blocks** (see "AGENTS.md sync"). Runs every fresh or re-init invocation, regardless of subsequent user choices or aborts, in both self-hosting and consumer mode — the managed block always generates from `t_AGENTS.md`/`t_CLAUDE.md` (`providers.md` ownership table). Sprint-mediated mode skips it: `config.yaml` is its only write.
0b. **Require `gh`**: run `gh --version`, then `gh auth status`. Either fails → stop before any further step, naming the fix: install gh (https://cli.github.com), or run `gh auth login`. `pr` opens and merges every sprint PR through it (`git-strategy.md` "PR creation").

## Workflow (fresh)

1. Detect greenfield vs brownfield via repo search on source files
2. Request user input, batch: chat/docs language, decomposition, compatibility, external review, self-hosting, `user_gates` (`strict` default or `adaptive`), and document settings. `documents.audit` is `auto|always|off` (`auto` default); other document flags keep their existing values. For self-hosting recommend audit `auto`, other documents disabled and `diagram_tool: none`.
3. If decomposition enabled → request user decision: diagram_tool (`none` | `likec4` | `mermaid`); disabled → `none`
4. Detect OS via command execution (silent; no confirm yet) — selects step 12's OS-specific commands, never written to config
5. Detect external tools (silent; record results, do not prompt per-tool yet):
   - `likec4 --version` (only if diagram_tool=likec4)
   - Node (only if `documents.ux_spec: enabled` — the design-system gate that needs it is skipped entirely otherwise, `sprint-lifecycle.md` "Optional documents"): check `node --version` and `npm --version`. Tooling invoked via `commands.yaml` (`designmd-*`); no `designmd` binary on PATH required. When `documents.ux_spec: disabled`, skip detection and omit the `designmd-*` custom commands entirely.
   - the *other* provider's CLI, wrapped by External Review (only if external_review=enabled): resolve its configured command or default lookup, then probe it — `system.tools.codex_command` or `codex` under Claude Code; `system.tools.claude_command` or `claude` under Codex (`.asd/rules/providers.md` § External review symmetry). Never probe the running host's own CLI.
   Record the resolved command and availability for the consolidated proposal
6. Pick review iteration defaults (low=1 medium=1 high=2 critical=10) — include in proposal, do not prompt yet
7. Pick git defaults (base_branch from `git symbolic-ref refs/remotes/origin/HEAD` or `main`; branch_pattern `sprint/<NNN>-<slug>`) — include in proposal
8. Auto-detect build commands from:
   - manifests: package.json scripts, Cargo.toml, pyproject.toml, go.mod, Makefile
   - code analysis: CI configs (.github/workflows, .gitlab-ci.yml, etc.), Dockerfile RUN lines, README command patterns
   - native affected/changed-test selector (`test_affected` — `sprint-lifecycle.md` "Impacted
     test set"): keyed on the detected test *runner*, never on a command string. package.json
     `devDependencies`/test script naming `jest` → `jest --changedSince=<BASE_REF>`; naming `vitest`
     → `vitest run --changed <BASE_REF>`; pyproject.toml/requirements.txt naming a picked/testmon-
     style pytest plugin → that plugin's ref-based invocation. No runner match, or a matched runner
     with no such flag (e.g. a bare pytest, `dotnet test --filter`'s name-filter, Go package
     selection) → omit the field entirely; never guess a flag for an undetected/unsupported runner —
     the search-derived impacted set is the safe fallback
   Record into proposal; do not prompt per-command yet
8a. **Consolidated proposal & edit gate** — present every auto-detected/defaulted value in one structured block in `language.chat`:
    - OS, tools, review limits, git settings, `user_gates`, audit mode, diagram_tool, detected build/test/lint/run commands and any affected-test selector
    Then request user decision: `accept-all` | `edit-section` | `abort`.
    - `edit-section` → request user decision on which section (os | tools | review | git | commands), collect new values, re-show proposal, loop until `accept-all`
    - Missing required tools → must resolve here: Node (if `documents.ux_spec: enabled`) → install or `documents.ux_spec: disabled`; likec4 (if diagram_tool=likec4) → install or diagram_tool `mermaid`/`none`; the wrapped external-review CLI (if external_review) → install, override path (`system.tools.codex_command`/`claude_command`) or `review.external_review: disabled`. Do NOT silently proceed with missing required tools.
    Only after `accept-all` proceed to write.
9. Write `.asd/project/config.yaml` from `t_config.yaml` with approved `user_gates`, audit mode, diagram_tool and other fields.
10. Ask user what custom rules to add (separately for common / design / coding scopes); write three files from templates: `.asd/project/custom-common-rules.md`, `custom-design-rules.md`, `custom-coding-rules.md`. Empty scope still writes template stub (header + intro), so agents always find the file.
11. Write `.asd/project/stubs.md` from `t_stubs.md` (empty registry — downstream phases expect the file to exist)
12. Write `.asd/project/commands.yaml` (from `t_commands.yaml` + detected + OS-specific `custom.designmd-*` only when `documents.ux_spec: enabled`); `test_affected` written only when detected, omitted (not written empty/guessed) otherwise — a `.asd/project/commands.yaml` from an older ASD version without the field keeps working unchanged since the impacted set falls back to the search-derived definition
13. If decomp enabled: write an empty registry `docs/architecture/subsystems.md` from `t_subsystems.md` (no rows, no diagram) when absent — Architect fills it (`artifact-layout.md` "Subsystem registry"). Only if `diagram_tool` is not `none`:
    - **likec4 mode**: seed `c4/model/main.c4`, `c4/views.c4` from templates. Seed `commands.yaml` with a `c4-build: "likec4 build docs/architecture/c4 --output docs/architecture/c4/dist"` build-to-view command — `dist/` itself is gitignored, not built here
    - **mermaid mode**: seed nothing more — the diagram lives in `subsystems.md`; no `c4/`, no `c4-build`
14. If decomp and likec4 mode: **seed `.gitignore`** for C4 build output — append (never clobber existing entries; create the file if absent) `docs/architecture/c4/dist/` if not already present
15. **Post-init artefact checks** — suggest dedicated skill for each missing required artefact (do NOT auto-dispatch). Order: concept → stack → design-system:
    - `docs/product/concept.html` absent → suggest `$asd-concept`
    - `docs/architecture/stack.html` absent → suggest `$asd-stack`
    - `docs/ux/DESIGN.md` OR `docs/ux/design-system.html` OR `docs/ux/accessibility.html` absent → suggest `$asd-design-system`
16. Brownfield: prompt user to start sprint with audit-only scope (optional)
17. **Run sync** — invoke the sync engine (`.asd/sync.js --check`) to confirm the bundled provider-view trees (`.claude/agents`, `.claude/skills`, `.claude/hooks`, `.agents/skills`, `.codex/agents`, `.codex/hooks`) are `current` against the shipped canon. Report any `stale`/`modified-foreign` finding to the user — do not silently apply.
18. **Codex trust warning** — unconditional (every project gets a generated `.codex/hooks.json`, regardless of which provider is primary or whether external review is enabled): warn the user that Codex requires explicitly trusting this project's `.codex/hooks.json` before its hooks run (Codex refuses untrusted project-level hooks by design). Point to Codex's own trust-approval step; do not attempt to bypass it.
19. Print summary + return contract

## Workflow (re-init)

1. Read current `.asd/project/config.yaml`
2. **Dump full current config to chat** in `language.chat` before any edit prompt. Render every field as structured block. User MUST see complete current state before being asked what to change. Do NOT skip or summarise — full values verbatim.
2a. List every field present in `t_config.yaml` but absent from the current config, each with its absent default (the template comment's, e.g. `self_hosting` → `disabled`), so a newly shipped field is editable
3. Request user decision on which sections to edit, absent fields included
4. Per section: ask new value → add to pending change-set (do not write yet)
5. Show consolidated diff of all pending edits → request user decision: `accept-all` | `edit-section` | `abort`; loop until accepted
6. Apply diff; write config
7. If `review.external_review=enabled`, resolve and probe the wrapped CLI from the final config exactly as fresh init does; report the resolved command and availability. An unavailable probe leaves the setting intact but is surfaced as the explicit runtime availability-skip reason (`external-review.md` "Detection and negative cache").

## Workflow (sprint-mediated)

Plan acceptance is the approval of record: no config dump, no section prompt, no `accept-all`.

1. Read current `.asd/project/config.yaml`
2. Validate every pair against the working-tree `.asd/templates/t_config.yaml` (a key an earlier-wave Task added counts) before any write: the dotted key must name a leaf field there, and the value must fit that field: one of its enumerated values where it enumerates them (`Values:` or an inline `a | b` comment), else the type of its template value — a non-negative integer for an integer, a string for a string. Any failing pair → `FAILED` naming it; nothing written.
3. Set each declared `<key>=<value>` pair (dotted path); its line takes that key's inline comment from the template, when the template line has one; touch no other field. A pair whose value and comment already match is a no-op.
4. Write config
5. Post the diff in `language.chat`: one `<key>: <old|absent> → <new>` line per pair
6. Re-init step 7 applies when a pair touches `review.external_review` or `system.tools`

## AGENTS.md sync

Idempotent, ownership-class **managed block** (`.asd/rules/providers.md` ownership table). Uses the sync engine's managed-block functions (`.asd/sync.js`: `findManagedBlock`, `statusManagedBlock`, `applyManagedBlock`) — do not hand-roll marker parsing.

- `AGENTS.md` at repo root: managed-block body = verbatim contents of `.asd/templates/t_AGENTS.md`. Block delimited by `<!-- asd:begin v=1 -->` / `<!-- asd:end -->`.
- `CLAUDE.md` at repo root: managed-block body = verbatim contents of `.asd/templates/t_CLAUDE.md` (thin — just an `@AGENTS.md` import). Same block markers, same mechanism.

Algorithm (per file, both files every run):

1. Compute status via `statusManagedBlock(targetPath, relKey, templateBody, syncState)` → `missing | modified-foreign | current | stale`.
2. `missing` → `applyManagedBlock` creates the file (or appends the block to an existing foreign file, preserving all its other content byte-for-byte).
3. `stale` → `applyManagedBlock` replaces only the block body, in place, leaving everything outside the markers untouched.
4. `current` → no-op.
5. `modified-foreign` (block exists but `.asd/sync-state.json` holds no matching record, or its recorded digest doesn't match — someone hand-edited inside the block) → STOP, request user decision: `overwrite` (accept sync's version) | `keep-local` (record current content as the new baseline) | `abort`. Never silently overwrite a hand-edit.

Rules:
- Never touch content outside the block markers.
- Record the applied block's digest in `.asd/sync-state.json` after every write (this is what lets step 1 detect a later hand-edit).

## OS-specific commands written to .asd/project/commands.yaml

Four custom commands emitted only when `documents.ux_spec: enabled` (else omitted entirely — no design-system gate to serve them). Linter always invoked via `designmd-lint`; `designmd-install` is session-scoped prerequisite on Windows (no-op elsewhere).

**Windows** (run from project root):
- `designmd-install: "npm install @google/design.md"`
- `designmd-lint: "node_modules\\.bin\\design.md.cmd lint docs\\ux\\DESIGN.md"`
- `designmd-diff: "node_modules\\.bin\\design.md.cmd diff"` (path args supplied at call time)
- `designmd-export: "node_modules\\.bin\\design.md.cmd export --format json-tailwind docs\\ux\\DESIGN.md"`

**Linux/macOS**:
- `designmd-install: ""` (no-op; `npx` fetches on demand)
- `designmd-lint: "npx @google/design.md lint docs/ux/DESIGN.md"`
- `designmd-diff: "npx @google/design.md diff"`
- `designmd-export: "npx @google/design.md export --format json-tailwind docs/ux/DESIGN.md"`

## Artefacts produced

- `.asd/project/config.yaml` (incl. `self_hosting`, `user_gates`, `documents.*`, `project.diagram_tool`)
- `AGENTS.md`, `CLAUDE.md` — managed block synced from `t_AGENTS.md`/`t_CLAUDE.md` in both consumer and self-hosting mode
- `.asd/project/custom-common-rules.md`, `custom-design-rules.md`, `custom-coding-rules.md`, `stubs.md`
- `.asd/project/commands.yaml`
- `docs/architecture/subsystems.md`, empty (decomp only)
- `docs/architecture/c4/` likec4 seed (decomp + likec4 only)
- `.gitignore` entry for likec4 build output (same condition; append-only, existing entries preserved)

Concept, stack, design system NOT produced here; owned by `$asd-concept`, `$asd-stack`, `$asd-design-system` respectively.

## Agents dispatched

None. Init runs solo; fresh and re-init have no sprint context, sprint-mediated reads none beyond its declared pairs.

## Return contract (single line)

```
INIT: <fresh|re-init|sprint-mediated> | MODE: <greenfield|brownfield|n/a> | DECOMP: <enabled|disabled> | DIAGRAM: <none|likec4|mermaid|n/a> | TOOLS: likec4=<ok|missing|skip|n/a> node=<ok|missing|skip|n/a> external_review_wrapped_cli=<ok|missing|skip|n/a>
```

Sprint-mediated: `MODE` and every `TOOLS` entry its step 6 did not probe are `n/a`; `DECOMP`/`DIAGRAM` are read from the written config.

Followed by file-creation summary.

## References

- Templates in `.asd/templates/`
- `.asd/rules/core.md`, `.asd/rules/artifact-layout.md`, `.asd/rules/providers.md`
- Google Labs DESIGN.md spec: https://github.com/google-labs-code/design.md
