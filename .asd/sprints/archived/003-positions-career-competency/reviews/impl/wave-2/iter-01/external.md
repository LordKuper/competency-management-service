[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | src/Competency.CareerFramework/MatrixWorkflow.cs:70 | After the superior approver's edits are accepted, an HR return that has only a comment still gets `returnedWithChanges=true`: `StateAsync` checks only that the latest variant is `Approver`. The author can then call accept-changes and publish despite HR's return (AC-16, A-8). | Decide "has changes" from the `VariantNumber` of the current Returned decision. Add a test for this sequence. |
| 2 | high | src/Competency.CareerFramework/MatrixWorkflow.cs:350 | `ApproveAsync` does not re-check completeness. After submit, a required score can be removed from a scale that can still be changed, or a competency can be moved to another scale; HR then publishes a version with invalid requirements and snapshot (AC-14, AC-15). | Repeat the completeness check under the lock before approval. Add a test where the catalog changes between submit and approve. |
| 3 | high | src/Competency.CareerFramework/MatrixCatalog.cs:156 | A draft category of a used competency is missing from `DraftsAsync` and from the approver's `RouteEntryIds`. Publishing makes the competency active while its category stays draft and invisible to ordinary users (AC-9). | Include used categories in route visibility and in approval at publish; check uniqueness and write audit. Add a matrix test with a draft category. |
| 4 | medium | src/Competency.CareerFramework/MatrixWorkflow.cs:157 | When the content is unchanged, `SaveChangesAsync` is skipped, so a stale If-Match is never checked: a repeat PUT with the old ETag returns 200 instead of 412 (AC-17). | Check the ETag whether or not the content changed; leave the version unchanged on a correct repeat PUT. Add a stale-ETag test with identical content. |
| 5 | medium | src/Competency.CareerFramework/MatrixWorkflow.cs:237 | A version can be deleted between the early `matrixId` read and the transition lock; the later `FirstAsync` throws and the API returns 500 instead of 404. | Re-find the version with `FirstOrDefaultAsync` under the lock and handle the missing case. Add a deterministic delete-versus-transition concurrency test. |
| 6 | medium | src/Competency.CareerFramework/MatrixContent.cs:85 | The requirement type is not checked with `Enum.IsDefined`; `JsonStringEnumConverter` accepts numbers, so `type=999` is saved in the JSON and passes publishing as a non-critical type. The same input on a competency reaches the DB constraint and returns 500. | Validate `Type` in `MatrixContent` and `CompetencyRequest` and return 400. Add tests for unknown numeric values. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none

## Verdict
CONCERNS: 6

## Next action
Fix F1 first (critical), then F2 and F3 (high), then F4 to F6 (medium), and re-run the wave 2 impl-review. Static review only (Codex read-only sandbox).
