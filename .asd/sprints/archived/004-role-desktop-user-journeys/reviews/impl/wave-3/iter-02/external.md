[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-3/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none
- **Interrupted attempts**: 1 (codex exec exit 1, then 600 s foreground timeout)

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | web/src/features/career/TrackModal.tsx:137 | codex finding 1 (major). After a 412 refresh the reused form instance keeps its old name even though the key changes. The next save sends the old name together with the refreshed version, so it can overwrite another user's rename. The existing tests return the same name and miss this. | After a 412, reset the name to the reread value and keep the pending levels. Add a test where the refresh returns a different name. |
| 2 | medium | web/src/features/org-structure/UnitHrModal.tsx:266 | codex finding 2 (minor). The employee picker stays editable while an assignment is saving. If the user selects B while A is saving, A is still assigned and the dialog closes, and the displayed choice is lost. | Lock the picker while the assignment is in flight. Add a regression test with a deferred response. |

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none

## Verdict
CONCERNS: 2

## Next action
Route both findings to review-fix. Stalemate check: not triggered (iteration 1 findings at CatalogModal.tsx:88, NameModal.tsx:78, UnitHrModal.tsx:225 — different set).
