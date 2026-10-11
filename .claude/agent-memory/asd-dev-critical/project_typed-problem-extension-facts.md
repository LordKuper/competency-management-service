---
name: typed-problem-extension-facts
description: Typed ProblemDetails extension fields in responses and OpenAPI, plus a quick scratch-Postgres check with a second login user copied from the admin row
metadata:
  type: project
---

Verified on ASP.NET Core 10 against a scratch PostgreSQL 18.6 (2026-10-11):

- **A `ProblemDetails` subclass with typed properties** (for example `DeletionRefusal` with `Blockers`, `MoreCount`), returned through `TypedResults.Problem(instance)`, is written with its runtime type: the extra fields come out camelCase next to `traceId`, content type `application/problem+json`. `.Produces<TSub>(409, "application/problem+json")` on the endpoint (instead of `.ProducesProblem(409)`) puts a typed schema in `openapi.json` and `schema.d.ts`. `required` init properties become `required` in the schema. There was no earlier typed precedent: `Rejection.Taken`'s `errors` is still documented as plain `ProblemDetails`.
- **Composable raw SQL id sets**: a `SqlQuery<Guid>(... AS "Value")` held in a local and used as `ids.Contains(x.Id)` works inside `CountAsync`, inside a `viewer.Sees<T>()` filter, and in an `OrderBy/Take/Select` page. The career store's `MatricesUsing` / `LevelsReferencing` return `IQueryable` this way.
- **Fast scratch check without the dev compose**: `docker run -d --rm --name <scratch> -p 127.0.0.1:28543:5432 -e POSTGRES_USER=competency ... postgres:18.6-trixie` (no named volume, so `--rm` cleans up). The API migrates the database and bootstraps the admin on start. A second sign-in user is an `INSERT INTO users ... SELECT` copy of the admin row with a new id, e-mail, normalized names and `role = 'User'`: the copied Identity `password_hash` works for the new row. Seed career rows straight by SQL inside `BEGIN; ... COMMIT;`, so a failed seed leaves nothing behind.

**Why:** the subclass approach and the user copy each saved a detour, and the seed without a transaction left half-inserted rows that had to be cleaned up by hand.
**How to apply:** any API change that adds fields to an error response, or a quick end-to-end check of a permission or visibility path. See also [[scratch-process-control]] and [[npgsql-search-and-result-type-gotchas]].
