---
name: scratch-process-control
description: Running and stopping scratch API/Vite/compose processes on this Windows box without hitting the user's stack - kill by PID, never `down -v` on the dev compose, scratch-only volume, content root of dotnet dll, closed-port timing, background pattern
metadata:
  type: project
---

- **Never run `docker compose ... down -v` (or `docker volume rm`/`prune`) against `deploy/dev/docker-compose.yml` or `deploy/`.** The dev compose pins its data volume by a fixed name (`competency-dev-pgdata`), so `-p <other project>` does NOT isolate it: `up` attaches to the user's real dev database and `down -v` deletes it. Remove only volumes this run created, by their exact scratch name.
- **Scratch database from the dev compose file**: write a scratch-only override in the scratchpad (never in the repo) that renames the volume, and pass both files:
  `volumes: {pgdata: {name: cmp-scratch-pgdata}}`, then `DEV_DB_PORT=28543 docker compose -p cmp-scratch -f deploy/dev/docker-compose.yml -f <scratchpad>/compose.scratch.yml config` first (offline, no daemon change) and check that it prints `name: cmp-scratch-pgdata` and the scratch port, never `competency-dev-pgdata` or 15432; only then `up -d --wait`. Clean up with plain `down` (no `-v`), then `docker volume rm cmp-scratch-pgdata`. Or skip compose: a separate `docker run` with its own named volume (see [[docker-spike-recipe]]).
- **Never kill by command-line pattern.** A PowerShell `Get-CimInstance ... CommandLine -match 'Competency.Api.dll'` also matched the shell running the command (exit 255) and could match the user's own API. Find the listener with `netstat -ano | grep ":<port> .*LISTENING"` and `taskkill //PID <pid> //F`; for a Vite chain add `//T`. Check `netstat` for 5000/5173/15432 before starting scratch processes: the user's stack (`competency-dev-db-1` on 15432) may or may not be up.
- **`dotnet <dll>` takes the working directory as content root**, so from the repo root `appsettings.json` is not found and the host dies with `Section 'RateLimiting:Login' not found`. Run from `src/Competency.Api` (as VS and `dotnet run` do) or use `dotnet run --no-build --launch-profile Competency.Api`; profile env vars apply.
- **A refused connect to a closed local port costs about 2 s on Windows**, so a wait loop bounded by attempt count overshoots its time budget by 3x; bound it by `Stopwatch` elapsed time (measured: 61 s for a 60 s budget).
- Long-running scratch processes: `run_in_background: true` on the Bash call with `exec bash <script>`; a plain `( ... ) &` died silently after 34 s. Write the scratch script with the write tool (backslashes and quoting), log to the scratchpad, poll with `until curl ...; do sleep 1; done` (a bare `sleep N; cat` is blocked by the tool).
- `-e`/heredoc node snippets with `\\` lose a backslash; put such checks in a `.cjs` file via the write tool.

**Why:** a `-p` project name looked like isolation but the compose file hard-codes the volume name, so a documented cleanup step would have wiped the user's dev data; the other facts each cost a failed run or risked the user's processes.
**How to apply:** any scratch run of the API, Vite or the dev compose stack; read the first two bullets before touching docker.
