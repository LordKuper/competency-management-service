---
name: org-employee-search-facts
description: Org-structure search by employee - server q semantics vs highlight, q length 200, path-only opening of searched units, absolute open/close choices, debounce/keepPreviousData masking, the one shared useDebouncedValue
metadata:
  type: project
---

Frontend only; facts from building and then fixing the search, not derivable from the code:

- **Server `q` is not a plain substring**: `EmployeeEndpoints.Matching` is `ILIKE %q%` on `FullName` OR Russian full-text on `SearchVector` (`plainto_tsquery`, stemmed, words ANDed). A matched employee may not contain the typed text contiguously ("Иванова" finds "Иванов"), so the client `<mark>` only appears when the lower-cased needle is a literal substring of the name or of "position · e-mail"; no highlight otherwise is expected, not a bug.
- **`q` longer than 200 characters is a 400** (`ListQueries.Validate`, `TextSearch.MaxLength`); the search `Input` has `maxLength={SEARCH_TEXT_MAX_LENGTH}` instead of handling the error.
- **A search opens a unit only as a path** (`UnitOpenness` is `closed | path | open`): a unit on the way to a match, or holding matched employees, lists its child units and only the employees the search found in it, whether or not its own name also matches. The full per-unit employee query (every page) runs only for units the user opened by hand: a name-matched unit that opened fully fired a set of requests per matching unit on a search for a common word.
- **The user's open/close choice during a search is absolute, per unit** (`chosen` in `useUnitExpansion`), never a flip against the search's own openings: a flip is inverted when a late employee answer adds that unit to the path.
- **Debounce masking**: `useEmployeeSearch` queries the debounced trimmed text with `keepPreviousData`, but returns data only while the live trimmed text is still >= 2 characters; otherwise shrinking to one character would show the old matches against the new needle for 300 ms. `isWaiting` (debounce pending or fetching) gates the "Ничего не найдено" message so it never flashes before the answer; a one-character text is never sent (`isTooShort`), and with no unit match the page says the minimum length instead of "nothing found".
- `useDebouncedValue` and `SEARCH_DEBOUNCE_MS` live once in `web/src/app/useDebouncedValue.ts`; the search hook and the employee picker both import it.

**Why:** each decided the design or would surprise the next change to the search.
**How to apply:** any later change to org search, `searchUnitTree`, `opennessByUnit`, `EmployeeCard` highlighting or the employee list endpoint's `q` (see also [[web-org-card-hierarchy-facts]], [[org-head-rule-facts]]).
