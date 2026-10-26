# ASD Workflow: Design

Owner: inline orchestrator; runs skill `asd-design-system`; dispatches `asd-advisor` only for an escalation consult (`agent-protocol.md` "Relay").

## Entry

```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/", "audit.md approved, or (frozen documents.audit false) the audit phase COMPLETED signal alone (checkpoints.md precondition chain)", "state.json phase advanced from audit"], "ops": ["context: node .asd/runtime.js context --phase design --role orchestrator at entry, read it; --role asd-advisor before each ## Dispatch delegation, passed as inputs.context; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: a dispatch op around each ## Dispatch delegation (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
[".asd/project/config.yaml (project.diagram_tool, language.chat, language.docs)", "state.json frozen documents.prd, ux_spec, design_system, adr, c4", "<sprint>/sprint.md", "<sprint>/audit.md (when present)", "research (sprint-lifecycle.md 'Input sources')", "existing drafts in <sprint>/design/ (provenance flag)"]
```

## Steps

1. Read `<sprint>/state.json` — frozen `documents.prd`, `documents.ux_spec`, `documents.design_system`, `documents.adr`, `documents.c4`. `documents.c4` already holds the effective diagram computed at `scope` (`sprint-lifecycle.md` "Optional documents"); never recompute it against the live config.
2. **Collapsed no-op path** — defensive fallback for a direct re-dispatch; the audit exit normally collapses first (`sprint-lifecycle.md` "Design/design-review/design-promote collapse"). If `prd`, `ux_spec`, `design_system`, `adr`, and `documents.c4` (already effective) all disabled: perform **one** deterministic check covering all three no-op phases at once — never dispatch `design-review`/`design-promote` as separate steps for this sprint. Write inline (mechanical, no gate, `sprint-lifecycle.md` "No-op phase rule" + "Skip record" multi-phase case), in a single write: set `phase="design-promote"`, append `["design", "design-review", "design-promote"]` to `state.json.skipped_phases`, append **one** decisions-log line "design/design-review/design-promote skipped (no documents enabled)"; emit phase COMPLETED with `NEXT: plan`; skip remaining steps.
3. Read `.asd/project/config.yaml`, `<sprint>/sprint.md`, `audit.md` (if it exists).
4. List existing drafts in `<sprint>/design/` (from audit, with `provenance` flag).
5. Write `state.json` (phase=design) inline (mechanical, no gate).
6. **Step PRD** — only if `prd` enabled (else downstream steps read `sprint.md` instead of `prd.html`): author the sprint PRD draft covering all scope as User stories + Acceptance criteria (plus optional one-line Problem) — Goals/Non-goals omitted entirely, deferred to design-promote's persistent-doc fold — to `<sprint>/design/prd.html` (`t_prd.html`), per "Authoring" in `## Rules` [GATE write-then-review-accept: PRD].
7. **Step Design-system gate** — only if `design_system` enabled (Windows: run command `designmd-install` once per session first), after step 6: search repo for existence of all three: `docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html`
   - if ANY missing → run skill `asd-design-system`; halt until COMPLETED; on FAILED/aborted → escalation consult, relay + halt phase
   - on `asd-design-system` COMPLETED → append decisions-log entry inline, one combined entry naming all three accepted paths ("`docs/ux/DESIGN.md`, `docs/ux/design-system.html`, `docs/ux/accessibility.html` accepted")
   - **token gate** [GATE adaptive: design token] — a missing, new or changed token found while authoring step 8, or with `ux_spec` disabled when the sprint touches UI, is proposed per `design-system.md` §12 "Token decisions"; each approved entry is appended to `<sprint>/design/design-md-delta.yaml` (created on first entry per `t_design-md-delta.yaml`) before a mockup uses it. Separate from the accept below (`checkpoints.md` "Gate mechanics")
8. **Step UX-spec** — only if `ux_spec` enabled, on gate cleared: author flows + UI mockups per `design-system.md` §12 "Mockups" to `<sprint>/design/ux-spec.html` (`t_ux-spec.html`), per "Authoring" in `## Rules` [GATE write-then-review-accept: UX-spec]
9. **Step ADR** — only if `adr` enabled, after step 8: author one+ ADRs for sprint scope (repeated `<article>` blocks), sprint-local numbering (`ADR-1`, `ADR-2`, …), status `proposed`/`accepted` only; optionally name a candidate Fold target per decision; for any new tech, create/update `tech-reference/<tech>-<version>.md` per `artifact-layout.md` "Tech reference docs" and `t_tech-reference.md` (no gate of its own: folded into the gated ADR); to `<sprint>/design/adr.html` (`t_adr.html`), per "Authoring" in `## Rules`, one ADR per section — **one `accept` covers the whole sprint ADR set**, never per-decision, and only once every ADR is written [GATE write-then-review-accept: ADR]
10. **Step c4-full** — only if frozen `documents.c4` (already effective) enabled, after step 9:
    - templates per `project.diagram_tool`: likec4 `t_c4-model.c4`, `t_c4-views.c4`, producing `<sprint>/design/c4-full/model/*.c4`, `views.c4` against `docs/architecture/c4/`; mermaid `t_subsystems.md`, producing `<sprint>/design/c4-full/subsystems.md` against the registry's diagram block
    - author a **delta patch** against the persistent diagram covering sprint scope; the **full schema** instead only when the persistent diagram does not yet exist; never build `dist/` here; write the files directly — `c4-full/` carries no lock-in or approval gate of either class (`checkpoints.md`); post the absolute path(s) + a brief summary in chat, no content dump (`checkpoints.md` link-and-summary rule)
    - if `documents.c4` disabled (`diagram_tool: none`, or `subsystem_decomposition: disabled`) → `c4-full/` omitted
11. On all enabled steps done → write `state.json` (drafts ready) inline; post a non-blocking chat note listing drafts produced and skipped — mechanical, no gate.
12. Exit per `## Exit`.
13. A failure halting the phase (the skill's FAILED) → escalation consult, then relay, halt.

## Rules

- Drafts cover the whole sprint scope in `<sprint>/design/`, one artifact per enabled document only — a disabled document produces no draft, no gate, no dependency on it.
- Order: PRD, design-system gate (blocks UX-spec), UX-spec, ADR, c4-full — each only when enabled.
- Read `research.md` first and research only the technical delta it leaves (`design-principles.md` §11).
- Integrate an existing draft, preserving `provenance` + `source` when present, and write the draft in `language.docs`.
- **Authoring** (`core.md` "Incremental writing"; steps 6, 8, 9): write the template's skeleton to the draft path, then per section draft → write → delta summary in `language.chat` (`language-policy.md` "Write-then-review-accept: chat-language self-sufficiency") → `Lock in` or `Revise this section`, on `Lock in` appending the decisions-log line "`<draft path>` § `<section>` locked"; after the last section, one artifact-level `accept`, on which append a decisions-log entry ("`<draft path>` accepted") and a `gate_decisions` entry (`gate: <document> accept`, `decision_actor: user`). Re-entry: a section is locked in only with its lock line, never re-asked; a section holding content and no `{{` placeholder but no lock line resumes at its pending `Lock in` or `Revise this section`; a draft with every section locked in and no `gate_decisions` entry resumes at the accept.
- Acceptance criteria are atomic, one assertion each; every story cross-references its ACs and every AC its story, none without a story. A requirement dropped, or two sources in conflict, is a user decision before the draft carries either.
- `design-md-delta.yaml` holds proposed DESIGN.md token changes, each approved at the step 7 token gate.

## Dispatch

```json
[{"agent": "asd-advisor", "when": "escalation consult before a user escalation (agent-protocol.md 'Relay')", "payload": ["turn_budget", "inputs.context", "question, options, context paths"], "signals": []}]
```

## Exit

```json
{"writes": ["<sprint>/design/prd.html (documents.prd)", "<sprint>/design/ux-spec.html (documents.ux_spec)", "<sprint>/design/adr.html (documents.adr)", "<sprint>/design/design-md-delta.yaml (documents.design_system, token gap only)", "<sprint>/design/c4-full/ (frozen documents.c4; layout per diagram_tool)", "docs/architecture/tech-reference/<tech>-<version>.md (new tech in ADR)", "docs/ux/DESIGN.md, design-system.html, accessibility.html (via asd-design-system, step 7)", "state.json", "decisions-log.md"], "next": [{"if": "collapsed no-op path (step 2)", "next": "plan"}, {"if": "otherwise", "next": "design-review"}]}
```

## Return

```
PHASE: design | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <design-review | plan>
```

## References

- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/language-policy.md` ("Write-then-review-accept: chat-language self-sufficiency", quote translation)
- `.asd/rules/artifact-layout.md`
- `.asd/rules/design-system.md` §12
- Templates: `t_prd.html`, `t_ux-spec.html`, `t_adr.html`, `t_design-md-delta.yaml`, `t_c4-model.c4`, `t_c4-views.c4`, `t_subsystems.md`, `t_tech-reference.md`
