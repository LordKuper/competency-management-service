# ASD Workflow: Cleanup

Owner: inline orchestrator; dispatches `asd-advisor` only for the consult before a `FAILED` escalation (`agent-protocol.md` "Relay").

## Entry

```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/", "review DoD met at impl-review (checkpoints.md per-phase preconditions)", "state.json phase advanced from the frozen workflow's predecessor (sprint-lifecycle.md 'Workflows'): impl-review (standard) or design-promote (lite)"], "ops": ["context: node .asd/runtime.js context --phase cleanup --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: a suite op around step 5's test run (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
[".asd/project/config.yaml (git.base_branch, self_hosting)", "state.json frozen cleanup, workflow, skipped_phases", ".asd/project/commands.yaml (build, lint, test)", "sprint diff (<base_branch>...HEAD)", ".asd/project/stubs.md", "<sprint>/decisions-log.md"]
```

## Steps

The rule home is `sprint-lifecycle.md` "Cleanup phase" (reach, evidence bar, write points, re-entry); this file orders the work.

1. Read `<sprint>/state.json`, config and `commands.yaml`; write `state.json` (phase=cleanup) inline.
2. Frozen `cleanup` `false` (an absent snapshot reads `true`, `sprint-lifecycle.md` "Optional documents") is a mechanical no-op: one inline write appends `"cleanup"` to `skipped_phases`, with the line `- YYYY-MM-DD — cleanup skipped: cleanup disabled`; go to step 7.
3. Re-entry decides first, from `decisions-log.md`: a record line → step 7; a checkpoint line without one → discard every uncommitted path outside the bookkeeping files against current `HEAD` (`git restore --source=HEAD --staged --worktree -- <paths>`), keeping the committed removals, go to step 5. Otherwise start: commit a dirty `timing.jsonl` alone (`git-strategy.md` "Commit before review" Timing ledger), then `state.json`, `decisions-log.md` and any other bookkeeping of the orchestrator. `git status --porcelain` then lists any other dirty path → `FAILED` naming it, never committed or discarded. Append `- YYYY-MM-DD — cleanup checkpoint <git rev-parse HEAD>`.
4. Find removal candidates in the sprint diff and the code it left superfluous, within the reach and evidence bar of "Cleanup phase", in its order. Per removal, preserving the file's EOL (`code-style.md` §19): edit, then commit over the named paths in the one-command form — `git add -- <paths> && git diff --cached --check -- <paths> && git commit --only -- <paths>`, subject `chore(cleanup): <what>`, no `ASD-Task` trailer; a lint failure → `git reset -q -- <paths>` and correct. A removed TODO marker deletes its `stubs.md` row in the same commit. No candidate → go to step 6.
5. Verify after the removals: build and lint per `commands.yaml` `build` and `lint`; `self_hosting: enabled` first runs `node "$(git rev-parse --show-toplevel)/.asd/sync.js" --apply <generated-view-path...>` over the views a canon removal made stale, commits them with the manifest ledgers as `chore(cleanup): regenerate views`, then judges `--check` by its report items (every item `current`, `AGENTS.md` `modified-foreign` excepted), never its exit code. Then the impacted tests, the safety valve of `sprint-lifecycle.md` "Impacted test set" applied first. Red (build, lint or tests): diagnose; fix only what a cleanup removal broke, or restore the offending removal's paths from the checkpoint sha and commit that as `chore(cleanup): revert <what>`; still red with every removal reverted → consult `asd-advisor` (`agent-protocol.md` "Relay" Escalation consult), then `FAILED` naming the check.
6. Append the record `- YYYY-MM-DD — cleanup: removed <paths/symbols|none>; reverted <ids|none>; verified <build, lint, tests @sha|n/a: no removals>`.
7. Exit per `## Exit`; its bookkeeping commit is `git-strategy.md` "Commit before review"'s.

## Exit

```json
{"writes": ["removal and revert commits (steps 4, 5)", "self_hosting: regenerated views and manifest ledgers (step 5)", ".asd/project/stubs.md rows of removed TODO markers (step 4)", "state.json (phase=cleanup; skipped_phases on a no-op)", "decisions-log.md"], "next": [{"if": "complete or no-op", "next": "retro"}]}
```

## Return

```
PHASE: cleanup | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: retro
```

## References

- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/git-strategy.md`
- `.asd/rules/agent-protocol.md`
- Templates: `t_decisions-log.md`
