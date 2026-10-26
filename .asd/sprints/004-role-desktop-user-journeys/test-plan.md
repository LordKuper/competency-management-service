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

## Risk → check decisions

Дельта записи 9 — `git diff a1b6ed8...HEAD`: исправление `D-3` (`8d9f3a5`), удаление не вызываемого `approveDraft` (`b7db7b5`), отметка `D-3` (`db1bf1c`).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `BlockModal`: `CompetencyPicker` получает `isSaving`, `getTitleCheckboxProps` запирает «Select all» на время сохранения (`8d9f3a5`) | change: workflow state machine — «Select all» менял состав блока, пока шло сохранение (AC-61) | уже написанный тест `BlockModal.test.tsx` › `holds the name, the unit and the composition while the save is in flight, and frees them when it is refused`; красный на `a1b6ed8` (`D-3`), зелёный теперь | none | Регрессионный тест записи 8 покрывает риск, нового не нужно; fail-first — красный прогон записи 8 (строка `D-3`) |
| `catalogApi.ts`: удалён `approveDraft` (`b7db7b5`) | нет поведения: функция без вызывающих, тест удалён в записи 8 | `tsc` (`lint`, `build`) ловит оставшиеся ссылки; `grep approveDraft web/src` — пусто | none | Исчезнувший экспорт проверяет компилятор |

## Removed tests

Нет.

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| — | нет новых тестов |

## Suite run

Command (запись 9): `npm --prefix web exec vitest run <14 файлов>` — тесты, ссылающиеся на `BlockModal`, `BlocksTab` или `catalogApi`. Safety valve не сработал: правки — одна форма каталога и удаление не вызываемого экспорта, общая инфраструктура не затронута; backend не менялся.
Scope: impacted (14 файлов web).
Result: pass — 14 файлов, 142 теста успешно, 0 упало, exit 0 (до стратегии и после: файлы тестов не менялись); `D-3` зелёный.
Lint / build: pass — `npm --prefix web run lint` exit 0; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0.
HEAD: 7663eac

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
