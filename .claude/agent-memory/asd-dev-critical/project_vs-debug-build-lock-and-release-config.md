---
name: vs-debug-build-lock-and-release-config
description: While the user's VS F5 session runs, Debug build of Competency.slnx fails on locked Api bin files; use -c Release (also regenerates openapi.json, ef --configuration Release --no-build); scratch API runs and a git-archive timing baseline are impl-test / tester only
metadata:
  type: project
---

2026-10-06, user's API (VS, PID listening on 5000) was running. Bullets marked **impl-test / tester only** are never run in impl: impl verifies by build, lint, `ef migrations has-pending-model-changes` and `check:api` (custom-coding-rules "Verification depth in impl").

- `dotnet build Competency.slnx --tl:off` (Debug) fails with 8 x MSB3027/MSB3021 ("Microsoft Visual Studio, Competency.Api blocks the file") copying the module dlls into `src/Competency.Api/bin/Debug`; no CS errors, 40 retry warnings. Never stop the user's process. **`-c Release` builds clean (0/0) next to it**, uses its own `bin/Release` + `obj/Release`, and still regenerates `openapi/openapi.json` (ApiDescription.Server runs in any configuration). `dotnet ef migrations has-pending-model-changes ... --configuration Release --no-build` works the same way. Report the Debug failure as environmental.
- **impl-test / tester only.** A scratch API run from `bin/Release/net10.0` (cwd `src/Competency.Api`) locks that dll: kill it by listener PID before each rebuild, then restart. Builds are 3-5 s incremental, so the edit-kill-build-start loop is cheap.
- **impl-test / tester only.** Timing baseline of the pre-change code: `git archive HEAD | tar -x -C <scratch>/base`, `dotnet build src/Competency.Api -c Release` there, run it on a second port against the same scratch DB. The `.competency.auth` cookie from the first instance works on the second when both use the same `DataProtection__KeysPath` (cookies ignore ports), so one login serves both; compare the two interleaved in one script.
- **impl-test / tester only.** A bash `export` of a variable whose name has dots (`Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command`) is invalid; set it with `exec env "NAME=value" dotnet ...`. With `Database.Command=Information` the log shows the generated SQL as `Executed DbCommand` messages (appsettings silences it by default).
- **impl-test / tester only.** Background `run_in_background` runs of a script that I later kill by PID report "failed with exit code 1" when they end; that is the expected kill, not a crash.

**Why:** each cost a failed command or would have touched the user's running stack.
**How to apply:** any build while the user's VS session is up; a before/after performance comparison only when dispatched as impl-test / tester (see also [[scratch-process-control]], [[docker-spike-recipe]]).
