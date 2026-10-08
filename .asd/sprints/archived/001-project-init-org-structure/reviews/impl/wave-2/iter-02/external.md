[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | src/Competency.UserManagement/AuthEndpoints.cs:149 | codex F1 (major). When the current password is wrong, `VerifyPasswordAsync` returns `LoginDenial.LockoutStarted` once the failed attempt reaches the limit. ChangePasswordAsync only checks `is not null` and drops that value, so a lockout started by password change writes no `Auth.LockedOut` event and never appears in the journal (AC-15). The login path writes it in `DenyAsync` (lines 223-228). | When the result is `LockoutStarted`, write `Auth.LockedOut` for the account (as `DenyAsync` does). Add a check for that event to the password-change test. |

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none

## Prior findings (stalemate check)

- P1 resolved; P2 resolved; P3 resolved; P4 resolved. The finding set differs from iter-01: no stalemate.

## Verdict
CONCERNS: 1

## Next action
Fix F1 (audit `Auth.LockedOut` on the password-change path) with test coverage, then iter 3 (floor high).
