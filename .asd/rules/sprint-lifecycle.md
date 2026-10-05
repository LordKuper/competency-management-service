# Sprint Lifecycle

## Orchestration and adaptive gates

The main orchestrator owns scope and plan writing, phase/state transitions, decision logging, manual-step validation, Git/PR, archival, merge recovery and self-hosting release. It delegates only artefact creation, testing, review or advice.

**Retrospective-derived criteria are re-verified at scope.** A retrospective is sprint-scoped and never promoted ("Retro phase" below), so its rows state what was true at the HEAD that produced them. Before such a row becomes an `AC-N` in `sprint.md`, the orchestrator checks it against current `HEAD` and carries only what is still unresolved — an already-satisfied row is closed, a partly delivered one narrowed to its remaining half. Recording home is `decisions-log.md` alone: one entry naming the verified HEAD sha, each row's outcome and the evidence for it. `sprint.md` gains no section for this. A row asserting host behaviour (a provider tool, CLI or runtime) is verified against that host's docs or a live dispatch — never against the row itself or memory — and the doc URL or dispatch is its evidence.

**Retro intake.** At scope, after the decisions log is seeded and before the scope gate (`asd-phase-scope.md` step 2a), the orchestrator offers open retro rows. Candidates: `node .asd/runtime.js retro-candidates --sprints <dir> --backlog <path> [--self-hosting]` (`--self-hosting` iff `self_hosting: enabled`) — the retro rows ("Retro phase" row id) of the most recent `archived/` sprint with `phase=done` and a `retrospective.html`, minus rows the retro backlog (`artifact-layout.md` "Retro backlog") already disposes, plus its `deferred` rows; `covered by:` rows are dropped. Output: a JSON array of `{row, acts_on, guardrail, home}`; outside `--self-hosting` the latest retro's `asd` rows stay in it, tagged `upstream: true`. A non-zero exit blocks intake: its stderr goes to the user as a scope-gate decision — fix the backlog or retro and re-run, or skip intake this sprint with one decisions-log line. An empty array, or one of only `upstream: true` rows, is a no-op for dispositions: one decisions-log line, no question, no backlog write; its `upstream: true` rows are still shown at the gate. A row tagged `upstream: true` is no candidate for disposition: the scope gate shows it as an upstream proposal for the framework repo — row id, guardrail, home — with no question; it is never an `AC-N` and is written to no backlog. Each other candidate is re-verified per the rule above; a resolved one is written `closed` without asking. The rest go to the user inside the hard scope gate (`checkpoints.md` "Gate policy"), a recommendation each; the user decides all or some: include → an `AC-N`, reject → never offered again, undecided → `deferred`. Dispositions are written to the backlog on scope acceptance and land with the sprint PR; an aborted sprint loses them, so they are offered again.

At scope, accept only `documents.audit: auto|always|off`; any other value, legacy `enabled`/`disabled` included, blocks (`checkpoints.md` "Gate policy" invalid-policy rule). Freeze the effective audit boolean in state (`documents.audit`) — the rule below is deterministic, so no separate reason field is stored. `auto` skips only a complete, verifiably mechanical scope with no behaviour, contract, migration or gate impact; unknown/risky scope audits. An accepted scope expansion ("Scope amendment") reevaluates it. Architect owns audit. An ambiguity only authority or preference can settle goes straight to the user; BA is dispatched only for evidenced material product/domain ambiguity its sources can resolve.

Use `checkpoints.md` for every user-gate decision. A merged sprint PR completes the sprint; the next sprint's scope archives it mechanically, no user gate ("PR phase"), and it never blocks the one-active-sprint rule.

## Workflows

A workflow is one sprint lifecycle, declared in `.asd/workflows/<name>.json`; only `standard` and `lite` exist. Keys, and nothing else: `name`; `phases` (ordered, ending before the `done` pseudo-phase); `next` (phase → its allowed `NEXT:` targets); `reviewers.design`/`reviewers.impl` (the verdict keys each review phase dispatches); `rollback_reset` (review node → the phases whose setting resets it, "Review iteration counters").

**Selection**: a hard user decision at `asd-phase-scope.md` step 1 (`checkpoints.md` "Gate policy"), never defaulted — no config key holds one. Frozen in `state.json.workflow`, never changed mid-sprint; an absent field reads `standard`. `NEXT:` routing, preconditions, resume, the re-run menu and the rollback reset follow the frozen workflow.

**Derived from `phases`, never stored**: the design-block collapse ("Optional documents") applies only to a workflow whose `phases` contain `design`; design-promote promotes drafts when `design` precedes it, the accepted implementation when it follows `impl-review`.

**`lite`** — every rule not named here is `standard`'s:
- Chain: scope → audit → plan → impl ⇄ impl-test → impl-review → design-promote → retro → pr. No `design`, no `design-review`, no drafts.
- Acceptance criteria: always `sprint.md`'s `AC-N` list, whatever `documents.prd`. `plan` reads `sprint.md` plus `audit.md` when audit ran; it needs no promoted doc.
- impl-review dispatches `combined` plus External Review (`reviewers.impl`); waves, counters, severity floors, latches, the terminal full suite and fix routing are unchanged. Its green exit is `NEXT: design-promote`.
- design-promote runs after impl-review DoD, "Design-promote phase" steps 1-5 with `sprint.md`, `plan.md`, the sprint diff and `audit.md` when present in place of drafts: each domain creator writes or updates the persistent docs of every document the frozen `documents.*` enables — no draft, no review. With `ux_spec` enabled the design-system gate ("Design phase") precedes UX's write. Hard gates stay: a new subsystem, material stack/UX/brand direction, each `DESIGN.md` token change (`checkpoints.md` "Gate policy"). No-op when frozen `prd`, `ux_spec`, `adr` and `c4` are all `false`. Its writes are committed before `NEXT: retro`.

## Phases (all mandatory)

The chain below is `standard`'s; `lite`'s: "Workflows".

```
scope → audit → design → design-review → design-promote → plan → impl ⇄ impl-test → impl-review → retro → pr
                                                                   ↑__________________________|
```

`impl`, `impl-test`, `impl-review` form one cycle:

- `impl` always routes to `impl-test`. impl writes **no tests**, a `Test-only` Task (run by `asd-tester`, "Plan file format") excepted — its gate is build + lint; a dev may run the impacted set (below) for self-verification only, never as a substitute for `impl-test`/`impl-review`.
- `impl-test` selects the test approach for the whole change scope, prunes redundant tests, writes missing ones, runs the **impacted set** (below) as its suite gate. Code defects → back to `impl` (test-fix mode), then `impl-test` again. Impacted set green → `impl-review`.
- `impl-review` does NOT fix findings — routes back to `impl` (review-fix mode) on unresolved findings, low-severity test-only ones excepted (`review-policy.md` "Low-severity test-only findings"); the sprint then re-enters `impl-test` (code changed → tests re-selected + re-run) before returning to `impl-review`. Once the last review wave's required reviewers return `APPROVE` or are latched ("Review iteration counters" below), `impl-review` runs the **full suite exactly once** — the cycle's only full-suite run — via `asd-tester`, before its green exit. On red: test defects are fixed by `asd-tester` and the suite re-run; code defects instead become `D-N` rows in `test-plan.md` + `state.json.test_defects_pending`, and the phase exits to `impl` test-fix mode rather than fixing code in place. Either red path also clears every APPROVE latch sprint-wide (`APPROVE latch` below).

No cap on `impl⇄impl-test` rounds: loop until the impacted set is green, a dev blocker escalates (`FAILED`/`QUESTION`) or a stalemate escalates ("Impl-test phase" below). `impl-review` keeps its iteration cap. Phase routing follows the `NEXT:` token in each phase skill's return contract, not a fixed linear chain.

**Impl-review clean-worktree precondition** (home statement; mechanic in `asd-phase-impl-review.md` "Preconditions"): `impl-review` refuses to start while `git status --porcelain` is non-empty, measured at phase entry before any dispatch — the iteration diff is computed from commits, so uncommitted work (including pre-existing sprint bookkeeping files) is invisible to every reviewer. The phase's own later writes (review files, `state.json`, `decisions-log.md`, `test-plan.md`) are produced after this gate and are not subject to it. `design-review` has no matching precondition — it builds its manifest from on-disk drafts, so the git-invisibility blind spot does not exist there.

**Impl-test commits its own output** (home statement; mechanic in `asd-phase-impl-test.md`): `impl-test` commits its authored/pruned tests and `test-plan.md` changes, Conventional Commits, before signalling COMPLETED — it runs immediately before the clean-worktree precondition above, and always writes tests plus `test-plan.md`, so the two rules only compose if `impl-test` leaves a clean worktree behind it (`git-strategy.md` "Commit before review").


## Review iteration counters

Independent counters in `state.json`, never shared:

- `reviews.design.iteration` — design-review iterations
- `reviews.impl.waves[K-1].iteration` — impl-review iterations of review wave K; `reviews.impl.wave` = K, the current wave

Each review phase reads, increments, and reports only its own counter; severity floor and iteration cap (`review-policy.md`) are computed per counter, so per wave in impl-review, less a scope amendment's base ("Scope amendment").

**Review wave** (sole definition; the only other "wave" is the plan Task wave, "Plan file format" Wave declaration): one of the disjoint, sequentially reviewed slices of the impl-review scope, each a standard impl-review over its own node — `iteration`, `verdicts`, `iteration_heads`, `latched` (`t_state.json`), keys inside staying `iter-NN`. Design-review has no waves.

- **Division point**: `reviews.impl.waves[0].iteration` is `0` — first entry, or first after a rollback reset. There `node .asd/runtime.js review-waves` measures the scope file list over `<base_branch>...HEAD` — added plus deleted lines (binary files and pure renames count 0), file count, diff bytes — and returns the wave count n: one per threshold begun (`.asd/runtime.js` `WAVE_THRESHOLD_LINES`, `WAVE_THRESHOLD_FILES`, `WAVE_THRESHOLD_BYTES`), whichever measure needs most, at least 1, at most `MAX_REVIEW_WAVES` and at most the scope's file count. n = 1 is one unsplit review.
- **Division**: the orchestrator groups the scope *files*, never Tasks, into n logically cohesive waves. Plan Tasks, `ASD-Task` trailers and directory areas are hints only; a file several Tasks touched, or an untrailered one (agent memory), goes by judgment into exactly one wave. `review-waves --division <json> --out <sprint>/reviews/impl/waves.json` accepts only exactly n non-empty, disjoint lists covering the scope — an empty scope is the one empty wave `[[]]` — and writes them there with `base`, `head`, the measured `lines`, `files`, `bytes` and the `thresholds` applied — file lists live only there, never in `state.json`. Then `reviews.impl` = `wave: 1` plus n seed nodes. n > 1 adds one decisions-log entry.
- **Iteration id**: an impl-review iteration's id is `wave-<K>/iter-NN`, `NN` = wave K's counter; every impl-review literal citing `iter-NN` — review paths, `review_fixes_pending`, fix-round tails, `ASD-Task` trailers, decisions-log lines — means that id. Design-review ids stay `iter-NN`.
- **Scope per iteration**: wave K, iteration 1 = its `waves.json` list, each path a commit since the division renamed mapped to its current name (`node .asd/runtime.js wave-files`), ∪ files changed since wave K−1's last iteration head (review-fix commits, the reviewer memory writes the orchestrator commits with review files — `review-policy.md` "Diff reachability"), the whole list diffed over `<base_branch>...HEAD`. Iteration NN ≥ 2 = files changed in that wave's `iteration_heads["iter-(NN-1)"]...HEAD`, diffed over that range, whichever wave they were divided into, new files included — every commit of the intervening review-fix/test-fix + impl-test cycle, mirroring `test-plan.md`'s `Entry log` → `HEAD analysed`. A change to an earlier wave's file is reviewed in the current wave; a closed wave is never reopened.

**Lifecycle:**

- Created at `0` in `scope` (`t_state.json`: impl as one seed wave node).
- `reviews.design.iteration` increments at the **start of every entry** (`1` on first); design-review is entered once and loops internally.
- A wave's counter increments at the **start of every iteration of that wave** (`1` on its first); `impl` and `impl-test` never touch it. Wave K's reviewer roster met (`review-policy.md` "DoD per review phase") and K < n → the same entry moves to wave K+1, iteration 1, with no `impl`/`impl-test` between (no code changed). Unresolved findings in wave K → `impl` review-fix → `impl-test` → re-entry on wave K. The terminal full suite ("Impacted test set") runs once, after wave n's roster is met.
- **Rollback reset.** When `state.json.phase` is set strictly earlier in the chain than a review's input-producing phase, that review's state resets: `reviews.design` counter to `0`, `verdicts` and `latched` to `{}`; `reviews.impl` to its `t_state.json` seed (wave 1, one fresh node), every wave dropped, so the next entry re-divides and a fresh `waves.json` overwrites the old. Severity floors reset with the counters, latches with the rest (APPROVE latch, below — no second mechanism); nothing on disk is deleted. Input-producing phases: `design` for design-review, `impl` for impl-review. The phases that reset `reviews.<node>` are the frozen workflow's `rollback_reset.<node>` ("Workflows"). Setting phase to `impl` or `impl-test` is not earlier than `impl` — normal cycle re-entry never resets. Reset fires only on a genuine rollback (the `asd-sprint` resume menu's *re-run earlier phase*).
- On iteration-cap override (per wave in impl-review), the counter keeps incrementing — not reset. Severity floor stays pinned at `critical` until a later scope amendment re-bases it ("Scope amendment").
- **Legacy shape**: a `reviews.impl` without `waves` (a sprint in flight before review waves) reads as `{wave: 1, waves: [<that node>]}`, and its `reviews/impl/iter-NN/` dirs and a bare `review_fixes_pending: "iter-NN"` as wave 1; the first wave-aware write, at impl-review entry, stores that shape verbatim.

Verdict files: design-review → `<sprint>/reviews/design/iter-NN/`, impl-review → `<sprint>/reviews/impl/wave-<K>/iter-NN/`, `NN` = that counter.

## APPROVE latch

In impl-review, `reviews.<phase>.<field>` below means the current wave node's (`reviews.impl.waves[K-1]`, "Review iteration counters"); "same phase" means same wave, and a new wave starts unlatched.

**Invariant**: a reviewer's key is ALWAYS written to `state.json.reviews.<phase>.verdicts["iter-NN"]` for every iteration of that phase that runs. A latch-skipped reviewer gets its inherited `APPROVE` recorded there without being dispatched. `latched` is purely a dispatch-time optimisation — it decides whether a reviewer is called again — and NEVER participates in DoD or pr-gate aggregation; both read `verdicts["iter-NN"]` alone, except per "State recovery" "User-resolved findings". Consequence: clearing `latched` can never change satisfied-vs-blocking for any iteration, because the verdict keys it would have gated are already there.

Persisted per phase per reviewer key in `state.json.reviews.<phase>.latched` (`t_state.json`) — a map from reviewer key (the same keys used in `verdicts["iter-NN"]`: the frozen workflow's `reviewers.impl` for impl-review, `reviewers.design` for design-review — "Workflows") to the iteration number at which that reviewer returned `APPROVE`. An absent key means that reviewer has never latched, or its latch was cleared. A sprint in flight when this field shipped carries no `latched` object at all under one or both phase nodes — treat a wholly absent `latched` object the same as an empty one (`{}`, no latches), mirroring the `iteration_heads` absent-key fallback ("State recovery" below); never an error.

A reviewer key present in `reviews.<phase>.latched` is NOT dispatched on any later iteration of the same phase (`asd-phase-impl-review.md` step 6, `asd-phase-design-review.md` step 7). Per the invariant above, its inherited `APPROVE` is still written into that iteration's `verdicts["iter-NN"]` — its existing review file from the iteration it actually latched stands as the evidence backing that entry, unchanged.

The dispatching phase workflow writes a reviewer's latch entry the moment that reviewer's parsed verdict token for the current iteration is `APPROVE` — same step that records the token into `verdicts["iter-NN"]`. A reviewer already latched from an earlier iteration is left untouched by the latch write itself (it produced no new token, having not been dispatched) but still receives this iteration's `verdicts["iter-NN"]` entry per the invariant.

**Availability-skip carve-out.** Only a verdict produced by an actual review of the whole scope latches — an availability skip is not one. External Review's availability skip (`external-review.md` "Detection and negative cache" — phase-supplied preflight returns non-ready, or an active negative cache, not a judgment on the diff) is recorded in `verdicts["iter-NN"].external` as `"APPROVE (skipped: <reason>)"` — distinct from the bare `"APPROVE"` token a completed review writes — and satisfies DoD identically (`review-policy.md` "DoD per review phase") but is NEVER written to `latched`: the dispatching phase workflow's latch-write step (`asd-phase-design-review.md` step 9, `asd-phase-impl-review.md` step 8) writes `latched[<key>] = N` only for the bare `"APPROVE"` token, never for the `"APPROVE (skipped: ...)"` form. A latch means "already reviewed, skip re-review"; an availability skip means only "unavailable this iteration" and must not permanently remove External Review from the sprint once availability returns.

**Reset.** The rollback reset above already clears `latched` to `{}` alongside `iteration`/`verdicts` for the affected phase — no second mechanism for that route.

**Late-return admission.** A further clearing route: admitting a verified late duplicate return for a reviewer clears that reviewer's latch, that one key only, never in a closed review wave. Mechanics and the admission test stay in `review-policy.md` "Late duplicate return"; this line exists so the sole home of latch persistence names every route that clears it.

**Scope amendment.** A further clearing route: an amendment accepted after impl-review's division point clears the current wave's `latched` to `{}` in its own write ("Scope amendment" step 3), so the amended code meets the full roster. Recorded verdicts are untouched.

**Red-full-suite invalidation.** A red full suite (the end-of-`impl-review` terminal suite run) proves previously-approved code was wrong: on that failure, clear `reviews.design.latched` and every impl-review wave node's `latched` to `{}` sprint-wide — not only the reviewer(s) whose domain the regression touched — before the sprint routes back to `impl`. This clears the dispatch-skip optimisation only: the next `impl-review` entry re-dispatches its full required roster. A closed wave is never dispatched again, so this re-arms only the current (last) wave. Recorded verdicts are untouched (invariant above). A DISTINCT clearing route from the rollback reset above, not a consequence of it: a red-suite failure routes to `impl` in test-fix mode, and re-entering `impl`/`impl-test` from `impl-review` is normal cycle re-entry, never a rollback — "Setting phase to `impl` or `impl-test` is not earlier than `impl`" above, so `rollback_reset` never fires for this route. This paragraph is the contract the full-suite step must satisfy; where in that step the clearing happens is out of scope.

## Impacted test set

Every scoped test run in `impl` and `impl-test` uses the **impacted set** — sole statement of the impacted set; every other file cross-links this section. `impl-review`'s one terminal run is deliberately unscoped (below).

**Definition.** The impacted set is the union of:
1. test files present in the change-surface diff;
2. tests exercising a changed unit, resolved by repo search over references/imports of the changed modules;
3. tests tagged with an AC-N the change touches (the AC-citation convention — the tag lives in the test's name/path, `t_test-plan.md` "Added tests"; the one exception to `code-style.md` §8's in-code document-reference ban).

**Native selector override.** When `commands.yaml` carries a `test_affected` field (a native runner flag such as `--changedSince`/`--onlyChanged`, or a filter expression), that field's result REPLACES the search-derived set above — the runner's own answer is used, not a second derivation. Field absent → fall back to the search-derived set. The field's shape and `t_commands.yaml`/`asd-init` detection are defined where `commands.yaml` is — this section only names the override mechanism and its key.

**Safety valve — mandatory, not heuristic, checked BEFORE the selector or the search-derived set is used.** `asd-tester` MUST apply this test before every scoped run: when the change surface touches shared infrastructure — build config, CI config, shared/common modules, any framework-wide file — the impacted set degrades to the **full suite** for that run.

**Where impacted-only applies**: `impl` (self-verification only, below — devs never author/modify/prune a test); `impl-test`'s suite gate (below).

**Where the full suite still runs**: exactly once per sprint cycle, at the end of `impl-review`, after the last review wave's required reviewers return `APPROVE` or are latched and before the phase's green exit — dispatched to `asd-tester` (reviewers are read-only, `providers.md`; the phase gains this capability only through that one dispatch). Recorded in `test-plan.md`'s existing `Suite run` section including `HEAD`; the `pr` gate keeps reading it from there, wording unchanged (`PR phase` below). Red path and latch-clearing: `impl` bullet above and `APPROVE latch` above. Green full suite is part of impl-review's DoD (`review-policy.md` "DoD per review phase").

Each `impl-test` entry's `Suite run` record measures the tree that entry analysed. Only the terminal full-suite run at the end of `impl-review` measures the final tree.

## Phase table

Rows are `standard`'s; `lite` deltas: "Workflows".

| Phase | Owner | Input | Output | Exit criteria |
|---|---|---|---|---|
| scope | Main orchestrator | user request, retro intake candidates and upstream proposals ("Retro intake" above) | `sprint.md`, sprint id, branch, retro backlog dispositions | scope gate passed, branch created |
| audit | Architect (BA conditional) | `sprint.md`, codebase, `docs/`, existing docs any format/location | `audit.md`; optional reverse-engineered/migrated drafts in `<sprint>/design/`; subsystem registry when absent (decomposition enabled) | audit gate passed |
| design | BA → UX → Architect | `audit.md` | drafts in `<sprint>/design/` | drafts complete |
| design-review | Correctness (UI section, conditional) + Efficiency + Documentation + External Review | `<sprint>/design/` | `reviews/design/iter-NN/<reviewer>.md` | DoD met |
| design-promote | Orchestrator + Architect + BA + UX | approved drafts | persistent docs in `docs/` | drafts merged, decisions-log entry |
| plan | Main orchestrator | promoted persistent docs | `plan.md` | plan gate passed |
| impl | Dev (Tester for a `Test-only` Task) | `plan.md` (initial), `reviews/impl/wave-<K>/iter-NN/` findings (review-fix), or `test-plan.md` Defects (test-fix) | code, `manual-steps.md`, a `Test-only` Task's tests | all tasks/findings/defects done; build + lint pass (completion gate) |
| impl-test | Tester | code diff, `plan.md`, PRD ACs, existing tests (a `Test-only` Task's included) | `test-plan.md`, tests in repo | impacted set green (`Impacted test set` above) → `impl-review`; code defects → `impl` test-fix mode |
| impl-review | Correctness + Efficiency + Testing + Documentation + External Review | code + tests + `test-plan.md` | `reviews/impl/waves.json`, `reviews/impl/wave-<K>/iter-NN/<reviewer>.md` | every review wave's reviewers APPROVE/latched, in order (`Review iteration counters` above), AND terminal full suite green (`Impacted test set` above) → `retro`; red suite → `impl` test-fix mode, latches cleared; unresolved findings → `impl` review-fix mode, same wave, low-severity test-only ones excepted (`review-policy.md` "Low-severity test-only findings") |
| retro | Main orchestrator | `friction-log.md` (may be absent) | `retrospective.html` | retrospective written, empty-log branch included → `pr` |
| pr | Main orchestrator | everything | PR, merged; self-hosting release | sprint PR merged → `done`; the next sprint's scope archives it, no user gate ("PR phase") |

## Self-hosting

`self_hosting: enabled` in `.asd/project/config.yaml` — sole source of truth, no marker file. Absent field or `disabled` = consumer mode (backward compatible, unchanged behavior).

When enabled: Dev may write canonical `.asd/rules/`, `.asd/templates/`, `.asd/agents/`, `.asd/skills/`, `.asd/workflows/`, `.asd/hooks/`, `.asd/runtime.js`, `.asd/migrations/`, `.asd/sync.js`, `.asd/sync-state.json`, `.asd/release-manifest.json`, root `AGENTS.md`, `README.md`, `CHANGELOG.md`, `.gitignore`, `.gitattributes`, `tests/**`, plus its own `.claude/agent-memory/<agent>/` (not generated output — `artifact-layout.md` "Agent memory"). Generated provider views stay read-only: Dev edits canon and never runs `sync.js --apply`; the main orchestrator runs it once, over every view whose canon changed, after the last canon-editing dispatch of a wave or fix round, and commits the regenerated views.

Root `AGENTS.md`'s managed-block/hand-edited-tail split: `providers.md` "Canonical path -> per-provider path" (ownership home). `asd-update` is a no-op here (it pulls framework files INTO a consumer; this repo IS the framework).

Versioning: bump `asd_version` and update `CHANGELOG.md` before PR review; tag and release in `pr` merge mode once the merge is confirmed, on the base commit carrying that bump (`git-strategy.md` "Versioning & Changelog (self-hosting only)").

Framework impl-review/External Review change surface: the whole repo diff (everything here IS framework source — canonical `.asd/**`, `README.md`, `AGENTS.md`, `tests/**`, and anything else added later, e.g. CI configs), minus `.asd/project/**`, `.asd/sprints/**`, the generated provider views (`.claude/{agents,skills,hooks}/**`, `.codex/{agents,hooks}/**`, `.agents/skills/**`; the JSON-merge `.claude/settings.json` and `.codex/hooks.json` can hold user content, so they stay in), build output — never an allow-list of named paths, so nothing new needs a matching rule edit to be reviewed. `.claude/agent-memory/**` is not one of those views — `artifact-layout.md` "Agent memory".

## Optional documents

`documents.<name>` in config (`audit | prd | ux_spec | adr`), frozen into `state.json.documents` at `scope` — phases read that frozen snapshot, never live config, so a mid-sprint config edit never changes an active sprint's preconditions. Old config without the `documents` group, or an active sprint's `state.json` without a `documents` snapshot, means every value `enabled` (no behavior change). Fail-closed default is per-field, not per-group: when the `documents` group is present but a given field is absent from it, that field is `disabled` — only a wholly-absent group defaults everything to `enabled`. `state.json.documents.c4` is the frozen **effective diagram** (computed once, here, at `scope` — never recomputed later): `true` only when `project.diagram_tool` (absent → `likec4`) is not `none` and `project.subsystem_decomposition: enabled`.

**Per-sprint skip** (the one exception to "frozen, never recomputed", narrow-only): the user may flip a frozen `true` `prd`/`ux_spec`/`adr`/`c4` to `false` for this sprint only, at the scope gate (`asd-phase-scope.md` step 3a) or the audit exit (`asd-phase-audit.md` step 5), and only while no draft of that document exists in `<sprint>/design/` (`c4` → `c4-full/`). Hard gate (`checkpoints.md` "Gate policy"). Record: the frozen `false` plus one decisions-log line "<doc> skipped this sprint by user", distinct from a config-disabled document's line; `config.yaml` is untouched. Never `false`→`true`, never `audit` (its `off`/`auto` already cover it).

**Config string → state boolean**: `t_state.json`'s `documents` map holds `"{{DOC_AUDIT}}"`/`"{{DOC_PRD}}"`/`"{{DOC_UX_SPEC}}"`/`"{{DOC_ADR}}"`/`"{{DOC_C4}}"` as quoted placeholders — quoted so the template file itself stays valid, parseable JSON as shipped. At `scope` write time, replace each entire quoted token (**including its surrounding quotes**) with the bare JSON boolean `true`/`false` matching that document's normalized value (`{{DOC_AUDIT}}` the effective audit boolean, `{{DOC_C4}}` the effective diagram) — the written `state.json` must end up with `"audit": true`, never `"audit": "{{DOC_AUDIT}}"` or `"audit": "true"`. Never leave a placeholder token, quoted or not, in a written `state.json`.

**Skip record**: `t_state.json.skipped_phases` starts `[]`. A no-op phase (below) appends its own phase name to this array in the same write that advances `phase` — this is what lets a resumed sprint or a later audit tell "phase legitimately skipped, empty applicable-artifact set" apart from "phase ran and produced nothing," which the `phase`/`updated_at` fields alone cannot distinguish. Never removed or reordered; a phase re-run after a rollback (`checkpoints.md` "Re-run") that turns out non-empty this time does not retroactively remove its earlier skip entry — the array is a historical record, not current status.

**Multi-phase skip**: when one deterministic check subsumes several consecutive no-op phases in a single write — the `design`/`design-review`/`design-promote` collapse below — that one write appends **every** subsumed phase name to `skipped_phases` (`["design", "design-review", "design-promote"]`) and sets `phase` to the **last** subsumed phase name, never one array append per phase and never the first; at the audit exit, a skipped audit's own `"audit"` precedes them in that same write. This way the successor of `phase` in the workflow's `phases` mechanically yields the next real phase and a resumed session cannot re-enter the collapsed block. The subsumed phases are never separately dispatched, so they never make their own individual `skipped_phases` write.

Never optional: `sprint.md`, `state.json`, `plan.md`, `test-plan.md`, impl-review reports, `manual-steps.md` (already lazy), `friction-log.md` (already lazy), `retrospective.html`, `<sprint>/decisions-log.md`, `stubs.md`. A disabled document is never written as an empty stub — skip recorded in `state.json` plus one decisions-log line.

**Acceptance-criteria source**: PRD AC-N when `documents.prd` enabled; else `sprint.md`'s own `AC-N` list (`t_sprint.md`); `lite`: "Workflows". Every phase citing AC-N (plan, impl, impl-test, impl-review, pr) uses the source this rule selects.

**Independent design docs** (replaces the old hard PRD→UX→ADR chain):
- PRD (`prd`) reads `sprint.md` + `audit.md` (if `audit` enabled).
- UX-spec (`ux_spec`) reads PRD if enabled, else `sprint.md`; audit optional. Disabling `ux_spec` also disables the design-system gate, `design-md-delta.yaml`, and UX promotion.
- ADR (`adr`) reads whichever of PRD/UX-spec exist, else `sprint.md`; audit optional. ADRs are sprint-scoped only (`<sprint>/design/adr.html`, sprint-local `ADR-1`, `ADR-2`, … numbering) and are never promoted as a standalone persistent document — see "Design-promote phase" fold rule.
- C4 (effective `c4`) reads whichever design drafts exist, current stack, the subsystem registry, `sprint.md`; ADR not required.
- Audit disabled → creators scan the repo themselves for context; the plan workflow greps touched files and reads `.asd/project/stubs.md` directly instead of `audit.md`'s "Related open stubs" section.

**No-op phase rule**: a phase whose entire applicable-artifact set is empty skips dispatch, records its skip inline and returns `COMPLETED`. It has no artifact gate.

| Phase | No-op when |
|---|---|
| audit | `audit` disabled |
| design | `prd`, `ux_spec`, `adr`, effective `c4` all disabled |
| design-review | design phase produced zero drafts |
| design-promote | zero approved drafts to promote (`lite`: "Workflows") |

`plan`, `impl`, `impl-test`, `impl-review`, `retro`, `pr` are never no-op.

**Design/design-review/design-promote collapse**: `standard` only ("Workflows"). One deterministic no-op write at the audit exit — design is never dispatched either; the design workflow repeats it only as a defensive fallback for a direct re-dispatch. Design-review and design-promote are not dispatched. Collapse test, on frozen state and never on the historical `skipped_phases`: `documents.prd`/`ux_spec`/`adr`/`c4` are all `false`. A legacy `state.json.skip_design_phases` is ignored: the scope that froze it `true` froze those documents `false`. Under it no design draft is produced or promoted, so `phase="design-promote"` is the collapse write: resume dispatches `plan`, and `plan` needs no promotion.

## Audit phase

No-op when frozen `documents.audit` is `false` (see "Optional documents").

An absent optional section in `audit.md` (`t_audit.md`) means an empty finding set for that check — the check ran and found nothing — never that the check was skipped. BA/Architect omit an optional section entirely when it has no findings; they never emit a mandated placeholder row to signal "none". A check that could not run at all is a `FAILED`/`ABORT` from the responsible agent, not a silently-omitted section.

Scans: existing source in touched areas; existing docs in **any format/location** (MD, RST, Confluence/Notion exports, HTML, Wiki, text-extractable PDF, READMEs outside ASD layout); **every** persistent doc in `docs/` bearing on a touched area, never a sample, each listed in `audit.md` "Existing docs found".

**Contradictions**: a canonical ASD document is a persistent doc at its `artifact-layout.md` path-map location, and under `self_hosting` also a `.asd/rules/` doc. Canonical beats non-canonical, no gate. Canonical vs canonical, or no canonical side, is a hard user decision before the audit gate (`checkpoints.md` "Gate policy"). `audit.md` "Contradictions" records each with both sources and the winner or the user's answer.

**Criterion check**: Architect checks every `AC-N` for deliverability as stated — the host, runtime and contract facts it relies on hold — and for consistency with every other criterion. An undeliverable or conflicting criterion is a "Contradictions" entry with no canonical side: a hard user decision.

Output `audit.md` — findings (touched areas, existing docs/code, gaps, risks) plus **Documentation migration plan** listing found external docs to promote into ASD format. Where sprint scope directly overlaps found content, the agent may pre-formulate reverse-engineered/migrated drafts in `<sprint>/design/` (prd.html / adr.html) — **only for documents whose frozen `documents.*` flag is enabled**; a disabled document is never draft-created here either, its finding stays migration-plan text — with `provenance` + `source` frontmatter; these flow through design and design-review like any draft. Migration items not covered by drafts wait for design-promote.

**Subsystem registry** (decomposition enabled; `artifact-layout.md` "Subsystem registry"): Architect reads `docs/architecture/subsystems.md` and each touched subsystem's `<id>.md` to locate the code in scope. When the registry is absent, Architect proposes it — per subsystem: id, purpose, key paths — from an existing C4 registry under `docs/architecture/c4/` (likec4 model or mermaid `subsystems.yaml`), else from code. Every proposed subsystem needs explicit user confirmation (`checkpoints.md` "Gate policy" hard list); only then does Architect write the registry and the `<id>.md` of each confirmed one, before the audit gate. Migrating from a mermaid `subsystems.yaml` with `project.diagram_tool: mermaid`, that write also carries its diagram — the confirmed subsystems and their relations — into the registry's `## Diagram` (`t_subsystems.md`). A registered subsystem without its `<id>.md` gets it backfilled, no gate (nothing is added). Only after that migration lands, a legacy `docs/architecture/c4/` the rules make redundant — `project.diagram_tool` other than `likec4` — is deleted only on user approval (same hard list), with its `.gitignore` entries and `commands.yaml` `c4-build`; declined keeps it.


## Design phase

No-op when `prd`, `ux_spec`, `adr`, and effective `c4` are all disabled (see "Optional documents").

Agents produce a draft set for the whole sprint scope in `<sprint>/design/`, one artifact per enabled document only — a disabled document produces no draft, no gate, no dependency on it:

- `prd.html` — requirements + acceptance criteria (`documents.prd`)
- `ux-spec.html` — flows + accessibility notes (`documents.ux_spec`)
- `adr.html` — architecture decisions (`documents.adr`)
- `design-md-delta.yaml` — proposed DESIGN.md token changes, produced inline during UX-spec authoring (only on token gap; each entry user-approved)
- `c4-full/` — delta patch against the persistent diagram for sprint scope — likec4 `model/*.c4`, `views.c4` against `docs/architecture/c4/`; mermaid `subsystems.md` against the registry's diagram block; full schema only when that diagram does not yet exist (frozen `documents.c4`). Never build `dist/` here — generated output no reviewer sees (`external-review.md`).

Order among enabled documents: PRD (if enabled) before design-system gate. Design-system gate (existence check on `docs/ux/DESIGN.md`, `design-system.html`, `accessibility.html`; dispatches `/asd-design-system` when any missing) applies only when `ux_spec` enabled, and blocks UX-spec. UX-spec (if enabled) before ADR. ADR (if enabled) before c4-full. If frozen `documents.c4` is `false` (`diagram_tool: none`, or `subsystem_decomposition: disabled`), `c4-full/` omitted.


## Design-promote phase

Promotion from drafts (`standard`); from the accepted implementation: "Workflows". No-op when the design phase produced zero drafts (see "Optional documents"). Otherwise each domain creator promotes only the draft(s) that exist for its domain.

The main orchestrator handles gates; three domain creators promote (Documentation reviewer NOT involved):

1. The main orchestrator applies the adaptive gate policy to decomposition.
2. A new subsystem remains hard; after approval Architect writes it to `docs/architecture/subsystems.md` — the sole subsystem registry (`artifact-layout.md` "Subsystem registry") — and to its `docs/architecture/<id>.md`, and creates its folders. A changed subsystem gets the same two writes.
3. The main orchestrator distributes audit migration items to the matching domain.
4. Parallel promotion:
   - `asd-ba` → per-subsystem (or flat) `docs/product/requirements/<subsystem>.html` from prd draft; product migration items.
   - `asd-architect` → folds every ADR approved in `adr.html` into whichever existing persistent doc's `responsibility.owns` frontmatter already declares ownership of that decision's subject (see fold rule below); updates `stack.html`, `tech-reference/`; only when frozen `documents.c4` is `true`, applies the sprint's c4 delta patch (or, when the persistent diagram did not exist before this sprint, writes the full schema directly) — likec4 to `docs/architecture/c4/`, mermaid to the diagram block in `subsystems.md`; architecture migration items. Rendering (`dist/`) is not regenerated here — build on demand via the `commands.yaml` build-to-view command.
   - `asd-ux` → `docs/ux/<subsystem>.html` from ux-spec draft; patches `DESIGN.md` from `design-md-delta.yaml`; regenerates `design-system.html`; ux migration items.
5. The dispatching workflow composes promotion records and writes state inline.

Dropping the per-persistent-write and final-mutation gates (former steps 4's trailing sentence and step 5) also drops the **partial rollback** affordance they used to offer (confirm / rollback / partial rollback on the whole batch) — no direct replacement exists at this gate level. The compensating control is a non-blocking post-promotion summary the dispatching workflow posts after all writes land (implemented in `asd-phase-design-promote.md`, not this rule doc).

**ADR fold rule**: every architectural decision approved in a sprint's `adr.html` is folded, at `design-promote`, into whichever existing persistent doc already declares ownership of that decision's subject in its `responsibility.owns` frontmatter — never from a lookup table. The `adr.html` article's optional "Fold target" line names the candidate and the matched `owns:` clause; the Architect verifies the match, not invents it. A binding rejected alternative folds as one line into the target doc's Constraints-equivalent section (or the fold target's nearest analogous section); a non-binding rejected alternative stays sprint-archive-only, never promoted. When no existing doc's `owns` matches, that is a Complication Approval, not a licence to invent a document — API contracts fold the same way: into a subsystem requirements/architecture doc, `stack.html`, a project-generated OpenAPI/SDL/proto artifact, or, only via Complication Approval, a brand-new doc with no pre-made template. The design gate stays **one approval for the sprint's whole ADR set** — fold-target selection happens after that gate, during promotion, and never re-opens it.

If `subsystem_decomposition: disabled`: drafts merge into flat project-level docs (`requirements.html`, `ux-spec.html`); ADRs still fold per the rule above, never into a flat `adr/` tree. No subsystem folders, no subsystem registry, no c4 model.

## Impl phase

Devs implement plan tasks. A human-only operational action is registered as `MS-N`; the main orchestrator validates necessity and presents validated pending entries. On resume the dev verifies and completes them.

After a wave's last signal the orchestrator ticks its `plan.md` checkboxes, appends the `MEMORY.md` index lines its Task agents returned and commits; devs and testers never edit `plan.md`, and in a wave of more than one Task leave a shared `MEMORY.md` untouched, returning their index line in `COMPLETED`.

Devs write **production code only** — no tests, no test runs, except self-verification: a dev may run the impacted set (`Impacted test set` above) to self-check work in progress, but never authors, modifies, or prunes a test, and this run never substitutes for or satisfies the `impl-test`/`impl-review` gates. All test work belongs to `impl-test`, except a `Test-only` Task, which `asd-tester` runs in this phase ("Plan file format").

**Modes** — detected from `state.json`:

- **Initial** (`review_fixes_pending` and `test_defects_pending` both null) — implement `plan.md` tasks. Ends with the user-facing impl assessment gate.
- **Review-fix** (`review_fixes_pending` = `wave-<K>/iter-NN`) — entered when impl-review routed back. Devs read findings in `<sprint>/reviews/impl/wave-<K>/iter-NN/`, resolve every CONCERNS finding plus every user-approved FAIL finding. Clears `review_fixes_pending` on completion.
- **Test-fix** (`test_defects_pending` = `true`) — entered when impl-test found code defects. Devs resolve every open defect in the `Defects` section of `<sprint>/test-plan.md`, marking each `fixed` with the fixing commit. Clears `test_defects_pending` on completion.

Only one fix flag is ever set: each fix mode clears its own before routing on. Fix modes skip the impl assessment gate; blockers escalate as in initial mode. All modes return `NEXT: impl-test`.

**Completion gate** (all modes) — impl MUST NOT emit `COMPLETED` until, verified via `commands.yaml`: `build` and `lint` ran with no errors and no warnings. The gate itself never runs tests — the optional self-verification run above is not part of it. On failure: devs fix and re-run; unrecoverable failure escalates as `FAILED`. Automatic verification, not a user pause.

## Impl-test phase

Owner: Tester — a fresh one per entry and per terminal full-suite run (`Impacted test set` above), never resumed across entries; `test-plan.md` is the only hand-off. Its `Entry log` and segment rotation are impl-test's alone (a review-fix tester's rows: `artifact-layout.md` "Test plan"). Runs after every `impl` exit. Selects the test approach **after** the implementation exists, so tests follow the real change surface instead of a speculative one. Before selecting anything new, it runs the existing impacted tests (`Impacted test set` above) so the strategy pass observes actual post-impl behaviour and catches an `impl` regression before any new test is authored. A `Test-only` Task's commits ("Plan file format") are existing tests in entry 1's change surface: its pre-strategy run covers them and entry 1 does not re-author them; strategy, pruning and the suite gate stay impl-test's.

**Principles**: check-ladder selection, prune criteria, no-new-test decision rule, and fail-first regression proof are all defined once in `code-style.md` §17 — binding here, not restated here.

**Workflow**: change-surface analysis → pre-strategy impacted run (existing tests) → `test-plan.md` (risk → chosen check → decision) → prune + author → impacted-set suite run.

**Manual verification**: each `test-plan.md` `Manual verification` row gets its user smoke check once, at the first impl-test entry whose suite gate is green after the row is written (`asd-phase-impl-test.md`); its result is recorded in `test-plan.md`, where impl-review reads it.

**Re-entry** (every `impl` exit after the first re-enters this phase): the strategy and prune passes scope to the **delta since the prior entry** (the review-fix/test-fix commits, via `test-plan.md`'s `Entry log`), not the whole change surface again — `test-plan.md` is amended, not rewritten, after the prior entry's narrative rows rotate (`artifact-layout.md` "Test plan"). The **suite gate re-runs on every entry**, scoped per `Impacted test set` above (never the whole repo, subject to its safety valve). Bounded risk: a defect introduced by a fix outside the impacted set's reach is not caught here — the end-of-`impl-review` full suite (`Impacted test set` above) is the backstop.

**Removal gate** — deleting a test outside scope uses `checkpoints.md`: explicit approval in strict, or adaptive evidence when authority and checks cover it. In-scope removals record a reason.

**Suite gate** — verdict comes from the actual `test` runner output (exit code plus report), never from an agent's claim, scoped to the impacted set (`Impacted test set` above) — the full suite runs only once, at the end of `impl-review`. Failures triaged:

- **test defect** (bad assertion, wrong fixture, flaky pattern) → fixed inside impl-test, suite re-run.
- **code defect** → appended to the `Defects` section of `test-plan.md`, `state.json.test_defects_pending = true`, `NEXT: impl` (test-fix mode).

Loops until the impacted set passes. No iteration cap — an unfixable state surfaces as a dev/tester `FAILED`, not as a silent exit.

**Stalemate** — when two consecutive impl-test entries routed the same code-defect identity set (a green entry between them breaks the run), the phase escalates `FAILED: stalemate` instead of routing. Identity is the `Defects` file path, runner failure line and failing test, never `D-N`; `impl-review` rows never count. `node .asd/runtime.js defect-stalemate` compares deterministically. A user answer, logged with the set's digest, covers only the next routing of that set; the same set repeating after it escalates again. Mechanics: `asd-phase-impl-test.md` step 9.

## Friction log

`<sprint>/friction-log.md` per `t_friction-log.md`. Sprint-scoped, created lazily on the first entry, append-only, archived with the sprint. Never promoted; no cross-sprint history. Entry id `F-N`, sequential, never reused; every entry names the phase it arose in.

**Records** workflow malfunction only: an ambiguous, contradictory or unfollowable rule; a phase, gate or routing step that misfired; an agent or skill that behaved wrong; a template or artefact shape that could not be conformed to; a provider CLI or host tool that failed. One entry per distinct problem.

**Never records** what another file owns — the entry cites that owner's id and stops:

| Owner | Log may record | Log never records |
|---|---|---|
| `test-plan.md` `D-N` | that finding or fixing the defect was obstructed | the symptom or the fix |
| `reviews/<phase>/iter-NN/` | that the review process itself misbehaved | the finding or the verdict |
| `manual-steps.md` `MS-N` | that the step was unexpected or unworkable | the steps or their verification |
| `decisions-log.md` | that deciding was blocked | the decision |

One problem that is both a code defect and a workflow malfunction (routine under `self_hosting`, where workflow source IS the code) gets a `D-N` row for the defect and an `F-N` entry for the malfunction, cross-referenced by id — never the same content twice.

**Writer mechanism** — sole statement of the writer mechanism, referenced by every phase workflow: the main orchestrator running the phase workflow appends every entry itself, from what it observes — including what a dispatched agent's return text, signal or failure reveals. No agent writes the file and none is asked to self-report friction; reviewers write no sprint artefact at all (`review-policy.md`). This is the single channel for workflow friction; `state.json` holds no parallel escalation list.

## Retro phase

Runs between `impl-review` (`lite`: `design-promote`) and `pr`. Unconditional (never no-op). Owner: main orchestrator (`asd-phase-retro.md`).

Input `<sprint>/friction-log.md`; output `<sprint>/retrospective.html` per `t_retrospective.html` — derived analysis, sprint-scoped, archived with the sprint. Nothing is promoted to a persistent doc; the retro backlog holds only scope-time dispositions of its rows ("Retro intake" above), never retro content.

**Retro row id** — `A-N` names the Nth row of the Actions table, `P-N` the Nth of the Systemic proposals table; N is the 1-based `<tbody>` ordinal, `covered by:` rows counted. Written as `<tr id="A-N">`; a legacy retro without ids gets the same ids from the same ordinal. Cross-sprint address: `<NNN-slug>#A-N`. Row ids, `Acts on` values and the `covered by:` prefix are English literals under any `language.docs`.

**Two output classes.** Both are split into consumer-project and ASD-framework actions so every row names the side that acts and its home; both are proposals the phase never executes and never promotes — promoting a guardrail is the user's decision.

1. **Remediation** — answers *what went wrong*. Every `F-N` entry analysed to a root cause; each finding traced to every entry id it addresses. Bounded by the log.
2. **Systemic proposals** — answers *what would have made this sprint cheaper*, never *what went wrong*. Evidence is how the sprint actually ran (review iterations, rework loops, gate waits, task churn, dispatch cost), not the entry set: a proposal may cite an `F-N` as supporting evidence, but is neither derived from nor limited by the log. A fact a friction entry already owns is remediation only — rewording it as a proposal is the double-channel duplication this split exists to prevent.

**Findings, in order** (both classes; dedup before any drafting):

1. **Merge** — entries and proposals sharing one root cause become one finding citing every source `F-N`; a finding merged with any `F-N` is remediation.
2. **Coverage** — search the finding's candidate home (list below) for its subject, reading no other file; a finding an existing rule already covers is dropped, its row citing that rule in place of a guardrail.
3. **Draft** — each surviving finding gets `Guardrail`, one imperative line stating the prohibition or requirement, and `Home`, finalizing the candidate (a changed home re-runs step 2): `custom-coding-rules.md`, `custom-design-rules.md`, `custom-common-rules.md`, `.claude/agent-memory/<agent>/` (Claude-only), a `.asd/rules/` doc when `self_hosting` is enabled, or "upstream ASD" (a framework proposal, never applied; shown at the next scope without a disposition, "Retro intake") when it is not.

**Empty-log branch**: an absent or entry-free log is a legitimate outcome — record "no friction recorded" and skip class 1; class 2 is still produced, so an entry-free log is never an empty retrospective. Never invent friction entries; never mutate sprint state to reach this branch.

Closes with a short `language.chat` summary covering both classes, then `NEXT: pr`. Adds no gate of its own; only `checkpoints.md`'s existing gates apply.

## PR phase

Modes are `pr=null` (open the PR, `NEXT: await-merge`, unless the "Merged-unclosed" lookup finds one) and `pr.state="open"` (merge it). A dispatch carrying a confirmed `MERGED` number, `asd-sprint`'s release retry, enters merge mode whatever `pr` holds, skipping open mode's DoD gate. DoD checks gate publication but are not user approvals. The main orchestrator performs the merge itself where the Git host allows it — who merges is stated once in `git-strategy.md` "Merging a PR". Merge mode confirms the sprint PR merged, publishes the self-hosting release (`git-strategy.md` "Versioning & Changelog (self-hosting only)"), and returns `NEXT: done`. It writes no state, archive move or commit on any branch; a tag ref and a host release are not branch writes. `NEXT: done` names completion, not a write: the sprint stays at its active path with `phase="pr"`, `pr.state="open"` on base until the next sprint's closure write. One PR per sprint.

**Merged-unclosed**: a sprint folder at the active path whose sprint PR `gh` reports `MERGED`, whatever its `phase`. With `pr.number` set, `gh pr view <n> --json state,mergeCommit` decides; a PR `pr` open mode opened carries `phase="pr"` and `pr.number` to base, because open mode commits the phase write before any push and commits and pushes its `state.json.pr` write before `NEXT: await-merge`. Every sprint without `pr.number`, at any `phase`, is looked up by head branch — a PR merged before that write reached the sprint branch (after a failed push), one opened and merged by hand before the `pr` phase, or an open-mode `MERGED` hit, which continues in merge mode with that number and writes no state: `gh pr list --head <state.branch> --state all --json number,state,mergeCommit`. A `CLOSED` hit counts as no hit, and across several hits `MERGED` wins, then `OPEN`. A `MERGED` hit is merged-unclosed with that number, which the closure write records; an `OPEN` hit resumes merge mode with that number only at `phase="pr"`, and at any other phase is ignored, the sprint resuming normally; no hit resumes normally (open mode at `phase="pr"`). A lookup that cannot reach `gh` outside `phase="pr"` warns that a merged PR stays undetected until an online run, and resumes normally; at `phase="pr"`, and for `gh pr view <pr.number>`, it is `FAILED` naming "host unreachable, retry online" (`git-strategy.md` "PR creation"). `asd-sprint` detects the state on `NEXT: done` and on any later invocation and routes it with no closure gate. Release retry: under `self_hosting`, while the release is missing — no release commit (`git-strategy.md` "Versioning & Changelog (self-hosting only)"), the tag `v<asd_version>` read at it absent on `origin` (`git ls-remote --tags origin`), or `gh release view v<asd_version>` failing, so a merge-mode release ended `FAILED` or never ran — `asd-sprint` requests a user decision, each time: retry, back to merge mode, which, the PR already merged, runs only the release; or continue without it, which the entry the closure write appends to the closing sprint's decisions log records (`decision_actor: user`). Otherwise, and on continue, it enters the new-sprint flow, carrying the sprint's path and PR number; the one-active-sprint rule exempts that sprint within it. The session-start hook reports the state offline (`phase="pr"`, `pr.number` set, `state.branch` ≠ the current branch), display only, so it misses a sprint without `pr.number`; `asd-sprint` confirms through `gh`.

**Closure write** — `asd-phase-scope.md` step 1, for the merged-unclosed sprint `asd-sprint` passes, right after the new sprint branch is created and before the new sprint is seeded; no user gate:
1. `git mv` the closing sprint folder to `archived/`;
2. write its `pr.state="merged"`, `pr.merge_commit` (the `mergeCommit` above), `pr.number` when `pr` is null (the one `asd-sprint` carries), `phase="done"`, `updated_at`, `archived_at`, and a `sprint closure` `gate_decisions` entry with `decision_actor: orchestrator`, its evidence the merge commit;
3. append one entry to its decisions log;
4. commit, as the new branch's first commit.

A scope aborted before its branch exists writes nothing; the next `asd-sprint` detects the sprint again. The closure reaches `git.base_branch` with the next sprint's PR.

**Legacy shapes**, each resolved by the flow above:
- `pr.state="closure-pending"` (local state of the retired companion flow; the base copy says `open`) — a merged-unclosed sprint.
- an archived non-done sprint — its terminal write lands in place, inside the closure write, with no `git mv` ("Sprint immutability").
- an open `chore/finalize-sprint-<NNN-slug>` PR (`gh pr list --head`) — merged at detection; the closure write is skipped for that sprint.

**Open mode's DoD verification is conditional on two checks, neither a `checkpoints.md` gate** (`asd-phase-pr.md` open mode step 1 — internal verification only, gates PR opening, never a user-facing pause):
- **Tests/lint re-run**: content-scoped, not HEAD-sha-equality (HEAD always moves past the recorded sha — the recording commit itself, plus later phase-transition commits, guarantee it). Skipped when `git diff --quiet <recorded HEAD>...HEAD -- <code/test/stub pathspec, excluding .asd/sprints/** and .asd/project/**>` is empty, where `<recorded HEAD>` is the sha in test-plan.md's `Suite run` section — the commit impl-review's terminal full-suite step (`Impacted test set` above) last verified the full suite at, which is also the last point any code/test/stub file can change before `pr`. The check is sha-independent, not read-only-dependent: whatever landed since that recording — a review-fix commit, or the rare in-phase test-defect fix — shows up as a non-empty diff and forces a re-run; an empty diff means nothing changed, full stop.
- **Reviews-green source**: `state.json.reviews.impl.wave` equals the wave count (`waves.length`), and every wave node's `verdicts["iter-NN"]` for its highest iteration is satisfied, read from state first; parse review files under `<sprint>/reviews/impl/wave-<K>/iter-NN/` only as an explicit fallback when `state.json` data is missing or stale, or for a user-resolved entry. Satisfied-vs-blocking semantics for each entry: "State recovery" below.

## Signal vocabulary

- `COMPLETED` — phase work done, ready for next
- `FAILED` — cannot proceed, reason in body
- `REVIEW_DONE` — reviewer finished, verdict in body
- `QUESTION` — needs user input, body has the question and options; emitter: any dispatched agent except a reviewer, whose question rides its verdict report (`review-policy.md` "Gate Verdict Format"). Handled per the `QUESTION` protocol below.
- `PLAN_DRAFT` — plan written, not approved
- `PLAN_READY` — plan approved
- `BLOCKED_MANUAL` — task needs a human-performed manual action; entry registered in `manual-steps.md`
- `ADVICE_NEEDED` — emitter: any dispatched agent other than `asd-advisor`, on non-gate uncertainty only (analysis/judgment question, never a HARD-gate approval decision — see `core.md`'s autonomy/escalation rule for the gate-vs-non-gate distinction). Payload: the question plus relevant context paths. Relay obligation: the dispatching phase workflow catches the signal, dispatches `asd-advisor`, then re-dispatches the consulting agent with its answer appended — the per-workflow relay branch is implemented in each `asd-phase-*.md`.

**`ADVICE_NEEDED` protocol** (every `asd-phase-*.md`'s relay branch is this exact sequence, invoked as "relay per `sprint-lifecycle.md`'s `ADVICE_NEEDED` protocol"):
1. Dispatching phase workflow catches `ADVICE_NEEDED` from a dispatched agent other than `asd-advisor`, mid-task.
2. Dispatches `asd-advisor` with the question plus the context paths as given by the consulting agent — no other context injected.
3. On the advisor's returned recommendation → re-dispatch the consulting agent (`delegate to agent X`, `providers.md`) with its original task context plus the advisor's answer appended; no other context injected except the running consult count/remaining budget (step 6). Not a same-turn resume (no host tool suspends and resumes a dispatched agent mid-execution — `providers.md` has no such operation); it is a fresh dispatch carrying forward the same task.
4. On `asd-advisor` `FAILED` (question turned out to be a HARD gate) → relay that finding to the consulting agent unchanged; the consulting agent then treats it as gate uncertainty per `core.md`'s Autonomy and escalation rule and returns `QUESTION` — a reviewer, its question carrier per `review-policy.md` "Gate Verdict Format".
5. No halt, no user contact, no logged trail — the round-trip is autonomous and intra-phase (`asd-advisor.md` Don'ts: consults are not logged).
6. Capped at 3 consults per consulting-agent **task** — the dispatching phase workflow owns this counter (the consulting agent does not; each re-dispatch in step 3 is a fresh dispatch and would otherwise reset a self-held count), increments it once per completed advisor round-trip, and does not reset it across the task's re-dispatches. At the cap, the workflow stops relaying further `ADVICE_NEEDED` signals for that task; the agent proceeds on its own judgment or re-classifies the question as gate uncertainty and returns `QUESTION` per `core.md`'s Autonomy and escalation rule — a reviewer, its question carrier (step 4).

**`QUESTION` protocol** (cited as "per `sprint-lifecycle.md`'s `QUESTION` protocol"):
1. The dispatching workflow catches `QUESTION` — the question plus options (`core.md` "User-decision presentation format") — from a dispatched agent.
2. The main orchestrator asks the user via request user decision (free-form input as a plain chat message) and records the answer in `decisions-log.md` before any further work (`core.md` "Context hygiene").
3. Re-dispatches the agent fresh with its original task context plus the answer appended — not a resume, for the reason in `ADVICE_NEEDED` step 3.

## Plan file format

See `t_plan.md` for canonical structure.

**Material risk declaration** (one required line per `### Task N:` block, never a checkbox — a checkbox outside a subtask breaks task parsing): `Material risk: none`, or one `Material risk: change: <short risk class>` / `Material risk: artifact: <short risk class>` line per declared risk. The two kinds are distinct and are not interchangeable:
- **change** — the edit's own correctness is uncertain: unfamiliar domain, ambiguous judgment, a contract whose right wording is not yet known, security/authentication/migration/public-contract/workflow-gate work. Routes `critical`. Reserved-class rule: `providers.md` "Task-class variants and routing".
- **artifact** — the edit is small and objectively verifiable, but lands in a high-stakes file. Routes on the task's own evidence, so a mechanical edit to a critical artifact is no longer critical by that fact alone.

The main orchestrator passes these lines as `route-task`'s `risks` input, one entry per line, typed by its kind (`providers.md` "Task-class variants and routing", which owns the routing semantics). When in doubt between the two kinds, declare `change`. A Task block with no conforming line — absent, or off-grammar (prose, or inside a subtask checkbox) — reads `change: unclassified` and routes `critical` until the plan is updated; missing is never `none`.

**Reachability declaration** (conditional, at most one per `### Task N:` block, its own plain-text line under the `Material risk` line(s) and any `Test-only` line, never a checkbox): a task whose value depends on two phases agreeing carries `Reachability: <phase> writes <value> at <point>; <phase> reads it at <point>`. A value crossing a push or merge also names, per interruption point between write and read, the value the receiving branch then holds: `; interrupted at <point>, <branch> holds <value>`. Accepting the plan means checking that those two points observe the same value, and that the reader handles each named interrupted value — a purpose unreachable by construction is rewritten or dropped at plan approval, never planned and then closed finding by finding.

Its absence semantics are deliberately not `Material risk`'s, and the two are never conflated: an absent `Reachability` line asserts the task has no cross-phase dependency — never `unclassified`, never `critical`, and never input to `route-task`, which reads `Material risk` lines only. An off-grammar `Reachability` line is a plan defect fixed before approval, not a fail-closed default.

**Settings change declaration** (conditional, at most one per `### Task N:` block, plain-text line under the `Material risk`/`Test-only`/`Reachability` lines, never a checkbox): `Settings change: <key>=<value>[, <key>=<value>…]`, `<key>` a dotted `.asd/project/config.yaml` path `t_config.yaml` carries — already, or once an earlier-wave Task of this sprint adds it. Plan acceptance is the approval of record for exactly those pairs: `impl` applies them through `asd-init` sprint-mediated mode as the carrying Task's wave opens, validated against the working-tree `t_config.yaml` then (`asd-phase-impl.md` step 6), never as an `MS-N`, and no other setting. A Task carrying the line is alone in its wave: wave 1 when `t_config.yaml` already carries every key, a Task changing the dispatch or commit contract (below) then following alone in wave 2; otherwise a wave after every Task adding one of its keys, the contract rule below then applying unchanged. The change never rewrites the running sprint's frozen `state.json` snapshot ("Optional documents" above): a frozen setting binds from the next sprint, a live-read one from its next read.

**Test-only declaration** (conditional, at most one per `### Task N:` block, its own plain-text line directly under the `Material risk` line(s), ahead of any `Reachability` line, never a checkbox): `Test-only: <test paths or globs>`. `impl` dispatches the Task to `asd-tester` (`asd-tester-<tier>` through `route-task`), never a dev. It touches only test files, never `test-plan.md`; it adapts existing tests to the code change and adds a test only where `code-style.md` §17 qualifies one; its commits carry `ASD-Task: Task N`.

**Wave declaration** (plan-level, one table in the required `## Dependencies` section, never a per-Task line): rows in ascending wave order, first column the wave number, second the ids of the Tasks dispatched in that wave. Every Task appears in exactly one wave. A Task changing the dispatch or commit contract other Tasks are dispatched under is ordered ahead of them and is alone in its wave. A `Test-only` Task sits in a wave after every Task whose code it covers. `impl` schedules from this table alone (`asd-phase-impl.md` steps 5-6); the dependency lines under it explain the grouping and never override it. A missing table, or a Task in none or in two waves, is a plan defect fixed before approval — except in a plan authored before this rule, which falls back to a topological sort over its dependency lines.

**Decomposition rules** (checked before plan acceptance; acting site `asd-phase-plan.md` step 4):
- A plan changing a rule other files restate lists every restating site a repo grep finds, each assigned to exactly one Task.
- Tasks in one wave that cite each other's new rule homes name each home by exact file and section heading, listed once in `## Overview`.
- Every edit several ACs make to one rule doc sits in one Task.
- A Task holds only work its dispatched agent may perform. An orchestrator-only action is its own plain line outside every `### Task N:` block, never a checkbox, naming its execution point.
- No two Tasks in one wave touch overlapping paths, a `Test-only` Task's paths and globs included.
- A subtask adds no helper, guard or export unless it names a caller or a reachable failure (`code-style.md` §1, §3).
- The plan lists the external APIs each Task needs and extends the tech reference (`artifact-layout.md` "Tech reference docs") where one is missing, before acceptance.
- A stub offered for inclusion states its verified cost and behaviour change. A stub whose files are tests only routes to `impl-test`, never to a Task.

**Standing Definition of Done** (constant across every sprint, never restated in `plan.md`): all AC-N from the acceptance-criteria source covered by Tasks; impacted test set green at `impl-test` (`Impacted test set` above); full test suite green once, at the end of `impl-review`; all required reviewers green in every `impl-review` wave. `plan.md`'s own Definition of Done section holds only sprint-specific additions to this standing set, referencing it rather than repeating it.

## Scope amendment

A criterion added, changed or retired after scope acceptance needs the hard `new or changed scope` approval (`checkpoints.md` "Gate policy") and then, in one orchestrator write:
1. `sprint.md`: the `AC-N` — a new id, never a reused one;
2. `plan.md`: the Task carrying it, new or amended, in a wave not yet dispatched (a new last wave when none is left), per "Plan file format";
3. the gate records: a `gate_decisions` entry and one decisions-log entry naming the AC, the Task and its wave. Accepted after impl-review's division point ("Review iteration counters"), the entry's evidence adds `floor_base=wave-<K>/<A>`, A being current review wave K's counter then, and the write clears wave K's `latched` ("APPROVE latch"). impl-review computes wave K's severity floor and iteration cap on `iteration − A`, A from the latest such record accepted since the current `waves.json` division (none → 0), so a rollback reset, which re-divides, retires every earlier one, and the amended code's first iteration `wave-<K>/iter-(A+1)` reviews at a first iteration's floor.

The audit boolean is then reevaluated ("Orchestration and adaptive gates").

## Sprint immutability

A sprint folder under `.asd/sprints/archived/<NNN-slug>/` is read-only. The archive move and the terminal write happen together, in the closure write ("PR phase") — the one write that lands in an archived folder, in place for a legacy sprint archived before closure. Once `phase=done`, truly immutable — follow-up work creates a new sprint.

## State recovery

`state.json` is the single recovery point. The dispatching main orchestrator is its sole writer for transitions, verdicts and gate evidence; no delegated agent writes it. A merged sprint keeps its recorded `phase` and `pr` (after merge mode: `phase="pr"`, `pr.state="open"`) until the closure write ("PR phase"), the only writer of the terminal state and archive move; no merged-but-unclosed record exists, the state is detected ("PR phase" "Merged-unclosed"), and so is a missing self-hosting release (its release retry). A legacy `closure-pending` or archived non-done sprint stays recoverable through the same flow.

**Failed dispatch**: a creator (`asd-ba`, `asd-ux`, `asd-architect`, `asd-dev`) or `asd-tester` dispatch that ends without a completion signal — a stall stop ("Agent liveness") included — is never read as done; its task is re-dispatched fresh. An `impl` (initial, review-fix, test-fix) or `impl-test` dispatch is reconstructed before that re-dispatch. Anchor: the `dispatch HEAD <sha>` of the latest decisions-log routing line naming that dispatch's ids. Run `git status --porcelain` and `git log --format='%h %s%n%(trailers:key=ASD-Task,valueonly)' <anchor>..HEAD` (trailer-less commits contribute nothing). Ids named by an `ASD-Task` trailer (`git-strategy.md` "Commits") landed and are dropped from the re-dispatch — except `impl-test entry N`, which never marks the entry landed: the re-dispatched tester resumes the interrupted current entry from on-disk evidence (`asd-phase-impl-test.md` step 1), re-running each step lacking its output (e.g. no `Suite run` for the entry); and `D-N`, which lands only once its `test-plan.md` `Defects` row reads `fixed` with its fix commit — otherwise the re-dispatch carries that `D-N` only to set the row from the trailer commit's sha, no second fix. The payload names each uncommitted leftover path the failed dispatch was authorised to touch — never orchestrator-owned bookkeeping or an in-flight sibling dispatch's paths — for the agent to finish or revert; the orchestrator never stages, commits or discards them. Append one decisions-log line `- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>`. One cause (session-wide limit, host outage) ending every dispatch then in flight at the same moment is one event, never one per dispatch: one reconstruction covers them all and no dispatch's stall count rises ("Agent liveness"); anything narrower stays per-dispatch — the creator/tester form of `review-policy.md` "Correlated interruption".

`iteration_heads["iter-NN"]` of impl-review wave K's node (`t_state.json` schema) holds the `git rev-parse HEAD` sha recorded when that wave's iteration NN starts (the same `asd-phase-impl-review.md` step 2 write that increments its counter); it anchors the incremental ranges of "Review iteration counters" above. An absent or empty `iter-(NN-1)` key (a sprint in flight when this field shipped) falls back to `<base_branch>` — never an empty left operand — with the widened scope noted in that iteration's decisions-log entry.

Each impl-review wave node's `verdicts["iter-NN"]` (`t_state.json` schema) holds one entry per reviewer key of the frozen workflow's `reviewers.impl` ("Workflows") — every one of these reviewers is always dispatched (`review-policy.md` "DoD per review phase") UNLESS the APPROVE latch above skips it, and per that section's invariant a latch-skipped reviewer still gets its inherited `APPROVE` written here. Each value is one of five distinct things, never conflated:
- a bare verdict token string (`"APPROVE"`/`"CONCERNS"`/`"FAIL"`, parsed from that reviewer's written review file, covering whatever rubric sections it reviewed that dispatch — a section the reviewer itself marked `n/a: <predicate>` in its own returned section-coverage ledger (`review-policy.md` "Diff-scoped impl-review fan-out") is bookkeeping internal to that reviewer's file, never a separate state value);
- External Review's availability-skip verdict, `"APPROVE (skipped: <reason>)"` — never the bare token — written when the phase-supplied preflight returns a non-ready status (unavailable command/auth, or active negative cache; `external-review.md` "Detection and negative cache"); satisfies DoD identically to a bare `"APPROVE"` but is never written to `latched` ("APPROVE latch" "Availability-skip carve-out" above);
- a legacy External Review partial outcome, `"APPROVE (partial: <n>/<m> files; <cause>)"` (never written by a current-version workflow; recorded before review waves replaced External Review's file batches) — satisfied for its iteration, never latched (same carve-out);
- a legacy `"skipped: <predicate>"` string (no `APPROVE` prefix, never written by any current-version workflow) — may still be present in `state.json` when a consumer upgrades mid-sprint from a pre-4.0.0 `scoped_fan_out`-driven agent-level dispatch skip; every consumer of this map (`asd-phase-pr.md` open mode step 1 included) treats it as **satisfied**, identically to `APPROVE`;
- an absent key for the current iteration — the reviewer was required, dispatched, and its dispatch was lost, crashed, or ledger-rejected without ever producing a recorded verdict. Always **blocking**, no exception: the APPROVE latch invariant above guarantees a latch-skipped reviewer's key is written anyway, so an absent key never has a second, satisfied meaning.

**User-resolved findings**: a finding resolved outside review-fix — by the user without a fix (a FAIL override, a stalemate **stop** (`external-review.md` "Stalemate detection"), an iteration-cap accept), or by impl-review's in-place tester fix (`review-policy.md` "Low-severity test-only findings") — is recorded by the orchestrator as one line appended to that reviewer's review file: `resolved: <finding id, …> — <override | stop | cap-accept | test-fix>, <YYYY-MM-DD>`. The review-fix collector skips each named finding. A bare `"CONCERNS"`/`"FAIL"` whose file names every one of its findings so is **satisfied** for both gating consumers below and for each review phase's own aggregation — the one case they read the review file behind a state value.

`null` is never written deliberately. Two gating consumers of this map — `asd-phase-pr.md` open mode step 1, and impl-review's own DoD aggregation (this rule's "Impl-review" phase-table row / `review-policy.md` "DoD per review phase") — treat an absent key for a required reviewer as blocking, full stop; neither consults `latched`. `.asd/hooks/session-start.js`'s `lastReviewVerdict` is a third, display-only consumer (session-summary text, never a gate): it reads only `verdicts["iter-NN"]` for the relevant review node (impl-review: the current wave's, legacy shape included) — no `latched` awareness — counting any value starting with `"APPROVE"` (bare, availability-skip or legacy partial) or a legacy `"skipped: ..."` string as satisfied, and — like every hook — must keep failing silently (exit 0, never throw) on any malformed or missing shape.

## Agent liveness

While any dispatched agent is in flight, the main orchestrator runs **observe in-flight agent** on each at least every 5 minutes — a read of its progress, never a wait for its completion. Host mapping and verified host facts: `providers.md` "Agent liveness per host".

**Stall**: no progress since the previous check while no tool call of the agent is still open inside its own timeout (a call without its result, 10 minutes at most); or elapsed time past the dispatch's elapsed budget, where one is set, whatever its progress. A long tool call inside its timeout — a wrapped External Review CLI, a suite run — is never a stall.

**On a stall**: **stop in-flight agent** and log it at once in `decisions-log.md` — a reviewer's as its interrupted attempt with cause `stall` (`review-policy.md` "Interrupted dispatch"), any other's as `- YYYY-MM-DD — stall: <agent> <dispatch ids>` — then recover it: a reviewer per that `review-policy.md` section, any other agent per "State recovery" "Failed dispatch". A second stall of the same dispatch, counted from those lines, escalates to the user: retry fresh or abort. One cause stalling every dispatch in flight at once is one event ("Failed dispatch"; `review-policy.md` "Correlated interruption").

**Degraded mode**: where the host offers no periodic observation, checks run only at completion notifications; the orchestrator logs that once per sprint as an `F-N` ("Friction log").
