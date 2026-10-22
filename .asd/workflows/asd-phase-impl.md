# ASD Workflow: Impl

Owner: inline orchestrator; dispatches `asd-dev`, `asd-tester`, a memory-fix owner, skill `asd-init`.

## Entry
```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/"], "modes": {"initial": "review_fixes_pending and test_defects_pending both null; plan.md approved (checkpoints precondition chain); state.json.phase advanced from plan", "review-fix": "review_fixes_pending = <id> (wave-<K>/iter-NN; a legacy bare iter-NN reads as wave 1, its dir the legacy one, sprint-lifecycle.md 'Review iteration counters'); <sprint>/reviews/impl/<id>/ reviewer files exist", "test-fix": "test_defects_pending = true; <sprint>/test-plan.md has pending Defects rows"}, "ops": ["context: node .asd/runtime.js context --phase impl --role orchestrator at entry, read it; --role <agent> before each agent delegation in ## Dispatch, passed as inputs.context, each Conditional grant whose condition holds named in inputs; --out per agent-protocol.md 'Dispatch payload'", "run command: git status --porcelain / git diff for step 9's round diff", "dispatch a skill: asd-init sprint-mediated mode (step 6)", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: a dispatch op around each agent delegation in ## Dispatch (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs
```json
[".asd/project/config.yaml", "<sprint>/state.json", "<sprint>/plan.md", "<sprint>/reviews/impl/<id>/ (review-fix)", "<sprint>/test-plan.md (test-fix)", "persistent docs per sprint-lifecycle.md 'Input sources'", ".asd/project/custom-common-rules.md, custom-coding-rules.md", ".asd/project/stubs.md", "<sprint>/manual-steps.md"]
```

## Steps

Autonomous in all modes: once work is dispatched, no user contact until (a) all plan tasks (initial), findings (review-fix) or defects (test-fix) signal COMPLETED — initial mode then reaches step 10, its first and only user pause, fix modes none; (b) all unblocked work is COMPLETED and validated manual steps remain pending — step 8; or (c) a blocker (`## Signals`) halts and is relayed. Any other mid-impl pause is a protocol violation. Fix modes skip the impl assessment gate and are unbounded: impl-test may route defects back any number of times, short of a stalemate (`asd-phase-impl-test.md` step 9). Step 9 applies in all modes and runs no test; test authoring, pruning and running belong to `impl-test`, a `Test-only` Task excepted (`sprint-lifecycle.md` "Plan file format").

1. Read `.asd/project/config.yaml` (`backward_compat`, `system.tools`, `self_hosting`, `language.chat`, `language.docs`); `self_hosting: enabled` binds `## Rules`' self-hosting bullet into every dispatch.
2. Read `<sprint>/state.json` → **detect mode** per `## Entry` `modes`:
   - both fix flags null/absent → **initial**; confirm `plan.md` approved
   - `review_fixes_pending` = `<id>` → **review-fix**; confirm `<sprint>/reviews/impl/<id>/` exists (else `ABORT — precondition not met: reviews/impl/<id> missing`)
   - `test_defects_pending` set → **test-fix**; confirm `<sprint>/test-plan.md` exists with pending `D-N` rows (else `ABORT — precondition not met: test-plan.md defects missing`)
   - only one fix flag is ever set: each fix mode clears its own before routing on, save an amendment accepted during a review-fix round, which continues into initial mode first (step 11, `sprint-lifecycle.md` "Scope amendment")
3. **Build work set** per mode:
   - **initial** — read `<sprint>/plan.md` → parse Task blocks (title, subtask checkboxes) plus the `## Dependencies` wave table and dependency lines
   - **review-fix** — read every reviewer file in `<sprint>/reviews/impl/<id>/`, `<reviewer>.late.md` included; collect all CONCERNS findings, every admitted late finding, plus all FAIL findings the user accepted for fix or kept by a stalemate **continue fixing**, skipping each finding a `resolved:` line names (`sprint-lifecycle.md` "State recovery" user-resolved findings), plus every `answer:` line under a `question:` item (`review-policy.md` "Gate Verdict Format"), part of that reviewer's finding set; deduplicate across reviewers (`review-policy.md` "Autofix vs escalation" "Deduplication"), then group into fix tasks, one dev task per independent group; findings in test files route to `asd-tester`
   - **test-fix** — read `<sprint>/test-plan.md` `Defects`; collect every `D-N` with status `pending`; group into fix tasks, one dev task per independent group
4. Write `state.json` (phase=impl) inline (mechanical, no gate)
5. **Build execution graph**:
   - initial — the plan's wave table is the graph (`sprint-lifecycle.md` "Plan file format"): waves ascending, tasks within one wave parallelisable, never re-derived from the dependency lines. A plan predating that rule carries no table: fall back to a topological sort over its dependency lines. Ticked work is never re-dispatched: the graph is built from the waves holding an unticked Task, and a wave whose Tasks are all ticked is skipped (`sprint-lifecycle.md` "Scope amendment")
   - fix modes — one ordered chain, never a concurrent set: every fix task depends on its predecessor by construction, so exactly one is in flight at a time (order: colliding tasks adjacent, else by finding/defect id). The chain is dispatched to ONE agent that works it in order, never a fresh instance per task — tier per 5a; test-file findings route to `asd-tester` as their own chain, dispatched only after the dev chain completes and carrying its outcome (fixes already committed — `git-strategy.md` "Commit before review"), never alongside it, so exactly one agent is in flight across the whole round. That tester chain edits only the `test-plan.md` rows `artifact-layout.md` "Test plan" grants the review-fix tester.
5a. Before each task dispatch, run `node .asd/runtime.js route-task --input <path|->`, its input keys per `providers.md` "Task-class variants and routing", `risks` the task's `Material risk` lines as typed entries (`sprint-lifecycle.md` "Plan file format"; a fix round's: same section). `execution="command"` runs directly; `execution="agent"` dispatches the Task's agent — `asd-dev`, or `asd-tester` for a Task carrying a `Test-only:` line — as `<agent>-<tier>` for `mechanical`/`critical`, or the base agent for `tier: standard` (no `-standard` variant exists — `providers.md` "Task-class variants and routing"). Persist the record in `state.json.task_routing[taskId]` per `providers.md`, supplying its tier as `priorTier` only on a re-dispatch of the same id, and append the decisions-log routing line `- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]` (`git rev-parse HEAD`, one per dispatch; the `risk` suffix for a fix round's ids per `providers.md` "Task-class variants and routing"). Invalid routing blocks. In a fix mode, route every task of the chain first and persist each record, then dispatch the whole chain to a single agent at the highest tier returned — one agent holding every fix in the round is what keeps a later fix from contradicting an earlier one it never saw.
6. **Dispatch tasks** per execution graph:
   - **Declared settings change** (initial mode) — as the wave holding a Task's `Settings change:` line (`sprint-lifecycle.md` "Plan file format") opens, before any of that wave's dispatch, the line is applied, never an `MS-N` and never handed to a dev: the main orchestrator dispatches `asd-init` sprint-mediated mode with exactly the declared pairs — validated against the working-tree `t_config.yaml`, so a key an earlier wave added counts — and commits `.asd/project/config.yaml` itself; on `FAILED`, halt as a blocker before any of that wave's dispatch; else that Task's other subtasks, if any, then dispatch in that wave
   - initial mode: waves ascending, every task of a wave dispatched concurrently (caller schedules concurrent delegations), the next wave opening only once all their signals are in; a fix mode: step 5's single ordered chain, dev chain before tester chain
   - per task, or once per chain in a fix mode (5a): delegate to the agent `## Dispatch` names (`dev` in `## Rules` reads as that agent); payload per `agent-protocol.md` "Dispatch payload" with `## Dispatch`'s role keys: `work` — initial: Task block excerpt (title + subtasks + dependencies); review-fix: grouped finding list (each finding's id per `git-strategy.md` "Commits", severity, location, description, plus the user-approved change note for accepted FAIL findings); test-fix: grouped defect list (`D-N`, location, symptom, failing test) plus the guidance of a stalemate continue answer logged for it in the live `decisions-log.md` (`artifact-layout.md` "Decisions log" rotation), if any; `inputs` — "Input sources" keys plus `commands`, `tech_reference`, `custom_rules`, review-fix `reviews` (`reviews/impl/<id>/`), test-fix `test_plan`; `language` — `language.chat`, `language.docs`; instruction: `## Rules`
7. Wait all task signals (COMPLETED and/or BLOCKED_MANUAL); a dispatch returning none → `sprint-lifecycle.md` "State recovery" failed dispatch. Initial mode, after a wave's last signal and before the next wave opens: tick the `plan.md` checkboxes of the subtasks that wave's COMPLETED signals report done, append each returned `memory_index` line to its `MEMORY.md`, and commit; a `memory-check` hit on that commit goes to its owner's memory-fix dispatch here (`review-policy.md` "Memory-fix dispatch"). `self_hosting: enabled`: once a wave's (initial) or the round's (fix modes) last canon-editing dispatch has signalled, before the next wave opens or step 9 runs, run `node .asd/sync.js --apply <generated-view-path...>` (generated view paths only, per `providers.md` "Canonical path -> per-provider path") over every view whose canon changed — `--apply AGENTS.md` when canon changed but no view did, since any valid `--apply` recomputes `release-manifest.json`'s hash ledgers — then `node .asd/sync.js --check`, and commit the regenerated views
8. **Manual-steps validation + gate** [GATE hard: manual steps] — when any `BLOCKED_MANUAL` emitted:
   - validate each new `MS-N` for necessity: keep only when the action genuinely cannot be done autonomously (needs access, secret, external account, or authority the agent lacks); reject any entry the agent could do with its own tools → re-dispatch its owning dev with feedback "implement autonomously, remove MS-N"; loop step 7
   - once all remaining `MS-N` are validated and all unblocked tasks COMPLETED: append a decisions-log entry; add an `F-N` citing the blocking `MS-N` ids only when the halt itself was a malfunction — the step was unexpected, unworkable, or raised at the wrong point (`sprint-lifecycle.md` "Friction log"); a validated, genuinely necessary halt records no `F-N`; present `manual-steps.md` to the user (`checkpoints.md` "Gate mechanics"); wait for an explicit continue command
   - on continue: re-dispatch each deferred task to its owning dev to verify its `MS-N` (`## Rules`); on COMPLETED tick its `plan.md` checkboxes as step 7 does; a repeated `BLOCKED_MANUAL` is relayed to the user
   - loop until every `MS-N` is `done` and every deferred task COMPLETED
9. **Impl completion gate** (all modes) — phase MUST NOT emit COMPLETED until the orchestrator verifies, via `commands.yaml`:
   - `build` and `lint` each executed and finished with no errors and no warnings
   - the round's diff — what its agents committed plus anything still uncommitted — read before committing or advancing: every path it touches is one those agents were authorised to touch, plus `.asd/project/config.yaml` when step 6 applied a declared settings change, plus each generated view step 7's sync regenerated, plus `<sprint>/plan.md` and each `MEMORY.md` step 7 wrote, plus each memory file the orchestrator applied from step 7's memory-fix dispatch, plus `<sprint>/timing.jsonl`. Any other path fails the gate as a build error does — a file no dispatched task named, a generated view an agent edited, a scripted edit that rewrote more than its target. Distinct from `code-style.md` §19's staged-content lint: same tool, different question
   - the gate never runs tests — a dev's impacted-set self-verification run is not part of it; the suite/impacted-set gates belong to `impl-test`/`impl-review`
   - any condition fails → relay the specific failure to the owning dev(s) to fix and re-run; loop step 7. Unrecoverable failure escalates as a blocker (`FAILED`). Automatic verification — no user pause
10. **Impl assessment checkpoint** [GATE adaptive: initial impl assessment] — **initial mode only** (fix modes skip to step 11) — apply `checkpoints.md`, recording adaptive evidence or requesting the user:
   - read updated `<sprint>/plan.md` → verify all checkboxes ticked
   - a non-empty `flagged` in any COMPLETED is an unresolved material alternative (`checkpoints.md` "Gate policy"): no adaptive pass until the orchestrator resolves it or routes it back to the dev (loop step 7). Before accepting a choice, search the decisions log, rotated parts included (`artifact-layout.md` "Decisions log"), for an earlier disposition of it; an acceptance reversing one appends a decisions-log line naming the earlier entry
   - read `.asd/project/stubs.md` → list stubs introduced this sprint (filter Sprint=<NNN-slug>; all rows open by definition since delete-on-resolve)
   - compose impl summary: tasks done, AC-N coverage map, files changed, build + lint status, sprint-introduced stubs
   - present via request for user decision: approve (advance to impl-test) / request changes / abort
   - on approve: update `state.json`, append decisions-log entry ("impl assessment approved")
   - on request changes: relay specific feedback to relevant dev(s); loop step 7
   - on abort: emit ABORT
11. **Fix-mode finalize** — fix modes only — write inline (mechanical, no gate), after resolving or routing back any non-empty `flagged` as step 10 does:
   - review-fix: clear `state.json.review_fixes_pending` (set null), append decisions-log entry "impl fix for <id>: findings resolved"; when `plan.md` still holds an unticked Task (an amendment accepted while the flag was set), continue in initial mode, steps 3 to 10, before step 12 (`sprint-lifecycle.md` "Scope amendment")
   - test-fix: clear `state.json.test_defects_pending` (set null), append decisions-log entry "impl test-fix: defects <D-N list> resolved"
12. Emit phase COMPLETED with `## Return` (`NEXT: impl-test` in all modes)

## Rules
- must: read the payload context first; tech-reference precondition (refuse-to-implement): `artifact-layout.md` "Tech reference docs"; apply the checklists and iron rules of `code-style.md` §1 while authoring, not only at review.
- must: work autonomously within plan + persistent docs scope — never pause the user for routine approach choices, make the reasonable call and proceed; escalate only on a blocker: `QUESTION` for requirement ambiguity unresolvable from plan + persistent docs, a Complication Approval `QUESTION` only when a Simplicity Default trigger (`core.md`) fires (new abstraction / dependency / config flag / generalization), `FAILED` for a missing tech-reference, an unrecoverable lint/build failure, a defect it cannot fix (never a silent loop), or a command its host denied — committing its edits first and naming that command.
- must: register a human-only operational action as an `MS-N` in `<sprint>/manual-steps.md` (`t_manual-steps.md`), mark its subtasks `BLOCKED:`, defer only those and continue all unblocked work; emit COMPLETED for the unblocked portion plus `BLOCKED_MANUAL` listing the deferred `MS-N`. Rejected as avoidable (step 8): delete the entry, unmark the `BLOCKED:` subtask, implement it. On resume verify the `MS-N` per its `Verification` field: verified → flip its `Status` to `done`, finish the `BLOCKED:` subtasks, emit COMPLETED; not verified → `BLOCKED_MANUAL` again, entry stays `pending`. A manual step is an operational *setup* action, distinct from a `test-plan.md` Manual verification spec.
- never: author, modify or prune a test — write production code only. The impacted set (`sprint-lifecycle.md` "Impacted test set") may be run in any mode to self-check work in progress, never satisfying or substituting for step 9 or the `impl-test`/`impl-review` gates. Exception: a `Test-only` Task, whose `asd-tester` edits only that Task's test paths, never `test-plan.md` (`sprint-lifecycle.md` "Plan file format").
- must: review-fix — resolve every finding of `work`, applying `review-policy.md` "Verify before applying"; a fix changing a rule other files consume runs the consumer search of its "Autofix vs escalation"; a finding about a rule's reach is fixed per its "Reach findings".
- must: test-fix — fix the root cause behind the failing test, never weakening or deleting the test, then set the defect row `Status` to `fixed` with the fixing commit sha in `<sprint>/test-plan.md`.
- must: run `build` and `lint` per `commands.yaml`; never advance with failures or warnings unreported.
- must: stubs per `git-strategy.md` "TODO stubs"; staging and commit ownership — concurrently dispatched tasks share one worktree — per `git-strategy.md` "Commit before review"; commit per Conventional Commits (one logical change per commit; subject ≤50 chars; body describes WHY).
- must: `self_hosting: enabled` — write scope extends per plan scope to the exhaustive allowlist in `sprint-lifecycle.md` "Self-hosting"; edit canon only, never run `sync.js --apply` (the orchestrator syncs at step 7); generated `.claude/`/`.codex/`/`.agents/skills/` stay off-limits always.
- never: edit `<sprint>/plan.md` (step 7 ticks it); in a wave of more than one Task leave a shared `MEMORY.md` untouched, returning its index line as `memory_index`.
- must: COMPLETED (`agent-protocol.md` "Return") when all subtasks/findings/defects are done: `changed` the files touched; `resolved` — initial: AC-N satisfied; review-fix: finding ids; test-fix: `D-N`; review-fix body adds the consumers the consumer search updated, initial body the stubs added; `flagged` in every mode.

## Dispatch
```json
[{"agent": "asd-dev", "when": "each Task without a Test-only line (initial); the fix chain (review-fix, test-fix)", "payload": ["mode", "task", "work", "inputs.context", "inputs", "write_scope", "language"], "signals": ["COMPLETED", "QUESTION", "ADVICE_NEEDED", "BLOCKED_MANUAL", "FAILED", "ABORT"]}, {"agent": "asd-tester", "when": "a Test-only Task (initial); the test-file findings chain, after the dev chain (review-fix)", "payload": ["mode", "task", "work", "inputs.context", "inputs", "write_scope", "language"], "signals": ["COMPLETED", "QUESTION", "ADVICE_NEEDED", "BLOCKED_MANUAL", "FAILED", "ABORT"]}, {"agent": "<owner> of .claude/agent-memory/<owner>/", "when": "a memory-check hit on step 7's MEMORY.md commit (Claude only)", "payload": ["mode", "task", "work", "inputs.context", "write_scope"], "signals": ["COMPLETED", "FAILED"]}, {"agent": "asd-init (skill, sprint-mediated mode)", "when": "a wave holding a Settings change: line opens (initial), step 6", "payload": ["the declared pairs"], "signals": ["COMPLETED", "FAILED"]}]
```

## Signals
```json
{"QUESTION": "blocker: relay per agent-protocol.md 'Relay'", "FAILED": "blocker, asd-init's at step 6 included: relay, halt, resume on the user's decision", "ABORT": "blocker: relay, halt", "BLOCKED_MANUAL": "no immediate halt: unblocked work continues; the phase halts at step 8 only after every unblocked task COMPLETED", "ADVICE_NEEDED": "relay per agent-protocol.md 'Relay'; execution resumes, never a halt or a blocker"}
```

## Exit
```json
{"writes": ["production source code (no tests; a Test-only Task's excepted)", ".asd/project/stubs.md", "<sprint>/manual-steps.md", "<sprint>/plan.md checkboxes and MEMORY.md index lines (step 7)", "reviewer files in <sprint>/reviews/impl/<id>/ with user-approved change notes (review-fix)", "<sprint>/test-plan.md defect Status (test-fix)", "state.json", "commits", "decisions-log.md"], "next": [{"if": "every mode", "next": "impl-test"}]}
```

## Return
```
PHASE: impl | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: impl-test
```

## References
- `.asd/rules/agent-protocol.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/review-policy.md`
- `.asd/rules/git-strategy.md`
- `.asd/rules/artifact-layout.md`
- `.asd/rules/language-policy.md`
- Templates: `t_plan.md`, `t_review.md`, `t_test-plan.md`, `t_stubs.md`, `t_manual-steps.md`
