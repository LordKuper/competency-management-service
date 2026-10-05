---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

<!--
Format rules (parser-critical):
- Overview, Definition of Done — prose only, NO checkboxes
- Checkboxes (- [ ]/- [x]) appear ONLY inside `### Task N:` sections
- Checkboxes in any non-task section break orchestrator task parsing
- Subtask deferred for a manual action stays `- [ ]`, suffixed ` — BLOCKED: MS-N` (see manual-steps.md)
- No test-authoring tasks or subtasks: tests are selected and written in impl-test, after the code exists — the one carve-out is a `Test-only:` task (sprint-lifecycle.md "Plan file format")
- Every task carries a `Material risk:` line, plain text, never a checkbox (see sprint-lifecycle.md "Plan file format")
- A test-only task carries a `Test-only: <test paths or globs>` line, plain text, never a checkbox, directly under the `Material risk` line(s) and ahead of any `Reachability` line; it sits in a wave after every task whose code it covers (same section)
- A task whose value depends on two phases agreeing also carries a `Reachability:` line, under the `Material risk` line(s) and any `Test-only` line; a value crossing a push or merge also names what each interruption point leaves on the receiving branch; absent = no cross-phase dependency, never a fail-closed default (same section)
- A task declaring a project-settings change carries a `Settings change: <key>=<value>[, …]` line, under the `Material risk`, `Test-only` and `Reachability` lines; plan acceptance approves exactly those pairs, and the task sits alone in its wave: wave 1, ahead of any contract-changing task, or a wave after the task adding its key to t_config.yaml (same section)
- `## Dependencies` is required and opens with the wave table impl dispatches from; every task sits in exactly one wave, and no two tasks in one wave touch overlapping paths (same section)
- An orchestrator-only action (naming its execution point) or a tests-only stub (`Stub <ref> → impl-test`) is its own plain-text line outside every `### Task N:` block, never a checkbox (same section)
- The other task decomposition rules: sprint-lifecycle.md "Plan file format"
-->

## Overview
{{what plan covers, prose}}

## Definition of Done
Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.
{{sprint-specific DoD additions, if any — prose, NO checkboxes; omit this line entirely when none}}

### Task 1: {{title}}
Material risk: change: {{short risk class — the edit's own correctness is uncertain}}
Reachability: {{which two phases must agree, on what value, and the point in each where it is written and read; a value crossing a push or merge adds `; interrupted at <point>, <branch> holds <value>` per interruption point — omit this line entirely when the task has no cross-phase dependency}}
- [ ] {{subtask}}
- [ ] {{subtask}}

### Task 2: {{title}}
Material risk: artifact: {{short risk class — verifiable edit in a high-stakes file}}
- [ ] {{subtask}}

### Task 3: {{title}}
Material risk: none
- [ ] {{subtask}}

### Task 4: {{title — tests only, in a wave after the tasks whose code it covers}}
Material risk: none
Test-only: {{test paths or globs}}
- [ ] {{subtask}}

## Risks (optional)
- {{risk}}

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | {{task ids dispatched together in this wave}} |
| 2 | {{task ids}} |

- Task {{N}} depends on Task {{M}} (optional lines, under the table)

## Out of scope (optional)
- {{exclusion}}
