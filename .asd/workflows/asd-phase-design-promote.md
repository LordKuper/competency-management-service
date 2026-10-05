# ASD Workflow: Design Promote

The main phase orchestrator owns decomposition, state and gates inline. Mode follows the frozen `workflow` (`sprint-lifecycle.md` "Workflows"): `standard` promotes approved drafts after design-review; `lite` promotes from the accepted implementation after impl-review, with no draft and no review.

1. Read frozen `workflow`, documents, drafts and audit. `standard`: intersect enabled documents with existing drafts; after a design-block collapse (`sprint-lifecycle.md`) this phase is never dispatched. `lite`: impl-review DoD met (`review-policy.md` "DoD per review phase") and `state.json.phase` advanced from `impl-review`, else `ABORT — precondition not met: impl-review DoD`; scope is the enabled documents. Empty scope is a mechanical no-op: record skipped phase and advance per step 5.
2. Compute decomposition and migration targets. Under `checkpoints.md`, execute an already approved/in-bounds decomposition adaptively with evidence; a new subsystem or material boundary is hard and waits for the user. Record the decision before mutation.
3. For an approved new or a changed subsystem, dispatch `asd-architect` to write it to `docs/architecture/subsystems.md` and its `<id>.md` and create its folders (`sprint-lifecycle.md` "Design-promote phase").
4. Dispatch only applicable domain creators in parallel: BA promotes PRD, Architect promotes ADR/stack/tech references and, only with frozen `documents.c4` true, the diagram per `project.diagram_tool`, UX promotes UX/DESIGN and regenerates its view when changed — when `DESIGN.md` is patched, on Windows, run command `designmd-install` once per session before dispatching `asd-ux`. BA/UX do not run git writes: either returns a rename or deletion of one of its persistent docs as a proposal in its final text; the user approves it (deletion is hard, rename follows `checkpoints.md` "Gate policy"), the main orchestrator runs `git mv`/`git rm` inline, then re-dispatches that creator fresh, before step 5, to update content and inbound links. Any creator `QUESTION` follows `sprint-lifecycle.md`'s `QUESTION` protocol. `lite`: each creator writes or updates its enabled persistent docs from `sprint.md`, `plan.md`, the sprint diff (`<base_branch>...HEAD`) and `audit.md` when present, in place of drafts; with `ux_spec` enabled, run the design-system gate of `asd-phase-design.md` step 7 before dispatching `asd-ux`. Its hard gates stay (`sprint-lifecycle.md` "Workflows"), each approved before its write.
5. Wait for creators; append artifact records and update `state.json` inline. `lite`: commit the promoted docs (`git-strategy.md` "Commits"). Post a non-blocking summary and emit `NEXT: plan` (`standard`) or `NEXT: retro` (`lite`).

Append friction: `F-N` entries to `<sprint>/friction-log.md` per `sprint-lifecycle.md` "Friction log".

## Delegates

- `asd-ba`, `asd-architect`, `asd-ux` only for their domain artifacts

## Return contract

```
PHASE: design-promote | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: <plan|retro>
```

## References

- `.asd/rules/checkpoints.md`
- `.asd/rules/sprint-lifecycle.md`
- `.asd/rules/artifact-layout.md`
- `.asd/rules/git-strategy.md`
