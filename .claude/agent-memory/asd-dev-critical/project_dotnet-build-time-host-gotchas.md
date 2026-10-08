---
name: dotnet-build-time-host-gotchas
description: .NET 10 build-time OpenAPI/EF tooling facts verified empirically here - hosted services do run, ef skips Program.cs, EF Relational needs a pin
metadata:
  type: project
---

Verified on SDK 10.0.400 with Microsoft.Extensions.ApiDescription.Server 10.0.12 and EF Core 10.0.12:

- `GetDocument.Insider` really starts the host (stub server): hosted services run and middleware is instantiated. The framework `DataProtectionHostedService` creates a key file in the user profile during `dotnet build`. Guard registrations that have startup side effects (data-protection key storage), not only post-`Build()` code; migrations and similar explicit calls go under the same entry-assembly check. A cookie-auth registration (`AddAuthentication`) re-registers default data protection, so the tooling branch then needs `UseEphemeralDataProtectionProvider()`.
- Options validation: `ValidateOnStart()` runs at host start, so in `GetDocument.Insider` too; register `.Validate(...)` only and resolve `IOptions<T>.Value` explicitly under `!isToolingRun` (Sprint 002 `ValidateMailSettings`). The first failing options type throws, so errors of a second type show only after the first is fixed. A binder conversion failure (`Failed to convert configuration value abc at ...`) prints the raw value: keep secrets as `string` properties.
- `dotnet ef` with an `IDesignTimeDbContextFactory` in the startup assembly never runs `Program.cs`; no `ef` entry-assembly guard is needed. A factory that reuses the application's own service registration keeps the design-time model identical to runtime.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 only requires `Relational >= 10.0.4`, so NuGet resolves Relational 10.0.4 next to EF 10.0.12 in every project without a direct pin; pin `Microsoft.EntityFrameworkCore.Relational` explicitly in the project that owns the context and read the lock files to confirm.
- `PrivateAssets="all"` on `Microsoft.EntityFrameworkCore.Design` already keeps it and its Roslyn-based dependencies out of `dotnet publish` output; no `ExcludeAssets` needed. Check `deps.json`, not just file names.
- `MapFallbackToFile` with a regex constraint on a catch-all never matches `/` (empty value is not evaluated); pair it with `UseDefaultFiles()`.

**Why:** each of these cost a verification round; the docs in tech-reference are explicitly unverified on them.
**How to apply:** when touching host startup, composition, migrations or the `.csproj` package set in this repo.
