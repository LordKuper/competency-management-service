# ASD Workflow: Retro

Owner: inline orchestrator; dispatches none.

## Entry

```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/", "review DoD met at impl-review (checkpoints.md per-phase preconditions)", "state.json phase advanced from the frozen workflow's predecessor (sprint-lifecycle.md 'Workflows'): impl-review (standard) or design-promote (lite)"], "ops": ["context: node .asd/runtime.js context --phase retro --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'"]}
```

## Inputs

```json
[".asd/project/config.yaml (language.chat, language.docs)", "<sprint>/friction-log.md", "run record as systemic-class evidence: state.json, plan.md, decisions-log.md with its segments (artifact-layout.md 'Decisions log'), node .asd/runtime.js timing-summary --ledger <sprint>/timing.jsonl --archive .asd/sprints/archived", "the file behind a cited id (manual-steps.md, reviews/<phase>/[wave-<K>/]iter-NN/<reviewer>) only when an F-N cites it"]
```

## Steps

1. Unconditional (never no-op); adds no gate of its own — only `checkpoints.md`'s existing gates apply. Read `.asd/project/config.yaml`; write `state.json` (phase=retro) inline.
2. Read `<sprint>/friction-log.md`. Absent, or present with zero `F-N` entries → **empty-log branch** (step 5).
3. Per `F-N` entry: root cause, and scope (consumer / asd / both). Ids the entry cites (`D-N`, `MS-N`, `reviews/<phase>/[wave-<K>/]iter-NN/<reviewer>`) are read as evidence only — the artefact references ids and never copies what their owner holds (`sprint-lifecycle.md` "Friction log" boundary table).
4. Derive both output classes, then run the findings order; apply and promote nothing.
   - **Two output classes**. Both are split into consumer-project and ASD-framework actions so every row names the side that acts and its home — a finding with both halves takes two rows, an empty half none.
     1. **Remediation** — answers *what went wrong*. Every `F-N` entry analysed to a root cause (step 3); each finding traced to every entry id it addresses. Bounded by the log.
     2. **Systemic proposals** — answers *what would have made this sprint cheaper*, never *what went wrong*. Evidence is how the sprint actually ran (review iterations, rework loops, gate waits, task churn, dispatch cost, the `timing-summary` output), not the entry set: a proposal may cite an `F-N` as supporting evidence, but is neither derived from nor limited by the log. A fact a friction entry already owns is remediation only.
   - **Timing candidates**: each `slow` op and each `user_waits` entry of the `timing-summary` output enters the findings order as a systemic candidate. A slow op's row names what took long, its cause from the run record, the change, and an expected saving drawn from its `excess_seconds`. A user wait is checked for whether its escalation was avoidable (adaptive evidence, a deterministic rule, a decision already authorised); an avoidable one proposes removing that escalation, never speeding up the user, an unavoidable one no row. Raise one `F-N` when the summary's `gaps` list anything but this retro's own still-open phase op. `{"timing": null}` (no ledger) means no timing candidates.
   - **Findings, in order** (both classes; dedup before any drafting):
     1. **Merge** — entries and proposals sharing one root cause become one finding citing every source `F-N`; a finding merged with any `F-N` is remediation.
     2. **Coverage** — search the finding's candidate home (list below) for its subject, reading no other file; a finding an existing rule already covers is dropped, its row citing that rule in place of a guardrail.
     3. **Draft** — each surviving finding gets `Guardrail`, one imperative line stating the prohibition or requirement, and `Home`, finalizing the candidate (a changed home re-runs Coverage (item 2 above)): `custom-coding-rules.md`, `custom-design-rules.md`, `custom-common-rules.md`, `.claude/agent-memory/<agent>/` (Claude-only), a `.asd/rules/` doc when `self_hosting` is enabled, or "upstream ASD" (a framework proposal, never applied; shown at the next scope without a disposition, `asd-phase-scope.md` "Retro intake") when it is not.
5. **Empty-log branch** — record "no friction recorded", skip step 3 and step 4's remediation class only; the systemic class is still derived and run through the findings order. Never invent friction entries; never mutate sprint state to reach this branch. Fragment shape per `t_retrospective.html`'s empty-log comment.
6. Write `<sprint>/retrospective.html` in `language.docs`: `t_retrospective.html` wrapped per `artifact-layout.md` "HTML shell wrapping", `{{DOC_TYPE}}`=`Retrospective`, `{{STATUS}}`=`final`, every other placeholder computed per its "Placeholder fill" table; nothing in it is promoted to a persistent doc or the retro backlog (`asd-phase-scope.md` "Retro intake"). Every Actions and Systemic proposals `<tr>` carries its retro row id (`sprint-lifecycle.md` "Retro row ids"). The duration section, kept on both branches, holds the summary table, the slow set and the user waits, each slow op or avoidable wait naming its cause and linking its `P-N` or `A-N` row; "No timing data" without a ledger.
7. Post the closing `language.chat` summary — problems observed, then systemic proposals, one line each.
8. Append the decisions-log entry (`<sprint>/retrospective.html` written, entry count, proposal count, branch taken); exit per `## Exit`.

## Exit

```json
{"writes": ["<sprint>/retrospective.html (both branches)", "state.json (phase=retro)", "decisions-log.md"], "next": [{"if": "complete", "next": "pr"}]}
```

## Return

```
PHASE: retro | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: pr
```

## References

- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/artifact-layout.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/language-policy.md`
- Templates: `t_retrospective.html`, `t_friction-log.md`
