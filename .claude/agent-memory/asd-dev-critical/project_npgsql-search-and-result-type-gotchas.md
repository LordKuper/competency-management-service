---
name: npgsql-search-and-result-type-gotchas
description: Facts that decide Npgsql search queries and minimal-API result types - 2-arg ILike has no escape, generated tsvector builder shape, Conflict<T> content type, required init props
metadata:
  type: project
---

Verified on EF Core 10.0.12, Npgsql provider 10.0.3, ASP.NET Core 10.0.11 by capturing generated SQL and running a scratch host (no PostgreSQL):

- `EF.Functions.ILike(col, pattern)` (two arguments) emits `ILIKE @p ESCAPE ''`, which means no escape character: a backslash-escaped pattern is then matched literally. Use the three-argument form with `"\\"`, which emits `ESCAPE '\'`. Escape `\`, `%` and `_` in user text first.
- `HasGeneratedTsVectorColumn(...)` returns `EntityTypeBuilder`, not a property builder: set the column name with `entity.Property(e => e.SearchVector).HasColumnName(...)`. A column named `position` is fine unquoted inside the generated `to_tsvector('russian', a || ' ' || position)` expression.
- `context.Database.SqlQuery<Guid>($"... AS \"Value\" ...")` composes: `ids.Contains(x.Id)` becomes `IN (SELECT s."Value" FROM (WITH RECURSIVE ... ) AS s)` and `AnyAsync(v => v == x)` works, so one recursive CTE serves several queries. Parameters from the interpolation are real parameters.
- `TypedResults.Conflict(new ProblemDetails{...})` writes `application/json`; `TypedResults.Problem(detail:, statusCode:, title:)` writes `application/problem+json` and adds no stray 500 entry to the OpenAPI document when put in a `Results<...>` union. Add `.ProducesProblem(status)` at endpoint level; a group-level one is overridden by the result type's own metadata.
- `required` init properties on a request record: System.Text.Json answers a missing property with 400 (a nullable one must still be sent as null), and the document lists it under `required`. Positional record parameters with defaults are left out of `required`, which is how admin-only response fields are made optional.
- `Results<...>` unions accept at most six result types; `ValidationProblem` documents 400 as `application/problem+json`.

**Why:** each looked fine in code review and failed or misled only when the SQL or the response was read.
**How to apply:** writing search queries, raw SQL composition or endpoint return types in a module.
