---
name: web-test-harness-quirks
description: web vitest tricks - leave-guard needs a data router, RowActions visible/menu split, OrgUnitSelect has no accessible name, mutation runner method
metadata:
  type: project
---

- `LeaveGuardProvider` uses `useBlocker`, so a page test under it needs `createMemoryRouter` + `RouterProvider`, not `MemoryRouter`; query the dialog by `findByRole("dialog", { name })` (the title text matches twice).
- `RowActions` keeps the first two non-danger actions visible, the rest (danger ones always) in the «…» menu; `MatrixActions` keeps the first two of the whole list visible. Derive expected lists from that, not from guesses.
- `OrgUnitSelect` (TreeSelect) in a column filter has no accessible name and a collapsed tree rendered in a body portal; `UnitTreeFilter` has `aria-label` and expands. Pick the combobox inside `.ant-table-filter-dropdown` and choose a root unit.
- Absence assertions need a positive trace first; where the code leaves none, re-run the action with a working response and assert exactly one dialog and no message.
- Mutation proofs: `git archive HEAD web` into the scratchpad, overlay changed tests, `mklink /J` node_modules, mutate one anchor per run with a node script (assert one hit, restore in `finally`), then remove the junction with `[System.IO.Directory]::Delete(path,$false)` before `rm -rf`. Python heredocs with `\n` inside JS strings break; write scripts with the Write tool.
