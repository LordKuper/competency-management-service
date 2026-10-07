---
name: vs-debug-build-lock-and-release-config
description: While the user's VS F5 session runs, Debug build of Competency.slnx fails on locked Api bin files; use -c Release (also regenerates openapi.json, ef --configuration Release --no-build) and a git-archive baseline for timings
metadata:
  type: project
---

Task 14 (2026-10-06), user's API (VS, PID listening on 5000) was running:

- `dotnet build Competency.slnx --tl:off` (Debug) fails with 8 x MSB3027/MSB3021 ("Microsoft Visual Studio, Competency.Api blocks the file") copying the module dlls into `src/Competency.Api/bin/Debug`; no CS errors, 40 retry warnings. Never stop the user's process. **`-c Release` builds clean (0/0) next to it**, uses its own `bin/Release` + `obj/Release`, and still regenerates `openapi/openapi.json` (ApiDescription.Server runs in any configuration). `dotnet ef migrations has-pending-model-changes ... --configuration Release --no-build` works the same way. Report the Debug failure as environmental.
- A scratch API run from `bin/Release/net10.0` (cwd `src/Competency.Api`) locks that dll: kill it by listener PID before each rebuild, then restart. Builds are 3-5 s incremental, so the edit-kill-build-start loop is cheap.
- Timing baseline of the pre-change code: `git archive HEAD | tar -x -C <scratch>/base`, `dotnet build src/Competency.Api -c Release` there, run it on a second port against the same scratch DB. The `.competency.auth` cookie from the first instance works on the second when both use the same `DataProtection__KeysPath` (cookies ignore ports), so one login serves both; compare the two interleaved in one script.
- A bash `export` of a variable whose name has dots (`Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command`) is invalid; set it with `exec env "NAME=value" dotnet ...`. With `Database.Command=Information` the log shows the generated SQL as `Executed DbCommand` messages (appsettings silences it by default).
- Background `run_in_background` runs of a script that I later kill by PID report "failed with exit code 1" when they end; that is the expected kill, not a crash.

**Why:** each cost a failed command or would have touched the user's running stack.
**How to apply:** any task verified while the user's VS session is up, or any before/after performance comparison (see also [[scratch-process-control]], [[docker-spike-recipe]]).
