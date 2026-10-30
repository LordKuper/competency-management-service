---
responsibility:
  owns: сегмент повествования записи 4 test-plan.md (решения риск→проверка, удаления, добавленные тесты)
  excludes: Entry log, Suite run, Defects, Manual verification (остаются в живом test-plan.md)
  delegates_to: test-plan.md
---

# Test plan — sprint 006-promotion-waves, запись 4

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
