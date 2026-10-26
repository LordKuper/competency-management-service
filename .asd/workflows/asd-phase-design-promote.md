# ASD Workflow: Design Promote

Owner: inline orchestrator; dispatches none.

## Entry

```json
{"requires": ["standard: design-review done", "lite: impl-review DoD met (review-policy.md 'DoD per review phase') and state.json phase advanced from impl-review, else ABORT — precondition not met: impl-review DoD"], "modes": {"standard": "frozen workflow=standard: promote approved drafts after design-review", "lite": "frozen workflow=lite: promote from the accepted implementation after impl-review, no draft, no review"}, "ops": ["context: node .asd/runtime.js context --phase design-promote --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: user-wait ops only (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
["state.json frozen workflow and documents.*", "standard: approved drafts in <sprint>/design/", "lite: sprint.md, plan.md, sprint diff (<base_branch>...HEAD), audit.md when present", "audit.md"]
```

## Steps

1. Read frozen `workflow`, documents, drafts and audit. Scope: `standard` — enabled documents intersected with existing drafts; `lite` — the enabled documents. Empty scope is a mechanical no-op: record skipped phase and advance per step 5.
2. Compute decomposition and migration targets. Under `checkpoints.md`, execute an already approved/in-bounds decomposition adaptively with evidence [GATE adaptive: decomposition]; a new subsystem or material boundary is hard and waits for the user [GATE hard: new subsystem]. Record the decision before mutation.
3. For an approved new or a changed subsystem, write it to `docs/architecture/subsystems.md` and its `<id>.md` and create its folders (`sprint-lifecycle.md` "Design-promote phase"; `artifact-layout.md` "Subsystem registry").
4. Promote each in-scope document inline per "Design-promote phase" step 4: PRD; ADR, stack and tech references and, only with frozen `documents.c4` true, the diagram per `project.diagram_tool`; the ux-spec under frozen `ux_spec`; under frozen `design_system`, `DESIGN.md` patched from `design-md-delta.yaml`, `design-system.html` and `accessibility.html` — on Windows, run command `designmd-install` once per session before patching `DESIGN.md`. A rename or deletion of a persistent doc needs the user's approval first [GATE hard: doc deletion] [GATE adaptive: doc rename]; then the orchestrator runs `git mv`/`git rm` inline and updates content and inbound links before step 5. `lite`: write each enabled persistent doc from the `lite` inputs in place of drafts; with `design_system` enabled, run the design-system gate of `asd-phase-design.md` step 7 before the ux write. Its hard gates stay (`sprint-lifecycle.md` "Workflows"), each approved before its write.
5. Append artifact records and update `state.json` inline. `lite`: commit the promoted docs (`git-strategy.md` "Commits"). Post a non-blocking summary; exit per `## Exit`.

## Exit

```json
{"writes": ["persistent docs per enabled document", "docs/architecture/subsystems.md, <id>.md and folders (step 3)", "state.json", "artifact records", "lite: promoted-docs commit"], "next": [{"if": "standard", "next": "plan"}, {"if": "lite", "next": "cleanup"}]}
```

## Return

```
PHASE: design-promote | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <plan|cleanup>
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/artifact-layout.md`
- `.asd/rules/git-strategy.md`
