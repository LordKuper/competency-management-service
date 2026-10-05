# ASD Workflow: Scope

The main orchestrator owns this phase inline.

1. Read config and existing active/legacy archived sprints. Obtain raw scope when absent, as a plain chat message, fast-forward the base branch (`git-strategy.md` "Branch"), require a clean tree, create the sprint branch, run the closure write below when `asd-sprint` passed a merged-unclosed sprint, then create the sprint folder. Request user decision on the sprint workflow, `standard` or `lite` (`sprint-lifecycle.md` "Workflows"): hard `workflow choice` gate, never defaulted in either gate mode, asked only here; a scope re-run keeps the frozen value. The answer seeds `{{WORKFLOW}}`; step 2's seeding records it in `gate_decisions` and one decisions-log line.
   **Closure write** (`sprint-lifecycle.md` "PR phase"; the closing sprint is exempt from the one-active rule):
   - `git mv` the closing sprint folder into `.asd/sprints/archived/`; a legacy folder already there is written in place (`sprint-lifecycle.md` "Sprint immutability");
   - in its `state.json` write `pr.state="merged"`, `pr.merge_commit` (`gh pr view <pr.number> --json mergeCommit`; a null `pr` takes `pr.number` from the confirmed number `asd-sprint` carries and writes it too), `phase="done"`, `updated_at`, `archived_at`, and append a `gate_decisions` entry `gate: sprint-closure`, `decision_actor: orchestrator`, its evidence the merge commit; append one entry to its `decisions-log.md`, naming a continue choice `asd-sprint` carries (`decision_actor: user`, "Merged-unclosed");
   - commit those paths as the new branch's first commit.
2. Refine scope into `sprint.md` with stable `AC-N` ids; ask the user only for ambiguity that prevents a concrete scope. Verify every retrospective-derived criterion against current `HEAD` — one asserting host behaviour also against the host docs or a live dispatch — before writing it, and record that verification in `decisions-log.md` per `sprint-lifecycle.md` "Orchestration and adaptive gates". Seed state and decisions log.
2a. **Retro intake** per `sprint-lifecycle.md` "Retro intake" (sole SSoT): run `node .asd/runtime.js retro-candidates --sprints .asd/sprints --backlog .asd/project/retro-backlog.md`, adding `--self-hosting` when `self_hosting: enabled`; verify each candidate at `HEAD`, a row tagged `upstream: true` excepted — it is no candidate, only shown at the step 4 gate; the unresolved rest ride that gate, and an included one becomes an `AC-N` in `sprint.md`.
3. Read `documents.audit`: accept only `auto|always|off`, any other value blocks (`sprint-lifecycle.md` "Orchestration and adaptive gates"); `auto` skips only a complete mechanical scope with no behaviour, contract, migration or gate impact. Freeze the effective boolean into `state.json.documents.audit` (no separate reason field — the rule is deterministic, per `sprint-lifecycle.md`). Reevaluate after an accepted scope expansion.
3a. Seed the remaining state placeholders: `{{USER_GATES}}` from `config.user_gates` (accept only `adaptive|strict`; absent -> `strict`); `{{DOC_PRD}}`/`{{DOC_UX_SPEC}}`/`{{DOC_ADR}}` from normalized `documents.*` (absent group -> all enabled); `{{DOC_C4}}` as the effective diagram (`sprint-lifecycle.md` "Optional documents"). No placeholder literal may survive the write. At the scope gate (step 4) the user may skip an enabled optional document (`prd`/`ux_spec`/`adr`/`c4`) for this sprint only — hard, narrow-only: overwrite its frozen value with `false` and log one decisions-log line "<doc> skipped this sprint by user" (`sprint-lifecycle.md` "Optional documents").
4. The initial scope is hard until it is explicitly accepted, because it establishes authority for the adaptive policy. That gate offers to split unrelated strands, or `AC-N`s changing independent contracts, into consecutive sprints; a split-off part goes to `sprint.md` Out of scope plus one decisions-log line. It also shows each `upstream: true` row of step 2a as an upstream proposal for the framework repo — row id, guardrail, home — with no question; such a row is never an `AC-N`, takes no disposition and is written to no backlog. Afterwards record the accepted user decision and write step 2a's dispositions to `.asd/project/retro-backlog.md` (created from `t_retro-backlog.md` at the first write). A fully specified already-authorized outcome is recorded without inventing another decision.
5. Emit `NEXT: audit`.

Append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log".

## Artefacts

- `.asd/sprints/<NNN-slug>/sprint.md`
- `state.json`, `decisions-log.md`, branch
- `.asd/project/retro-backlog.md` (when intake records a disposition)
- closing sprint's archive move and terminal state (closure write)

## Return contract

```
PHASE: scope | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: audit
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/git-strategy.md`
- `t_sprint.md`, `t_state.json`, `t_decisions-log.md`, `t_retro-backlog.md`
