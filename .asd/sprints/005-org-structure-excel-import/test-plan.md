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
|  3 | c33c15a | дельта с входа 2 (a2f0821): правки по ревью 05af24b…b828f81 — `MaxLevels`, `ImportText.Key`, адресаты ссылок рабочего стола для не-администратора, отказ на `null` в списках `decisions`/`heads`; общая инфраструктура не затронута, прогнаны тесты импорта и рабочего стола |

## Risk → check decisions

Строки входов 1 и 2 — в `test-plan.entry-01.md` и `test-plan.entry-02.md`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `OrgImportParser.MaxLevels = 30`, ошибка заголовка «В файле больше 30 уровней подразделений.» (external-1) | Заголовок с 31-й колонкой уровня принят или ошибка не привязана к строке заголовка и первой колонке сверх предела; ровно 30 уровней отвергнут | unit (в процессе, без БД): `OrgImportParserTests` `Ac11_AHeaderWithOneLevelColumnOverTheLimit_…`, `Ac11_AHeaderWithExactlyTheLimitOfLevelColumns_IsAccepted` | add | Граница с обеих сторон: 31 → одна ошибка (строка 1, колонка «Подразделение 31-го уровня»), 30 → принят. Предел берётся из `OrgImportParser.MaxLevels`, не числом |
| `ImportText.Key` чистит пробелы сам (external-2) | Сохранённое ФИО с двойным пробелом внутри («Али  оглы») не совпадает с файлом: создаётся дубль, оригинал предлагается уволить | component на реальной БД: `OrgImportTests` `Ac14_Preview_MatchesAStoredNameWithADoubleInnerSpace_…` | add | Через API создаётся сотрудник с отчеством «Али  оглы», файл даёт «Али оглы»: в плане нет `newEmployees`, сотрудник не в `missingEmployees`. Существующий Ac14 (регистр, ё, пробелы в имени файла) двойной пробел в хранимом имени не покрывает |
| `OrgImportEndpoints.TryReadList` отвергает список с `null` (external-4) | `decisions=[null]` или `heads=[null]` при верном отпечатке даёт 500 вместо 400 | component на реальной БД: `OrgImportApplyTests` `Ac18_Apply_RefusesAListWithANullElement_With400_NotAFailure` (теория: `decisions`, `heads`) | add | 400 с одной ошибкой поля, ничего не применено (`AssertNothingAppliedAsync`). Соседние Ac18-отказы `null`-элемент не задевают |
| Ссылки рабочего стола для не-администратора в группе «подключить подразделение»: `connectUnitTarget`, `itemAction`, `itemLink`, `OrgFinding`, `DeskGroups` (external-3) | HR получает ссылку на сценарий администратора (`/desk/connect-unit/:id`), недоступный ему; администратор теряет свой сценарий | unit: `deskTexts.test.ts` (шесть видов группы выводятся из `ACTION_PATH`, не списком; подписи кнопки обоих вариантов); component: `DeskPage.test.tsx` «leads HR, who cannot open …» (имя и действие строки), `OrgCleanupPage.test.tsx` «leads a $role to $href» (Hr и GlobalAdmin) | add | Администратор по-прежнему ведёт на connect-unit: `ACTION_PATH` (с `isAdmin=true`), существующий DeskPage-тест с `GlobalAdmin`, случай `GlobalAdmin` в OrgCleanup |
| `DeskScenarios.entryPath` передаёт `isAdmin` в `itemAction` (external-3) | Вход сценария для не-администратора ведёт на connect-unit | не автоматизируется | none | Ветка `itemAction` достижима только у сценариев без собственного `path` («Доработать», «Согласовать»), чьи группы содержат лишь версии; элементов группы «подключить подразделение» в них нет, поведение не меняется. Существующие тесты `DeskScenarios` прогнаны |
| `ResultStep.tsx`, `ImportText.cs` — правка doc-комментариев (combined-F1) | — | не автоматизируется | none | Поведения не меняют |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | Нечего удалять: дельта — исправления по ревью, устаревших и дублирующих проверок нет | yes |

## Added tests

| Test | Regression proof |
|---|---|
| `OrgImportParserTests.Ac11_AHeaderWithOneLevelColumnOverTheLimit_IsAnErrorOfTheHeaderRow_NamingThatColumn` (external-1) | Мутация `maxLevel > MaxLevels` → `maxLevel > MaxLevels + 1000` в копии HEAD: `dotnet test --project tests/Competency.Tests --filter-method "*Ac11_AHeader*"` → exit 2, упал этот тест (сбой 1 из 2); runs: 1; мутация только в копии, рабочее дерево не менялось |
| `OrgImportParserTests.Ac11_AHeaderWithExactlyTheLimitOfLevelColumns_IsAccepted` (external-1) | Охранный тест границы «ровно 30»: проходит и до исправления, fail-first не применим |
| `OrgImportTests.Ac14_Preview_MatchesAStoredNameWithADoubleInnerSpace_ToTheNameOfTheFile_NotAsANewEmployeeAndADismissal` (external-2) | Мутация `Clean(name).ToLowerInvariant()` → `name.ToLowerInvariant()` в `ImportText.Key` копии HEAD: `dotnet test --project tests/Competency.Tests --filter-method "*Ac14_Preview_MatchesAStoredName*"` → exit 2, упал этот тест; runs: 1 |
| `OrgImportApplyTests.Ac18_Apply_RefusesAListWithANullElement_With400_NotAFailure` (теория `decisions`, `heads`; external-4) | Мутация `return requested.All(item => item is not null);` → `return true;` в копии HEAD: `dotnet test --project tests/Competency.Tests --filter-method "*Ac18_Apply_RefusesAListWithANullElement*"` → exit 2, упали оба случая `(field: "decisions")` и `(field: "heads")`, ответ 500 вместо 400; runs: 1 |
| `deskTexts.test.ts`: «opens the unit card, not the administrators' scenario, for a <kind> item when the user is not an administrator» (6 видов), «labels the button of a unit with a gap by where it leads …»; `DeskPage.test.tsx`: «leads HR, who cannot open the administrators' scenario, from a unit with a gap to the unit card …»; `OrgCleanupPage.test.tsx`: «leads a Hr to /org-structure?unit=u2» (external-3) | Прежний код: `git archive a2f0821 web` + новые тесты, `npx vitest run src/features/desk/deskTexts.test.ts src/features/desk/DeskPage.test.tsx src/features/desk/OrgCleanupPage.test.tsx` → exit 1, 9 сбоев из 158: шесть «opens the unit card … for a UnitWithoutHead / UnitWithoutHr / UnitWithoutPositions / HeadWithoutAccount / HeadAccountInvited / HeadAccountBlocked item …», «labels the button of a unit with a gap …», «leads HR, who cannot open …», «leads a Hr to /org-structure?unit=u2»; runs: 1 |
| `OrgCleanupPage.test.tsx`: «leads a GlobalAdmin to /desk/connect-unit/u2» (external-3) | Охранный тест: проходит и на прежнем коде (администратор там всегда вёл на connect-unit); защищает от обратной ошибки в `ConnectUnit` |

## Suite run

Полный прогон терминального impl-review wave-1/iter-02 (не impacted), HEAD: 50090b322135ffff26b0e60a3417b08ee10e86f5:

- dotnet: `dotnet test --solution Competency.slnx` → exit 0, итог 843, успешно 843, сбой 0, пропущено 0
- web: `npm --prefix web test -- --maxWorkers=4` → exit 0, 75 файлов пройдено из 75, 792 теста пройдено из 792, 0 сбоев; нестабильные `linkScreens.test.tsx` и `catalogueTables.test.tsx` не упали
- Lint: `npm --prefix web run lint` → exit 0
- Build: `dotnet build Competency.slnx --tl:off` → exit 0, 0 предупреждений, 0 ошибок; `npm --prefix web run build` → exit 0
- Дефекты: новых нет

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | web/src/features/org-structure/AssignmentFields.tsx | AssertionError: expected "vi.fn()" to be called once, but got 0 times | the assignments in the employee form (AC-1, AC-2, AC-3) > swaps a secondary assignment with the main one in one save (AC-3) | fixed | 77cce66 |

D-1, причина: после «Сделать основным: Совмещение 1» в обоих блоках («Подразделение» и «Должность») поле должности очищается, форма не отправляется ошибкой «Выберите должность». Если у каждого подразделения свой перечень должностей (обычный случай), выбор должности в `PositionSelect` сбрасывается эффектом `onChange(undefined)` в момент, когда значение уже новое, а подразделение и «текущая» должность (`keptPosition`) ещё прежние. Проверено: если оба подразделения предлагают обе должности, тест проходит. Исправление — за разработчиком (режим test-fix); тест не ослаблялся.

## Manual verification

Не требуется: все AC проверяются автоматически; визуальные проверки (экранные снимки) в этом проекте не выполняются.
