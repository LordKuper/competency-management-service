---
name: hosted-service-ctor-options
description: Singleton hosted services must not read validated IOptions in their constructor chain, or the build-time OpenAPI generation fails.
metadata:
  type: feedback
---

A `BackgroundService` registered with `AddHostedService` is constructed whenever the host is built, including by the OpenAPI document generation that runs inside `dotnet build` of `Kalibr.Api`; there `App:PublicBaseUrl` and similar validated options are unset, so any dependency that reads `IOptions<T>.Value` in its constructor (for example `PromotionCalendar`) throws `OptionsValidationException` and fails the build.

**Why:** the failure surfaces as 40 cryptic `Microsoft.Extensions.ApiDescription.Server` errors, not as a compile error.

**How to apply:** give a hosted service only dependencies that are cheap to build (`IServiceScopeFactory`, `TimeProvider`, logger, the mail queue); resolve option-reading services from a per-pass scope inside the loop.
