[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-03
- **Severity floor (this iter)**: high
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | src/Competency.UserManagement/AuthEndpoints.cs:195 | codex F1 (major). The result of `users.AccessFailedAsync(user)` is ignored. Static analysis: with concurrent wrong passwords the losing save gets a ConcurrencyFailure (AppUserStore) and the attempt is lost; at count 4 both requests see `IsLockedOutAsync` and return `LockoutStarted`, so `Auth.LockedOut` is written twice for one lockout. AC-6 (lockout) and AC-15 (journal). Shared by login and change-password (`VerifyPasswordAsync`). Not reproduced; static only. | Serialize the check and the attempt accounting per user against current DB state; handle the `AccessFailedAsync` result and journal only a lockout that was actually started; add a deterministic concurrent-attempts test. |

## Prior finding (stalemate input)

P1 (AuthEndpoints.cs:149, lockout from change-password not journaled): resolved — `AuditLockoutAsync` call, test in SessionTests.cs checks a single event, actor, role and request_id. F1 is new (different defect and line): no stalemate.

## Dropped findings (counts only)

- Below severity floor (iter 3, floor high): 0
- Nitpick, by category: none

## Verdict
CONCERNS: 1

## Next action
Decide whether to fix the attempt-accounting race (finding 1) in iter 4 or accept and document it as a limit. If fixed, a concurrent wrong-password test is needed.

- user decision (2026-10-07): finding 1 accepted for fix — password check and attempt accounting under a per-user row lock, journal only a lockout actually started, concurrent wrong-password test
