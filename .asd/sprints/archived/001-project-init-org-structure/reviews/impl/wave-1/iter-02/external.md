[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-1/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: n/a

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | tests/Competency.Tests/PlatformTests.cs:102 (helper: tests/Competency.Tests/Infrastructure/ApiHost.cs:63-77) | codex F1 (minor). `Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesOrSqlValues` runs on the shared host. `LogsAfterAsync("\"Status\":412")` returns as soon as the shared `Logs` buffer contains any 412 line, including one left by another test. Logs are written asynchronously, so the current request's records may not be in the buffer yet. The "no personal data in logs" assertions can then pass without looking at this test's records. Verified: the helper only matches the fragment `Contains` on the whole shared buffer. | Run this test on its own host (`await using var host = await environment.StartHostAsync()`), or wait for a record with a fragment unique to this request (its request id or the unique unit id from the URL) instead of the generic `"Status":412`. |

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none

## Prior finding set (stalemate input)

- P1 (critical, `src/Competency.Platform/Migrations/20261005170900_AuditEvents.cs:73`, audit triggers bypassable via `session_replication_role = replica`): resolved — migration `20261007141643_AuditTriggersEnableAlways` makes both triggers `ENABLE ALWAYS`; test `Ac15_Journal_RejectsUpdateDeleteAndTruncate_EvenWhenOrdinaryTriggersAreSilencedByTheReplicaRole` covers UPDATE, DELETE and TRUNCATE (not executed in the read-only sandbox).
- The iter-2 finding set differs from iter-1. No stalemate.

## Verdict
CONCERNS: 1

## Next action
Fix the log-wait race in `PlatformTests.cs:102` (dedicated host or a request-unique fragment), then iteration 3.
