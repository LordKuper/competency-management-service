# ASD Workflow: Impl Test

Owner: inline orchestrator; dispatches one fresh `asd-tester` per entry, plus a memory hit's owner (step 7a) and `asd-advisor`.

## Entry
```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/", "impl COMPLETED received (build + lint green); state.json.phase advanced from impl", "state.json.review_fixes_pending and test_defects_pending both cleared by the impl fix-mode finalize"], "modes": {"entry-1": "test-plan.md has no Entry log row (first entry this sprint)", "re-entry": "a prior Entry log row exists; its HEAD analysed is <prior-sha>", "resume": "the last Entry log row has an empty HEAD analysed (interrupted current entry, e.g. a stalemate halt)"}, "ops": ["context: node .asd/runtime.js context --phase impl-test --role orchestrator at entry, read it; --role <agent> before each agent delegation in ## Dispatch, passed as inputs.context, each Conditional grant whose condition holds named in inputs; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: a dispatch op around each tester dispatch, whose payload timing key carries <sprint>/timing.jsonl and that dispatch id, the suite ops' --parent (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs
```json
[".asd/project/config.yaml", "<sprint>/state.json", "<sprint>/plan.md", "<sprint>/test-plan.md", "persistent docs per sprint-lifecycle.md 'Input sources' (requirements, ux, contracts)", ".asd/project/commands.yaml", "custom-common-rules.md, custom-coding-rules.md", "existing test sources"]
```

## Steps

Autonomous: no user gate on a green impacted-set run, none on routing defects back to impl short of a stalemate. The one live `asd-tester` of an entry runs steps 3, 4, 7 and 8 and is never resumed into a later entry — `test-plan.md` is the only hand-off; recover from on-disk evidence only after session loss.

1. Read `.asd/project/config.yaml` (`language.chat`, `language.docs`, `backward_compat`, `self_hosting`), `<sprint>/state.json` → write `phase=impl-test` inline (mechanical, no gate). Detect the mode per `## Entry` `modes` from `<sprint>/test-plan.md`'s `Entry log`. **Resume**: keep its row (step 4 appends none), take `<prior-sha>` from the row before it (none → entry 1, amended not rewritten), never re-append its `D-N` rows; when they exist and a current answer (step 9) names their digest, go straight to step 9's current-answer branch. Entry 1 of a sprint that removes a mechanism or term also runs the leftover-term check `artifact-layout.md` "Agent memory" requires, in the strategy pass (step 4), its memory hits fixed at step 7a
1a. `asd-tester` is unrouted (`providers.md` "Task-class variants and routing"): no `route-task` call, no `task_routing` record. Dispatch the base agent, payload per `agent-protocol.md` "Dispatch payload" with `## Dispatch`'s role keys, appending the decisions-log line `- YYYY-MM-DD — route <taskId>: standard, dispatch HEAD <sha>` (`git rev-parse HEAD`) at each tester dispatch.
2. **Change surface**:
   - **Entry 1**: run command for `git diff <git.base_branch>...HEAD --stat <pathspec>` plus file list, the pathspec excluding the impl-review row's exclusions (`.asd/rules/external-review.md` "Phase-scoped payload"). This is the **full change surface**; a `Test-only` Task's commits (trailer `ASD-Task: Task N`, `sprint-lifecycle.md` "Plan file format") are existing tests in it
   - **Re-entry**: run command for `git diff <prior-sha>...HEAD --stat <pathspec>` (same pathspec) — the review-fix or test-fix commits made since the prior entry. This **delta** is the scope for steps 4 and 7 only
   - Either way: derive the **impacted set** per `sprint-lifecycle.md` "Impacted test set" (diff test files + reference/import search + AC-tag search, native selector override when `commands.yaml` carries one, mandatory shared-infrastructure safety valve checked before use) — the scope for steps 3 and 8
3. **Pre-strategy impacted run** — the tester runs the impacted existing tests before authoring; its raw result feeds the strategy pass.
4. **Strategy pass** — the tester receives change surface, prior plan evidence, ACs, contracts, commands and rules (`## Rules`), and:
   - **re-entry**: first carries each review-fix removal row forward as a live `Removed tests` row, which step 5 collects, then rotates the previous entry's narrative rows, review-fix rows included, into `test-plan.entry-NN.md` (`artifact-layout.md` "Test plan"); analyses only the delta — the material risk introduced or changed by the fix commits; leaves prior `Risk → check decisions` rows untouched unless a fix actually changed that risk's behaviour, in which case it supersedes a rotated one
   - **entry 1**: writes `<sprint>/test-plan.md` per `t_test-plan.md` (Risk → check decisions etc.), the first `Entry log` row's `HEAD analysed` left unfilled (scope = "full change surface"). **Re-entry**: amends it — appends new/updated rows, the new `Entry log` row's `HEAD analysed` left unfilled (scope = "delta since entry N-1"); never rewrites prior rows outside the ones actually revised. Emits COMPLETED. The `HEAD analysed` sha is written at step 9's routing exit or in step 10, after the prune/author commit (step 7) and the suite recording (step 8) — never before — so the next re-entry's delta excludes this entry's own test-authoring commits
5. Read `test-plan.md` → collect proposed removals; split into in-scope (test file inside the change surface) and out-of-scope — outside the sprint's **overall** change surface, not merely the current delta
6. **Removal gate** [GATE adaptive: test removal] — only when out-of-scope removals exist: apply `checkpoints.md`; strict requests the user's explicit approval, adaptive needs recorded authority/evidence. Rejected removals are struck from `test-plan.md`.
7. **Prune + author pass** — the tester handles every independent area serially, scope the set step 4 analysed:
   - deletes the approved removals; writes the `add` decisions at the chosen level, never re-authoring a `Test-only` Task's tests (strategy and pruning still cover them); fail-first regression proof and test-quality bars per `code-style.md` §17 (SSoT) — the proof recorded in the `Added tests` table
   - **entry 1**: resolves each stub a `plan.md` `Stub <ref> → impl-test` line routes here (`sprint-lifecycle.md` "Plan file format") per `git-strategy.md` "TODO stubs": its TODO resolved in the test file, its `.asd/project/stubs.md` row deleted
   - commits per Conventional Commits; emits COMPLETED
7a. **Memory fix** — before step 8, each hit in `.claude/agent-memory/<owner>/` the tester reports (step 4's leftover-term check, step 9's memory class) goes to its owner's memory-fix dispatch in this entry (`review-policy.md` "Memory-fix dispatch"), never a `D-N`.
8. **Suite gate** — the tester runs the impacted suite, lint/build and records raw results; verdict per `## Rules` suite gate.
9. **Triage** on any failure:
   - **test defect** (bad assertion, wrong fixture, flaky pattern) → re-dispatch step 7 for the offending tests, then step 8 again
   - **code defect** → append a `D-N` row per `t_test-plan.md` `Defects` (`Entry` = this entry, status `pending`); run command `node .asd/runtime.js defect-stalemate --plan <sprint>/test-plan.md` (detection shape only from `external-review.md` "Stalemate detection"). **Stalemate** [GATE hard: stalemate]: two consecutive entries routed the same code-defect identity set — a green entry between them breaks the run; identity is the `Defects` file path, runner failure line and failing test, never `D-N`; `impl-review` rows never count; the command compares deterministically:
     - a **current answer** is a decisions-log stalemate answer naming this `digest` and appended after the log's last routing line naming it; an earlier answer is spent — it covers only the next routing of that set, and the same set repeating after it escalates again
     - `stalemate: true` and no current answer → emit `FAILED: stalemate <digest>` instead of routing and, after an escalation consult, request a hard user decision (`checkpoints.md`): continue test-fix with guidance, accept as debt, or abort; append the answer with the digest to decisions-log, then apply it
     - `stalemate: true` and a current answer → apply it without re-asking: continue → route below, its guidance riding the test-fix payload; accept as debt → set those rows `accepted-debt`, register each failing test in `.asd/project/stubs.md` with an `(accepted-debt)` Reason, re-dispatch step 7 to skip exactly those tests, then step 8 again; abort → ABORT
     - otherwise, or on continue, **route**: fill this entry's `Entry log` `HEAD analysed` with `git rev-parse HEAD`; write `state.json.test_defects_pending = true` inline and append decisions-log "impl-test: defects <D-N list> → impl test-fix (digest <digest>)" (mechanical, no gate); commit these bookkeeping writes (`sprint-lifecycle.md` "Impl-test commits its own output"); emit COMPLETED with `NEXT: impl`
   - **memory defect** (the failure traces to text in `.claude/agent-memory/<owner>/`) → step 7a, then step 8 again
   - several kinds present → fix the test and memory defects first, re-run, then route the remaining code defects back
   - no iteration cap: loop until the impacted set passes; an unfixable state surfaces as a dev/tester `FAILED`, never a silent exit
10. **Green impacted run** — first, the smoke check (`artifact-layout.md` "Test plan" Manual verification): each `Manual verification` row gets it once, at the first entry whose suite gate is green after the row is written — request a user decision (pass / fail, with notes) for each row still lacking a result, record each as `result: <pass|fail> — <notes>` beneath its row (impl-review reads it there), and append one decisions-log line. Then write inline (mechanical, no gate): fill this entry's `Entry log` row `HEAD analysed` with current `git rev-parse HEAD`; append decisions-log "impl-test: impacted set green (<counts>), <added>/<removed> tests"; confirm `test_defects_pending` null; commit these bookkeeping writes (`sprint-lifecycle.md` "Impl-test commits its own output") and the timing ledger — `git status --porcelain` MUST be empty before this step's COMPLETED; emit COMPLETED with `NEXT: impl-review`
11. tester `QUESTION` or FAILED → escalation consult, then `agent-protocol.md` "Relay"; ABORT → relay, halt; no signal → `sprint-lifecycle.md` "State recovery" failed dispatch before re-dispatch
12. On `ADVICE_NEEDED` from the tester → relay per `agent-protocol.md` "Relay"; execution resumes, no halt.

## Rules
- must: select the test approach only after the implementation exists, against the real change surface — never speculatively from the plan, a `Test-only` Task's declared paths included; authoring bar, check-ladder selection, prune criteria, no-new-test decision rule (decision `none` with its reason in `test-plan.md` when no test qualifies) and fail-first regression proof: `code-style.md` §17 (SSoT), binding here.
- must: run the existing impacted tests before selecting anything new (step 3), so the strategy observes actual post-impl behaviour and catches an `impl` regression before any new test is authored. A `Test-only` Task's commits are existing tests in entry 1's change surface: the pre-strategy run covers them, entry 1 never re-authors them.
- must: re-entry — scope strategy and prune to the delta since the prior entry (`Entry log`), not the whole change surface; amend `test-plan.md`, never rewrite it, after rotating the prior entry's narrative rows (`artifact-layout.md` "Test plan").
- must: `Defects` rows persist across entries — a resolved row stays `fixed` for the record; a reappearing defect gets a new `D-N` row, never a reopened one.
- must: record a reason for each in-scope removal; a removal outside the sprint's overall change surface waits for step 6.
- must: suite gate — re-run on every entry, scoped to the impacted set (`sprint-lifecycle.md` "Impacted test set", subject to its safety valve, never the whole repo — the full suite runs only once, at the end of `impl-review`); verdict from the runner's exit code plus report, never an agent's claim; bracket each suite run (steps 3, 8) with a `suite` op. Bounded risk: a defect a fix introduces outside the impacted set's reach is not caught here — impl-review's terminal full suite is the backstop.
- must: triage a failure as a test defect (fix it, re-run), a memory defect (report it, step 7a) or a code defect (a `D-N` row, never a production-code fix).

## Dispatch
```json
[{"agent": "asd-tester", "when": "one fresh instance per entry, steps 3-9", "payload": ["mode", "task", "inputs.context", "inputs", "write_scope", "language", "timing"], "signals": ["COMPLETED", "QUESTION", "ADVICE_NEEDED", "FAILED", "ABORT"]}, {"agent": "<owner> of .claude/agent-memory/<owner>/", "when": "a memory hit (Claude only), step 7a", "payload": ["mode", "task", "work", "inputs.context", "write_scope"], "signals": ["COMPLETED", "FAILED"]}, {"agent": "asd-advisor", "when": "ADVICE_NEEDED relay; escalation consult before a user escalation (agent-protocol.md 'Relay')", "payload": ["turn_budget", "inputs.context", "question, options, context paths"], "signals": []}]
```

## Signals
```json
{"QUESTION": "blocker: AC behaviour genuinely ambiguous, or a Simplicity Default trigger (new test dependency or test infrastructure) needing Complication Approval; step 11", "FAILED": "blocker: test runner broken, tech-reference missing; step 11", "FAILED: stalemate <digest>": "emitted by this workflow at step 9, a hard decision"}
```

## Exit
```json
{"writes": ["<sprint>/test-plan.md, test-plan.entry-NN.md (re-entry)", "tests in repo", ".asd/project/stubs.md (accept-as-debt)", "state.json", "commits", "decisions-log.md"], "next": [{"if": "impacted set green", "next": "impl-review"}, {"if": "code defects route to impl test-fix mode", "next": "impl"}]}
```

## Return
```
PHASE: impl-test | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <impl-review|impl>
```

## References
- `.asd/rules/agent-protocol.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/code-style.md` §17
- `.asd/rules/checkpoints.md`
- `.asd/rules/artifact-layout.md`
- `.asd/rules/git-strategy.md`
- `.asd/rules/language-policy.md`
- Templates: `t_test-plan.md`
