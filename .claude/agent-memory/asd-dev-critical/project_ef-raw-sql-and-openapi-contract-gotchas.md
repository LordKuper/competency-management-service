---
name: ef-raw-sql-and-openapi-contract-gotchas
description: EF migration raw SQL needs its own trailing semicolon, and the .NET 10 OpenAPI document shapes the typed client (int unions, PascalCase AsParameters names, JsonElement) unless counteracted
metadata:
  type: project
---

Verified on SDK 10.0.400 with EF Core 10.0.12, Npgsql provider 10.0.3, Microsoft.AspNetCore.OpenApi 10.0.12 and openapi-typescript 7.13.0 (no PostgreSQL available):

- `migrationBuilder.Sql(...)` is not terminated by EF in an idempotent script: the text lands inside `DO $EF$ ... IF NOT EXISTS ... THEN <sql> END IF; END $EF$`, so a statement without its own `;` is a syntax error there. The plain script also stays single-terminated, so always end each `Sql` string with `;` (after `$$` for a function body) and read the output of `dotnet ef migrations script --idempotent`, not only the C# migration.
- A JS `String.replace` replacement turns `$$` into `$`. Editing SQL or shell text that contains `$$` through `node -e` silently corrupts it; use the Edit tool.
- Properties enumerate in alphabetical order (key first) through `EntityEntry.Properties`, so JSON built from them has alphabetical keys; never pin key order in a check.
- The generated OpenAPI document gives every `int32` `type: [integer, string]` plus a `pattern` (web JSON defaults allow reading numbers from strings), so the typed client sees `number | string` for counts, pages and `ProblemDetails.status`. `JsonNumberHandling.Strict` in the HTTP JSON options removes it host-wide; per-type attributes are too noisy.
- `[AsParameters]` on a positional record names query parameters after the PascalCase constructor parameters in the document and the client; `[FromQuery(Name = "camelCase")]` on each parameter fixes both. `JsonElement` becomes an empty schema and `unknown` in TypeScript; a typed `ValidationProblem` result becomes `HttpValidationProblemDetails`.
- Npgsql rejects a `DateTimeOffset` parameter with a non-zero offset; a query-string `from=...+03:00` bound to `DateTimeOffset?` must go through `ToUniversalTime()` before it reaches a filter.

**Why:** each cost a verification round or would have reached the database or the client contract unnoticed.

**How to apply:** writing migrations with raw SQL, adding endpoints with query parameters or DTOs with integers, regenerating `schema.d.ts`.
