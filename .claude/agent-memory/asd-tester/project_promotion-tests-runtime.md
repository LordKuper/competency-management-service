---
name: promotion-tests-runtime
description: how the promotion backend tests drive scheduler stages, and why the backend suite must not run beside the web suite
metadata:
  type: project
---

Backend suite (xunit v3, run the built dll directly; `dotnet test --project` ran zero tests) must run alone: with the web vitest suite in parallel, hosts miss the 60 s readiness window and ~200 tests fail with "The API host was not ready in 60 s". Full backend run is ~5 min.

Promotion stages: `PromotionScene.PutInStageAsync(n)` rewrites `promotion_waves.starts_on/ends_on` in SQL and polls `versions_fixed` and `processed_stage`; the host scheduler settles stages itself, and `PromotionHost` sets `Promotion__TickInterval=00:00:01`, so a shift takes seconds and the whole Promotion set about a minute. Stage 1 is the Submission stage; stage 7 is a session that is over. Dates keep a margin of days on both sides of the Moscow day, so no test depends on the hour of the run.

**Why:** a stage is global to its session, so tests that move stages cannot share one.
**How to apply:** `fixture.NewSceneAsync()` gives a test its own session (own org, optional `withSibling` second unit with its own matrix and chair); `fixture.SceneAsync()` is the class scene that stays in stage 1. A second session of the same unit needs the first moved to stage 7, then `scene.AnnounceNextAsync()`. Application ETags go stale after a stage end: reread before a write.

Race tests: hold the lock with `RowLock.HoldAdvisoryAsync(host, RowLock.PromotionKey)` (announcement is exclusive, commission and application changes shared) or `RowLock.HoldWaveAsync`, queue requests with `RowLock.StartWaitingAsync`, release. Do it on a host whose scheduler has no due wave, else its shared-lock waiter is counted.

Server answers: a user with no role on an application or wave gets 404 (not 403) from rating, review and ranking; marking the same feedback twice returns timestamps that differ in sub-millisecond precision.
