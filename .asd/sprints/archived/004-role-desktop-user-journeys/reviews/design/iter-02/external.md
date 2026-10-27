[REVIEW-design-external]: CONCERNS

# External Review Report

- **Phase**: design-review
- **Iteration**: 2
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | .asd/sprints/004-role-desktop-user-journeys/design/adr.html:648 (publication preview endpoint) with AC-34.5 | Creating a record directly as active can publish draft dependencies. The preview endpoint `GET .../{id}/publication` needs an existing record ID, and confirmation covers only approve/activate. AC-34.5 has no creation flow and the UX spec has no matching mockup. | Define preview and confirmation for unsaved records, including a UX mockup, before a creation with a cascade runs. |
| 2 | medium | .asd/sprints/004-role-desktop-user-journeys/design/design-md-delta.yaml:23 | `update` for `components.step-current` sets `new_value: "unchanged"`, a string, while the component is a mapping. Applied literally, this would replace the component definition even though `breaking: false`. | Give the existing component mapping as `new_value`, keeping its token references. Alternatively, express the note-only change in a form the delta spec allows. |
| 3 | medium | .asd/sprints/004-role-desktop-user-journeys/design/adr.html:356 (`desk` limit) | The fixed 2,000-row cap on a single reference group makes the tail of larger groups unreachable. This contradicts the ADR's own reachability intent, since no pagination or continuation reaches the remaining rows. | Cap each response and add pagination or continuation that reaches every item, or state explicitly that the cap is a documented ceiling. |

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none

## Prior iteration status (stalemate check)

- P1 (ux-spec m-review): resolved
- P2 (adr-3-decision): resolved
- P3 (adr-4-decision): resolved
- P4 (ux-spec m-drafts): resolved
- P5 (prd AC-30.4): resolved

All five prior findings are fixed. The three kept findings are new, so this is not a stalemate.

## Verdict
CONCERNS: 3

## Next action
The creator should fix F1 first by adding preview and confirmation for create-as-active, with a UX mockup, in the PRD, UX spec and ADR. Then fix F2 and F3. Re-run design-review iter 3, where the floor is high or higher.
