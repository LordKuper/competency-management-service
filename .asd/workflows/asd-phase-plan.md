# ASD Workflow: Plan

Owner: inline orchestrator; dispatches none.

## Entry

```json
{"requires": ["active sprint at .asd/sprints/<NNN-slug>/", "predecessor done per the frozen workflow (sprint-lifecycle.md 'Workflows'): standard — design-promote done (persistent docs reflect approved sprint design) or the collapse test holds (sprint-lifecycle.md 'Design/design-review/design-promote collapse'); lite — audit done or skipped", "state.json phase advanced from design-promote (standard) or audit (lite)"], "ops": ["context: node .asd/runtime.js context --phase plan --role orchestrator at entry, read it; --out per agent-protocol.md 'Dispatch payload'", "append friction: F-N to <sprint>/friction-log.md per sprint-lifecycle.md 'Friction log'", "timing: user-wait ops only (sprint-lifecycle.md 'Operation timing')"]}
```

## Inputs

```json
[".asd/project/config.yaml (language.chat, language.docs, project.subsystem_decomposition)", "state.json frozen workflow, documents.prd, documents.audit", "<sprint>/sprint.md", "requirements: acceptance-criteria source (sprint-lifecycle.md 'Input sources')", "persistent docs referenced: decomposition enabled — docs/architecture/subsystems.md and each touched subsystem's <id>.md, to locate its code; per-subsystem files updated this sprint, plus shared concept.html, stack.html, DESIGN.md, accessibility.html — only whichever exist", ".asd/project/commands.yaml", "lite: <sprint>/audit.md when audit ran", ".asd/project/stubs.md", "t_plan.md"]
```

## Steps

1. Read config and `<sprint>/state.json`.
2. Confirm the predecessor per `## Entry`.
3. Read the remaining `## Inputs`.
4. Write `state.json` (phase=plan) inline, then author `plan.md`:
   - plan.md skeleton first; per-section discussion in `language.chat` per QODDA (`core.md`) + `core.md`'s "Incremental writing"
   - **Stub inclusion step** (before task decomposition):
     - if `documents.audit` enabled: read `<sprint>/audit.md` "Related open stubs" section
     - if `documents.audit` disabled: grep touched-area files (from sprint.md scope) directly against `.asd/project/stubs.md` File:Line column for matches
     - if any found: request user decision per stub [GATE hard: stub disposition]: include resolution this sprint / defer (leave open) / mark accepted-debt; each offer states the stub's verified cost and behaviour change
     - per "include": add explicit `### Task N: Resolve stub <ref>` with owner derived from stub Owner column — except a stub whose files are all tests: it gets no Task and routes to `impl-test` as a plain-text line `Stub <ref> → impl-test` outside every `### Task N:` block, never a checkbox, also recorded in the decisions-log entry below
     - per "accepted-debt": edit stubs.md Reason field prepending `(accepted-debt)` inline after its applicable gate
     - decisions-log entry summarising stub decisions
   - **Task decomposition** per `sprint-lifecycle.md` "Plan file format" (declarations, Wave declaration, Decomposition rules), plus:
     - one Task per coherent unit of work; each Task cites in its body the AC-N it satisfies
     - subtasks as checkboxes inside `### Task N:` block only (parser-critical)
     - no test-authoring Tasks or subtasks, except a `Test-only` Task; note per Task only its `Material risk` declaration, as input for impl-test
     - write the wave table into `## Dependencies`; list non-trivial dependencies under the table
   - **Definition of Done**: reference the standing DoD (`sprint-lifecycle.md` "Plan file format") instead of restating it; author only sprint-specific additions, if any
   - translate skeleton + full draft to `language.docs`, write `<sprint>/plan.md`
5. Apply `checkpoints.md` to the completed plan [GATE write-then-review-accept: plan] and record either user approval (explicit `accept` in strict) or adaptive evidence; append decisions-log entry ("`<sprint>/plan.md` accepted"); exit per `## Exit`.

## Exit

```json
{"writes": ["<sprint>/plan.md", "state.json (phase=plan)", "decisions-log.md"], "next": [{"if": "plan accepted", "next": "impl"}]}
```

## Return

```
PHASE: plan | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: impl
```

## References

- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/language-policy.md` ("Write-then-review-accept: chat-language self-sufficiency")
- `.asd/rules/artifact-layout.md`
- Templates: `t_plan.md`
