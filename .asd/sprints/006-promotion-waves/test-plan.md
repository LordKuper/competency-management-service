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

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `CommissionReader.ReadAsync` берёт подразделения из снимка оргструктуры (фикс D-1) | бюджет запросов рабочего стола снова превышен; подразделение без записи в снимке попадает в ответ или пропадает чужое; состав комиссии/председатель читаются из другого статуса | integration (API на реальной БД) | none (новый тест не нужен) | поведение читателя уже закреплено `DeskQueryCountTests` (бюджет 25, малые = большие данные; красный до фикса — fail-first), `PromotionWaveTests` (карточки комиссии без председателя, состав по подразделениям), `PromotionApplicationTests`/`PromotionCycleTests` (ограничения по комиссии, этапы) и `AuthorizationTests` (маршруты комиссий); изменение — замена источника той же проекции `UnitStatusProjection`, отдельный тест на «нет в снимке → нет в ответе» повторял бы одно условие `ContainsKey` (§17) |
| Серверное поведение Promotion и экраны (заменяет решение `none` «Экраны Promotion в вебе» записи 1: серверное поведение AC-25, 32, 36, 50, 54, 61, 65, 70, 84, 85 API-тестами не было проверено) | ветка «не прошёл после круговой оценки» (артефакты, лимит 10, отметка), «Решение не принято» и письмо HR, письма публикации и обратной связи, сводка прошлых заявок, правка оценивающих руководителем, откат, обязательные члены комиссии, пункты стола | integration (API на реальной БД, планировщик с тиком 1 с) | add | `PromotionArtifactTests` (AC-65, 80, 85), `PromotionResultsTests` (AC-32, 36, 70), `PromotionHistoryTests` (AC-84), `PromotionRollbackTests` (AC-54), `PromotionDeskActionTests` (AC-50), `PromotionApplicationTests.Ac61_*`, `PromotionWaveTests.Ac25_*`; по одному сценарию на ветку, каждый на своей сессии (`NewSceneAsync`) |
| Демо без запросов (AC-44) и блок «Волны повышения» (AC-51) | демо шлёт запрос; блок виден без волн или даёт «Подать заявку» аккаунту без сотрудника | component | add | `DemoPage.test.tsx` (шпион на `fetch` за все шаги сценария), `PromotionWavesBlock.test.tsx` (5 случаев) |
| Многострочные инварианты Promotion: подразделение в одной незавершённой сессии (AC-2), одна незавершённая заявка сотрудника (AC-47), председатель у подразделения незавершённой сессии против правки комиссии (AC-66) | гонка двух запросов нарушает инвариант | integration (детерминированная гонка: блокировка удержана, запросы выстроены в очередь и отпущены вместе) | add | `PromotionRaceTests` (AC-2; AC-66 в обоих порядках), `PromotionApplicationTests.Ac47_TwoApplications…` (блокировка строки сессии) |
| Подразделение без действующего председателя задерживает только свои заявки (AC-66, AC-86; фикс F1) | остановка всей сессии; потеря задержанных заявок; конец этапа 5 не закрывает их | unit (`DueStep` ReachHeld) + integration (два подразделения, одно без председателя) | add | `PromotionWaveScheduleTests.Ac66_ApplicationsHeld…`, `PromotionHeldApplicationTests` (задержка при идущей сессии и передача со письмом после назначения председателя; «Решение не принято» без председателя к концу этапа 5) |
| Дата сцены зависит от суточной границы по Москве (F5); каждая смена этапа ждёт минутный тик (F6) | тест падает или идёт по другому пути после полуночи; прогон `PromotionCycleTests` около 5 мин | — | keep (изменена тестовая инфраструктура) | `PromotionHost` задаёт `Promotion__TickInterval=00:00:01`; концы этапов записываются на 2 дня назад и на 3 и более дней вперёд, так что полночь между записью и проходом ничего не меняет; набор Promotion идёт около минуты |
| Ветка «новая» компетенция в сводке AC-84 (`isNew`) | — | — | none | нужна смена опубликованной версии матрицы между сессиями; ветка — одно `Contains` в `ReviewReader.ChangeOf`, остальные ветки (прежняя средняя, изменение, без изменений) проверены |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| src/Competency.Promotion/CommissionReader.cs (регрессия D-1 закрыта `DeskQueryCountTests`, новых тестов нет) | fail-first vs D-1: мутация — возврат `FindUnitsAsync(ids)` вместо снимка; `dotnet Competency.Tests.dll -filter "/*/*/DeskQueryCountTests/*"` → exit 1, `Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk")` и `(route: "/api/v1/desk/count")`: «Expected small to be between 1 and 25, but found 26» (2 теста); мутация откачена `git checkout`, после фикса тот же фильтр → exit 0, 2 зелёных; runs: 1 |
| tests/Competency.Tests/PromotionWaveScheduleTests.cs: Ac66_ApplicationsHeldForWantOfAChair… (+1) | fail-first vs F1: мутация ветки `ReachHeld` в `DueStep.Next` (`stage <= PromotionStage.Review` → `stage <= PromotionStage.Rating - 99`), `dotnet Competency.Tests.dll -filter "/*/*/PromotionWaveScheduleTests/*"` → exit 1, `Competency.Tests.PromotionWaveScheduleTests.Ac66_ApplicationsHeldForWantOfAChair_AreOwedAHandOver_FromTheRatingCheckToTheEndOfTheReviewStage` (1 из 14); откачена `git checkout`; runs: 1 |
| tests/Competency.Tests/PromotionHeldApplicationTests.cs (2) | fail-first vs F1: (а) мутация `ReachCommissionAsync` (`ChairEmployeeId is null` → `rosters.Count < 0`), `-filter "/*/*/PromotionHeldApplicationTests/*"` → exit 1, `…Ac66_AnApplicationOfAUnitWithoutAChair_StaysAdmittedWhileTheOtherUnitsGoOn_AndReachesTheNewChairWithALetter`; (б) мутация `CloseReviewAsync` (без `Admitted`) → exit 1, `…Ac66_AnApplicationHeldForWantOfAChairToTheEnd_GetsNoDecisionWhileTheOthersAreDecided`; обе откачены; runs: 2 |
| tests/Competency.Tests/PromotionRaceTests.cs (Ac2 и Ac66 в двух порядках, 3 прогона) | n/a — покрытие готового кода; мутация: `BeginExclusiveAsync` берёт `pg_advisory_xact_lock_shared`, `-filter "/*/*/PromotionRaceTests/*"` → exit 1, красные все три прогона; откачена |
| tests/Competency.Tests/PromotionApplicationTests.cs: Ac47_TwoApplications…, Ac61_TheHeadOfTheTargetUnit…, Ac61_TheListOfRatersIsFinal… (+3) | Ac47: мутация (чтение волны в `CreateAsync` без `FOR UPDATE`) → exit 1, `…Ac47_TwoApplicationsOfOneCandidateFiledAtTheSameMoment_LeaveExactlyOneInTheSession` («The request did not wait for the lock»); откачена. Ac61: n/a |
| tests/Competency.Tests/PromotionArtifactTests.cs (1) | n/a; мутации `ArtifactInput.MaxLinks` 10 → 11 и `ArtifactsMissing = false` в `ReachCommissionAsync`, `-filter "/*/*/PromotionArtifactTests/*"` → exit 1, `…Ac65_Ac85_Ac80_ACandidateShortOfTheRequirements_…` оба раза; откачены |
| tests/Competency.Tests/PromotionResultsTests.cs (1), PromotionDeskActionTests.cs (1), PromotionHistoryTests.cs (1), PromotionRollbackTests.cs (1) | n/a — покрытие готового кода |
| tests/Competency.Tests/PromotionWaveTests.cs: Ac25_TheResponsibleHrAndTheHead… (+1) | n/a |
| web/src/features/promotion/demo/DemoPage.test.tsx (1) | n/a; мутация (`fetch` в теле `DemoPage`) → `vitest run src/features/promotion/demo/DemoPage.test.tsx` exit 1, «plays every step of the scenario without sending a single request»; откачена |
| web/src/features/desk/PromotionWavesBlock.test.tsx (5) | n/a; мутации: `canApply` всегда истина → красный «offers no application to an account without an employee»; `rows.length === 0` → `-1` в `PromotionWavesView` → красный «is hidden when no session offers a track»; откачены |
| tests/Competency.Tests/PromotionCycleTests.cs, Infrastructure/PromotionScene.cs, MailCatcher.cs, RowLock.cs | вспомогательный код и правка F10 (id AC убраны из doc-комментария и `because`); не тест |

## Suite run

- Command: `dotnet Competency.Tests.dll -filter "/*/*/Desk*/*" -filter "/*/*/Promotion*/*" -filter "/*/*/AuthorizationTests/*" -filter "/*/*/OptimisticConcurrencyTests/*"` (из `tests/Competency.Tests/bin/Debug/net10.0`, бэкенд один, без веб-набора); сборка и lint: `dotnet build Competency.slnx --tl:off`, `npm --prefix web run lint`
- Scope: impacted — дельта (`3659c0ae`) в одном файле модуля Promotion, общей инфраструктуры не касается (предохранитель не срабатывает); набор: все классы `Desk*` (каждый запрос рабочего стола идёт через источник Promotion), `Promotion*` (мастер, заявки, цикл, расчёт, календарь), `AuthorizationTests` и `OptimisticConcurrencyTests` (маршруты комиссий)
- Result: pass — бэкенд 202 прогона, 0 упало, 0 пропущено, 0 ошибок, exit 0; `DeskQueryCountTests` зелёный (бюджет 25). Веб-набор не затронут дельтой и не запускался
- Lint / build: pass — `dotnet build` 0 предупреждений и 0 ошибок; `biome ci` и `tsc` чисто
- HEAD: ae56f55d — измеренное дерево; последующие коммиты записи правят только `.asd/sprints/**`

## Defects

Прогон `DeskQueryCountTests` отдельно при бюджете 25: оба случая теории красные, наблюдаемое число команд малых данных — 26 для `/api/v1/desk` и для `/api/v1/desk/count` (сравнение «малые = большие» до него не доходит). SQL-журнал запроса `/api/v1/desk` (26 команд): после команд источника карьеры (№ 1–19) идут № 20 `promotion_commissions`, № 21 `promotion_commission_members`, № 22–25 `org_unit_hr`, `user_roles/users`, `employees`, `user_roles/users` и № 26 `promotion_wave_units/promotion_waves`. Источник Promotion: `CommissionFindingsAsync` (чтение составов комиссий `commissions.ReadAsync` — № 20–21, запрос волн подразделений — № 26, `career.ResponsibleUnitsAsync` — вероятно № 22–25). Достаточно убрать одну команду, например объединить чтение комиссий и волн подразделений в один запрос или вынести повторные проверки ролей.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.Promotion/PromotionDeskSource.cs | Expected small to be between 1 and 25, but found 26. | Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk") и (route: "/api/v1/desk/count") | fixed | 3659c0ae |

## Manual verification (optional)

Не требуется: визуальная сторона пояснений AC-92 и экранов Promotion проверяется ревью по макетам; единственного пути без автоматической проверки нет.
