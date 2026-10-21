# ASD Workflow: Scope

Owner: inline orchestrator; dispatches none.

## Entry

```json
{"requires": ["no other active sprint; a merged-unclosed sprint asd-sprint passes is exempt (step 1 closure write)"], "modes": {"new": "no sprint folder for this scope yet", "re-entry": "state.json phase=scope"}, "ops": ["context: node .asd/runtime.js context --phase scope --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'"]}
```

## Inputs

```json
["raw scope (plain chat message, obtained when absent)", ".asd/project/config.yaml", "active and legacy archived sprints", "asd-sprint carry: merged-unclosed sprint path and confirmed PR number, update choice, continue choice", "retro-candidates output (step 2a)"]
```

## Steps

1. Read config and existing active/legacy archived sprints. Obtain raw scope when absent, fast-forward the base branch (`git-strategy.md` "Branch"), require a clean tree, create the sprint branch, run the closure write below when `asd-sprint` passed a merged-unclosed sprint, then create the sprint folder and its timing ledger with the scope op (`node .asd/runtime.js timing --ledger <sprint>/timing.jsonl --create --open scope --kind phase`, `sprint-lifecycle.md` "Operation timing"). Request user decision on the sprint workflow, `standard` or `lite` (`sprint-lifecycle.md` "Workflows") [GATE hard: workflow choice]: never defaulted in either gate mode, asked only here; a scope re-run keeps the frozen value. The answer seeds `{{WORKFLOW}}`; step 2's seeding records it in `gate_decisions` and one decisions-log line.
   **Closure write** (`sprint-lifecycle.md` "PR phase"):
   - `git mv` the closing sprint folder into `.asd/sprints/archived/`; a legacy folder already there is written in place (`sprint-lifecycle.md` "Sprint immutability");
   - in its `state.json` write `pr.state="merged"`, `pr.merge_commit` (`gh pr view <pr.number> --json mergeCommit`; a null `pr` takes `pr.number` from the confirmed number `asd-sprint` carries and writes it too), `phase="done"`, `updated_at`, `archived_at`, and append a `gate_decisions` entry `gate: sprint-closure`, `decision_actor: orchestrator`, its evidence the merge commit; append one entry to its `decisions-log.md`, naming a continue choice `asd-sprint` carries (`decision_actor: user`, "Merged-unclosed");
   - commit those paths as the new branch's first commit.

   **ASD update**, when `asd-sprint` carried the *update* choice, after the workflow choice: seed `state.json` with no placeholder left (`phase="scope"`, `workflow`, timestamps, step 3a's values, and `documents.audit` `false` for `off`, else `true` until step 3 re-freezes it) and `decisions-log.md`, recording the choice as in step 2; write `sprint.md` with the raw scope as Goal; run `asd-update` sprint-mediated (its SKILL "Sprint-mediated mode"); `node "$(git rev-parse --show-toplevel)/.asd/sync.js" --apply` over the views `--check` reports stale; commit everything since the closure commit as one `chore(asd): update ASD <old> → <new>`; emit `FAILED` "ASD updated <old> → <new>; restart the session, then run /asd-sprint". A partial update (failed migration, declined `--force`) is reported, then request user decision [GATE hard: abort]: commit what landed and halt with that `FAILED`, or abort.

   **Re-entry**: skip each sub-step already done — fast-forward, clean tree, branch, closure write, folder and ledger, workflow choice, state seed, Goal (it stands as the raw scope) — then continue at step 2.
2. Refine scope into `sprint.md` with stable `AC-N` ids; ask the user only for ambiguity that prevents a concrete scope. Verify every retrospective-derived criterion against current `HEAD` — one asserting host behaviour also against the host docs or a live dispatch — before writing it, and record that verification in `decisions-log.md` per `sprint-lifecycle.md` "Orchestration and adaptive gates". Seed state and decisions log; a carried ASD update choice takes a `gate_decisions` entry `gate: asd update`, `decision_actor: user`, and one decisions-log line.
2a. **Retro intake** — offer open retro rows:
   - Candidates: `node .asd/runtime.js retro-candidates --sprints .asd/sprints --backlog .asd/project/retro-backlog.md`, adding `--self-hosting` iff `self_hosting: enabled` — the retro rows (`sprint-lifecycle.md` "Retro row ids") of the most recent `archived/` sprint with `phase=done` and a `retrospective.html`, minus rows the retro backlog (`artifact-layout.md` "Retro backlog") already disposes, plus its `deferred` rows; `covered by:` rows are dropped. Output: a JSON array of `{row, acts_on, guardrail, home}`; outside `--self-hosting` the latest retro's `asd` rows stay in it, tagged `upstream: true`.
   - A non-zero exit blocks intake: its stderr goes to the user as a scope-gate decision — fix the backlog or retro and re-run, or skip intake this sprint with one decisions-log line. An empty array, or one of only `upstream: true` rows, is a no-op for dispositions: one decisions-log line, no question, no backlog write.
   - A row tagged `upstream: true` is no candidate for disposition: step 4 shows it as an upstream proposal for the framework repo — row id, guardrail, home — with no question; it is never an `AC-N`, takes no disposition and is written to no backlog.
   - Each other candidate is re-verified at `HEAD` per step 2; a resolved one is written `closed` without asking. The rest go to the user inside the step 4 gate, a recommendation each; the user decides all or some: include → an `AC-N` in `sprint.md`, reject → never offered again, undecided → `deferred`.
   - Each row put to the user is shown at that gate before its disposition is asked, with its root cause, its proposed edits (files and change, from the `HEAD` re-verification), and the consequences of including it (cost per `checkpoints.md` "Criterion cost surfacing", risk, saving) and of deferring it, since a bare row id and guardrail is no basis for the decision. An `A-N` row's root cause is that of its `Addresses` F-N in the source retro's Analysis plus the source friction entry's Impact; a `P-N` row has no root-cause field, so the gate states its `Expected saving` and says so, never inventing a cause.
   - Dispositions are written to the backlog on scope acceptance (step 4) and land with the sprint PR; an aborted sprint loses them, so they are offered again.
3. Read `documents.audit`: accept only `auto|always|off`, any other value blocks (`sprint-lifecycle.md` "Orchestration and adaptive gates"); `auto` skips only a complete mechanical scope with no behaviour, contract, migration or gate impact. Freeze the effective boolean into `state.json.documents.audit` (no separate reason field — the rule is deterministic). Reevaluate after an accepted scope expansion.
3a. Seed the remaining state placeholders: `{{USER_GATES}}` from `config.user_gates` (accept only `adaptive|strict`; absent -> `strict`); `{{DOC_PRD}}`/`{{DOC_UX_SPEC}}`/`{{DOC_ADR}}` from normalized `documents.*` (absent group -> all enabled); `{{DOC_C4}}` as the effective diagram (`sprint-lifecycle.md` "Optional documents"). No placeholder literal may survive the write. At the scope gate (step 4) the user may skip an enabled optional document (`prd`/`ux_spec`/`adr`/`c4`) for this sprint only [GATE hard: document skip] — narrow-only: overwrite its frozen value with `false` and log one decisions-log line "<doc> skipped this sprint by user" (`sprint-lifecycle.md` "Optional documents").
4. The initial scope is hard until it is explicitly accepted [GATE hard: scope], because it establishes authority for the adaptive policy. That gate offers to split unrelated strands, or `AC-N`s changing independent contracts, into consecutive sprints; a split-off part goes to `sprint.md` Out of scope plus one decisions-log line. It shows step 2a's `upstream: true` rows and puts its other rows [GATE hard: retro intake dispositions], as step 2a requires. Afterwards record the accepted user decision and write step 2a's dispositions to `.asd/project/retro-backlog.md` (created from `t_retro-backlog.md` at the first write). A fully specified already-authorized outcome is recorded without inventing another decision.
5. Exit per `## Exit`.

## Exit

```json
{"writes": ["<sprint>/sprint.md", "state.json", "decisions-log.md", "timing.jsonl", "sprint branch", "ASD update commit (update choice)", ".asd/project/retro-backlog.md (intake disposition recorded)", "closing sprint's archive move and terminal state (closure write)"], "next": [{"if": "scope accepted", "next": "audit"}]}
```

## Return

```
PHASE: scope | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: audit
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/git-strategy.md`
- `t_sprint.md`, `t_state.json`, `t_decisions-log.md`, `t_retro-backlog.md`
