---
name: password-reset-link-facts
description: Sprint 002 Task 5 facts - shared rate-limit counter per policy, test host needs the new limit raised, Channel queue wiring, change-password vs reset race
metadata:
  type: project
---

- ASP.NET Core rate limiting keys a partition by (policy name, partition key) (`RateLimiterOptions.ConvertPartitioner` -> `DefaultKeyType`), not by endpoint: every endpoint under one named policy shares one counter per client address. The Learn page does not say it; the source does.
- `RateLimiting:PasswordReset` (10/60 s) covers `forgot-password`, `reset-password` and `accept-invitation` together. The test `ApiHost` only raises `RateLimiting__Login__PermitLimit`, so suites hitting these three endpoints from one address need `RateLimiting__PasswordReset__PermitLimit` raised too.
- The in-memory queue is one class: `PasswordResetQueue : BackgroundService` registered as a singleton plus `AddHostedService(sp => sp.GetRequiredService<...>())`, so endpoints inject the same instance they enqueue into. It runs under `GetDocument.Insider` too, but only waits on the channel, so build-time OpenAPI generation is unaffected.
- Change-password saves outside the row lock (`VerifyPasswordAsync` commits first); a reset by link committing in between makes its save fail on the concurrency stamp -> 412, never a lost update.

**Why:** each is non-obvious from the code and matters for impl-test and later link work.
**How to apply:** tests of the anonymous link endpoints, new anonymous endpoints, more background mail.
