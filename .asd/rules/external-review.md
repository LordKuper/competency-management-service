# External Review

External Review agent runs the wrapped CLI — Codex when running under Claude Code, Claude CLI when running under Codex (`.asd/rules/providers.md` § External review symmetry) — in parallel with internal reviewers during `design-review` and `impl-review`, merging findings into the common issue pool.

## Enablement

Controlled by `review.external_review` in config (`enabled` | `disabled`). If `disabled`: agent does nothing, no log entry.

## OS-specific invocation

OS read from the `external-preflight` output's `platform` (`process.platform`). Stdin syntax follows the shell that runs the command, not `platform` alone: Claude Code's run-command shell is POSIX bash on every OS (Git Bash on Windows); Codex's is PowerShell on `win32`.

The rendered prompt alone is passed via **heredoc/here-string straight into the wrapped CLI's stdin — never written to disk**; the scope manifest travels by path, never in it — the prompt names the review output dir's `external.scope.json`, which `emit-manifest` already wrote ("Phase-scoped payload"). This agent runs read-only on both providers and the wrapper writes nothing, so no step in the invocation may touch the filesystem. The wrapped CLI's own stdout is captured directly as its final message (the text verdict) — no `-o <out-file>`, no temp file, no cleanup step, because nothing was ever created on disk.

The command TAIL differs per wrapped CLI — this is a real syntax difference. Canonical tail per CLI, including explicit model, effort, and read-only boundary, lives once in the agent file's `wraps_invoke_args` (`asd-external-review.md` frontmatter) — not restated here.

`--allowedTools` alone is not a read-only boundary.

| Host, `platform` | Preflight | Review command |
|---|---|---|
| Codex, `win32` | runtime helper with direct arguments or its fixed PowerShell shim | `@'<rendered prompt>'@ \| <resolved-command> <wraps_invoke_args>` (here-string piped to stdin) |
| Claude Code, any; Codex, any other (`linux`, `darwin`) | runtime helper with direct arguments (fixed PowerShell shim allowed on `win32`) | `<resolved-command> <wraps_invoke_args> <<'EOF'` / `<rendered prompt>` / `EOF` (heredoc piped to stdin) |
Both forms read the rendered prompt from stdin; the wrapped CLI's own read-only filesystem tools read `external.scope.json` first, then resolve `files[]` and `diff` content from the repo (never from the manifest bytes) — the command's own stdout is the final message text verdict. No `-o <out-file>` for either CLI.

`<wrapped-cli>` is `codex` under Claude Code / `claude` under Codex — command name on every OS (each ships a shell shim plus OS-specific wrappers on Windows; no compiled `.exe`). `<resolved-command>` is that default unless the config override (`system.tools.codex_command` under Claude, `system.tools.claude_command` under Codex) is non-empty, in which case it replaces the lookup path for both probe and review.

## Detection and negative cache

Before wrapper dispatch or scope manifest emission, phase orchestration calls `node .asd/runtime.js external-preflight --input <json>`. Input supplies provider, strong wrapped model, resolved command, cache path — canonical, single value for every caller: `.asd/project/external-cache.json` (gitignored, machine-local retry/failure state, never committed) — and non-secret authentication generation or credential-file metadata reference; custom authentication arguments are rejected. The helper uses direct bounded process arguments, never a shell-interpolated command; on Windows it uses only a fixed PowerShell shim when direct executable lookup fails. It runs `--version`, then fixed `codex login status` or `claude auth status --json`; command/auth text is never persisted. `local-ready` means only that the executable and local authentication status were observed. Model access, quota, and reachability remain `unknown` until the first real review request.

On a real-request authentication, quota, reachability, or command failure, phase orchestration calls `node .asd/runtime.js external-record-failure --input <json>` with the preflight fingerprint and, as retry-after, the reset the provider reported with the failure, never a 5-minute default. The runtime caps retry-after at one hour and uses one hour when the provider reported no reset. The cache stores only status and retry-after. Its identity binds the selected model, resolved command, fixed auth check, auth status, and non-secret auth generation; expiry or a changed identity restores an attempt. Preflight always reruns local executable and auth checks before honoring a negative cache. It never sends a paid probe.

On a non-ready preflight (command/auth failure) or an active negative cache — never on a failure after invocation (Outcome contract):

- Return `APPROVE (skipped: external review unavailable: <specific status>)`; the dispatching workflow persists the exact status in that iteration's `external.md` through `persist-review` (`review-policy.md` "Coverage ledger" Persistence) — its first-line token plus an `Unreviewed files` line naming the `files[]` the scope manifest would have carried ("Iteration semantics"), no other section — appends it to `<sprint>/decisions-log.md` for sprint `<NNN-slug>` iteration `<id>` (the iteration id, `sprint-lifecycle.md` "Review iteration counters"), and appends an `F-N` friction entry for it (`sprint-lifecycle.md` "Friction log")
- Continue without external review, no user prompt

An availability skip satisfies only that iteration and never creates an APPROVE latch. A later local-ready result dispatches External Review normally.

## Outcome contract

Sole statement of what a dispatched External Review may return — exactly one of two outcomes:

- **verdict** — findings text whose first content line is `[REVIEW-<phase>-external]: APPROVE|CONCERNS|FAIL` (`review-policy.md` "Gate Verdict Format"), over the whole `files[]`
- **availability skip** — `APPROVE (skipped: external review unavailable: <specific status>)`, only on a non-ready preflight or an active negative cache (above)

Nothing else. A dispatch makes one wrapped-CLI invocation over its whole `files[]`, retried once — never batched or split; in impl-review, review waves bound its size (`sprint-lifecycle.md` "Review iteration counters"). The wrapper gives that run command an explicit timeout of at least 10 minutes, never the host's shorter default, and never redirects its stdout, which is the verdict text ("OS-specific invocation"). A failure after invocation — wrapped-CLI crash, hang, timeout, unusable output, the retry exhausted — is an interrupted dispatch, never a skip: the wrapper returns `external review interrupted: <cause>`. An authentication, quota, reachability or command cause is also recorded through `external-record-failure` (above), so the re-dispatch's preflight yields the skip. The wrapper awaits the wrapped CLI inside its own dispatch and never backgrounds it; no outcome means "started, still running". The contract scopes a dispatch that reached that invocation: a precondition missing before any invocation (prompt template absent) aborts the dispatch instead — a framework defect the orchestrator must see, never an availability skip.

An empty return, or prose carrying no outcome, is not permitted and is not a verdict. Its disposal, like the interrupted return's, is `review-policy.md` "Interrupted dispatch", imported here whole.

## Phase-scoped payload

External Review gets the hand-off every reviewer gets — list, diff file, whole files as context (`review-policy.md` "Scope hand-off", not restated here); this section holds only its External-specific form. The wrapper's dispatch payload opens with the `Repo root:` and `Turn budget:` header lines (`providers.md` "Dispatch payload header"). `node .asd/runtime.js emit-manifest --reviewer external --iteration <N> [--wave <K>]` (impl-review: `--wave` and a range required) writes `external.scope.json` per `external-review/t_review-scope.json` plus its diff file into the review output dir — no rubric, no ledger. The prompt names the manifest by path; the wrapped CLI reads it first, then `files[]` and the `diff` file from the repo with its own read-only tools, never content from the manifest bytes, and never computes a diff itself.

Manifest fields: `phase`; `iteration`; `wave` (impl-review only); `files[]`; `diff` — the diff file's path, `null` at design-review iteration 1. The diff file sits under `<sprint>/reviews/`, outside every scope below, so the prompt names it as readable context — never a finding location.

Pathspec exclusions per phase and mode, applied by the phase when it builds the list — never sent to the reviewer:

| Phase | scope (`files[]`) | exclusions |
|---|---|---|
| design-review | sprint design drafts only — `<sprint>/design/**`, minus generated output (only the drafts that exist per `documents.*`) | `c4-full/dist/` |
| impl-review, `self_hosting: disabled` (consumer, default) | changed files anywhere in the repo, minus the exclusions — code and tests in practice | `.asd/**`, `docs/**`, generated provider views (`.claude/{agents,skills,hooks}/**`, `.codex/{agents,hooks}/**`, `.agents/skills/**`) |
| impl-review, `self_hosting: enabled` (this repo) | changed files anywhere in the repo — everything here IS framework source (`sprint-lifecycle.md` "Self-hosting") | `.asd/project/**`, `.asd/sprints/**`, generated provider views per `sprint-lifecycle.md` "Self-hosting" |

Cross-phase reference material (concept, custom rules, accessibility baseline, prd/adr/stack/commands) travels as **paths only** in the rendered prompt (`t_prompt-external-{design,impl}.md` "project context"), never inside the scope manifest, never diffed. design-review scope never names source code; consumer-mode impl-review scope never names design/doc files (a doc-vs-code drift finding belongs to the internal Documentation reviewer). The exclusions also keep C4 schemas out of consumer impl-review: likec4 lives under `<sprint>/design/c4-full/` and `docs/architecture/c4/`.

**Generated output is always excluded.** `**/dist/**` (likec4 build), `design-system.html` — both derived from a source the reviewer already sees (`*.c4`, `DESIGN.md`). Review the source, not the build.

Both impl-review rows start from the whole repo and subtract the exclusions, never an allow-list — so any real source added later (CI configs, root-level configs, anything else) is in scope automatically, with no manifest or rule edit. Agent memory's status in both modes: `artifact-layout.md` "Agent memory".

## Iteration semantics

`files[]` each iteration = the iteration's scope list — impl-review per the current wave (`sprint-lifecycle.md` "Review iteration counters"); design-review every in-scope draft at iteration 1, then the drafts changed since the previous iteration's snapshot (`asd-phase-design-review.md` step 7 "Draft list") — unioned with the carried-over `Unreviewed files` below. Absent `iteration_heads` key fallback: `sprint-lifecycle.md` "State recovery" (sole SSoT).

**Unreviewed files.** Only an availability skip leaves files unreviewed: its `external.md` lists the whole `files[]` it would have sent as its `Unreviewed files` line (`t_review-report.md`). The next iteration of the same phase — in impl-review the same wave's, `wave-<K>/iter-(NN+1)`; a closed wave carries nothing — unions that list into its own `files[]`. They go to `emit-manifest` as `--full-files`: in impl-review with `--full-base <base_branch>`, so their diff spans `<base_branch>...HEAD`, not the incremental range; in design-review, which has no wider base, they are left out of the snapshot diff, so each carried-over draft is read whole, as at iteration 1.

Agent dispatched fresh each iteration (`review-policy.md` clean-context). Incremental manifest narrows *input*, not context.

## Output mapping

Wrapped-CLI severity terms in the captured stdout mapped to ASD severity:

| Wrapped CLI | ASD |
|---|---|
| blocker, critical | critical |
| major | high |
| minor | medium |
| info, suggestion | low |

Findings rendered to the review output dir supplied by the dispatching phase skill (`reviews/design/iter-NN/external.md` or `reviews/impl/wave-<K>/iter-NN/external.md`), using verdict format from `review-policy.md`. Dropped findings (below severity floor, nitpick) are never rendered as per-finding rows — only a count per category, per `t_review-report.md`; nothing downstream reads a dropped finding's detail.

## Stalemate detection

Phase skill supplies the finding set of the latest earlier verdict iteration — skip iterations are skipped; in impl-review of the same wave, by iteration id — as explicit payload input (from iteration 2). Agent compares against that supplied set only — does not read another iteration's review files.

If two consecutive verdict iterations produce an identical issue set (same files, lines, messages), the agent returns a verdict ("Outcome contract"): first line `[REVIEW-<phase>-external]: FAIL`, plus a `Stalemate: <N> iterations, identical findings` block (`t_review-report.md` `## Stalemate`). The orchestrator asks the user (`core.md` "Request user decision") and applies the choice to the stalemated findings; the review workflows' generic FAIL accept/override bullets do not apply:

- **stop** — accept the current state: the orchestrator records the findings resolved without fix (`sprint-lifecycle.md` "State recovery" user-resolved findings), and routing continues with the rest
- **continue fixing** — the findings stay in the fix set: impl-review routes them to review-fix, design-review to the creator fix and the next iteration
- **abort** — emit ABORT

## Aggregation

External Review verdict counts as one reviewer in the DoD check. APPROVE from External Review required when `external_review: enabled`; a skip satisfies its own iteration only (Outcome contract).
