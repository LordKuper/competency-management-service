# ASD Workflow: Design Review

Orchestration body for the `asd-phase-design-review` skill. Operation-mapping to host tools: `.asd/rules/providers.md`.

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- Whichever of prd.html/ux-spec.html/adr.html the sprint's frozen `documents.*` enabled are present in `<sprint>/design/` (per checkpoints precondition chain). After a design-block collapse (`sprint-lifecycle.md` "Design/design-review/design-promote collapse"), this phase is never dispatched — `state.json.phase` already advanced past it to `design-promote` — so this precondition is never evaluated in that case
- Optional drafts honored: design-md-delta.yaml, c4-full/
- `state.json.phase` advanced from `design`

## Operations used
- read: `.asd/project/config.yaml`, `state.json`, drafts in `<sprint>/design/`, review files
- write to scratch files — the directory `node .asd/runtime.js scratch-dir` prints (`artifact-layout.md` "Scratch directory"): External Review's returned text, for `persist-review` (step 8) — an internal reviewer writes its own return file — and the iteration's draft path list for step 7's `emit-manifest --files` and External Review's list; orchestrator state inline, and the iteration's `snapshot/` draft copies
- request user decision: reviewer questions, escalation on FAIL or iteration cap
- delegate to agent in parallel: reviewers; delegate to agent sequentially: creator autofix; the orchestrator writes state and decisions-log inline
- append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log"
- timing: per `sprint-lifecycle.md` "Operation timing" — a `review-iteration` op from step 4 to step 9's resolution (loop or exit); `dispatch` ops (`<reviewer key> iter-NN`; `external-review` kind for External Review) around step 7's reviewers and around step 9's creator dispatches

## Workflow

1. Read `<sprint>/state.json` — read frozen `documents.prd`/`ux_spec`/`adr`/`c4`. Compute review scope as the **intersection** of (a) frozen `documents.*` enabled and (b) the file actually existing in `<sprint>/design/` — a draft that physically exists but whose flag is disabled (e.g. audit pre-formulated it before this repo's own logic gated that — `sprint-lifecycle.md` "Audit phase") is NOT in scope and is NOT reviewed or counted toward DoD; existence alone never puts a file in scope
2. **No-op path** — a design-block collapse (`sprint-lifecycle.md` "Design/design-review/design-promote collapse") never reaches this phase as a separate dispatch. This step is the defensive fallback for the residual case of an intersected scope somehow empty on direct/explicit re-dispatch of this phase alone: write inline (mechanical, no gate — no user decision requested, `sprint-lifecycle.md` "No-op phase rule"): set `phase=design-review`, append `"design-review"` to `state.json.skipped_phases`, append decisions-log "design-review skipped (no in-scope drafts)"; emit phase COMPLETED with return contract; skip remaining steps
3. Read `.asd/project/config.yaml` (`review.external_review`, `review.iterations_low/medium/high/critical`, `self_hosting`, `language.chat`, `language.docs`); `self_hosting: enabled` adds `--self-hosting` to step 7's `emit-manifest`
3a. Before External Review dispatch or diff assembly, run `node .asd/runtime.js external-preflight --input <path>`, `cachePath` set to the canonical path from `external-review.md` "Detection and negative cache" (sole SSoT — not restated here). Its local-ready result means only executable/auth readiness; model access remains `unknown`. On non-ready — only then — record the transparent availability skip (`external-review.md` "Outcome contract"), still computing step 7's scope-manifest `files[]` for its `Unreviewed files`; after a real authentication/quota/reachability/command failure call `external-record-failure` with the returned fingerprint, the same cache path, and bounded retry data.
4. **Inline** — set phase and increment the review iteration.
5. Compute severity floor for current iteration per `review-policy.md` cumulative-budget algorithm (uses `reviews.design.iteration`)
6. Create folder `<sprint>/reviews/design/iter-NN/` if absent
7. **Parallel dispatch** — every reviewer delegated to as a **fresh agent** each iteration (clean-context dispatch per `review-policy.md`); no reviewer reused across iterations:
   - **Draft list** — the sole home of the design-review iteration snapshot: run `node .asd/runtime.js draft-snapshot --files <step 1's in-scope draft paths, one per line> --out <sprint>/reviews/design/iter-NN/`, adding `--previous <sprint>/reviews/design/iter-(NN-1)/` on iter 2+, and write its stdout to a scratch file. It copies each draft under `<sprint>/reviews/design/iter-NN/snapshot/`, and prints every draft on iter 1, and on iter 2+ only the drafts whose content differs from their previous copy (a missing copy counts as changed). Run it every iteration, latch-skipped reviewers or not.
   - **Emit manifests** — for each internal reviewer not latch-skipped (filter below), run `node .asd/runtime.js emit-manifest --reviewer <name> --phase design-review --files <the draft list above> --out <sprint>/reviews/design/iter-NN/ --custom-rules .asd/project/custom-common-rules.md,.asd/project/custom-design-rules.md`, adding `--snapshot <sprint>/reviews/design/iter-(NN-1)/` on iter 2+. It builds that reviewer's own file list (`review-policy.md` "Reviewer responsibility") and writes `<reviewer>.manifest.json`, plus on iter 2+ its list's `<fingerprint>.diff` against the previous iteration's snapshot copies (none at iter 1, `review-policy.md` "Scope hand-off").
   - **APPROVE latch filter first** (`sprint-lifecycle.md` "APPROVE latch" — sole SSoT for the mechanism): read `state.json.reviews.design.latched`; a reviewer key present there is skipped entirely this iteration — no fresh agent call, no new review file, no ledger gate at step 8 for it. Every internal reviewer is dispatched when not latch-skipped, for any non-empty draft set:
   - `asd-reviewer-documentation`
   - `asd-reviewer-efficiency`
   - `asd-reviewer-correctness` — judges draft correctness on every draft; with no ux-spec/design-system draft in scope, its UI section is n/a; the reviewer still dispatches and is counted toward DoD
   - if `review.external_review=enabled` AND not latch-skipped → `asd-external-review` with phase=`design-review`, scoped to whichever drafts are present. Its `files[]` = the draft list above unioned with the previous iteration's `Unreviewed files` (`external-review.md` "Iteration semantics"), minus the design-review exclusions (`external-review.md` "Phase-scoped payload"), written to a scratch file; step 3a ready → run `emit-manifest --reviewer external --phase design-review --iteration <N> --files <that list> --out <sprint>/reviews/design/iter-NN/`, adding the same `--snapshot` on iter 2+ and, when any are carried over, `--full-files <the carried-over Unreviewed files>`, which keeps them out of the diff so they are read whole: it writes `external.scope.json`, plus its `<fingerprint>.diff` on iter 2+
   - every payload below opens with the header per `providers.md` "Dispatch payload header"; payload to each internal reviewer (latch-skipped reviewers receive no dispatch, hence no payload): its emitted **manifest** path (every id it must cover in its `review-policy.md` coverage ledger, `n/a` authorizations included) and, on iter 2+, that manifest's `.diff` path — the hand-off per `review-policy.md` "Scope hand-off" — iteration N, review output dir `<sprint>/reviews/design/iter-NN/`, its return file `.asd/tmp/<NNN-slug>-design-iter-NN-<reviewer>.return.md` (`review-policy.md` "Coverage ledger" Persistence), severity floor, `language.chat`, `language.docs`; impl-only rubric sections never fire against drafts — the manifest's `n_a` carries that phase gate. Payload carries no authoring rationale, no prior-iteration verdicts; a step-8a re-dispatch additionally carries that reviewer's interrupted-attempt record for this iteration, count and cause rebuilt from `decisions-log.md` (`review-policy.md` "Clean-context review iteration"). Payload to `asd-external-review`: its `external.scope.json` path (plus its diff path on iter 2+; same hand-off), review output dir, severity floor, step 3a's preflight result (its `platform` included), `language.chat`, `language.docs`. For `asd-external-review` on iter ≥ 2, also pass the finding set per `external-review.md` "Stalemate detection"
   - each reviewer returns its findings + complete coverage ledger (file, rule, and section) + verdict as final text per `t_review.md` (or `external-review/t_review-report.md` for external), first-line verdict token `[REVIEW-design-<reviewer>]: ...`, an internal reviewer also writing it verbatim to its return file; **this workflow persists that text to `<sprint>/reviews/design/iter-NN/<reviewer>.md` through `persist-review`** (step 8) — the reviewer itself performs no write of that file, and the commit carrying it also carries any agent memory that reviewer authored (`git-strategy.md` "Commit before review")
8. Wait all REVIEW_DONE; run `node .asd/runtime.js persist-review --phase design --reviewer <key> --in <that reviewer's return file; External Review: its returned text, written to a scratch file> --out-dir <sprint>/reviews/design/iter-NN/` — the sole write of `<reviewer>.md` and `<reviewer>.findings.json`, never re-authored by hand (`review-policy.md` "Coverage ledger" Persistence). Exit 2 writes nothing: no verdict token, or any External Review failure (`external-review.md` "Outcome contract") → step 8a's interrupted path; an internal reviewer's ledger failure earns one transcription, re-run on the transcribed text per `review-policy.md` "Coverage ledger" enforcement — passing, the deviation is appended to `<sprint>/friction-log.md`; any other failure, or a failing re-run, rejects and re-dispatches fresh. A dispatched manifest is never re-stamped, by this workflow either: correcting one re-emits it and re-dispatches fresh (`review-policy.md` "Coverage ledger" immutability).
8a. **Interrupted dispatch** — `review-policy.md` "Interrupted dispatch" is the sole SSoT for the durable record, the escalation and the correlated branch, not restated here; its `<id>` is `iter-NN`. The step header carries no reach; each branch below states its own.
   - Interrupted dispatch — reaches any dispatch, External Review included, per `external-review.md` "Outcome contract": takes the same reject-and-re-dispatch-fresh path as a failed validation above, never that step's transcription branch; a second consecutive interruption on the same manifest → request user decision per that section's Escalation (retry fresh / abort), never a split.
   - Late duplicate return — reaches any replaced dispatch, External Review included, per `review-policy.md` "Late duplicate return" (sole SSoT for the admission test and every action it mandates). Phase bindings: a return delivering after step 9 recorded its replacement's verdict, once admitted, is persisted by step 8's `persist-review` plus `--late` to `<sprint>/reviews/design/iter-NN/<reviewer>.late.md`, linked from `<reviewer>.md`, and carries that section's `verdicts["iter-NN"]`, `latched` and decisions-log writes even though step 9 already ran.
9. Parse first-line tokens from all written reviewer files; write `state.json` `reviews.design.verdicts["iter-NN"]` (keyed `correctness`/`efficiency`/`documentation`/`external`) and `reviews.design.latched` per `sprint-lifecycle.md` "APPROVE latch" — sole SSoT for the every-reviewer-gets-an-entry invariant, the inherited `APPROVE` a latch-skipped reviewer receives and the bare-`APPROVE`-only latch condition; not restated here. Phase bindings: an interrupted attempt contributes no entry, its attempts already logged at step 8a. External Review's availability skip is recorded as `"APPROVE (skipped: <reason>)"` when the phase-supplied preflight returns non-ready; its `external.md` (the skip's `Unreviewed files` line, the `files[]` step 7's scope manifest would carry, included) is persisted through step 8's `persist-review` per `review-policy.md` "Coverage ledger" Persistence, its other duties `external-review.md` "Detection and negative cache"'s, not this step's. Aggregate per `sprint-lifecycle.md` "State recovery" satisfied-vs-blocking semantics, reading `verdicts["iter-NN"]` alone, except per `sprint-lifecycle.md` "State recovery" "User-resolved findings":
   - **Reviewer questions first** — a report carrying `question:` items under `## Escalations` (`review-policy.md` "Gate Verdict Format" question carrier) is verdict-bearing, never interrupted (step 8a): request user decision per item, append the answers to decisions-log, and write each under its item in that reviewer's file per that carrier, passing it with that reviewer's findings to the creator fix dispatch (such a report is at least CONCERNS, per that carrier)
   - **All APPROVE, latched or user-resolved** (`sprint-lifecycle.md` "State recovery" user-resolved findings) → DoD met; apply the adaptive gate policy and append the decision inline; emit phase COMPLETED
   - **Any FAIL** → escalation:
     - parse FAIL findings; group by escalation cause (concept change / new abstraction / scope expansion / contract change)
     - request user decision in `language.chat`: present each FAIL using Complication Approval format from `core.md`; collect decisions
     - External Review's stalemate FAIL (`Stalemate:` block, `external-review.md` "Stalemate detection") → request user decision; options and routing per that section, instead of the accept/override bullets below
     - on override → record that finding resolved (`sprint-lifecycle.md` "State recovery" user-resolved findings), continue
     - on accept → delegate to agent corresponding creator (BA / UX / Architect) to apply approved changes; on creator COMPLETED → loop step 4 (increment iteration)
   - **Only CONCERNS** (no FAIL) → autofix loop:
     - delegate to agent responsible creator(s) with finding list; each autofixes per `review-policy.md` "Autofix vs escalation"
     - on all creator COMPLETED → loop step 4
10. Iteration cap reached (no severity tier has remaining budget for next iter):
   - request user decision: override cap and continue / accept current findings / abort sprint
   - on override → loop step 4 (`reviews.design.iteration` keeps incrementing — not reset; severity floor pinned at `critical`)
   - on accept → record every open finding resolved (`cap-accept`, same home); COMPLETED note "iteration cap reached, user accepted"
   - on abort → emit ABORT
11. Any reviewer or creator FAILED / ABORT → relay, halt; creator `QUESTION` → per `sprint-lifecycle.md`'s `QUESTION` protocol; a reviewer never returns a bare `QUESTION` (step 9)
12. On `ADVICE_NEEDED` from any dispatched agent → relay per `sprint-lifecycle.md`'s `ADVICE_NEEDED` protocol; execution resumes, no halt.

## Artefacts produced
- `<sprint>/reviews/design/iter-NN/documentation.md` (written by this workflow when dispatched, through step 8's `persist-review`, which writes `<reviewer>.findings.json` beside each review file; not written this iteration when latch-skipped — step 7)
- `<sprint>/reviews/design/iter-NN/efficiency.md` (written by this workflow when dispatched; not written this iteration when latch-skipped — step 7)
- `<sprint>/reviews/design/iter-NN/correctness.md` (written by this workflow when dispatched; its UI section may be `n/a: outside phase gate`; not written when latch-skipped)
- `<sprint>/reviews/design/iter-NN/external.md` (when `external_review=enabled` and not latch-skipped; written by this workflow)
- `<sprint>/reviews/design/iter-NN/snapshot/` (step 7, every iteration: in-scope draft copies)
- `<sprint>/reviews/design/iter-NN/<reviewer>.manifest.json` (step 7, per dispatched internal reviewer), `external.scope.json` (step 7, External Review dispatched), plus on iter 2+ one `<fingerprint>.diff` per distinct list
- `<sprint>/reviews/design/iter-NN/<reviewer>.late.md` (only for a late duplicate return admitted at step 8a; linked from that reviewer's `<reviewer>.md`)
- Updated `<sprint>/design/` artifacts after autofix or escalation-approved fixes
- Updated `state.json` (phase, `reviews.design.iteration`, `reviews.design.verdicts`, `reviews.design.latched`)
- decisions-log entry on DoD met, override or reviewer-question answers (step 9)

## Agents delegated to
- 3 internal reviewers (Documentation, Efficiency, Correctness) — parallel, dispatched for any non-empty draft set unless APPROVE-latched (step 7); Correctness's UI section is `n/a: outside phase gate` when no ux-spec/design-system draft is in scope
- External Review — parallel (when enabled and not latch-skipped)
- Creators (BA, UX, Architect) — sequential, only when autofix or escalation requires
- The orchestrator records state and decisions inline; no orchestration agent is dispatched.

## Return contract (single line)
```
PHASE: design-review | SPRINT: <NNN-slug> | ITER: <N> | STATUS: <complete|blocked|aborted> | NEXT: design-promote
```

## References
- `.asd/rules/sprint-lifecycle.md` (design-review phase contract)
- `.asd/rules/review-policy.md` (severity floor, autofix, escalation, gate verdict format, DoD per phase, reviewer authorship)
- `.asd/rules/design-principles.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/language-policy.md`
- Templates: `t_review.md`, `external-review/t_review-report.md`
