# ASD Workflow: Audit

Owner: inline orchestrator; dispatches `asd-architect`, `asd-ba`.

## Entry

```json
{"requires": ["state.json phase advanced from scope"], "ops": ["context: node .asd/runtime.js context --phase audit --role orchestrator at entry, read it; --role <agent> before each agent delegation in ## Dispatch, passed as inputs.context, each Conditional grant whose condition holds named in inputs; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: a dispatch op around each ## Dispatch delegation (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
["state.json frozen workflow and documents.*", "sprint.md", "touched areas: source, every docs/ document bearing on them, other documentation, stubs", "docs/architecture/subsystems.md and touched <id>.md (subsystem_decomposition enabled)", "t_audit.md"]
```

## Steps

1. Read frozen `workflow` and `documents.*`. A false audit is a mechanical skip: go to step 5, whose write carries its record.
2. Dispatch `asd-architect` to audit per `## Rules`, returning all `t_audit.md` sections as text, plus a registry proposal beside them when decomposition is enabled and the registry is absent. When `git ls-files -- <touched areas>` counts more than `.asd/runtime.js` `AUDIT_BATCH_THRESHOLD_FILES`, the payload carries a batched-read plan: grep the touched areas first, then read only the matched sections, never whole files in bulk.
3. Only for source-resolvable ambiguity, dispatch `asd-ba` for the affected product/domain findings. Ambiguity only authority or preference can settle goes straight to the user as a `QUESTION` (step 3c), never to BA (`sprint-lifecycle.md` "Orchestration and adaptive gates"). The orchestrator merges returned text into `audit.md`.
3a. Registry migration (decomposition enabled): request explicit user confirmation per proposed subsystem [GATE hard: new subsystem]; dispatch `asd-architect` to write the registry (with any migrated diagram) and `<id>.md` of confirmed subsystems and backfill missing `<id>.md`, before the audit gate. Then, for a redundant legacy `docs/architecture/c4/` (`project.diagram_tool` other than `likec4`), request user decision [GATE hard: migration deletion] and on approval delete it, its `.gitignore` entries and `commands.yaml` `c4-build` inline; declined keeps it. Record each decision.
3b. Per unsettled `audit.md` "Contradictions" entry — canonical vs canonical, no canonical side, or an undeliverable or conflicting criterion — request user decision before the audit gate [GATE hard: audit contradiction]; record the answer in `decisions-log.md` and in that entry.
3c. Any architect/BA `QUESTION`, and step 3's authority- or preference-only ambiguity → `agent-protocol.md` "Relay" `QUESTION`; architect/BA `ADVICE_NEEDED` → "Relay" `ADVICE_NEEDED`.
4. Apply `checkpoints.md` [GATE adaptive: audit]: in adaptive mode record valid evidence and advance; otherwise present the audit and await the applicable decision. Record actor, evidence and revision.
5. The per-sprint document skip of a frozen-`true` `prd`/`ux_spec`/`adr`/`c4` with no draft yet [GATE hard: document skip] (`sprint-lifecycle.md` "Optional documents") applies here only on user request or inside a decision step 4 already awaits — never a standalone prompt on an adaptive or mechanical exit. Exit in at most one inline mechanical write (no further gate), the only write for any skip; when step 1 skipped audit, it first appends `"audit"` to `skipped_phases` with that skip's decisions-log line; each accepted document skip sets its frozen `false` with its decisions-log line, before the collapse test reads it. Under the design-block collapse test (`standard` only, `sprint-lifecycle.md` "Design/design-review/design-promote collapse"), the write sets `phase="design-promote"`, appends `["design", "design-review", "design-promote"]` to `skipped_phases` and one decisions-log line "design/design-review/design-promote skipped (no documents enabled)". Otherwise it sets `phase="audit"` (only when step 1 skipped audit). Exit per `## Exit`.

## Rules

- `asd-architect` owns the audit; `asd-ba` covers only evidenced material product/domain ambiguity a source can resolve.
- Scans: existing source in touched areas; existing docs in **any format/location** (MD, RST, Confluence/Notion exports, HTML, Wiki, text-extractable PDF, READMEs outside ASD layout); **every** persistent doc in `docs/` bearing on a touched area, never a sample, each listed in `audit.md` "Existing docs found". Also stubs, gaps, risks and migration needs.
- **Contradictions**: a canonical ASD document is a persistent doc at its `artifact-layout.md` path-map location, and under `self_hosting` also a `.asd/rules/` doc. Canonical beats non-canonical, no gate. Canonical vs canonical, or no canonical side, is a user decision (step 3b). `audit.md` "Contradictions" records each with both sources and the winner or the user's answer.
- **Criterion check**: the architect checks every `AC-N` for deliverability as stated — the host, runtime and contract facts it relies on hold — and for consistency with every other criterion. An undeliverable or conflicting criterion is a "Contradictions" entry with no canonical side.
- Material product/domain ambiguity is identified only with evidence, each marked as resolvable from a source or settled only by authority or preference.
- An absent optional section in `audit.md` (`t_audit.md`) means an empty finding set for that check — the check ran and found nothing — never that the check was skipped. BA/Architect omit an optional section entirely when it has no findings; they never emit a mandated placeholder row to signal "none". A check that could not run at all is a `FAILED`/`ABORT` from the responsible agent, not a silently-omitted section.
- Output `audit.md` — findings (touched areas, existing docs/code, gaps, risks) plus **Documentation migration plan** listing found external docs to promote into ASD format. Where sprint scope directly overlaps found content, the agent may pre-formulate reverse-engineered/migrated drafts in `<sprint>/design/` (prd.html / adr.html) — **only for documents whose frozen `documents.*` flag is enabled**; a disabled document's finding stays migration-plan text — with `provenance` + `source` frontmatter; these flow through design and design-review like any draft. Migration items not covered by drafts wait for design-promote.
- **Subsystem registry** (decomposition enabled; `artifact-layout.md` "Subsystem registry"): the architect reads `docs/architecture/subsystems.md` and each touched subsystem's `<id>.md` to locate the code in scope. When the registry is absent, it proposes it — per subsystem: id, purpose, key paths — from an existing C4 registry under `docs/architecture/c4/` (likec4 model or mermaid `subsystems.yaml`), else from code, and writes the registry and the `<id>.md` of each subsystem only once confirmed (step 3a). Migrating from a mermaid `subsystems.yaml` with `project.diagram_tool: mermaid`, that write also carries its diagram — the confirmed subsystems and their relations — into the registry's `## Diagram` (`t_subsystems.md`). A registered subsystem without its `<id>.md` gets it backfilled, no gate (nothing is added).

## Dispatch

```json
[{"agent": "asd-architect", "when": "step 2; step 3a", "payload": ["inputs.context", "inputs.requirements", "inputs.touched_areas", "read_plan (step 2)", "inputs.confirmed_subsystems (step 3a)"], "signals": ["COMPLETED", "QUESTION", "ADVICE_NEEDED", "FAILED", "ABORT"]}, {"agent": "asd-ba", "when": "step 3", "payload": ["inputs.context", "inputs.requirements", "inputs.findings"], "signals": ["COMPLETED", "QUESTION", "ADVICE_NEEDED", "FAILED", "ABORT"]}]
```

## Exit

```json
{"writes": ["<sprint>/audit.md", "state.json (step 5 write)", "decisions-log.md", "registry and <id>.md (step 3a)"], "next": [{"if": "design-block collapse", "next": "plan"}, {"if": "lite", "next": "plan"}, {"if": "otherwise", "next": "design"}]}
```

## Return

```
PHASE: audit | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <design | plan>
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `t_audit.md`
