---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 004-role-desktop-user-journeys

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 16478d5 | full change surface |
| 2 | 7fe0c10 | delta since entry 1 |
| 3 | f72d32e | delta since entry 2 |
| 4 | d8c397f | delta since entry 3 |
| 5 | 7b8de3c | delta since entry 4 |
| 6 | ce9240f | delta since entry 5 |
| 7 | 5382584 | delta since entry 6 |
| 8 | a1b6ed8 | delta since entry 7 |
| 9 | 6b07d87 | delta since entry 8 |
| 10 | 1c7af02 | delta since entry 9 |

## Risk → check decisions

Дельта записи 10 — `git diff 6b07d87...HEAD`: правки dev по замечаниям ревью волны 3, итерации 02 (`acb1e26`, `86eb60b`, `b6b4366`; `ce44112` — память dev), файлы ревью и память ревьюеров. Backend, `openapi/` и конфигурация сборки не менялись; общие модули (`useFormSubmit`, `useEditBase`, `fakeApi`) тоже, safety valve не сработал. Поиск остатков по удалённому `focusFirstRejectedField` по репозиторию и `.claude/agent-memory`: в коде, живых документах и памяти попаданий нет; остались временные файлы `.asd/tmp`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `CompetencyModal`: отклонённое поле хранится в `rejectedField`, фокус ставит эффект после `!isSubmitting` (`acb1e26`) | change: workflow state machine — форма заперта `disabled={isSubmitting}`, а фокус на первое отклонённое поле вызывался до разблокировки; `focus()` на запертом поле ничего не делает, поле оставалось без фокуса | component | add (заменяет страж записи 8) | `CompetencyModal.test.tsx`: ответ 400 удержан `deferred()`, тест ждёт `toBeDisabled()` у поля, отпускает ответ — поле получает фокус. Страж записи 8 отвечал сразу, цепочка шла в микрозадачах, отрисовка запертой формы не фиксировалась, и он остался зелёным на дефекте (см. `Removed tests`). Ceiling: мутанты `CF1` (эффект без проверки `isSubmitting`) и `CF3` (без сброса `rejectedField`) зелёные и эквивалентны: `setRejectedField` и `setIsSubmitting(false)` попадают в один пакет React, отрисовки «поле названо, форма заперта» не бывает, а новое значение `rejectedField` каждый отказ задаёт заново. Защитные условия, тестом не закреплены |
| `UnitHrModal.AssignHrForm`: `<Form disabled={isSubmitting}>` (`86eb60b`) | change: workflow state machine — выбор сотрудника оставался доступным, пока шло сохранение; правка терялась (правило «Mutating UI flows») | component | add | `UnitHrModal.test.tsx`: сохранение удержано `deferred()`, виды запертых элементов собраны в список в трёх моментах — покой `[]`, `PUT …/hr` в полёте `["employee", "assign"]`, отказ 500 `[]`. Кнопка «Закрепить» в списке: до правки она была только `loading`, не `disabled` |
| `TrackModal`: эффект записывает `current?.name ?? ""` в поле при каждой смене `current` (`b6b4366`) | change: stale-version — `form` живёт выше `Form key`, перемонтирование после 412 оставляло в хранилище отклонённое название; следующее сохранение уходило с перечитанной версией и перезаписывало чужое переименование (AC-61) | component | add (в двух существующих тестах 412) | Новые тесты не нужны: поток и риск те же, отдельный повторял бы расстановку с перетаскиванием. Тесты `creates a new track once, and after the order is refused…` (создание: `readTrack` отдаёт другое название) и `shows the name and sends the version of the track it reread…` (правка: `rerender` с другим названием и версией) теперь ждут перечитанное название в поле и не ждут второго сохранения отклонённого названия: ни одного `PUT …/tracks/created`, ровно один `PUT …/tracks/existing`; прежние проверки `If-Match` и порядка остаются, порядок после перечитывания уходит вторым сохранением. Раньше перечитанное название совпадало с набранным, и поле не отличало «показано перечитанное» от «осталось своё». Нужны оба теста: эффект по `base` ловит только создание, по `saved` — только правка (мутанты `TE2`, `TE3`) |

## Removed tests

Выполнено в том же коммите, что и замена.

| Test | Reason | In change scope |
|---|---|---|
| `CompetencyModal.test.tsx`: `moves the focus to the first field the server rejects` (мгновенный ответ 400) | заменён тестом с удержанным ответом в том же describe: прежний не фиксировал отрисовку запертой формы и не краснел на дефекте, а всё, что он ловил (вызов фокуса убран), ловит и новый (`CF2`, `CF4`) | да — файл в области изменений спринта |

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Fail-first. `node node_modules/vitest/vitest.mjs run <3 файла> --reporter=verbose` в копии `git archive 78bcf14 web` (родитель первого исправления `acb1e26`; `node_modules` — junction) с тестами рабочего дерева → exit 1, 4 красных из 27, 23 зелёных (прежние тесты тех же файлов). Vitest типы не проверяет, поэтому тесты идут против старого кода и падают на утверждениях.

Мутации — в копии `git archive HEAD web` (`HEAD` = `142d87f`; `node_modules` — junction, снят перед удалением копии) с тестами рабочего дерева; один якорь на правку (число попаданий проверяется), файл восстанавливается в `finally`; рабочее дерево не мутировалось. Команда: `node node_modules/vitest/vitest.mjs run <файл теста> --reporter=json`. Контрольные прогоны без мутации зелёные, exit 0 (`CompetencyModal` 7 из 7, `TrackModal` 11, `UnitHrModal` 9). «Убит» = exit 1 и runner назвал упавший тест; в ячейке — первая упавшая проверка из отчёта runner, `runs` — число мутационных прогонов, нацеленных на файл (контрольные не считаются).

| Test | Regression proof |
|---|---|
| `web/src/features/career/CompetencyModal.test.tsx` — `moves the focus to the first field the server rejects, once the form is enabled again` | до правки: `Error: expect(element).toHaveFocus()` (фокус остался на кнопке). Мутации: `CF2` (вызов `scrollToField` убран) и `CF4` (поле не запоминается) → exit 1, `Error: expect(element).toHaveFocus()`; `CF1` и `CF3` — exit 0, эквивалентны (см. `Risk → check decisions`); runs: 4 |
| `web/src/features/org-structure/UnitHrModal.test.tsx` — `holds the employee choice while the assignment is in flight, and frees it when it is refused (AC-61)` | до правки: `AssertionError: expected [] to deeply equal [ 'employee', 'assign' ]`. Мутация `UF1` (форма без `disabled`) → exit 1, та же проверка; runs: 1 |
| `web/src/features/career/TrackModal.test.tsx` — `creates a new track once, and after the order is refused…` и `shows the name and sends the version of the track it reread…` | до правки оба: `Error: expect(element).toHaveValue(Бэкенд-разработка)`. Мутации (каждая exit 1): `TE1` (эффект убран) → оба теста, та же проверка; `TE2` (эффект следует `base`) → только создание; `TE3` (эффект следует `saved`) → только правка; runs: 3 |

## Suite run

Command: `dotnet test --solution Competency.slnx --artifacts-path <scratchpad>/art` и `npm --prefix web test` (терминальный полный прогон impl-review wave-3/iter-03, без выбора набора). Единственное отклонение от commands.yaml: `--artifacts-path` в scratchpad, потому что `src/*/bin` блокирует запущенный Competency.Api и Visual Studio. Для `ContractTests` и `DeploymentTests`, которые ищут `Competency.slnx` от бинарников, в каталог артефактов скопированы `Competency.slnx`, `Dockerfile`, `deploy/`, `openapi/` и `src/*/*.csproj` с `packages.lock.json`.
Scope: full (весь backend и весь web).
Result backend: pass — 682 теста, 682 успешно, 0 упало, 0 пропущено, exit 0, 2 мин 57 с. Первый прогон без копии корня дал exit 2 (672/10): 9 падений `FileNotFoundException: Competency.slnx` из-за пути артефактов и один `Npgsql timeout` в `IdentityRolesMigrationTests.Ac63_...` (нагрузка Docker при старте); повтор того же дерева зелёный.
Result web: pass — 68 файлов, 707 тестов, 0 упало, exit 0, 58 с.
Lint / build: pass — `npm --prefix web run lint` exit 0; `dotnet build Competency.slnx --tl:off` (артефакты в scratchpad) и `npm --prefix web run build` exit 0.
HEAD: 1446fdeb13c15ac3a35fdcc1d636d4d7e56d6d3e

Замер стола на сиде `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, 1 000 подразделений, 348 учётных записей, 625 версий матриц; сборка `Debug`, один процесс, 60 замеров после 5 прогревочных; число SQL-команд — команды приложения, без `DISCARD ALL` сброса соединения Npgsql):

| Роль | `/desk/count` p50 / p95 мс | `/desk` p50 / p95 мс | SQL-команд | Элементов действий | Крупнейшая группа |
|---|---|---|---|---|---|
| администратор | 75 / 91 | 57 / 64 | 22 | 813 | 1077 (UnusedCatalogEntries) |
| HR с закреплением на корне | 53 / 61 | 53 / 63 | 22 | 275 | 1077 (UnusedCatalogEntries) |
| руководитель верхнего подразделения | 33 / 42 | 34 / 41 | 19 | 1 | 6 |
| сотрудник | 19 / 27 | 20 / 25 | 11 | 0 | 1 |

Бюджеты соблюдены (≤ 300 мс, ≤ 600 мс, ≤ 25 SQL-команд, наибольшая группа 1077 < 2000). Сырой счёт строк журнала PostgreSQL вдвое больше (44 / 44 / 38 / 22) из-за `DISCARD ALL` после каждой команды; для администратора три раза читаются учётные записи и сотрудники и два раза — справочники (запас до 25 небольшой).

Замер сделан в записи 3 и не повторялся: код стола в дельтах записей 4–7 не менялся (запись 5 меняет `PublicationPlan`, ответы предпросмотров публикации и web, записи 6 и 7 — только web и его тесты), а `CatalogStore.ViewerAsync` из записи 4 стол не вызывает.

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.CareerFramework/CareerDirectionEndpoints.cs | Expected refused.Json!["detail"]!.GetValue<string>() to be the same string, but they differ at index 0: | Competency.Tests.CatalogDeletionTests.Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices | fixed | 2725176 |
| D-2 | 1 | web/src/features/career/usePublishEntry.tsx | AssertionError: expected null not to be null | approving an entry under the list of what is published with it (AC-110…AC-112) > explains a refusal the server gives at the moment of approval, with the way to the track's positions | fixed | 4483101 |
| D-3 | 8 | web/src/features/career/BlockModal.tsx | AssertionError: expected [ 'name', 'unit', 'search', …(2) ] to deeply equal [ 'name', 'unit', 'search', …(3) ] | block form while the block is being saved (AC-61) > holds the name, the unit and the composition while the save is in flight, and frees them when it is refused | fixed | 8d9f3a5 |

Пояснения (вне строк таблицы): D-1 — константа `InUseMessage` («Направление используют треки, компетенции или матрицы — удалить нельзя») не обновлена под AC-37 (сущность называется «область»), `CareerMail` и остальной текст уже используют «область». D-2 — `RefusalContent` вставляет `<Link>` в содержимое `modal.error`, а модальное окно из `App.useApp()` в `Providers` отрисовывается вне `RouterProvider` (структура приложения такая же, как в тесте), поэтому при отказе «нет действующей должности» с известным треком `Link` падает на `useContext(...) is null` и экран остаётся пустым; проверено: замена `<Link>` на `<a>` в копии делает весь файл зелёным (13 из 13).

D-3 — флажок «Select all» в шапке таблицы состава блока (`CompetencyPicker`) остаётся включённым, пока идёт сохранение: antd считает запертость шапки по явному `disabled` строк, а не по контексту формы, а строки заперты только контекстом (`|| undefined`). Нажатие в это время меняет состав: «Выбрано: 2» при пустом составе в запросе, после успеха окно закрывается и выбор теряется. Остальные виды (название, подразделение, поиск, «Создать компетенцию», флажки строк) заперты. Проверено: заплатка `getTitleCheckboxProps: () => ({ disabled: componentDisabled })` в копии делает тест зелёным (мутанты `BM1`, `BM2` на копии с заплаткой красные). Вид правки выбирает dev; скрытие шапки (`hideSelectAll`) потребует изменить и тест.

## Manual verification

| AC | Steps | Expected observation |
|---|---|---|
| — | автоматизируется всё проверяемое; ручной проверки не требуется | — |
