---
{
  "name": "asd-sprint",
  "description": "Starts a new ASD sprint or resumes the active one, dispatching the matching asd-phase-* skill and routing phase signals back to the user. Use when the user runs /asd-sprint or asks to start, continue, resume, or work on an ASD sprint.",
  "claude": {
    "allowed-tools": "Read Glob Grep Bash AskUserQuestion Skill Task"
  }
}
---

# ASD Sprint

## Purpose

Start a new sprint or resume the active one: dispatch the matching phase skill, route its signal to the user.

## Preconditions

```json
{"requires": [".asd/project/config.yaml exists", "at most 1 active sprint (Step 1)"], "else": "no config: tell user /asd-init; more than 1 active: Step 1 FAILED", "ops": ["read files: custom-common-rules.md", "run command: git branch --show-current; git fetch, git show, git log, git ls-remote --tags origin, gh release view, gh issue view (release retry check)", "no other writes: phase skills and their inline orchestrator own writes"]}
```

## Steps

### Step 0: fast-forward the base branch
Before Step 1, fast-forward local `git.base_branch` exactly as `git-strategy.md` "Branch" states: a refusal halts and asks the user; an unreachable remote warns and continues on local state. Rules, workflows and phase skills are read only after this step; this skill's own text and the session-start files are the pre-fetch copy.

### Step 1: detect active sprint
- A sprint is active while `state.json.phase != "done"`, its folder at `.asd/sprints/<NNN-slug>/` or, legacy, `.asd/sprints/archived/<NNN-slug>/` (archived before closure by an older workflow version). Search repo for `.asd/sprints/*/state.json` (excluding `archived/`) UNION `.asd/sprints/archived/*/state.json` where `phase != "done"`
- Each, whatever its `phase`: `gh pr view <pr.number> --json state,mergeCommit` reporting `MERGED` — or, with no `pr.number`, a `MERGED` hit of `gh pr list --head <state.branch> --state all --json number,state,mergeCommit` (`sprint-lifecycle.md` "PR phase" "Merged-unclosed"), its number carried to the closure write — makes it **merged-unclosed**, whatever `pr.state` records (legacy `closure-pending` included) — unless `gh pr list --head chore/finalize-sprint-<NNN-slug> --state all` finds a legacy companion PR: an `OPEN` one is merged per `git-strategy.md` "Merging a PR"; an `OPEN` or `MERGED` one closes the sprint on `git.base_branch`, so it is no longer active and gets no closure write.
- A `CLOSED` head-branch hit counts as no hit. A head-branch lookup that cannot reach `gh` outside `phase="pr"` warns and resumes the sprint ("Merged-unclosed"); any other `gh` failure is FAILED naming the fix for its cause (`git-strategy.md` "PR creation").
- 0 active → new-sprint flow
- 1 active, merged-unclosed, `self_hosting: enabled`, its release commit, `v<asd_version>` tag or release missing, or an included issue still open → release retry ("Merged-unclosed"): request user decision, each time — retry dispatches `asd-phase-pr` with the confirmed PR number, its merge mode running only the missing release and issue-close steps; continue takes the route below, carrying that choice
- 1 active, merged-unclosed otherwise → new-sprint flow (Step 2A), carrying its path and the merged PR's number detection confirmed, whichever copy or lookup it came from; exempt from the one-active limit, the next sprint's scope archives it (`sprint-lifecycle.md` "PR phase")
- 1 active otherwise → resume flow; an `OPEN` head-branch hit changes nothing here, pr open mode adopting it at `phase="pr"` only ("Merged-unclosed")
- >1 → emit FAILED "multiple active sprints found, manual cleanup needed"

### Step 2A: new-sprint flow
1. Read `.asd/project/config.yaml` (confirm init complete)
2. `git status` — if dirty, request user decision: commit / stash / abort
2a. Unless `self_hosting: enabled`: `node "$(git rev-parse --show-toplevel)/.asd/skills/asd-update/update.js" --check-version`. `newer: true` → request user decision (hard, `checkpoints.md` "Gate policy"): update to v<remote> now, then start | continue on v<local>; carry the answer to scope. A warning → continue.
3. Collect scope as a plain chat message; request user decision only to confirm start or abort
4. Delegate to skill `asd-phase-scope`, passing scope text, any merged-unclosed sprint Step 1 carries and step 2a's answer; its step 1 asks the workflow choice, runs the closure write and, on *update*, the update
5. On COMPLETED → advance per Step 3

### Step 2B: resume flow
1. Read `.asd/sprints/<NNN-slug>/state.json` and its frozen workflow definition; run `node .asd/runtime.js timing-recover --ledger <sprint>/timing.jsonl`
2. Show: sprint id, workflow, current phase, review iteration (`reviews.design.iteration` when phase=`design-review`; when phase=`impl-review`, `wave <K>/<n>` = `reviews.impl.wave`/`waves.length` plus that wave's `iteration`, a legacy flat `reviews.impl` read as wave 1 of 1 — `sprint-lifecycle.md` "Review iteration counters"), last review verdict (if any)
3. Request user decision: resume (default) | re-run current phase | re-run earlier phase | abort sprint. Re-run options offer only phases of the definition's `phases`; under the design-block collapse test (`standard` only, `sprint-lifecycle.md` "Workflows"), neither offers `design`, `design-review` or `design-promote`.
4. Delegate to the matching phase skill. *resume* re-enters `phase`, except `phase="design-promote"` under the collapse test (`standard` only; `sprint-lifecycle.md` "Design/design-review/design-promote collapse"): then dispatch `plan`. *re-run earlier phase* = rollback: its inline state update resets the review state per **rollback reset** in `sprint-lifecycle.md`, reading the definition's `rollback_reset` — `reviews.design`'s counter, `reviews.impl` to its seed wave node — with the severity floors.

### Step 3: phase chain advancement
After any phase skill returns:
- `COMPLETED` → read the phase skill's `NEXT:` field; a target outside the frozen definition's `next[<phase>]` → relay FAILED, halt; else dispatch that phase skill. `NEXT:` is authoritative — follows the definition's `phases` order except the design-block collapse (`audit` returns `NEXT: plan` under the collapse test; `design` returns `NEXT: plan` on its defensive-fallback no-op) and the `impl`/`impl-test`/`impl-review` cycle: `impl` always returns `NEXT: impl-test`; `impl-test` returns `NEXT: impl` on code defects (routes to impl test-fix mode) or `NEXT: impl-review` on a green suite; `impl-review` returns `NEXT: impl` on unresolved findings (routes to impl review-fix mode) or the next phase of `phases` on DoD met; `cleanup` always returns `NEXT: retro`, run or no-op alike; `retro` always returns `NEXT: pr`, on its analysed and its empty-log branch alike. The `pr` phase ends the chain in two steps: open mode returns `NEXT: await-merge` (PR open, sprint at its active path, `phase="pr"` — halt, no further dispatch); a later resume re-enters `pr` in merge mode, which returns `NEXT: done` once the merge is confirmed and, self-hosting, the release published → halt; the next sprint's scope archives it (`sprint-lifecycle.md` "PR phase").
- `FAILED` → take the escalation consult (`agent-protocol.md` "Relay"), then relay with the Advisor line, halt
- `QUESTION` → take the escalation consult, then relay per `agent-protocol.md` "Relay" `QUESTION` with the Advisor line; halt until the reply, then re-delegate the phase skill
- `ABORT — precondition not met` → relay, halt

## Rules

- must: before a phase-skill delegation, rotate the decisions log when `.asd/rules/artifact-layout.md` "Decisions log" requires it.
- must: phase ops (`sprint-lifecycle.md` "Operation timing"): before each phase-skill delegation run `node .asd/runtime.js timing --ledger <sprint>/timing.jsonl --open <phase> --kind phase`, Step 3 closing the returned phase in the same call (`--close <phase>`); on return close it `done` on `COMPLETED`, `--outcome interrupted` on `FAILED`/`ABORT`; a `QUESTION` halt leaves it open: the relayed question opens a `user-wait` (`gate`: short question label) closed once the answer is processed, and the re-delegation reuses the open phase op (a repeat open is skipped). None for new-sprint scope (its step 1 opens its own), on pr open mode's return (it closed its own), for merge mode (resume at `phase="pr"`, `pr.state="open"`) or the release retry.
- must: re-detect state on every invocation; the user may interrupt anytime.
- never: delegate to a skill outside `## Delegates`.

## Delegates

```json
{"agents": ["asd-advisor"], "skills": ["asd-phase-* of the frozen workflow's phases (.asd/workflows/<workflow>.json)", "asd-init sprint-mediated mode: plan-declared settings change (asd-phase-impl.md step 6)", "asd-update sprint-mediated mode: Step 2A update choice (asd-phase-scope.md step 1)"]}
```

## Outputs

```json
["<sprint>/decisions-log.md rotation (artifact-layout.md Decisions log)", "<sprint>/timing.jsonl phase ops (runtime.js timing)"]
```

## Return

```
SKILL: asd-sprint | SPRINT: <NNN-slug> | PHASE: <phase> | STATUS: <complete|in-progress|blocked|aborted> | NEXT: <next-phase|await-merge|done|halted-on-question|halted-on-failure>
```

## References
- `.asd/rules/sprint-lifecycle.md` (phase chain, exit criteria)
- `.asd/rules/agent-protocol.md` (signals, relay)
- `.asd/rules/checkpoints.md` (precondition chain, auto-abort)
- `.asd/rules/core.md` (interaction protocol)
