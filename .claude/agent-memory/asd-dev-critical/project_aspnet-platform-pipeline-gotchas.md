---
name: aspnet-platform-pipeline-gotchas
description: ASP.NET Core 10 / EF Core 10 behaviours verified here that decide platform pipeline design - fallback policy reach, EF categories that log exception text, OriginalValue semantics, no-DB test tricks
metadata:
  type: project
---

Verified on SDK 10.0.400 / runtime 10.0.11 with EF Core 10.0.12 and Npgsql provider 10.0.3 (scratch hosts outside the repo, no PostgreSQL):

- `AuthorizationOptions.FallbackPolicy` also applies to requests that match no endpoint (anonymous unknown `/api/...` gets 401, not 404) and to `MapFallbackToFile`; the SPA fallback needs `.AllowAnonymous()` (control run without it: 401 on `/login`). `UseStaticFiles` runs before routing, so assets stay public.
- With no authentication scheme registered, the default `AuthorizationMiddlewareResultHandler` throws on challenge; a custom `IAuthorizationMiddlewareResultHandler` that only sets 401/403 and lets `UseStatusCodePages` write ProblemDetails avoids it.
- EF categories `Database.Command`, `Update`, `Query`, `Database.Transaction` log the whole exception text (Npgsql `Detail` with row values) at Error even at `Warning`; only level `None` silences them. `IExceptionHandler.TryHandleAsync` returning `true` suppresses ASP.NET's own exception log.
- Setting `OriginalValue` of a concurrency token to a stale value marks the property modified, so `SaveChanges` always emits `UPDATE ... WHERE Version = stale` (0 rows, `DbUpdateConcurrencyException`); a matching value with no other change emits nothing.
- `IBindableFromHttpContext<T>` requires `T : class`; `IEndpointParameterMetadataProvider.PopulateMetadata` on a parameter type attaches endpoint metadata automatically; `ProducesResponseTypeAttribute` works as endpoint metadata; `HeaderNames.SecFetchSite` does not exist.
- `logger.BeginScope("{request_id}", id)` gives a clean JSON-console scope entry; a `Dictionary` scope prints its type name as `Message`.
- No-DB checks: EF InMemory enforces concurrency tokens (412 path end to end); with the Npgsql provider, suppress `ConnectionOpening` and throw from `ReaderExecutingAsync` to capture generated SQL (a one-row `SaveChanges` opens no transaction; several rows do and fail). Setting `Database.AutoTransactionBehavior = Never` lets a multi-row save through to the command, and returning `InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader())` (a `count(*)` query needs one int cell) captures the whole batch and lets list queries answer 200.
- Swapping a registered Npgsql `AppDbContext` for InMemory in a scratch host needs `IDbContextOptionsConfiguration<AppDbContext>` removed too, next to the options and context registrations: EF 9+ stacks every `AddDbContext` configuration, otherwise "Only a single database provider can be registered".
- A module's `internal` types are reachable from a scratch host by `<Compile Include="...\Module\*.cs" />` plus a project reference to Platform; no `InternalsVisibleTo` needed.

**Why:** each cost a verification round and several contradict first assumptions about the framework.
**How to apply:** when touching the request pipeline, authorization, logging config or concurrency in this repo.
