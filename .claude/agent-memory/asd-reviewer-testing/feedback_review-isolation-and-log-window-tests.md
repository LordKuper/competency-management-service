---
name: review-isolation-and-log-window-tests
description: A Grep over the sprint dir surfaces earlier iterations' review files (clean-context breach) - scope every sprint-dir grep with a glob; tests that count host log records (EF command count) race unless the window start is synced after a probe request
metadata:
  type: feedback
---

**1. Scope sprint-dir greps.** `.asd/sprints/<sprint>/` holds `reviews/impl/wave-K/iter-NN/*.md` of earlier iterations, and a content grep (ids such as `TST-9`, AC numbers, test names) matches them. A reviewer must not read another iteration's review files (`review-policy.md` "Clean-context review iteration"). **How to apply:** pass `glob` (`test-plan*.md`, `decisions-log*.md`, `plan.md`, `sprint.md`) or an exact file path; never grep the sprint root without a glob. If a match slips through, do not use it as input and say so in the report.

**2. Log-window count tests.** The backend tests run the API as a child process whose console is captured (`ApiHost.Logs`, `LogsAfterAsync`). A test that counts records (EF `"EventId":20101,`) between two points is sound only if the start snapshot (`Logs.Length`) is taken after the console has caught up with the previous request. The console logger and the pipe are asynchronous, and the last EF commands of a request are logged right before its response is written, so a snapshot taken straight after a setup request can precede their arrival and inflate the next window. **How to apply:** for each measured window, check that a probe request (its own `... - 200` finish line awaited) precedes the snapshot; the existing warm-up before the "small" measurement is the pattern, and a second measurement after more setup needs its own.

**3. Per-class host is the isolation unit.** `HostFixture`/`CareerHost` give each test class its own database and run its tests one after another, so "no HR account yet", "nobody touched the default scale" and totals-add-up assertions are safe inside a class whose doc says it has the walk alone; the same assertions in a class sharing the lazily started host are not. **How to apply:** before calling a global-state assertion flaky, read the class doc and the fixture type.

See [[dotnet-api-test-review]], [[no-shell-review-method]].
