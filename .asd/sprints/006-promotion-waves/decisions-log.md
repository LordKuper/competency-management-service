---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
- YYYY-MM-DD — cleanup checkpoint <sha>
- YYYY-MM-DD — cleanup: removed <paths/symbols>; reverted <ids or none>; verified <build, lint, tests @sha>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->
- 2026-10-27 — route Task 1: standard, dispatch HEAD 4290e66
- 2026-10-27 — Task 1 flagged choices accepted: CompetencyType/ScalePoint public for the port; PublishedTrackMatricesAsync keyed by matrix unit (AC-47); extra public port records; Score edges (null threshold = none, no score → share 0 and not met); education row in MatrixBlocks; no earlier disposition found
- 2026-10-27 — route Task 2: standard, dispatch HEAD 2c26acc
- 2026-10-27 — Task 2 flagged choices accepted: ApplicationServices.cs authorised (module registration lives there, needed for EF design-time); lock files committed; TimeZone validated in AddPromotionModule; promotion_wave_matrices carries track_id/track_name; PUT waves/{id} takes 5006001 exclusive; commission is no unit-deletion blocker (released with the unit), wave units are; commission PUT semantics and 409/400 rules; date ceiling today+5y; Task 5 needs its own idempotency marker for stage-1 fixing; open gap: employee deletion blocked by promotion FKs with a generic 409 — carried to impl assessment; no earlier disposition found
- 2026-10-27 — route Task 3: standard, dispatch HEAD 4fa9fc4
- 2026-10-27 — Task 3 done (f391299): accepted flags — new `GET waves/{waveId}/targets/{matrixId}` (target form for the apply screen; any signed-in user while the wave is in stage 1) joins the AC-46 route list; warnings and formal failures returned as `FormalFailure` codes, wording in SPA; acting-employee check moved to `ActingEmployees`, `WaveReader.AccountNamesAsync` public, behaviour unchanged; formal data only by responsible HR of target unit, raters by candidate (stage 1) and head (stage 3); candidate sees Decided/NoDecision as UnderReview before stage 6. Rater-search gap closed: existing `GET /api/v1/employees` is `Authenticated` for every signed-in user, Task 8 uses it. Seams for Task 4: `CanBeWithdrawn` + decision columns, `ShownStatus`, FK rule when a head removes a rater who already rated. Not verified at runtime: EF translation of `UnfinishedStatuses.Contains`, `formal_failures` enum array, application `FOR UPDATE` — impl-test.
- 2026-10-27 — route Task 4: standard, dispatch HEAD 1edc3ac
- 2026-10-27 — Task 4 done (0aa7c36): accepted flags — `ApplicationRole.Chair` (acting chair of the target matrix unit's commission) gates review PUT; `ScoreRules.Problems` is the one score check (self, rater, final); rating PUT without If-Match (rater's own rows), artifacts/review/feedback use If-Match; decision changeable in stage 5, not clearable; review GET stage ≥5 / PUT stage 5 over UnderReview/Decided; unrounded rating/ranking, rank only among scored; 404 no part / 403 visible without right; candidate `Result` from stage 6 without rating, rank, readiness or rater data; feedback POST idempotent. New routes join the AC-46 list: `ratings/mine`, `…/rating` GET/PUT, `…/artifacts`, `…/feedback`, `…/review` GET/PUT, `waves/{id}/units/{unitId}/ranking`. Seams for Task 5: `CircularRating.Averages` + `ICareerModel.Score` → `meets_after_rating`; `UnmetCompetencyIds` vs artifacts → `artifacts_missing`; scheduler moves Admitted → UnderReview at end of stage 4, UnderReview → NoDecision at end of stage 5; AC-26 chair check and letters stay in Task 5. Not verified at runtime: migration, `links` jsonb, rater-score composite FK, enum `Contains` in joins — impl-test.
- 2026-10-27 — route Task 5: standard, dispatch HEAD e0517ea
- 2026-10-28 — Task 5 dispatch interrupted by an expired API token (no changes on disk); resumed the same agent.
- 2026-10-28 — Task 5 done (98e347f, 6b3847e): accepted flags — out-of-scope `IEmployeeDirectory.FindWorkingInUnitsAsync` (OrgStructure) for the AC-38 announcement; `promotion_waves.versions_fixed` idempotency marker for stage-1 binding; AC-26 hold: end-of-stage-4 step waits while a matrix unit of an Admitted application has no acting chair; announcement at POST only when StartsOn > today; `Letter.RecipientIds` batches same-text letters; first scheduler pass one interval after start; tick logic in `PromotionTick`/`WaveStep` for fake-time tests; rater reminder <2 days left in stage 3; recipients per stage as reported. Orchestrator fix request: raters added by the head in stage 3 get the invitation (6b3847e, `PromotionMail.InviteRatersAsync` shared with stage-2 end). For the impl assessment: chair missing through all of stage 5 → catch-up runs steps 4 and 5 in one pass → NoDecision; applications rejected (400 matrixId) during the first ≤1 min of stage 1 until versions are bound. Desk seams for Task 6: visibility from computed stage, status and flags, never `processed_stage`.
- 2026-10-28 — route Task 6: standard, dispatch HEAD 2087cc6
- 2026-10-28 — Task 6 done (82c34c5): accepted flags — compile-only rows for the 7 new kinds in `deskTexts.test.ts` `ACTION_PATH` (custom-coding-rules «Contract surface changes»); formal-check item per wave, only Submitted without `HiredOrPromotedOn`, link `/promotion/waves/{id}` for HR only; Platform `DeskDetails.ApplicationCount`/`Until` and seven promotion desk groups; `PromotionWaveOpen` stays a reference item per plan (AC-39 lists «подать заявку» among expected actions — named for the impl assessment), shown to every acting employee without an unfinished application once versions are bound; desk object names within AC-37; `deskPaths` in deskTexts.ts until Task 7 adds `routes.ts` — Task 7 points them at its route constants. Not verified at runtime: EF translation of the desk queries, SQL budget 25 — impl-test.
- 2026-10-28 — route Task 7: standard, dispatch HEAD 16ec9b5
- 2026-10-28 — Task 7 done (4b2d534, 6f9d0e6): accepted flags — menu group «Управление повышением» (Волны, Комиссии for admin/HR; Настройки for admin); AppShell current item = longest matching path; existing `AppShell.test.tsx` menu expectations likely red — impl-test; versions read from the ETag header (`Versioned<T>`), `gcTime: 0` on details; HR units from `GET career/me` `hrUnits`; unit tree: checking a parent checks descendants, unchecking removes only that unit; date defaults inclusive N days per stage; interval warning computed in the browser from settings, server decides at stage-2 end; formal dialog reads `GET applications/{id}`; `dayjs` via antd as in Providers.tsx; deskTexts → promotion/routes.ts import (lazy pages). Views for Task 10: SettingsView, CommissionView, WaveListView, WaveView, WaveApplicationsView, FormalView, UnitCheckTree (need Router + antd App context). Shared texts: promotionTexts.ts.
- 2026-10-28 — route Task 8: standard, dispatch HEAD 58ecfed
- 2026-10-28 — Task 8 done (0894814, adcde4a): accepted flags — top menu item «Повышение» (order 1), admin group items shifted to 2–4; shared code extracted from Task 7 (`useEmployeeOptions`, `StageSteps`, `STAGE`); `/promotion/apply/:waveId` creates or edits the candidate's Submitted application; withdraw button for Submitted/Admitted/UnderReview, a 409 «решение принято» shown as a message; candidate/rater/head routes have no role guard in SPA (server 403/404 → ErrorAlert), desk links checked per AC-46. Orchestrator fix request: `decision` added to `ApplicationSummaryResponse` via `ShownDecision` (candidate from stage 6), per-card `useQueries` removed (adcde4a). `AppShell.test.tsx` menu expectations likely red — impl-test.
- 2026-10-28 — route Task 9: standard, dispatch HEAD 36dd27a
- 2026-10-28 — Task 9 done (cc116b7): accepted flags — AC-29: the chair's form shows the server-computed rating, threshold and readiness after save (no second formula in TS; the review GET carries no blocks/weights); live per-competency «Требование» only — named for the impl assessment; commission routes without SPA role guard (server 403/404, as Task 8); AC-46 links: «Рассмотрение» in the wave applications list from stage 5, card ↔ ranking links; built on DESIGN.md without a promotion ux-spec. Views for Task 10: RankingView, ReviewView (+ Task 7–8 views).
- 2026-10-28 — route Task 10: standard, dispatch HEAD 26a0129
- 2026-10-28 — Task 10 done (05ba7cc): accepted flags — `demo/scenario.tsx` (JSX renders, not .ts); DemoPage blocks in-view link clicks, `canApply=false` and statuses without navigate buttons; ApplyView shown in edit mode with a prefilled application (no input needed); note «Данные условные…» above the steps; last step HR WaveView at stage 7 (feedback check is a server step, AC-36); role banner as antd Alert, prev/next navigation, 19 steps; menu item «Демонстрация» for admin. Views checked: no queries or mutations inside views (AC-44).
- 2026-10-28 — impl completion gate passed at eec5a93: `dotnet build` 0 warnings/0 errors, web build and lint clean; diff paths 4fa9fc4..HEAD within dispatched write scopes plus accepted flags (OrgStructure `FindWorkingInUnitsAsync`, `deskTexts.test.ts` compile rows); no stubs for this sprint in stubs.md; all plan.md checkboxes ticked.
- 2026-10-28 — initial impl assessment: user requested changes — for every employee a separate desk block with available promotion waves and their tracks, hidden when there are none (replaces the `PromotionWaveOpen` reference item); AC-29 kept: rating and readiness recalculated by the server after save. Known limits accepted with the assessment answer pending re-run: chair missing through stage 5 catch-up, ≤1 min stage-1 window, generic 409 on deleting referenced employees/units, tzdata unverified.
- 2026-10-28 — scope amendment accepted by user: AC-39 retired, AC-50 (desk actions without «подать заявку») and AC-51 (desk block «Волны повышения»: open waves with tracks, deadline and apply link; upcoming with tracks and start date; hidden when none) carried by Task 11 in wave 11; audit boolean unchanged (documents.audit stays true, no new migration or gate impact).
- 2026-10-28 — route Task 11: standard, dispatch HEAD 1da4aec
- 2026-10-28 — Task 11 done (06ce684): accepted flags — `deskTexts.test.ts` row for `PromotionWaveOpen` removed (compile only); block hidden while loading or on error of `waves/open`/`applications/mine`; waves without targets skipped; desk table view `PromotionWavesView` instead of reusing card-style `OpenWavesView`, `unfinishedApplicationIdByWave` shared with `/promotion`; removed only what the deleted item used (`SubmissionStage`, `OpenWaveFindingsAsync`, `deskPaths.promotionApply`); block placed between «Ожидают вашего действия» and «Что можно сделать» (no desk ux-spec entry — design-promote); links only for users with an employee, none for upcoming waves.
- 2026-10-28 — impl completion gate re-run passed at 06ce684: `dotnet build` 0 warnings/0 errors, web build and lint clean, Task 11 paths within its write scope, all plan.md checkboxes ticked.
