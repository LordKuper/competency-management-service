# ASD Workflow: PR

The main orchestrator owns this workflow and delegates no orchestration role.

Append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log".

## Open mode

1. Read config, state, plan, reviews, test-plan with its segments (`artifact-layout.md` "Test plan"), retrospective and stubs. Confirm every plan task, AC trace, required review verdict (reviews-green over every impl-review wave per `sprint-lifecycle.md` "PR phase"; satisfied per its "State recovery", External Review's skip form and legacy values included), full-suite record, lint/build record and stub rule; `pr` requires review DoD plus a completed `retro` (`checkpoints.md`), so `<sprint>/retrospective.html` is a DoD input and its absence blocks. Re-run required checks after a relevant diff. A failed or missing check blocks.
2. Never open a second PR for `state.branch`: before any write, look its PR up by head branch (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"). A `MERGED` hit writes nothing and continues in merge mode with that number, which there runs only the release. Otherwise, on an `OPEN` hit or none, write `phase=pr` (for self-hosting also bump version and changelog, unless the sprint branch already carries this sprint's bump) and commit it on the sprint branch before any push ("Merged-unclosed"). Then an `OPEN` hit is written as `state.json.pr` and continues in merge mode, whose step 1 publishes it with those commits; no hit composes the PR title/body and continues below.
3. Apply the active policy to publication. Adaptive publication needs recorded scope authority, evidence and host permission; otherwise request the user. Open the PR per `git-strategy.md` "PR creation"; a `gh` failure is `FAILED` naming the fix given there. On successful PR creation, write `state.json.pr` and append the decision/log record, then commit both and push the sprint branch before step 4, so the squash merge carries `pr.number` to base (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"). Do not archive or mark done.
4. Emit `NEXT: await-merge`; the active sprint remains at its normal path while the PR is open.

## Merge mode

1. Re-enter from either active or legacy archived path. When `gh pr view <pr.number> --json state` (with `pr` null, the number open mode or `asd-sprint` carries) already reports `MERGED`, go to step 2. Otherwise first confirm the PR head carries open mode step 3's publication, since the squash merge is base's only source of `pr.number` (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"): `git fetch origin <state.branch>`, then `git show origin/<state.branch>:<sprint>/state.json` must hold this `pr.number`. If it does not, commit the local `state.json.pr` write if uncommitted, push the sprint branch and re-confirm; a push failure is `FAILED`. Then merge the sprint PR through `gh` per `git-strategy.md` "Merging a PR"; a `gh` failure is `FAILED` naming the fix ("PR creation"). Confirm the merge landed before proceeding; a PR that did not merge leaves the sprint active.
2. `self_hosting: enabled`: `git fetch origin <git.base_branch>`, then publish the release per `git-strategy.md` "Versioning & Changelog (self-hosting only)", which first confirms a base commit carries this sprint's version bump and CHANGELOG section. Then, every project: write no state, archive move or commit, on any branch; the self-hosting tag and release are a tag ref and a host object, not a branch write (`sprint-lifecycle.md` "PR phase"). Emit `NEXT: done`.

## Artefacts

- `state.json.pr` and decisions-log records (open mode)
- PR when authorized
- self-hosting tag and release (merge mode)

## Return contract

```
PHASE: pr | SPRINT: <NNN-slug> | STATUS: <pr-open|merged|blocked|aborted> | NEXT: <await-merge|done|halted>
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/git-strategy.md`
- `.asd/rules/artifact-layout.md`
