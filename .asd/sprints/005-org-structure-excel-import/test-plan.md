---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/quality.md (verdict)
---

# Test plan — sprint 005-org-structure-excel-import

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
|  1 | b73aaac | full change surface (общая инфраструктура тестов изменена: схема сотрудника, `Scenarios`, хост — прогнан весь набор) |
|  2 | a2f0821 | дельта с входа 1: `PositionSelect.tsx` (77cce66, D-1); общая инфраструктура не затронута, прогнан набор тестов, рендерящих `PositionSelect` |

## Risk → check decisions

Строки входа 1 — в `test-plan.entry-01.md`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `PositionSelect`: эффект сброса учитывает, под каким подразделением выбрана должность (77cce66, D-1) | Простая смена подразделения перестала бы очищать должность, которой в новом подразделении нет | component: `EmployeeForm.test.tsx` «drops the old position once another unit is chosen …», «drops a position added in one unit when the form goes back to another» | keep (новых нет) | Тесты уже есть и проходят на исправленном коде. Мутация «очистка отключена» (`isChosenUnderOtherUnit` → `false`): `npx vitest run src/features/org-structure/EmployeeForm.test.tsx` → exit 1, оба теста; runs: 1; мутация возвращена |
| То же: смена подразделения и должности одним действием («Сделать основным») | Должность стирается до того, как форма сообщила и новое подразделение | component: `EmployeeAssignments.test.tsx` «swaps a secondary assignment …» | keep | Регрессия D-1 с доказательством fail-first на входе 1 (exit 1 на прежнем коде); на коде 77cce66 проходит |
| То же: должность, заданная при первой отрисовке и отсутствующая в перечне подразделения, больше не очищается (принятый побочный эффект) | Устаревшая должность попадёт в сохранение | не автоматизируется | none | Принятое разработчиком поведение: тест закрепил бы побочный эффект, а не требование. Правильность решает сервер (отказ при сохранении); для текущей должности сотрудника поведение закреплено `current` («keeps listing the position the employee holds …», `PositionSelect.test.tsx`), смена подразделения очищает — тесты выше |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | Нечего удалять: изменён один компонент, дублей и устаревших проверок нет | yes |

## Added tests

| Test | Regression proof |
|---|---|
| — | Новых тестов нет (см. решения выше) |

## Suite run

Вход 2 (дельта `PositionSelect.tsx`):

- Command: `npx vitest run --maxWorkers=4` над `PositionSelect`, `EmployeeForm`, `EmployeeAssignments`, `OrgStructurePage`, `MoveEmployeeModal`, `DeskScenarios`, `OnboardEmployeePage`, `OrgCleanupPage`, `editDialogs`; `npm --prefix web run lint`
- Scope: impacted (общая инфраструктура не затронута)
- Result: pass, exit 0 — web 9 файлов, 74 теста пройдено, 0 сбоев, 0 пропущено; D-1-тест «swaps a secondary assignment …» зелёный
- Lint: pass, exit 0 (biome ci, typecheck); сборка не запускалась — менялся один `.tsx`, проверенный typecheck
- HEAD: f87c37d

Вход 1 (полный набор, HEAD 78c2550): dotnet 838 из 838 пройдено; web 782 всего, 781 пройдено, 1 сбой (D-1, теперь `fixed`); lint и сборка pass. Два нестабильных теста вне области спринта (`linkScreens.test.tsx`, `catalogueTables.test.tsx`) упали по таймауту только при параллельном прогоне без `--maxWorkers=4`; подробности — `test-plan.entry-01.md`.

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | web/src/features/org-structure/AssignmentFields.tsx | AssertionError: expected "vi.fn()" to be called once, but got 0 times | the assignments in the employee form (AC-1, AC-2, AC-3) > swaps a secondary assignment with the main one in one save (AC-3) | fixed | 77cce66 |

D-1, причина: после «Сделать основным: Совмещение 1» в обоих блоках («Подразделение» и «Должность») поле должности очищается, форма не отправляется ошибкой «Выберите должность». Если у каждого подразделения свой перечень должностей (обычный случай), выбор должности в `PositionSelect` сбрасывается эффектом `onChange(undefined)` в момент, когда значение уже новое, а подразделение и «текущая» должность (`keptPosition`) ещё прежние. Проверено: если оба подразделения предлагают обе должности, тест проходит. Исправление — за разработчиком (режим test-fix); тест не ослаблялся.

## Manual verification

Не требуется: все AC проверяются автоматически; визуальные проверки (экранные снимки) в этом проекте не выполняются.
