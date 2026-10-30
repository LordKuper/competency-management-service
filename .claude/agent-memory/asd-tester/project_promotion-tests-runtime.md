---
name: promotion-tests-runtime
description: how the promotion backend tests drive scheduler stages, and why the backend suite must not run beside the web suite
metadata:
  type: project
---

Backend suite (xunit v3, run the built dll directly; `dotnet test --project` ran zero tests) must run alone: with the web vitest suite in parallel, hosts miss the 60 s readiness window and ~200 tests fail with "The API host was not ready in 60 s". Full backend run is ~5 min.

Promotion stages: `PromotionScene.PutInStageAsync(n)` rewrites `promotion_waves.starts_on/ends_on` in SQL and polls `versions_fixed` and `processed_stage` (up to 180 s); the host scheduler settles stages itself. Stage 1 is the Submission stage; `PromotionCycleTests` (4 stage shifts) takes about 5 min, keep it as one scenario.

**Why:** each stage shift waits for a scheduler tick, so splitting the lifecycle into several tests multiplies minutes.
**How to apply:** add lifecycle assertions to `PromotionCycleTests`; use a fresh `PromotionHost` per class (the scene is per host); application tests share the stage-1 scene.

Server answers: a user with no role on an application or wave gets 404 (not 403) from rating, review and ranking; marking the same feedback twice returns timestamps that differ in sub-millisecond precision.
