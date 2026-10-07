---
name: org-employee-search-facts
description: Task 21 (org-structure search by employee) - server q semantics vs highlight, q length 400, matches-mode precedence over name match, debounce/keepPreviousData masking, duplicated useDebouncedValue
metadata:
  type: project
---

Task 21 (2026-10-07), frontend only, minimal checks (lint, build, check:api); no browser run was done:

- **Server `q` is not a plain substring**: `EmployeeEndpoints.Matching` is `ILIKE %q%` on `FullName` OR Russian full-text on `SearchVector` (`plainto_tsquery`, stemmed, words ANDed). A matched employee may not contain the typed text contiguously ("Иванова" finds "Иванов"), so the client `<mark>` only appears when the lower-cased needle is a literal substring of the name or of "position · e-mail"; no highlight otherwise is expected, not a bug.
- **`q` longer than 200 characters is a 400** (`ListQueries.Validate`, `TextSearch.MaxLength`); the search `Input` has `maxLength={SEARCH_TEXT_MAX_LENGTH}` instead of handling the error.
- **A unit with matched employees lists only them ("matches" openness) even when its own name also matches**: otherwise every name-matched unit with a hit would auto-open and fire a full per-unit employee query (hundreds of cards each). Full lists load only for units the user opens by hand, as before. `UnitOpenness` is `closed | path | matches | open`.
- **Debounce masking**: `useEmployeeSearch` queries the debounced trimmed text with `keepPreviousData`, but returns data only while the live trimmed text is still >= 2 characters; otherwise shrinking to one character would show the old matches against the new needle for 300 ms. `isWaiting` (debounce pending or fetching) gates the "Ничего не найдено" message so it never flashes before the answer.
- **`useDebouncedValue` is private in `features/users/EmployeePicker.tsx` and copied into `useEmployeeSearch.ts`** because the task confined edits to `features/org-structure`; hoisting one shared hook is the follow-up.
- A `flipped` toggle made while the employee answer is still in flight is inverted when the answer adds that unit to `pathIds` (flip is relative to the search's own openings); left as is, a window of ~300 ms plus the request.

**Why:** each decided the design or would surprise the next change to the search.
**How to apply:** any later change to org search, `searchUnitTree`, `EmployeeCard` highlighting or the employee list endpoint's `q` (see also [[web-org-card-hierarchy-facts]], [[org-head-rule-facts]]).
