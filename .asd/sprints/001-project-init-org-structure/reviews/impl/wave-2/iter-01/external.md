[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | src/Competency.UserManagement/ActiveAdministrators.cs:51 | Dismissing administrator B and then unblocking B without rehiring leaves B unable to sign in but still counted as an active administrator. Blocking or demoting administrator A afterwards leaves no usable administrator. (codex: major) | Exclude administrators bound to inactive employees from the active-admin checks. Add a regression test. |
| 2 | high | src/Competency.UserManagement/UserEndpoints.cs:134 | Account creation does not share the dismissal lock. Dismissal can find no bound account, and creation then commits an unblocked account bound to the dismissed employee. Rehiring later restores login without an explicit unblock. (codex: major) | Serialize creation and dismissal under a shared transaction lock. Validate employee activity after acquiring it. Test the race. |
| 3 | high | src/Competency.UserManagement/UserEndpoints.cs:310 | Password reset commits credentials before writing its audit event in a separate transaction. An audit failure or cancellation leaves the password changed with no journal entry. ChangePasswordAsync has the same defect. (codex: major) | Commit the credential change and its success audit event in the same database transaction. Test audit-write failure rollback. |
| 4 | medium | src/Competency.OrgStructure/EmployeeEndpoints.cs:359 | SaveAsync releases the tree lock before projecting the response. A concurrent edit can return a body whose version differs from its ETag. A concurrent deletion makes SingleAsync throw after the write has already committed. (codex: minor) | Project the response before committing and derive the ETag from that same snapshot. Add a concurrent-write regression check. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none reported

## Verdict
CONCERNS: 4

## Next action
Fix findings 1-3 (high) and 4 (medium), then re-run impl-review wave 2 iteration 2. Static-only codex run (read-only sandbox).
