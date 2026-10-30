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
| 4 | 905a7fa938d04a53117fc8a465acb9892e0d5e27 | delta since entry 3 (`47b455e4`): review-fix wave-1/iter-02 (F1 `WasHeldAtClose` и миграция, F2 `PromotionScene`, F3 JSDoc `useUnitTreeSearch`, F4 условие передачи в `WaveStep`/`PromotionTick`) |
| 5 | 25f8198d6ebf8db47a6121f890079cf033b3ab15 | delta since entry 4 (`905a7fa9`): review-fix wave-1/iter-03 F1 — комментарий `hasAdmitted` в `DueStep.cs` |

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Текст XML-комментария `<param name="hasAdmitted">` в `DueStep.Next` (review-fix wave-1/iter-03 F1, `a9709ef6`) | — | — | none | изменён только комментарий документации, код и тесты не тронуты; поведение то же, проверять нечего. Затронутый набор (`PromotionWaveScheduleTests`, использует `DueStep`) прогнан как страховка |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|

## Suite run

- Command: `dotnet test --solution Competency.slnx`, затем `npm --prefix web test` (последовательно, бэкенд отдельно первым); `npm --prefix web run lint`; `dotnet build Competency.slnx --tl:off && npm --prefix web run build`
- Scope: full — полный неограниченный прогон (терминальный прогон impl-review)
- Result: pass — бэкенд: итог 952, успешно 952, сбой 0, пропущено 0, exit 0 (5 мин 13 с); web (vitest): 83 файла из 83, 850 тестов из 850, exit 0 (84 с); известный флак `linkScreens.test.tsx` не повторился
- Lint / build: pass — `npm --prefix web run lint` exit 0 (biome, 363 файла, без правок); `dotnet build` + `npm --prefix web run build` (tsc, vite) exit 0
- HEAD: 4546dfef6de9eb789a915b7530fe334bdefe1525 — измеренное дерево; коммит записи правит лишь `.asd/sprints/**`

## Defects

Прогон `DeskQueryCountTests` отдельно при бюджете 25: оба случая теории красные, наблюдаемое число команд малых данных — 26 для `/api/v1/desk` и для `/api/v1/desk/count` (сравнение «малые = большие» до него не доходит). SQL-журнал запроса `/api/v1/desk` (26 команд): после команд источника карьеры (№ 1–19) идут № 20 `promotion_commissions`, № 21 `promotion_commission_members`, № 22–25 `org_unit_hr`, `user_roles/users`, `employees`, `user_roles/users` и № 26 `promotion_wave_units/promotion_waves`. Источник Promotion: `CommissionFindingsAsync` (чтение составов комиссий `commissions.ReadAsync` — № 20–21, запрос волн подразделений — № 26, `career.ResponsibleUnitsAsync` — вероятно № 22–25). Достаточно убрать одну команду, например объединить чтение комиссий и волн подразделений в один запрос или вынести повторные проверки ролей.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.Promotion/PromotionDeskSource.cs | Expected small to be between 1 and 25, but found 26. | Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk") и (route: "/api/v1/desk/count") | fixed | 3659c0ae |

## Manual verification (optional)

Не требуется: визуальная сторона пояснений AC-92 и экранов Promotion проверяется ревью по макетам; единственного пути без автоматической проверки нет.
