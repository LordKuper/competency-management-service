[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-03
- **Severity floor (this iter)**: high
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | src/Competency.CareerFramework/PublicationPlan.cs:69 | codex finding 1, labelled critical in the codex output, so no severity remapping. The publication preview exposes restricted drafts through `entries`. In the new hidden-level test, the superior approver still receives the outside level's ID and name, because `DescribeEntries` serializes every cascade entry without checking visibility. `blockers` hides that level, but `entries` does not. | Filter the response entries by the reader's visibility rules. Keep the complete internal cascade and the tag. Assert in the test that restricted IDs and names are absent from the preview. |

## Dropped findings (counts only)

- Below severity floor (iter 3, floor high): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 1

## Next action
Route finding 1 to review-fix. Codex validated by source inspection only and did not run tests.

Stalemate check: not triggered. The previous iteration's finding set was CatalogStore.cs:65 and CatalogPublication.cs:176. The only finding now is PublicationPlan.cs:69, so the sets differ.
