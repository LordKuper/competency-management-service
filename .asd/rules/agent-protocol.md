# Agent Protocol

Orchestrator↔agent communication: signals, dispatch payload, return shape, relay. JSON keys and return fields stay English (`language-policy.md`).

## Signals

| Token | Emitter | Handler |
|---|---|---|
| `COMPLETED` | creator agent; phase workflow — work done, ready for next | dispatching workflow continues; a phase's → `asd-sprint` Step 3 |
| `QUESTION` | creator agent — needs user input. A reviewer never emits it: its question rides its verdict `question:` carrier (`review-policy.md` "Gate Verdict Format") | "Relay" `QUESTION` |
| `ADVICE_NEEDED` | creator agent (BA, UX, Architect, Dev, Tester) on non-gate uncertainty only — an analysis/judgment question, never a HARD-gate approval decision (`core.md` "Autonomy and escalation"). Never a reviewer, never `asd-advisor` | "Relay" `ADVICE_NEEDED`: the relay branch of every `asd-phase-*.md` that dispatches a creator |
| `BLOCKED_MANUAL` | `asd-dev`, `asd-tester` — task needs a human-performed manual action; entry registered in `manual-steps.md` | `asd-phase-impl.md` |
| `FAILED` | any agent except External Review — cannot proceed. `asd-advisor` (its only token): the question is a HARD-gate matter, gate named | dispatching workflow; the advisor's → "Relay" `ADVICE_NEEDED` step 4 |
| `ABORT` | any agent, workflow or skill — a precondition is missing; first line `ABORT — precondition not met: <artefact>` | dispatching workflow; a phase's → `asd-sprint` |
| `REVIEW_DONE` | reviewer, External Review — its return, first line the verdict line (`review-policy.md` "Gate Verdict Format") | review workflow waits for every dispatched reviewer, then persists |

Returns outside the tokens: External Review's `APPROVE (skipped: …)` and `external review interrupted: <cause>` (`external-review.md` "Outcome contract"); impl-test's `FAILED: stalemate <digest>` (`asd-phase-impl-test.md` step 9); `asd-advisor`'s recommendation, free text with no token.

## Dispatch payload

Every `delegate to agent` payload opens with one fenced JSON object; the orchestrator first returns the shell to the repo root.

```json
{"repo_root": "<absolute path>", "sprint": "<NNN-slug>", "phase": "<phase>", "mode": "<mode|null>", "task": ["<Task N|finding id|D-N>"], "inputs": {"context": "<path>", "<key>": "<path|[paths]>"}, "write_scope": ["<glob>"], "turn_budget": null, "answers": [], "advice": null, "consults_left": null}
```

- `inputs`: `sprint-lifecycle.md` "Input sources" keys, resolved by the orchestrator, plus phase-specific keys. `context`, required on every agent dispatch: the bundle `node .asd/runtime.js context --role <agent> --phase <phase> --out <scratch-dir>/<NNN-slug>-<dispatch id, "/" and spaces as "-">.context.md` writes (`providers.md` "Role-scoped context"); a failed build blocks the dispatch. Each `providers.md` "Conditional grants" ref whose condition holds is named in `inputs` beside it. The orchestrator's own bundle goes to `<scratch-dir>/<NNN-slug>-orchestrator-<phase>.context.md` — `<scratch-dir>/orchestrator-scope.context.md` in scope `new` mode, before a slug exists.
- Reading: an agent reads its own definition, `inputs.context` and the payload's other inputs, and opens a rule or workflow file outside its `write_scope` or review manifest only at a section the payload names — never whole. The orchestrator builds its own bundle at each phase entry (`--role orchestrator --phase <phase>`) and reads the workflow it executes whole; any other section it cites it reads with `node .asd/runtime.js section --file <path> --heading <text>`, never the file whole.
- `turn_budget`: reviewer, External Review and advisor only, else `null` — `{"max": <maxTurns>, "report_by": <maxTurns − 5>, "plan": null}`, `max` from that agent's canon frontmatter. `plan` is `{"ledger_by": <maxTurns − 10>}` for an internal reviewer on an impl-review wave listing more than `.asd/runtime.js` `LARGE_WAVE_FILES` files; under it: coverage ledger due by turn `ledger_by`; read the diff file in few large reads sized to the per-call token limit; batch independent reads in one turn; a short complete return beats a thorough unfinished one — short means shallower analysis per file, never fewer ledger rows (`review-policy.md` "Coverage ledger"). Host enforcement: `providers.md` "Dispatch payload header".
- `answers` (`[{"question", "answer"}]`), `advice`, `consults_left`: set on a relay re-dispatch ("Relay").
- Role keys a workflow's dispatch names join the same object. Reviewer: `manifest` (its emitted manifest path), `diff` (its `.diff` path, or `null`), `floor` (severity floor), `iteration`, `interrupted` (its own interrupted-attempt record for this iteration, `{"count", "cause"}`, or `null`) — scope per `review-policy.md` "Clean-context review iteration".

## Return

Line 1: the signal token (`ABORT`: its whole first line; a reviewer: the verdict line). Then one fenced JSON object with that signal's fields. Then an optional Markdown body (audit sections, proposals, recommendation).

| Signal | Object |
|---|---|
| `COMPLETED` | `{"changed": [], "commits": [], "resolved": [], "flagged": [{"choice", "departs_from"}], "memory_index": null}` |
| `QUESTION` | `{"problem", "options": [{"id", "label", "consequence"}], "recommended": {"id", "reason"}, "complication": null}` |
| `ADVICE_NEEDED` | `{"question", "context": []}` |
| `BLOCKED_MANUAL` | `{"task", "manual_step"}` |
| `FAILED` | `{"cause", "fix", "committed": []}` |
| `ABORT` | `{"missing"}` |

- `resolved`: ids closed — AC-N, finding ids, `D-N`. `flagged`: choices the orchestrator should check, `departs_from` naming the earlier decisions-log disposition departed from, or `null`; `[]` means none. `memory_index`: the `MEMORY.md` index line, returned instead of editing a shared `MEMORY.md` in a multi-Task wave.
- `QUESTION` renders `core.md` "User-decision presentation format"; `complication` is `{"what", "why", "justification", "alternatives"}` for a Complication Approval.
- `ADVICE_NEEDED` `context`: paths the advisor reads.
- `FAILED` `committed`: shas of the edits committed before failing.
- Reviewer and External Review returns: `review-policy.md` "Gate Verdict Format" and `external-review.md` "Outcome contract" (verdict line, findings table, one ledger JSON block).

## Relay

Main orchestrator only.

**`ADVICE_NEEDED`** (every relay branch in an `asd-phase-*.md` is this exact sequence):
1. Dispatching phase workflow catches `ADVICE_NEEDED` from a dispatched creator, mid-task.
2. Dispatches `asd-advisor` with the question plus the context paths as given by the consulting agent and its own `inputs.context` — no other context injected.
3. On the advisor's returned recommendation → re-dispatch the consulting agent (`delegate to agent X`, `providers.md`) with its original task context plus the answer in `advice`; no other context injected except `consults_left` (step 6). Not a same-turn resume (no host tool suspends and resumes a dispatched agent mid-execution — `providers.md` has no such operation); it is a fresh dispatch carrying forward the same task.
4. On `asd-advisor` `FAILED` (question turned out to be a HARD gate) → relay that finding to the consulting agent unchanged; the consulting agent then treats it as gate uncertainty per `core.md` "Autonomy and escalation" and returns `QUESTION`.
5. No halt, no user contact, no logged trail — the round-trip is autonomous and intra-phase (consults are not logged, `asd-advisor.md`).
6. Capped at 3 consults per consulting-agent **task** — the dispatching phase workflow owns this counter (the consulting agent does not; each re-dispatch in step 3 is a fresh dispatch and would otherwise reset a self-held count), increments it once per completed advisor round-trip, and does not reset it across the task's re-dispatches. At the cap, the workflow stops relaying further `ADVICE_NEEDED` signals for that task; the agent proceeds on its own judgment or re-classifies the question as gate uncertainty and returns `QUESTION` per `core.md` "Autonomy and escalation".

**`QUESTION`**:
1. The dispatching workflow catches `QUESTION` — the question plus options (`core.md` "User-decision presentation format") — from a dispatched agent.
2. The main orchestrator asks the user via request user decision (free-form input as a plain chat message) and records the answer in `decisions-log.md` before any further work (`core.md` "Context hygiene").
3. Re-dispatches the agent fresh with its original task context plus the answer in `answers` — not a resume, for the reason in `ADVICE_NEEDED` step 3.
