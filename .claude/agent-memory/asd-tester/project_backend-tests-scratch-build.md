---
name: backend-tests-scratch-build
description: How to run tests/Competency.Tests when the user's Api/Visual Studio locks in-place bin folders, and the kit helpers that keep API tests green
metadata:
  type: project
---

In-place `dotnet test` fails with MSB3021 while the user's Competency.Api or Visual Studio runs. Use `dotnet test --project tests/Competency.Tests --artifacts-path <scratch>/build < /dev/null` (Testcontainers own ephemeral Postgres, ~2 min for the whole suite; never touch the dev DB).

**Why:** ContractTests and DeploymentTests find the repo by walking up from the test binary to `Competency.slnx`; under a scratch artifacts path they fail with FileNotFoundException. Copy `Competency.slnx`, `Dockerfile`, `deploy/`, `openapi/` and every `src/*/*.csproj` + `packages.lock.json` into `<scratch>/build` once, then those classes run.

**How to apply:**
- Redirect stdin (`< /dev/null`) for any command that could read it; a bare `cat > file` in a Bash call hangs the call.
- API tests build state through `MatrixKit` and `CareerScenarios` in `tests/Competency.Tests/Infrastructure`; change those helpers, not every test: a level needs a linked active position, a track is a draft until approved with `?cascade=<tag>` read from `.../publication`, publishing matrix steps need the same query.
- HR is assigned by employee: `HrCandidateAsync` (employee plus Hr-role account) and `AssignHrAsync` in CareerScenarios; changing an account's roles ends its sessions, so re-sign in (`GrantHrRoleAsync` returns the refreshed member). A unit with working employees cannot be deactivated, so build HR candidates in another unit.
- Perf seed check: start the scratch-build Api against a throwaway `postgres:18.6-trixie` container (needs `Smtp__Host/Port/SecureSocketOptions/From`, `App__PublicBaseUrl`, bootstrap admin env), apply `tests/perf/seed-10k.sql` with `docker exec -i ... psql`, read it back through the API, then `docker rm -f -v` the container by name.
- Python edit scripts with Cyrillic literals: write them to a file and run it; keep file EOL (`\r\n` check) when rewriting.
- Mutation proofs: edit the production file in place, run `dotnet test --solution Competency.slnx --artifacts-path <scratch> --filter-class "*Class"` (flag repeats for several classes), and `git checkout -- <file>` in the same command (try/finally in a script). Source files are often CRLF while shell heredocs are LF: anchor on a normalised copy and write back with the file's EOL; a `str.index` on a repeated line mutates the wrong method.
- Counting SQL commands of a request: start the host with `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information` and `...Microsoft.AspNetCore.Hosting.Diagnostics=Information` (`HostFixture` subclass with settings); the console is JSON, so count `"EventId":20101,` (a message appears twice per record); wait for the unique `?probe=<guid> - 200` end-of-request line with `ApiHost.LogsAfterAsync`.
