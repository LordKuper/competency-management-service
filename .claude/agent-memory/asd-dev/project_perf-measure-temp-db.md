---
name: perf-measure-temp-db
description: How to measure a request on the 10k seed without touching the user's dev database or browser
metadata:
  type: project
---

Measure a perf-risk request with a throwaway database inside the running dev Postgres container, never in the dev database itself.

**Why:** the dev stack runs one container with a fixed volume; custom-common-rules forbids destructive Docker commands against it, but `CREATE DATABASE`/`DROP DATABASE` of a database the agent made itself is its own temp resource.

**How to apply:**
- `docker exec <db-container> psql -U <user> -d postgres -c "CREATE DATABASE <tmp>"`; start `dotnet src/Kalibr.Api/bin/Release/net10.0/Kalibr.Api.dll` from a Release build with `ASPNETCORE_URLS` on a spare port, `ConnectionStrings__Default` pointing at the temp database and the `Bootstrap__*` variables from `launchSettings.json`; it migrates and bootstraps the administrator on start.
- Seed with `docker exec -i <db-container> psql ... -v ON_ERROR_STOP=1 -q < tests/perf/seed-10k.sql` after the first start; the seed refuses a database that already has units, so recreate the database (stop the API first) for each fresh scenario.
- Stop the API by the PID listening on that port (`netstat -ano`), never by image name; drop the temp database by its exact name at the end. The built Kalibr.Api.dll is locked while it runs, so stop it before the next build.
- Drive requests with `curl -b cookiejar` after a login POST; `-F file=@...` for multipart.
- Start the API with its working directory at `src/Kalibr.Api`, not the repo root: `appsettings.Development.json` (Smtp settings) is read from the content root, and a start from the root dies on options validation.
- A workbook that needs head candidates can be made from the seed CSV by renaming the first employee's position of each unit to a head word before running the builder; the seed's own positions have none.
- EF Core cannot insert in one save two new rows that reference each other (a new unit and its new head employee, whose own unit is that unit): save the rest first, then set the nullable FK in a second save inside the same transaction.
- EF Core: `Entry()` runs DetectChanges, so tracking thousands of entities that way is quadratic; switch `ChangeTracker.AutoDetectChangesEnabled` off while tracking and back on before the save.
