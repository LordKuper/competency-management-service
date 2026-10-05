# ASD Workflow: Design

Orchestration body for the `asd-phase-design` skill. Operation-mapping to host tools: `.asd/rules/providers.md`.

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- `audit.md` approved, OR (frozen `documents.audit` false) audit phase COMPLETED signal alone (per checkpoints precondition chain)
- `state.json.phase` advanced from `audit`

## Operations used
- read: `.asd/project/config.yaml`, `state.json`, `sprint.md`, `audit.md`, audit-produced reverse/migrated drafts in `<sprint>/design/`
- search repo: design-system gate — existence of `docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html`
- write a file: `state.json` inline, mechanical non-gate writes at steps 2, 5, 11 (`sprint-lifecycle.md` "State recovery"); decisions-log inline, per-artifact entries at steps 6/7/8/9, triggered by that artifact's `accept`
- request user decision: the accept loops at steps 6/8/9, step 8's token gate, creator `QUESTION`s (step 13), phase-level escalation
- delegate to agent, sequential: BA, UX, Architect, optional Architect (c4-full)
- dispatch skill `asd-design-system` when gate detects missing files
- append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log"

## Workflow

1. Read `<sprint>/state.json` — frozen `documents.prd`, `documents.ux_spec`, `documents.adr`, `documents.c4`. `state.json.documents.c4` already holds the effective diagram computed once at `scope` (`sprint-lifecycle.md` "Optional documents"); read it as-is here, never recompute against the live config — a mid-sprint `diagram_tool` or `subsystem_decomposition` edit must not change this sprint's preconditions.
2. **Collapsed no-op path** — defensive fallback for a direct re-dispatch; the audit exit normally collapses first (`sprint-lifecycle.md` "Design/design-review/design-promote collapse"). If `prd`, `ux_spec`, `adr`, and `documents.c4` (already effective) all disabled: perform **one** deterministic check covering all three no-op phases at once — never dispatch `design-review`/`design-promote` as separate steps for this sprint. Write inline (mechanical, no gate — no user decision requested, `sprint-lifecycle.md` "No-op phase rule" + "Skip record" multi-phase case), in a single write: set `phase="design-promote"` (the last of the three collapsed phases, so the next phase in the `standard` definition's `phases` is `plan` and a resumed session cannot re-enter this block), append `["design", "design-review", "design-promote"]` to `state.json.skipped_phases`, append **one** decisions-log line "design/design-review/design-promote skipped (no documents enabled)"; emit phase COMPLETED with `NEXT: plan` (return contract below covers both paths); skip remaining steps
3. Read `.asd/project/config.yaml` (`project.diagram_tool`, `language.chat`, `language.docs`); read `<sprint>/sprint.md`, `audit.md` (if it exists)
4. List existing drafts in `<sprint>/design/` (from audit, with `provenance` flag)
5. Write `state.json` (phase=design) inline (mechanical, no gate)
6. **Step PRD** — only if `prd` enabled: delegate to agent `asd-ba`:
   - inputs: sprint.md, audit.md (if present), existing prd draft if any, `language.chat`, `language.docs`; template `t_prd.html`
   - instruction: integrate existing draft (preserve `provenance` + `source` if present); author sprint PRD draft covering all scope as User stories + Acceptance criteria (plus optional one-line Problem) — Goals/Non-goals omitted entirely, deferred to design-promote's persistent-doc fold; translate to `language.docs`, write `<sprint>/design/prd.html`; emit COMPLETED with a per-section delta summary in `language.chat`
   - **accept loop** (orchestrator; **write-then-review-accept**, `checkpoints.md` mechanic): post the absolute path + that summary, discuss sections with the user in `language.chat`; on requested changes → re-dispatch the creator fresh with its original task plus the feedback; loop until explicit `accept`
   - on `accept` → workflow appends decisions-log entry inline ("`<sprint>/design/prd.html` accepted")
   - if `prd` disabled → skip to step 7 (downstream steps read `sprint.md` directly instead of `prd.html`)
7. **Step Design-system gate** — only if `ux_spec` enabled: on PRD step done → search repo for existence of all three: `docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html`
   - if ANY missing → dispatch skill `asd-design-system`; halt until COMPLETED; on FAILED/aborted → relay + halt phase
   - on `asd-design-system` COMPLETED → workflow appends decisions-log entry inline, one combined entry naming all three accepted paths ("`docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html` accepted")
   - if all present, or `ux_spec` disabled → Step 8
8. **Step UX-spec** — only if `ux_spec` enabled: on gate cleared → delegate to agent `asd-ux`:
   - before dispatch: on Windows, run command `designmd-install` once per session before dispatching `asd-ux`
   - inputs: prd.html if enabled else sprint.md, audit.md (if present), existing ux-spec draft if any, current `docs/ux/DESIGN.md`, `design-system.html`, `accessibility.html`; templates `t_ux-spec.html`, `t_design-md-delta.yaml`
   - instruction: integrate existing draft; author flows + UI mockups using ONLY existing DESIGN.md tokens; when a needed token is missing/must change, return `QUESTION` proposing the token add/update; once re-dispatched with the approval, append the entry to `<sprint>/design/design-md-delta.yaml` (create on first entry per `t_design-md-delta.yaml`), THEN continue mockup referencing the new token; translate + write `<sprint>/design/ux-spec.html`; emit COMPLETED with a per-section delta summary in `language.chat` (delta file produced inline iff a token gap surfaced — else omitted)
   - **token gate** (orchestrator): UX's token `QUESTION` is approve-before-write (`checkpoints.md` "Gate mechanics"), separate from the accept loop below, handled per `sprint-lifecycle.md`'s `QUESTION` protocol
   - accept loop as step 6
   - on `accept` → workflow appends decisions-log entry inline ("`<sprint>/design/ux-spec.html` accepted")
   - if `ux_spec` disabled → skip to step 9
9. **Step ADR** — only if `adr` enabled: on UX step done → delegate to agent `asd-architect`:
   - inputs: whichever of prd.html/ux-spec.html exist, else sprint.md; audit.md (if present); existing adr draft if any, `docs/architecture/stack.html`, persistent `docs/` (to identify likely fold targets by `responsibility.owns`), `tech-reference/`; template `t_adr.html`
   - instruction: integrate existing draft; author one+ ADRs for sprint scope (repeated `<article>` blocks), sprint-local numbering (`ADR-1`, `ADR-2`, …), status `proposed`/`accepted` only; optionally name a candidate Fold target per decision; for any new tech, create/update `tech-reference/<tech>-<version>.md` via fetch-external-doc-by-URL + `t_tech-reference.md` (this sub-write carries no gate of its own — it is source material folded into the ADR, which itself carries the accept loop below); translate + write `<sprint>/design/adr.html`; emit COMPLETED with a per-decision delta summary in `language.chat`
   - accept loop as step 6 — **one `accept` covers the whole sprint ADR set**, never per-decision
   - on `accept` → workflow appends decisions-log entry inline ("`<sprint>/design/adr.html` accepted")
   - if `adr` disabled → skip to step 10
10. **Step c4-full** — only if frozen `documents.c4` (already effective, from `state.json`) enabled: on ADR step done → delegate to agent `asd-architect`
    - inputs: whichever design drafts exist, `docs/architecture/stack.html`, `docs/architecture/subsystems.md`, persistent diagram (diff target: likec4 `docs/architecture/c4/`, mermaid the registry's diagram block), sprint.md; ADR not required
    - templates per `project.diagram_tool`:
      - likec4: `t_c4-model.c4`, `t_c4-views.c4`; produce `<sprint>/design/c4-full/model/*.c4`, `views.c4` — never build `dist/` here (generated output no reviewer sees, `external-review.md`)
      - mermaid: `t_subsystems.md`; produce `<sprint>/design/c4-full/subsystems.md`
    - instruction: author a **delta patch** against the persistent diagram covering sprint scope; author the **full schema** instead only when the persistent diagram does not yet exist; write the files directly — `c4-full/` carries no approval gate of any kind (neither class, `checkpoints.md`), so there is no discuss/approve step here; post the absolute path(s) + a brief summary in chat (still no content dumps — `checkpoints.md`'s link-and-summary rule applies even without a gate); emit COMPLETED
    - if `documents.c4` disabled → skip
11. On all enabled steps COMPLETED → write `state.json` (drafts ready) inline; post a non-blocking rollup SUMMARY chat note listing drafts produced and skipped (informational only — per-artifact decisions-log entries already recorded at each step's `accept`, steps 6/7/8/9) — mechanical, no gate
12. Emit phase COMPLETED with return contract
13. Any creator `QUESTION` → per `sprint-lifecycle.md`'s `QUESTION` protocol
14. Any creator FAILED / ABORT → relay, halt
15. On `ADVICE_NEEDED` from any dispatched agent → relay per `sprint-lifecycle.md`'s `ADVICE_NEEDED` protocol; execution resumes, no halt.

## Artefacts produced
- `<sprint>/design/prd.html` (if `documents.prd` enabled)
- `<sprint>/design/ux-spec.html` (if `documents.ux_spec` enabled)
- `<sprint>/design/adr.html` (if `documents.adr` enabled)
- `<sprint>/design/design-md-delta.yaml` (optional, produced inline by UX-spec step iff token gaps surfaced)
- `<sprint>/design/c4-full/` (if frozen `documents.c4` enabled; layout per `diagram_tool`)
- New/updated `docs/architecture/tech-reference/<tech>-<version>.md` (when new tech in ADR)

Indirect (via design-system gate): `docs/ux/DESIGN.md`, `design-system.html`, `accessibility.html` (when gate dispatches `asd-design-system`).

## Agents delegated to
- State and decision-log writes are inline; artifact gates follow `checkpoints.md`.
- `asd-ba` (PRD)
- `asd-ux` (UX-spec; inline delta)
- `asd-architect` (ADR; optional c4-full; tech-reference)

## Skills/workflows dispatched
- `asd-design-system` (only when gate detects missing DESIGN.md / design-system.html / accessibility.html)

## Return contract (single line)
```
PHASE: design | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <design-review | plan>
```
`NEXT: plan` on the collapsed no-op path (step 2); `NEXT: design-review` otherwise.

## References
- `.asd/rules/sprint-lifecycle.md` (design phase contract, in-phase precondition chain)
- `.asd/rules/checkpoints.md` (per-artifact approval)
- `.asd/rules/language-policy.md` ("Write-then-review-accept: chat-language self-sufficiency", quote translation)
- `.asd/rules/artifact-layout.md` (sprint design folder, provenance, c4 mode layouts)
- Templates: `t_prd.html`, `t_ux-spec.html`, `t_adr.html`, `t_design-md-delta.yaml`, `t_c4-model.c4`, `t_c4-views.c4`, `t_subsystems.md`, `t_tech-reference.md`
