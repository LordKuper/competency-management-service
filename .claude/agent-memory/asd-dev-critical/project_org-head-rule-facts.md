---
name: org-head-rule-facts
description: Task 20 (unit head only from own unit, transfer cascade, head-first order) - dropped create field, picker current-head quirk, Intl.Collator ru yo order, antd 6 select DOM and click pitfalls, CRLF edit behaviour
metadata:
  type: project
---

Task 20 (2026-10-07), EF Core 10.0.12 / antd 6.6.5 / Node 24 ICU, scratch DB + Release API + Vite 5900, all removed:

- **Create no longer has `headEmployeeId`** (field removed from `CreateOrgUnitRequest`, not rejected): a stale client that still sends it gets 201 and the value is dropped silently (System.Text.Json ignores unknown members; verified). Contract change forced openapi + schema + `UnitFormModal` into one commit.
- **Head rule is checked only when the head changes** (`headId != unit.HeadEmployeeId`) inside the tree lock; a unit whose head already sits in another unit can be renamed (200) and its head kept, but setting another foreign head is 400. A transfer clears only the head of the unit the employee leaves (`Id == employee.OrgUnitId && HeadEmployeeId == employee.Id`), not other units they head: a foreign-unit head stays (no silent repair). Race of head-set vs transfer, 10 rounds with two background curls: always 200/200 (set first, then cleared) or 200/400, never a head outside its unit; no deadlock.
- **`EmployeePicker` always lists `current`** (the bound employee) even when the search returns nobody, so the head picker with a search for another unit's person still shows the current head; fine for a consistent head, a legacy foreign head is also listed by name.
- **`Intl.Collator("ru")`** treats yo as ye at primary level: Елисеев < Ёлкин < Ершов; default sensitivity gives Семенов < Семёнов (total order), `{ sensitivity: "base" }` would make them equal and fall back to server order. 10 000 names sort in ~12 ms. `tsconfig` lib is ES2022: no `toSorted`, use `[...a].sort`.
- **antd 6 DOM** (differs from v5 notes): selected value is `.ant-select-content` (title attr = label), popup list is `.ant-select-dropdown-list-holder` (virtual: 7 of 11 options rendered, list opens scrolled to the selected item, so read at scrollTop 0, bottom, 0 and union); `scrollIntoView` before clicking a popup option realigns the popup and the click lands on the modal mask, closing the dialog: click popup options without scrolling.
- **`modal.confirm` over an open `Modal`** (hook from `App.useApp`) stacks above it (z-index 1150) and Cancel/Esc/X resolve through `onCancel`, so a `Promise<boolean>` wrapper keeps the form dialog untouched on cancel.
- **Edit tool keeps CRLF**, the Write tool rewrites a CRLF file as LF (`sed -i 's/$/\r/'` to restore); `autocrlf=true` makes the diff the size of the change either way. A `node -e '...'` patch with backslash template text lost the backslashes: patch scripts go in a `.cjs` via the write tool or use Edit.
- Reset of scratch data between UI runs without touching audit (append-only): `update org_units set head_employee_id = null; delete from employees; delete from org_units;` then the seed.
- At cleanup the user's VS stack (API :5000, Vite :5173) was no longer listening; only my own PIDs were killed (scratch API by listener PID, Vite/Chrome by the script's `taskkill /PID /T`).

**Why:** each decided the design or cost a failed scratch run.
**How to apply:** later changes to unit head rules, employee transfer, EmployeePicker, sorted employee lists, or antd 6 select scripting (see also [[employee-dismissal-cascade-facts]], [[web-cdp-real-backend-smoke-facts]], [[pg-behaviour-facts]]).
