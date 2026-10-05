# ASD Workflow: Impl

Orchestration body for the `asd-phase-impl` skill. Operation-mapping to host tools: `.asd/rules/providers.md`.

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- **Initial mode**: `plan.md` approved (per checkpoints precondition chain); `state.json.phase` advanced from `plan`
- **Review-fix mode**: `state.json.review_fixes_pending` set to an impl-review iteration id `<id>` (`wave-<K>/iter-NN`; a legacy bare `iter-NN` reads as wave 1, its dir the legacy one — `sprint-lifecycle.md` "Review iteration counters"); `<sprint>/reviews/impl/<id>/` reviewer files exist
- **Test-fix mode**: `state.json.test_defects_pending` set; `<sprint>/test-plan.md` has pending `Defects` rows

## Operations used
- read: `.asd/project/config.yaml`, `state.json`, `plan.md`, `<sprint>/reviews/impl/<id>/` (review-fix), `<sprint>/test-plan.md` (test-fix), persistent docs, `.asd/project/custom-common-rules.md`, `custom-coding-rules.md`, `stubs.md`, `<sprint>/manual-steps.md`
- run command: `git status --porcelain`/`git diff` to read the round's committed-plus-uncommitted diff for step 9's authorised-paths gate; `commands.yaml` `build`/`lint` for the same gate; `git rm` of an ownerless agent-memory file (step 3); `node .asd/sync.js --apply`/`--check` and the commit of the regenerated views (step 7, `self_hosting: enabled`); the commit of step 7's plan ticks and index lines
- write a file: `state.json` inline, for the mechanical non-gate writes at steps 4, 11 (`sprint-lifecycle.md` "State recovery"); `plan.md` checkboxes and devs' returned `MEMORY.md` index lines (step 7); a memory-fix dispatch's returned text, verbatim, and a memory `D-N`'s `Status` in `test-plan.md` (step 3)
- request user decision: escalation only (see Execution mode)
- delegate to agent: `asd-dev` per task / finding group / defect group (a `Test-only` Task and test-file findings to `asd-tester`, `sprint-lifecycle.md` "Plan file format"; memory findings to their owner — step 3); the main orchestrator owns manual-step validation, gates and decisions-log inline
- dispatch a skill: `asd-init` sprint-mediated mode, for a declared settings change, as its Task's wave opens (step 6)
- append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log"

## Modes

Mode set and semantics: `sprint-lifecycle.md` "Impl phase" Modes (sole SSoT, not restated here). Detection is step 2; per-mode preconditions are above; the entered mode's flag is cleared at step 11.

Fix modes **skip the impl assessment gate**. Impl completion gate (step 9) applies in **all** modes and runs no test — this is a scoping rule, not a new gate: a dev may run the impacted set (`sprint-lifecycle.md` "Impacted test set") to self-check work in progress, in any mode, but never authors, modifies, or prunes a test, and that run neither satisfies nor substitutes for this gate. Test authoring, pruning, and running belong to `impl-test`, except a `Test-only` Task (`sprint-lifecycle.md` "Plan file format").

## Execution mode

Impl runs **autonomously** in all modes. Once tasks/fixes dispatched, devs work without user contact until **one** of:

- **all plan tasks (initial), findings (review-fix), or defects (test-fix) signal COMPLETED** — then initial mode hits impl assessment gate (step 10), the first and only user pause; fix modes have no such pause; or
- **all unblocked work COMPLETED and validated manual steps remain pending** — phase halts at manual-steps gate (step 8); or
- **a blocker requiring escalation arises** — execution halts, blocker relayed.

A blocker is exactly one of:
- dev `QUESTION` — requirement ambiguity unresolvable from plan + persistent docs;
- dev `FAILED`/`ABORT` — missing tech-reference, unrecoverable lint/build failure, or a command its host denied: the dev commits its edits first, then returns `FAILED` naming that command;
- `asd-init` sprint-mediated `FAILED` at step 6 — a declared settings-change pair failed validation;
- a Simplicity Default trigger (`core.md`) — new abstraction, dependency, config flag, or generalization — needs Complication Approval before proceeding: the dev returns it as a `QUESTION`.

A dev `BLOCKED_MANUAL` does **not** halt immediately: dev registers the manual action, defers only affected subtasks, continues all unblocked work. Phase halts at manual-steps gate (step 8) only after every unblocked task COMPLETED.

Devs do **not** pause user for routine "non-trivial approach" decisions. Within plan + persistent docs scope, make the reasonable call and proceed. Pausing mid-impl for anything other than a blocker above (or manual-steps gate) is a protocol violation.

Fix modes are unbounded by design: impl-test may route defects back any number of times, short of a stalemate (`sprint-lifecycle.md` "Impl-test phase"). A dev that cannot fix a defect emits `FAILED` rather than looping silently.

## Workflow

1. Read `.asd/project/config.yaml` (`backward_compat`, `system.tools`, `self_hosting`, `language.chat`, `language.docs`). When `self_hosting: enabled`, devs' write scope extends per plan scope to the exhaustive allowlist in `sprint-lifecycle.md` "Self-hosting" (do not restate it here); dev instruction (step 6) adds: edit canon only, never run `sync.js --apply` — the orchestrator syncs at step 7 (`sprint-lifecycle.md` "Self-hosting"); generated `.claude/`/`.codex/`/`.agents/skills/` stay off-limits to devs always
2. Read `<sprint>/state.json` → **detect mode**:
   - both fix flags null/absent → **initial mode**; confirm `plan.md` approved
   - `review_fixes_pending` = `<id>` → **review-fix mode**; confirm `<sprint>/reviews/impl/<id>/` exists (else `ABORT — precondition not met: reviews/impl/<id> missing`)
   - `test_defects_pending` set → **test-fix mode**; confirm `<sprint>/test-plan.md` exists with pending `D-N` rows (else `ABORT — precondition not met: test-plan.md defects missing`)
3. **Build work set** per mode:
   - **initial** — read `<sprint>/plan.md` → parse Task blocks (title, subtask checkboxes) plus the `## Dependencies` wave table and dependency lines
   - **review-fix** — read every reviewer file in `<sprint>/reviews/impl/<id>/`, `<reviewer>.late.md` included; collect all CONCERNS findings, every admitted late finding, plus all FAIL findings the user accepted for fix or kept by a stalemate **continue fixing**, skipping each finding a `resolved:` line names (`sprint-lifecycle.md` "State recovery" user-resolved findings), plus every `answer:` line under a `question:` item (`review-policy.md` "Gate Verdict Format"), part of that reviewer's finding set; deduplicate the collected set across reviewers (`review-policy.md` "Autofix vs escalation" "Deduplication"), then group into fix tasks, one dev task per independent group; findings located in test files route to `asd-tester` instead; findings located in `.claude/agent-memory/<owner>/` route to `<owner>`'s fresh memory-fix dispatch (Claude only, `review-policy.md` "Autofix vs escalation"): the owner fixes the file itself, the orchestrator committing it when the owner holds no commit tool; an owner whose definition withholds a write tool returns `MEMORY-FIX <path>` text the orchestrator applies verbatim, commits and logs in one decisions-log line; a finding in the directory of an agent that no longer exists is deleted by the orchestrator per the same rule
   - **test-fix** — read `<sprint>/test-plan.md` `Defects` section; collect every `D-N` with status `pending`; group into fix tasks, one dev task per independent group; a `D-N` located in `.claude/agent-memory/<owner>/` routes to `<owner>` exactly as a review-fix memory finding does, the orchestrator then setting that row's `Status` to `fixed` (`review-policy.md` "Autofix vs escalation" memory-fix dispatch)
4. Write `state.json` (phase=impl) inline (mechanical, no gate)
5. **Build execution graph**:
   - initial — the plan's wave table is the graph (`sprint-lifecycle.md` "Plan file format"): waves ascending, tasks within one wave parallelisable, never re-derived from the dependency lines. A plan predating that rule carries no table: fall back to a topological sort over its dependency lines
   - fix modes (review-fix and test-fix alike) — one ordered chain, never a concurrent set: every fix task depends on its predecessor by construction, so exactly one is in flight at a time (order: colliding tasks adjacent, else by finding/defect id). The chain is dispatched to ONE agent that works it in order, never a fresh instance per task — tier per 5a; test-file findings still route to `asd-tester` as their own chain, dispatched only after the dev chain completes and carrying its outcome (fixes already committed — `git-strategy.md` "Commit before review"), never alongside it, so exactly one agent is in flight across the whole round. That tester chain edits only the `test-plan.md` rows `artifact-layout.md` "Test plan" grants the review-fix tester. Memory-fix dispatches (step 3) run last, one at a time, in the order `review-policy.md` "Autofix vs escalation" sets.
5a. Before each task dispatch, run `node .asd/runtime.js route-task --input <path>` with kind, objective inputs/checks, the task's `Material risk` lines as typed `risks` entries (`sprint-lifecycle.md` "Plan file format"; a fix round's `risks`: `providers.md` "Task-class variants and routing"), correction attempts and `priorTier`. A result with `execution="command"` runs directly; `execution="agent"` dispatches the Task's agent — `asd-dev`, or `asd-tester` for a Task carrying a `Test-only:` line — as `<agent>-<tier>` for `mechanical`/`critical`, or the base agent for `tier: standard` (no `-standard` variant exists — `providers.md` "Task-class variants and routing"). Persist the record in `state.json.task_routing[taskId]` per `providers.md`, supplying its tier as `priorTier` only on a re-dispatch of the same id, and append the decisions-log routing line `- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>` (`git rev-parse HEAD`, one per dispatch). Invalid routing blocks. In a fix mode, route every task of the chain first and persist each record, then dispatch the whole chain to a single agent at the highest tier returned — one agent holding every fix in the round is what keeps a later fix from contradicting an earlier one it never saw.
6. **Dispatch tasks** per execution graph:
   - **Declared settings change** (initial mode) — as the wave holding a Task's `Settings change:` line (`sprint-lifecycle.md` "Plan file format") opens, before any of that wave's dispatch, the line is applied, never an `MS-N` and never handed to a dev: the main orchestrator dispatches `asd-init` sprint-mediated mode with exactly the declared pairs — validated against the working-tree `t_config.yaml`, so a key an earlier wave added counts — and commits `.asd/project/config.yaml` itself; on `FAILED`, halt as a blocker before any of that wave's dispatch; else that Task's other subtasks, if any, then dispatch in that wave
   - per step 5's wave table, sequential where dependent; parallel where independent: waves ascending, every task of a wave dispatched concurrently (caller schedules concurrent delegations), the next wave opening only once all their signals are in — initial mode only; in a fix mode step 5's single ordered chain governs, dev chain before tester chain
   - per task, or once per chain in a fix mode (5a): delegate to the Task's agent (`dev` below reads as that agent) — `asd-dev`; `asd-tester` for a `Test-only` Task and for review findings in test files; a memory finding's owner per step 3 — with payload opening with the header per `providers.md` "Dispatch payload header":
     - initial — Task block excerpt (title + subtasks + dependencies); review-fix — grouped finding list (each finding's id per `git-strategy.md` "Commits", severity, location, description; plus user-approved change note for accepted FAIL findings); test-fix — grouped defect list (`D-N`, location, symptom, failing test) plus the guidance of a stalemate continue answer logged for it in the live `decisions-log.md` (`artifact-layout.md` "Decisions log" rotation), if any
     - relevant context paths (PRD AC-N referenced, ADRs, ux-spec, DESIGN.md, accessibility, stack, commands.yaml, tech-reference/, custom-common-rules.md, custom-coding-rules.md; review-fix also: reviewer files in `reviews/impl/<id>/`; test-fix also: `test-plan.md`)
     - `language.chat`, `language.docs`
     - instruction:
       - read context first
       - tech-reference precondition (refuse-to-implement rule): see `artifact-layout.md` "Tech reference docs" — do not restate here
       - apply the checklists and iron rules while authoring, not only at review: `code-style.md` §1 — do not restate here
       - work autonomously within plan + persistent docs scope; do NOT pause user for routine approach choices — make the reasonable call and proceed
       - escalate only on a blocker (see Execution mode): emit `QUESTION` for unresolvable requirement ambiguity, `FAILED` for missing tech-reference / unrecoverable failure, or return a Complication Approval `QUESTION` **only** when a Simplicity Default trigger fires (new abstraction / dependency / config flag / generalization)
       - manual-steps handling: see `sprint-lifecycle.md` "Impl phase" — do not restate here
       - write production code only — **no tests, no authoring, no modifying, no pruning**; the impacted set (`sprint-lifecycle.md` "Impacted test set") may be run for self-verification only, never as a substitute for `impl-test`'s gate; test selection, authoring, pruning, and running belong to `impl-test` — except a `Test-only` Task, whose `asd-tester` edits only that Task's test paths, never `test-plan.md` (`sprint-lifecycle.md` "Plan file format")
       - review-fix — `review-policy.md` "Verify before applying" — do not restate here; a fix changing a rule other files consume runs the consumer search of its "Autofix vs escalation"; a finding about a rule's reach is fixed per its "Reach findings"; test-fix — fix the root cause behind the failing test (never weaken or delete the test), then set the defect row `Status` to `fixed` with the fixing commit sha in `<sprint>/test-plan.md`
       - run `build` and `lint` per `commands.yaml`; do not advance with failures or warnings unreported
       - stub handling: see `git-strategy.md` "TODO stubs" — do not restate here
       - staging + commit ownership — concurrently dispatched tasks share one worktree: see `git-strategy.md` "Commit before review" — do not restate here
       - commit per Conventional Commits (one logical change per commit; subject ≤50 chars; body describes WHY)
       - initial — leave `<sprint>/plan.md` untouched (step 7 ticks it); a wave of more than one Task adds: leave a shared `MEMORY.md` untouched and return its index line in COMPLETED (`sprint-lifecycle.md` "Impl phase")
       - emit COMPLETED with summary (files touched; initial: AC-N satisfied, stubs added; review-fix: findings resolved by id, plus the consumers the consumer search updated; test-fix: defects resolved by `D-N`; every mode: `Flagged choices:` `none` or a list) when all subtasks/findings/defects done; when some subtasks manual-blocked, emit COMPLETED for unblocked portion plus `BLOCKED_MANUAL` listing deferred `MS-N`
7. Wait all task signals (COMPLETED and/or BLOCKED_MANUAL); a dispatch returning none → `sprint-lifecycle.md` "State recovery" failed dispatch. Initial mode, after a wave's last signal and before the next wave opens: the orchestrator ticks the `plan.md` checkboxes of the subtasks that wave's COMPLETED signals report done, appends each returned index line to its `MEMORY.md`, and commits (`sprint-lifecycle.md` "Impl phase"). `self_hosting: enabled`: once a wave's (initial) or the round's (fix modes) last canon-editing dispatch has signalled, before the next wave opens or step 9 runs, the orchestrator runs `node .asd/sync.js --apply <generated-view-path...>` (generated view paths only, per `providers.md` "Canonical path -> per-provider path") over every view whose canon changed — `--apply AGENTS.md` when canon changed but no view did, since any valid `--apply` recomputes `release-manifest.json`'s hash ledgers — then `node .asd/sync.js --check`, and commits the regenerated views
8. **Manual-steps validation + gate** — when any `BLOCKED_MANUAL` emitted:
   - the main orchestrator validates each new `MS-N` for necessity:
     - keep only when action genuinely cannot be done autonomously (needs access, secret, external account, or authority agent lacks)
     - reject any entry agent could do with own tools → re-dispatch its owning dev with feedback "implement autonomously, remove MS-N"; dev deletes entry, unmarks `BLOCKED:` subtask, implements it; loop step 7
   - once all remaining `MS-N` are validated and all unblocked tasks COMPLETED, the main orchestrator:
     - append decisions-log entry; add an `F-N` citing the blocking `MS-N` ids only when the halt itself was a malfunction — the step was unexpected, unworkable, or raised at the wrong point (`sprint-lifecycle.md` "Friction log"); a validated, genuinely necessary halt records no `F-N`
     - present `manual-steps.md` to user (per `checkpoints.md` "Gate mechanics"); wait for explicit continue command
   - on user continue: re-dispatch each deferred task to owning dev with instruction:
     - verify referenced `MS-N` per its `Verification` field
     - if verified → flip entry `Status` to `done`, finish `BLOCKED:` subtasks, emit COMPLETED; the orchestrator then ticks their `plan.md` checkboxes as step 7 does
     - if not verified → emit `BLOCKED_MANUAL` again (entry stays `pending`); relay to user
   - loop until every `MS-N` is `done` and every deferred task COMPLETED
9. **Impl completion gate** (all modes) — the main orchestrator verifies, via `commands.yaml`:
   - `build` command executed and finished with no errors and no warnings
   - `lint` command executed and finished with no errors and no warnings
   - the round's diff — what its agents committed plus anything still uncommitted — read before committing or advancing: every path it touches is one those agents were authorised to touch, plus `.asd/project/config.yaml` when step 6 applied a declared settings change, plus each memory file the orchestrator applied from a memory-fix dispatch or deleted as ownerless, and `<sprint>/test-plan.md` when it set a memory `D-N`'s `Status` (step 3), plus each generated view step 7's sync regenerated, plus `<sprint>/plan.md` and each `MEMORY.md` step 7 wrote. Any other path fails the gate as a build error does — a file no dispatched task named, a generated view an agent edited, a scripted edit that rewrote more than its target. Distinct from `code-style.md` §19's staged-content lint: same tool, different question
   - the gate itself never runs tests — a dev's optional impacted-set self-verification run (`sprint-lifecycle.md` "Impacted test set") is not part of it; the suite/impacted-set gates belong to `impl-test`/`impl-review`
   - if any condition fails → phase MUST NOT advance: relay specific failure to owning dev(s) to fix and re-run; loop step 7. Unrecoverable failure escalates as a blocker (`FAILED`).
   - automatic verification — no user pause
10. **Impl assessment checkpoint** — **initial mode only** (fix modes skip to step 11) — the main orchestrator applies `checkpoints.md`, recording adaptive evidence or requesting the user:
   - read updated `<sprint>/plan.md` → verify all checkboxes ticked
   - a non-`none` `Flagged choices:` in any dev COMPLETED is an unresolved material alternative (`checkpoints.md` "Gate policy"): no adaptive pass until the orchestrator resolves it or routes it back to the dev (loop step 7). Before accepting a choice, search the decisions log, rotated parts included (`artifact-layout.md` "Decisions log"), for an earlier disposition of it; an acceptance reversing one appends a decisions-log line naming the earlier entry
   - read `.asd/project/stubs.md` → list stubs introduced this sprint (filter Sprint=<NNN-slug>; all rows open by definition since delete-on-resolve)
   - compose impl summary: tasks done, AC-N coverage map, files changed, build + lint status, sprint-introduced stubs
   - present via request for user decision: approve (advance to impl-test) / request changes / abort
   - on approve: update `state.json`, append decisions-log entry ("impl assessment approved")
   - on request changes: relay specific feedback to relevant dev(s); loop step 7
   - on abort: emit ABORT
11. **Fix-mode finalize** — fix modes only — write inline (mechanical, no gate), after resolving or routing back any non-`none` `Flagged choices:` as step 10 does:
   - review-fix: clear `state.json.review_fixes_pending` (set null), append decisions-log entry "impl fix for <id>: findings resolved"
   - test-fix: clear `state.json.test_defects_pending` (set null), append decisions-log entry "impl test-fix: defects <D-N list> resolved"
12. Emit phase COMPLETED with return contract (`NEXT: impl-test` in all modes)

## Escalation (interruptions before phase exit)

The only reasons impl contacts the user before all tasks/findings/defects complete, in every mode, are the blockers enumerated under **Execution mode** above plus the manual-steps gate (step 8). A dev `QUESTION` follows `sprint-lifecycle.md`'s `QUESTION` protocol; every other blocker relays and halts, resuming on the user's decision or continue command.

On `ADVICE_NEEDED` from any dispatched agent → relay per `sprint-lifecycle.md`'s `ADVICE_NEEDED` protocol; execution resumes, no halt. Not a blocker — the branches above are the only ones that halt.

Impl completion gate (step 9) and, initial mode only, impl assessment gate (step 10) are the post-work gates. Fix modes have no user-facing assessment gate.

## Artefacts produced
- Production source code in repo (no tests — see `impl-test`, a `Test-only` Task's excepted)
- Updated `.asd/project/stubs.md` (project-global; open stubs only, deleted on resolution)
- `<sprint>/manual-steps.md` when a manual action arose (per-sprint, append-only)
- Updated `<sprint>/plan.md` checkboxes and devs' returned `MEMORY.md` index lines, written by the orchestrator (initial mode, step 7)
- Updated reviewer files in `<sprint>/reviews/impl/<id>/` with user-approved change notes (review-fix mode)
- Updated `<sprint>/test-plan.md` defect rows flipped to `fixed` (test-fix mode)
- Updated `state.json` (phase=impl; the entered mode's fix flag cleared on exit)
- Git commits per Conventional Commits
- decisions-log entry on impl assessment approval (initial) or fix-mode finalize

## Agents delegated to
- The main orchestrator (manual-step validation, completion/assessment gates and decisions-log); no orchestration agent is dispatched.
- `asd-dev` (per Task other than a `Test-only` one, finding group, or defect group)
- `asd-tester` (a `Test-only` Task, initial mode; for findings located in test files, review-fix mode)
- a memory finding's owner (review-fix and test-fix modes, Claude only; step 3)

## Return contract (single line)
```
PHASE: impl | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: impl-test
```

## References
- `.asd/rules/sprint-lifecycle.md` (impl phase contract, impl modes, completion gate, impl⇄impl-test⇄impl-review cycle, impacted test set)
- `.asd/rules/checkpoints.md` (impl assessment gate, fix-mode preconditions)
- `.asd/rules/review-policy.md` (severity, finding format consumed in review-fix mode)
- `.asd/rules/git-strategy.md` (commits, project-global stubs, dirty tree)
- `.asd/rules/artifact-layout.md` (tech-reference refuse-to-implement rule, project stubs path)
- `.asd/rules/language-policy.md`
- Templates: `t_plan.md` (Task parsing reference), `t_review.md` (reviewer finding format, review-fix mode), `t_test-plan.md` (defect format, test-fix mode), `t_stubs.md`, `t_manual-steps.md`
