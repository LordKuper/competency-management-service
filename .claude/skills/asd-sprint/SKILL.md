---
# ASD generated. Edit .asd/skills/asd-sprint/SKILL.md. source_digest=sha256:4ad5db219df88083e6144b7310f825f5c0aee9650b856ce638ad4844aa7bdf02 content_digest=sha256:93de79b7db5a274e6b7f0a15df5d50659d24cd9ffbe8ebc370c1a62ef1344e79 asd_version=13.5.0 schema=1
name: asd-sprint
description: "Starts a new ASD sprint or resumes the active one, dispatching the matching asd-phase-* skill and routing phase signals back to the user. Use when the user runs /asd-sprint or asks to start, continue, resume, or work on an ASD sprint."
allowed-tools: "Read Glob Grep Bash AskUserQuestion Skill"
---

Operation mapping: see `.asd/rules/providers.md`.

# ASD Sprint

## Preconditions
- `.asd/project/config.yaml` exists (else: tell user `/asd-init`)
- ≤1 active sprint. A sprint counts as active while `state.json.phase != "done"`, whether its folder lives at `.asd/sprints/<NNN-slug>/` or, legacy, at `.asd/sprints/archived/<NNN-slug>/` (archived before closure by an older workflow version). Exempt: a merged-unclosed sprint (Step 1), which the next sprint's scope archives (`sprint-lifecycle.md` "PR phase").

## Operations used
- Read files / search repo — detect active sprint; read state.json, its frozen workflow definition `.asd/workflows/<workflow>.json` (`sprint-lifecycle.md` "Workflows"), config.yaml, custom-common-rules.md
- Run command — Step 0's `git fetch` and `git merge --ff-only`; `git status`, `git branch --show-current`; `gh pr view`/`gh pr list` (merged-unclosed detection), `git fetch`/`git show`/`git log`/`git ls-remote --tags origin`/`gh release view` (release retry check), `gh pr merge` (a legacy finalize PR only); decisions-log rotation (rename, copy template, commit those paths)
- Request user decision — new-sprint confirm, resume/abort choice, release retry or continue (never free-form scope text)
- Delegate to skill — phase skills, plus `asd-init` per "Skills dispatched"
- No other writes — phase skills and their inline orchestrator own writes

## Workflow

Before a phase-skill delegation below, rotate the decisions log when `.asd/rules/artifact-layout.md` "Decisions log" requires it.

### Step 0: fast-forward the base branch
Before Step 1, fast-forward local `git.base_branch` exactly as `git-strategy.md` "Branch" states: a refusal halts and asks the user; an unreachable remote warns and continues on local state. Rules, workflows and phase skills are read only after this step; this skill's own text and the session-start files are the pre-fetch copy.

### Step 1: detect active sprint
- Search repo for `.asd/sprints/*/state.json` (excluding `archived/`) UNION `.asd/sprints/archived/*/state.json` where `phase != "done"` (legacy archived-non-done shape)
- Each, whatever its `phase`: `gh pr view <pr.number> --json state,mergeCommit` reporting `MERGED` — or, with no `pr.number`, a `MERGED` hit of `gh pr list --head <state.branch> --state all --json number,state,mergeCommit` (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"), its number carried to the closure write — makes it **merged-unclosed**, whatever `pr.state` records (legacy `closure-pending` included) — unless `gh pr list --head chore/finalize-sprint-<NNN-slug> --state all` finds a legacy companion PR: an `OPEN` one is merged per `git-strategy.md` "Merging a PR"; an `OPEN` or `MERGED` one closes the sprint on `git.base_branch`, so it is no longer active and gets no closure write.
- A `CLOSED` head-branch hit counts as no hit. A head-branch lookup that cannot reach `gh` outside `phase="pr"` warns and resumes the sprint ("Merged-unclosed"); any other `gh` failure is FAILED naming the fix for its cause (`git-strategy.md` "PR creation").
- 0 active → new-sprint flow
- 1 active, merged-unclosed, `self_hosting: enabled`, its release commit, `v<asd_version>` tag or release missing → release retry ("Merged-unclosed"): request user decision, each time — retry dispatches `asd-phase-pr` with the confirmed PR number, its merge mode running only the release; continue takes the route below, carrying that choice
- 1 active, merged-unclosed otherwise → new-sprint flow (Step 2A), carrying its path and the merged PR's number detection confirmed, whichever copy or lookup it came from
- 1 active otherwise → resume flow; an `OPEN` head-branch hit changes nothing here, pr open mode adopting it at `phase="pr"` only ("Merged-unclosed")
- >1 → emit FAILED "multiple active sprints found, manual cleanup needed"

### Step 2A: new-sprint flow
1. Read `.asd/project/config.yaml` (confirm init complete)
2. `git status` — if dirty, request user decision: commit / stash / abort
3. Collect scope as a plain chat message; request user decision only to confirm start or abort
4. Delegate to skill `asd-phase-scope`, passing scope text and any merged-unclosed sprint Step 1 carries; its step 1 asks the workflow choice and runs the closure write
5. On COMPLETED → advance per Step 3

### Step 2B: resume flow
1. Read `.asd/sprints/<NNN-slug>/state.json` and its frozen workflow definition
2. Show: sprint id, workflow, current phase, review iteration (`reviews.design.iteration` when phase=`design-review`; when phase=`impl-review`, `wave <K>/<n>` = `reviews.impl.wave`/`waves.length` plus that wave's `iteration`, a legacy flat `reviews.impl` read as wave 1 of 1 — `sprint-lifecycle.md` "Review iteration counters"), last review verdict (if any)
3. Request user decision: resume (default) | re-run current phase | re-run earlier phase | abort sprint. Re-run options offer only phases of the definition's `phases`; under the design-block collapse test (`standard` only, `sprint-lifecycle.md` "Workflows"), neither offers `design`, `design-review` or `design-promote`.
4. Delegate to the matching phase skill. *resume* re-enters `phase`, except `phase="design-promote"` under the collapse test (`standard` only; `sprint-lifecycle.md` "Design/design-review/design-promote collapse"): then dispatch `plan`. *re-run earlier phase* = rollback: its inline state update resets the review state per **rollback reset** in `sprint-lifecycle.md`, reading the definition's `rollback_reset` — `reviews.design`'s counter, `reviews.impl` to its seed wave node — with the severity floors.

### Step 3: phase chain advancement
After any phase skill returns:
- `COMPLETED` → read the phase skill's `NEXT:` field; a target outside the frozen definition's `next[<phase>]` → relay FAILED, halt; else dispatch that phase skill. `NEXT:` is authoritative — follows the definition's `phases` order except the design-block collapse (`audit` returns `NEXT: plan` under the collapse test; `design` returns `NEXT: plan` on its defensive-fallback no-op) and the `impl`/`impl-test`/`impl-review` cycle: `impl` always returns `NEXT: impl-test`; `impl-test` returns `NEXT: impl` on code defects (routes to impl test-fix mode) or `NEXT: impl-review` on a green suite; `impl-review` returns `NEXT: impl` on unresolved findings (routes to impl review-fix mode) or the next phase of `phases` on DoD met; `retro` always returns `NEXT: pr`, on its analysed and its empty-log branch alike. The `pr` phase ends the chain in two steps: open mode returns `NEXT: await-merge` (PR open, sprint at its active path, `phase="pr"` — halt, no further dispatch); a later resume re-enters `pr` in merge mode, which returns `NEXT: done` once the merge is confirmed and, self-hosting, the release published → halt; the next sprint's scope archives it (`sprint-lifecycle.md` "PR phase").
- `FAILED` → relay, halt
- `QUESTION` → relay pending question, halt until reply
- `ABORT — precondition not met` → relay, halt

User may interrupt anytime; asd-sprint re-detects state on next invocation.

## Skills dispatched
Phase skills of the frozen workflow's `phases` (`.asd/workflows/<workflow>.json`), plus `asd-init` sprint-mediated mode for a plan-declared settings change (`asd-phase-impl.md` step 6). No other skill set.

## Return contract (single line)
```
SPRINT: <NNN-slug> | PHASE: <phase> | STATUS: <complete|in-progress|blocked|aborted> | NEXT: <next-phase|await-merge|done|halted-on-question|halted-on-failure>
```

## References
- `.asd/rules/sprint-lifecycle.md` (phase chain, signals, exit criteria)
- `.asd/rules/checkpoints.md` (precondition chain, auto-abort)
- `.asd/rules/core.md` (interaction protocol)
