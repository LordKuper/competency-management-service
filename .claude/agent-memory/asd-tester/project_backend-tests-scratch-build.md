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
- Python edit scripts with Cyrillic literals: write them to a file and run it; keep file EOL (`\r\n` check) when rewriting.
