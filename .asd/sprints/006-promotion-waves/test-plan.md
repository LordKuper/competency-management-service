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
| 2 |  | delta since entry 1 (`3659c0ae`, фикс D-1: `CommissionReader.ReadAsync`) |

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `CommissionReader.ReadAsync` берёт подразделения из снимка оргструктуры (фикс D-1) | бюджет запросов рабочего стола снова превышен; подразделение без записи в снимке попадает в ответ или пропадает чужое; состав комиссии/председатель читаются из другого статуса | integration (API на реальной БД) | none (новый тест не нужен) | поведение читателя уже закреплено `DeskQueryCountTests` (бюджет 25, малые = большие данные; красный до фикса — fail-first), `PromotionWaveTests` (карточки комиссии без председателя, состав по подразделениям), `PromotionApplicationTests`/`PromotionCycleTests` (ограничения по комиссии, этапы) и `AuthorizationTests` (маршруты комиссий); изменение — замена источника той же проекции `UnitStatusProjection`, отдельный тест на «нет в снимке → нет в ответе» повторял бы одно условие `ContainsKey` (§17) |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| src/Competency.Promotion/CommissionReader.cs (регрессия D-1 закрыта `DeskQueryCountTests`, новых тестов нет) | fail-first vs D-1: мутация — возврат `FindUnitsAsync(ids)` вместо снимка; `dotnet Competency.Tests.dll -filter "/*/*/DeskQueryCountTests/*"` → exit 1, `Competency.Tests.DeskQueryCountTests.Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk")` и `(route: "/api/v1/desk/count")`: «Expected small to be between 1 and 25, but found 26» (2 теста); мутация откачена `git checkout`, после фикса тот же фильтр → exit 0, 2 зелёных; runs: 1 |

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
