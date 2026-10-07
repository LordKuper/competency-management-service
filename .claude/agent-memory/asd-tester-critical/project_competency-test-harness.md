---
name: competency-test-harness
description: How the Competency service's backend and web tests are built and the traps met building them - child-process API host, shared Testcontainers, race tests, scratch-copy mutations, jsdom and Testing Library quirks, the tech-reference gate
metadata:
  type: project
---

Backend integration tests run the real API (`dotnet Competency.Api.dll` from the test bin directory, configuration through environment variables) as a child process against one shared Testcontainers PostgreSQL; every host gets its own database. A child process beat an in-process host because per-host settings (rate limit, bootstrap administrator) stay independent and middleware, migrations and locks are the deployed ones. `Program` is internal and `InternalsVisibleTo` exists, but tests prefer HTTP plus raw Npgsql.

- xunit v3 parallelism is per collection (one class); theory rows of a method run serially; a class fixture may take the assembly fixture in its constructor. A test project with zero tests fails (`dotnet test` exit 8, vitest exit 1), so a green baseline needs tests, not configuration.
- Tests sharing a database must not rely on global state. Lists page by name (200 at most), so look an entity up by a unique-name filter, never by position. Anything about the set of administrators needs a dedicated host and a helper that checks the role at sign-in, because a demoted administrator can still sign in.
- Race tests: start both requests behind a `TaskCompletionSource` created with `RunContinuationsAsynchronously`, fetch every version before the gate, accept {200, 409} or {200, 401} (the loser's session may already be ended), and loop a few rounds with fresh data.
- A host that unexpectedly starts inside a "must fail to start" test has to be disposed: a leaked child keeps the runner's stdout pipe open and hangs `dotnet test` (and a `spawnSync` around it) until it is killed by its port.
- Mutation proofs for production code run in a `git archive HEAD` copy outside the repository, never in the working tree; a `node_modules` junction in that copy is removed with `rmdir` before `rm -rf`. A fixture that sorts identically under the wrong algorithm survives its mutation: a green mutation is a finding about the test.
- Web tests: jsdom has no `ResizeObserver` (stubbed in `web/src/test/setup.ts`); Testing Library's accessible name collapses a non-breaking space, so assert the `aria-label` attribute for it; `Intl.Collator("ru")` sorts ё as е, so an ordering fixture must tell ICU order, code-point order and "ё right after е" apart.
- `docs/architecture/tech-reference/` gates test libraries too: Playwright and `Microsoft.AspNetCore.Mvc.Testing` have no reference, so the Tester may not implement with them until the Architect adds one. Check the directory before planning e2e.
