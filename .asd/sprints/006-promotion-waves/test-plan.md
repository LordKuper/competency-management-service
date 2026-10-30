---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/quality.md (verdict)
---

# Test plan — sprint 006-promotion-waves

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | e16a232e2d9b7d86c6e2672a3a23a2b03c2ce926 | full change surface (Tasks 1–27, AC-1…AC-93; AC-9, AC-27, AC-39 сняты) |
| 2 | 0a2ae2834d7d84621b92648f16241b4d21ed30cb | delta since entry 1 (`3659c0ae`, фикс D-1: `CommissionReader.ReadAsync`) |
| 3 | 47b455e4e6716896e4f6652118fc13b1eaeb98aa | delta since entry 2 (`0a2ae283`): review-fix wave-1/iter-01 (F1–F10), Task 28 (AC-94, AC-95), правка веб-поиска подразделений (`useUnitTreeSearch`) |
| 4 |  | delta since entry 3 (`47b455e4`): review-fix wave-1/iter-02 (F1 `WasHeldAtClose` и миграция, F2 `PromotionScene`, F3 JSDoc `useUnitTreeSearch`, F4 условие передачи в `WaveStep`/`PromotionTick`) |

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Метка `WasHeldAtClose` и откат (F1): заявка, задержанная без председателя до конца этапа 5, при откате возвращается в «Допущена», а не уходит к комиссии без проверки председателя | откат на этап 4 или 5 отдаёт задержанную заявку комиссии, у которой нет председателя (письмо, отметка артефактов и проверка пропущены); метка остаётся и портит следующий откат | unit (`ReturnToStage`) + integration (API на реальной БД, планировщик с тиком 1 с) | add | `PromotionScoringTests.Ac66_AnApplicationHeldForWantOfAChair…` (этапы 4 и 5: «Допущена», метка снята, письмо не ждёт); `PromotionHeldApplicationTests.Ac66_Ac53_AnApplicationHeldToTheEnd…` (этапы 4 и 5: подразделение без председателя после отката снова «Допущена» сразу, подразделение с председателем возвращается к комиссии как прежде, после назначения председателя заявка поступает с письмом). Строка «NoDecision → UnderReview» без метки уже закреплена `PromotionScoringTests.Ac53_ReturningAWaveToAStage…`; расширять её колонкой метки не стали |
| `ReturnToStage` сбрасывает `IsSentToReview` у «Допущена»: что станет с задержанной заявкой, которую кандидат отправил на рассмотрение на этапе 4 | кандидат теряет отправку или отправленная заявка получает метку задержки | — | none | отправка без председателя отклонена до записи состояния: `StageActions.SendToReviewAsync` возвращает `false` раньше `IsSentToReview = true` (ответ 409), поэтому задержанная заявка флага не несёт, а заявка, отправленная при председателе, после этого «На рассмотрении» и метку не получает (метка ставится только у «Допущена»); противоречия с AC-54 нет, `D-N` не заводился |
| Условие передачи в `WaveStep.HasAdmittedAsync` и `PromotionTick` — `MeetsAfterRating == true || ProcessedStage >= Artifacts` (F4) | задержанная заявка, которая должна двигаться, не подхватывается планировщиком, в том числе после отката по F1 | integration | add | ветка «прошёл» закреплена тем же тестом откатов на этап 4 (заявка проходит при `ProcessedStage = 3`); ветка «после артефактов» не закреплена ничем (мутация ниже оставляла `PromotionArtifactTests` зелёным — конец этапа 4 передаёт заявки сам, минуя `ReachHeld`): `PromotionHeldApplicationTests.Ac66_Ac65_AnApplicationOwingArtifactsHeldPastTheDeadline…` — заявка с недостающими артефактами в подразделении без председателя дожидается его и поступает с отметкой AC-65. Ускорение холостых проходов в jsdom/интеграции не измеряется |
| `PromotionScene.PutInStageAsync` без неиспользуемого `waveId` (F2); перенос комментариев в JSDoc `useUnitTreeSearch.ts` (F3) | — | — | keep (изменена тестовая инфраструктура; оформление) | поведение не менялось; `useUnitTreeSearch.test.tsx` и `OrgUnitSelect.test.tsx` в полном прогоне зелёные; F5 затрагивает только `test-plan.md` |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| tests/Competency.Tests/PromotionScoringTests.cs: Ac66_AnApplicationHeldForWantOfAChairWhenTheCommissionStageEnded_GoesBackToAdmitted_AndIsMarkedNoMore (2) | мутации (откат `git checkout -- <файл>` в той же команде), `dotnet Competency.Tests.dll -method "*Ac66_Ac53_AnApplicationHeldToTheEnd*" -method "*Ac66_AnApplicationHeldForWantOfAChairWhenTheCommissionStageEnded*"` из `tests/Competency.Tests/bin/Debug/net10.0` после `dotnet build`: (а) из `PromotionApplication.ReturnToStage` убрано `|| WasHeldAtClose` → exit 1, 4 из 4 красных, среди них `…GoesBackToAdmitted_AndIsMarkedNoMore(stage: 4)` и `(stage: 5)`; (в) `WasHeldAtClose = false;` убрано → exit 1, красны оба `…IsMarkedNoMore(stage: 4)` и `(stage: 5)` (фильтр только на юнит-тест); обе откачены; runs: 2 |
| tests/Competency.Tests/PromotionHeldApplicationTests.cs: Ac66_Ac53_AnApplicationHeldToTheEndOfTheCommissionStage_AfterARollbackWaitsForTheChairAgain_AndReachesTheCommissionWithALetter (2) | мутации тем же приёмом: (а) та же, что выше (один прогон на 4 теста) → exit 1, красны `…AndReachesTheCommissionWithALetter(stage: 4)` и `(stage: 5)`; (б) в `WaveStep.HasAdmittedAsync` условие сужено до `processedStage >= PromotionStage.Artifacts` (без `MeetsAfterRating == true`) → exit 1, красен `…AndReachesTheCommissionWithALetter(stage: 4)` (заявка при `ProcessedStage = 3` не подхвачена, ожидание 60 с истекает); обе откачены; runs: 1 ((а) — тот же прогон, что в строке выше) |
| tests/Competency.Tests/PromotionHeldApplicationTests.cs: Ac66_Ac65_AnApplicationOwingArtifactsHeldPastTheDeadline_ReachesTheNewChairMarkedAsMissingThem | мутация (г): условие `HasAdmittedAsync` сужено до `application.MeetsAfterRating == true` (без `processedStage >= Artifacts`) → `-method "*Ac66_Ac65_*"`, exit 1, красен `Ac66_Ac65_AnApplicationOwingArtifactsHeldPastTheDeadline_ReachesTheNewChairMarkedAsMissingThem`; откачена; та же мутация с `-filter "/*/*/PromotionArtifactTests/*"` → exit 0 (существующий тест её не ловил, ради этого тест и добавлен); runs: 1 |

## Suite run

- Command: `dotnet test --solution Competency.slnx` (бэкенд один) и затем `npm --prefix web test`; сборка и lint: `dotnet build Competency.slnx --tl:off`, `npm --prefix web run lint`
- Scope: full — предохранитель общей инфраструктуры сработал (дельта в `Competency.Platform`: `MailSender`, `WebAddress`, миграция, снимок модели); набор не сужался
- Result: pass — бэкенд 952 прогона, 0 упало, 0 пропущено, exit 0; веб 83 файла и 850 тестов зелёных, exit 0. До записи: бэкенд 947 зелёных. Веб с первого запуска: 849 из 850, тайм-аут 5 с в `auth/linkScreens.test.tsx` (файл вне дельты; отдельно 18 из 18, полный повтор 850 из 850) — шум нагрузки параллельных воркеров, как в записи 3
- Lint / build: pass — `dotnet build` 0 предупреждений и 0 ошибок; `biome ci` и `tsc` чисто
- HEAD: dc568cde0ead622df62db640ed12becffa68843b — измеренное дерево, коммит тестов этой записи; последующие коммиты записи правят только `.asd/sprints/**`

## Defects

Прогон `DeskQueryCountTests` отдельно при бюджете 25: оба случая теории красные, наблюдаемое число команд малых данных — 26 для `/api/v1/desk` и для `/api/v1/desk/count` (сравнение «малые = большие» до него не доходит). SQL-журнал запроса `/api/v1/desk` (26 команд): после команд источника карьеры (№ 1–19) идут № 20 `promotion_commissions`, № 21 `promotion_commission_members`, № 22–25 `org_unit_hr`, `user_roles/users`, `employees`, `user_roles/users` и № 26 `promotion_wave_units/promotion_waves`. Источник Promotion: `CommissionFindingsAsync` (чтение составов комиссий `commissions.ReadAsync` — № 20–21, запрос волн подразделений — № 26, `career.ResponsibleUnitsAsync` — вероятно № 22–25). Достаточно убрать одну команду, например объединить чтение комиссий и волн подразделений в один запрос или вынести повторные проверки ролей.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.Promotion/PromotionDeskSource.cs | Expected small to be between 1 and 25, but found 26. | Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk") и (route: "/api/v1/desk/count") | fixed | 3659c0ae |

## Manual verification (optional)

Не требуется: визуальная сторона пояснений AC-92 и экранов Promotion проверяется ревью по макетам; единственного пути без автоматической проверки нет.
