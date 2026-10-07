---
name: full-verification-cycle-facts
description: One-time full verification cycle before impl-test (2026-10-07) - antd toast thenable keeps confirm dialogs open, self-inflicted lockout, stale-edit test must be a real change, 10 500-employee numbers, AdminEmployeeBinding Down
metadata:
  type: project
---

Full cycle (scratch PostgreSQL 18.6 on 35432, Release API, Vite dev 5900, headless Chrome over CDP, all removed) found and fixed two UI defects and measured AC-11:

- **`message.success/error/loading` return a thenable that settles when the toast closes (3 s).** Returning it from an `onOk` chain (`.then(() => message.success(...))`) kept the confirm dialog open with a busy button: 3.4-3.6 s in `useBlockUser` and `useUnitActivation` (employee lifecycle was fine, its `onOk` is an async function that returns nothing). Handlers must use braces. Measure by timing click on the confirm button until `.ant-modal-confirm` is gone.
- **Child unit count on a unit card must not depend on the search**: `UnitTree` passed the number of visible children, so a search showed `Подразделений: 0` next to the real employee count; now `node.children.length`.
- **A "stale version" test needs a real change.** A PUT with values identical to the stored ones issues no UPDATE and does not bump `version`, so a later request with the old `If-Match` succeeds. Bump with a unique value (timestamp in the position or name). After a 412 the dialogs re-key their form from the fresh data (typed edits are dropped by design, message says to check and repeat).
- **Self-inflicted lockout**: wrong-password attempts against the bootstrap administrator (rate-limit test, 12 tries) locked it for 15 minutes; use a throwaway account, or clear with `update users set lockout_end=null, access_failed_count=0`.
- `full` is a reserved word in PostgreSQL: `create database "full"`. A scratch DB reset between runs: `drop database "x" with (force)`; units and employees can be deleted by SQL, only `audit_events` is append-only.
- **AdminEmployeeBinding**: `ef database update <prev> --connection` is a loud 23514 failure while an administrator is bound (history stays at head); after unbinding, Down restores `ck_users_role_employee` and Up drops it again.
- **Numbers (10 500 employees, 1000 units, 3000 accounts, 50 000 journal rows)**: API p95 below 30 ms for tree, unit page, employee search; users list 45-100 ms (one directory lookup per row, about 0.8 ms each, page size 200 = 170 ms). UI on the production build: tree 154 ms, typical unit 40 ms, 1750-employee unit 440 ms, search 265-620 ms with the 300 ms debounce. **One unit holding all 10 500 employees expands in 3.9 s** (script 1.2 s, layout 1.7 s, 53 parallel page requests, API 0.3 s): cost is about 0.37 ms per card, 2 s is reached near 5000 cards in one unit.
- `q=фед` finds nobody for «Фёдоров» (ILIKE and FTS both separate ё and е); `q=федоров` finds him through FTS.
- `Network.requestWillBeSent` logging of the `If-Match` header is the quickest way to see which version a dialog sends.

**Why:** each cost a failed run or hid a defect that only timing or a real backend shows.
**How to apply:** a later verification cycle, any `modal.confirm` with a toast in `onOk`, performance claims for the card hierarchy (see also [[web-cdp-real-backend-smoke-facts]], [[web-org-card-hierarchy-facts]], [[migration-scratch-verification-recipe]]).
