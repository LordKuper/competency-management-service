---
name: scratch-process-control
description: Running and stopping scratch API/Vite processes on this Windows box without hitting the user's stack - kill by PID, content root of dotnet dll, closed-port timing, background pattern
metadata:
  type: project
---

- **Never kill by command-line pattern.** A PowerShell `Get-CimInstance ... CommandLine -match 'Competency.Api.dll'` also matched the shell running the command (exit 255) and could match the user's own API. Find the listener with `netstat -ano | grep ":<port> .*LISTENING"` and `taskkill //PID <pid> //F`; for a Vite chain add `//T`. Check `netstat` for 5000/5173/15432 before starting scratch processes: the user's stack (`cmp-dev-db` on 15432) may or may not be up.
- **`dotnet <dll>` takes the working directory as content root**, so from the repo root `appsettings.json` is not found and the host dies with `Section 'RateLimiting:Login' not found`. Run from `src/Competency.Api` (as VS and `dotnet run` do) or use `dotnet run --no-build --launch-profile Competency.Api`; profile env vars apply.
- **A refused connect to a closed local port costs about 2 s on Windows**, so a wait loop bounded by attempt count overshoots its time budget by 3x; bound it by `Stopwatch` elapsed time (measured: 61 s for a 60 s budget).
- Long-running scratch processes: `run_in_background: true` on the Bash call with `exec bash <script>`; a plain `( ... ) &` died silently after 34 s. Write the scratch script with the write tool (backslashes and quoting), log to the scratchpad, poll with `until curl ...; do sleep 1; done` (a bare `sleep N; cat` is blocked by the tool).
- `-e`/heredoc node snippets with `\\` lose a backslash; put such checks in a `.cjs` file via the write tool.
- Compose in a scratch run: `DEV_DB_PORT=28543 docker compose -p cmp-scratch -f deploy/dev/docker-compose.yml up -d --wait`, remove with `down -v` (the data volume has the fixed name `competency-dev-pgdata`, so `-v` is what frees it).

**Why:** each cost a failed run or risked the user's processes during Task 11.
**How to apply:** any scratch run of the API, Vite or the dev compose stack.
