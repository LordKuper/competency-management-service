# ASD Workflow: PR

Owner: inline orchestrator; dispatches none.

## Entry

```json
{"requires": ["review DoD plus a completed retro (checkpoints.md); a cleanup record is never required"], "modes": {"open": "pr=null, unless the Merged-unclosed lookup finds a PR (sprint-lifecycle.md 'PR phase')", "merge": "pr.state=open, or a dispatch carrying a confirmed MERGED number (asd-sprint's release retry, skipping open mode's DoD gate)"}, "ops": ["context: node .asd/runtime.js context --phase pr --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: open mode closes the pr phase op before step 3's final bookkeeping commit and push, or before step 2's hand-over to merge mode, its ledger committed with step 2's commit, alone when that writes none (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
[".asd/project/config.yaml", "state.json", "plan.md", "reviews", "test-plan.md with its segments (artifact-layout.md 'Test plan')", "<sprint>/retrospective.html", ".asd/project/stubs.md"]
```

## Steps

### Open mode

1. Read the `## Inputs`. Confirm every plan task, AC trace, required review verdict, full-suite record, lint/build record and stub rule; `<sprint>/retrospective.html` is a DoD input and its absence blocks. A failed or missing check blocks. These checks gate PR opening — internal verification, never a `checkpoints.md` gate or user-facing pause:
   - **Tests/lint re-run**: content-scoped, never HEAD-sha equality (HEAD always moves past the recorded sha). Skipped when `git diff --quiet <recorded HEAD>...HEAD -- <code/test/stub pathspec, excluding .asd/sprints/** and .asd/project/**>` is empty, where `<recorded HEAD>` is the sha in test-plan.md's `Suite run` section — the commit impl-review's terminal full-suite step (`sprint-lifecycle.md` "Impacted test set") last verified the full suite at; anything landed since forces a re-run.
   - **Reviews-green source**: `state.json.reviews.impl.wave` equals the wave count (`waves.length`), and every wave node's `verdicts["iter-NN"]` for its highest iteration is satisfied, read from state first; parse review files under `<sprint>/reviews/impl/wave-<K>/iter-NN/` only as an explicit fallback when `state.json` data is missing or stale, or for a user-resolved entry. Satisfied-vs-blocking semantics for each entry, External Review's skip form and legacy values included: `sprint-lifecycle.md` "State recovery".
2. Never open a second PR for `state.branch`: before any write, look its PR up by head branch (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"). A `MERGED` hit writes nothing and continues in merge mode with that number, which there runs only the release. Otherwise, on an `OPEN` hit or none, write `phase=pr` (for self-hosting also bump version and changelog, unless the sprint branch already carries this sprint's bump) and commit it on the sprint branch before any push ("Merged-unclosed"). Then an `OPEN` hit is written as `state.json.pr` and continues in merge mode, whose step 1 publishes it with those commits; no hit composes the PR title/body and continues below.
3. Apply the active policy to publication [GATE adaptive: PR publication]. Adaptive publication needs recorded scope authority, evidence and host permission; otherwise request the user. Open the PR per `git-strategy.md` "PR creation"; a `gh` failure is `FAILED` naming the fix given there. On successful PR creation, write `state.json.pr` and append the decision/log record, then commit both and push the sprint branch before step 4, so the squash merge carries `pr.number` to base (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"). Do not archive or mark done.
4. Emit `NEXT: await-merge`; the active sprint remains at its normal path while the PR is open.

### Merge mode

1. Re-enter from either active or legacy archived path. When `gh pr view <pr.number> --json state` (with `pr` null, the number open mode or `asd-sprint` carries) already reports `MERGED`, go to step 2. Otherwise first confirm the PR head carries open mode step 3's publication, since the squash merge is base's only source of `pr.number` (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"): `git fetch origin <state.branch>`, then `git show origin/<state.branch>:<sprint>/state.json` must hold this `pr.number`. If it does not, commit the local `state.json.pr` write if uncommitted, push the sprint branch and re-confirm; a push failure is `FAILED`. Then merge the sprint PR through `gh` per `git-strategy.md` "Merging a PR"; a `gh` failure is `FAILED` naming the fix ("PR creation"). Confirm the merge landed before proceeding; a PR that did not merge leaves the sprint active.
2. `self_hosting: enabled`: `git fetch origin <git.base_branch>`, then publish the release per `git-strategy.md` "Versioning & Changelog (self-hosting only)", which first confirms a base commit carries this sprint's version bump and CHANGELOG section. Then, every project: write no state, archive move or commit, on any branch; the self-hosting tag and release are a tag ref and a host object, not a branch write (`sprint-lifecycle.md` "PR phase"). Emit `NEXT: done`.

## Exit

```json
{"writes": ["open mode: state.json.pr and decisions-log records", "PR when authorized", "merge mode: self-hosting tag and release"], "next": [{"if": "open mode, PR opened", "next": "await-merge"}, {"if": "merge mode, merged", "next": "done"}, {"if": "blocked or FAILED", "next": "halted"}]}
```

## Return

```
PHASE: pr | SPRINT: <NNN-slug> | STATUS: <pr-open|merged|blocked|aborted> | NEXT: <await-merge|done|halted>
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/git-strategy.md`
- `.asd/rules/artifact-layout.md`
