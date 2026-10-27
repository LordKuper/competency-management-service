[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | web/src/features/desk/OnboardEmployeePage.tsx:289 | A background employee refresh remounts the dirty form and silently discards unsaved edits. Codex reproduced this with a newer server version (codex finding 1, major). | Freeze the edit base while editing and rebase after a 412 refusal. Add a background-refresh regression test. |
| 2 | high | web/src/features/users/UserForm.tsx:89 | The email stays editable while account creation is pending. Edits made after submit are discarded, and the invitation uses the earlier address (codex finding 2, major). | Lock editable controls while isSubmitting. Test with a deferred response. |
| 3 | high | web/src/features/org-structure/EmployeeForm.tsx:128 | Employee fields, pickers included, stay editable during submit. Later edits are silently lost when onboarding advances with the submitted values (codex finding 3, major). | Lock all editable controls during submit, pickers included. Add a deferred-save test. |
| 4 | high | web/src/features/desk/BuilderCompetenciesStep.tsx:433 | An explicit disabled=false overrides the saving context for the competency checkboxes. Codex reproduced selecting a competency after the content PUT started, and that selection is missing from the saved request (codex finding 4, major). | Include isSaving in the checkbox disabled conditions. Test selection while the PUT is pending. |

All four locations are in files[] of external.scope.json.

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 4

## Next action
Route the four high findings to review-fix. No stalemate: iteration 1's findings were located in different files and at different lines, and this set is not identical to it. Codex ran in-memory UI probes and did not run the full suite or a real backend.
