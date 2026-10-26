[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none
- **Interrupted attempts**: 1 (persist-review rejected: unescaped «||» inside a table cell)

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | src/Competency.CareerFramework/CatalogStore.cs:65 | codex finding 1 (major). Granting HR to a superior approver removes that approver's access to subordinate draft tracks and levels. The HR shortcut leaves RouteEntryIds empty, while the visibility filters still require route access outside the assigned units. | Keep the shortcut for administrators only. Add a regression test for a superior approver who has HR but no assignment to the matrix unit. |
| 2 | high | src/Competency.CareerFramework/CatalogPublication.cs:176 | codex finding 2 (major). Planning marks drafts Active before PublicationPlan filters blockers. An extra draft level outside the matrix content that hits NameTaken therefore passes the Active visibility branch. This exposes its details to the superior approver, who has no draft access to it. | Evaluate blocker visibility against the original statuses. Add a hidden-blocker regression test. codex validated statically and did not run tests. |

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 2

## Next action
Route both findings to review-fix (impl) and re-review in the next iteration. No stalemate: the iteration-01 finding set (locations in MatrixCatalog.cs:261, CatalogEndpoints.cs:470, CatalogViewer.cs:88, CatalogPublication.cs:82, CompetencyBlockRequest.cs:22) does not match this set.
