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
| 2 | | delta since entry 1 |

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `usePublishEntry` вызывает `useNavigate()` (исправление D-2); хук подключён в модальных окнах и страницах стола | change: workflow — окно, показанное вне маршрутизатора, не должно падать; окно, показанное в нём, должно вести по ссылке | component | add | существующий `usePublishEntry.test.tsx` закрепляет отказ в окне вне маршрутизатора и подпись ссылки; два теста без маршрутизатора (`ScaleModal`, `CompetencyModal`) исправлены как дефект теста (см. «Added tests») |
| `InUseMessage` «Область используют…» (исправление D-1) | change: public contract — текст отказа | component/contract | keep | `CatalogDeletionTests.Ac25_…` закрепляет текст; зелёный |

Предыдущая запись рассматривалась по записи 1; её строки — в `test-plan.entry-01.md`. Для остальных вызывающих хук (`BlockModal`, `NameModal`, `DraftsSection`, `CatalogParts`, `BuilderSubmitStep`, `CascadeTable`, `CatalogCleanupPage`, `DecisionError`, `DraftsQueuePage`, `ReviewPage`, `ReworkPage`, `useVersionTransition`) новых тестов не нужно: их тесты уже рендерят под маршрутизатором и зелёные (`vitest run` 492 из 492).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | ни один тест не удалён | — |

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| `web/src/features/career/ScaleModal.test.tsx`, `CompetencyModal.test.tsx` | исправление дефекта теста (не новый тест): окна отрисованы без `MemoryRouter`, а `usePublishEntry` теперь читает маршрутизатор; fail-first: `node node_modules/vitest/vitest.mjs run src/features/career src/features/desk` → exit 1, 10 упавших, напр. `ScaleModal.test.tsx > scale form (AC-13) > asks for a name, a score and a value` (`useNavigate() may be used only in the context of a <Router> component.`); после обёртки в `MemoryRouter` — exit 0; runs: 275 |

## Suite run

Command (запись 2, impacted set): `dotnet test --solution Competency.slnx --artifacts-path <scratch> --filter-class "*CatalogDeletionTests"` и `node node_modules/vitest/vitest.mjs run` в `web` (хук `usePublishEntry` подключён в 15 файлах, весь набор web — 47 файлов, дешёв); safety valve не сработал: дельта — одна строка в `Competency.CareerFramework` и один хук SPA, общая инфраструктура не затронута.
Scope: impacted.
Result: pass — backend 16 тестов: 16 успешно, 0 упало, 0 пропущено, exit 0 (D-1 закрыт); web 492 теста: 492 успешно, 0 упало, 47 файлов, exit 0 (D-2 закрыт). До стратегии: backend 16/0, web career+desk 265 успешно, 10 упало (дефект теста: окна `ScaleModal`/`CompetencyModal` без маршрутизатора, исправлен), остальные web не запускались до исправления.
Lint / build: pass — `npm --prefix web run lint` (biome ci + tsc) exit 0; backend не менялся кроме строки константы (сборка прошла в тесте).
HEAD: 0d8070e (плюс правки тестов этой записи)

Замер стола на сиде `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, 1 000 подразделений, 348 учётных записей, 625 версий матриц; сборка `Debug`, один процесс, 60 замеров после 5 прогревочных; число SQL-команд — команды приложения, без `DISCARD ALL` сброса соединения Npgsql):

| Роль | `/desk/count` p50 / p95 мс | `/desk` p50 / p95 мс | SQL-команд | Элементов действий | Крупнейшая группа |
|---|---|---|---|---|---|
| администратор | 75 / 91 | 57 / 64 | 22 | 813 | 1077 (UnusedCatalogEntries) |
| HR с закреплением на корне | 53 / 61 | 53 / 63 | 22 | 275 | 1077 (UnusedCatalogEntries) |
| руководитель верхнего подразделения | 33 / 42 | 34 / 41 | 19 | 1 | 6 |
| сотрудник | 19 / 27 | 20 / 25 | 11 | 0 | 1 |

Бюджеты соблюдены (≤ 300 мс, ≤ 600 мс, ≤ 25 SQL-команд, наибольшая группа 1077 < 2000). Сырой счёт строк журнала PostgreSQL вдвое больше (44 / 44 / 38 / 22) из-за `DISCARD ALL` после каждой команды; для администратора три раза читаются учётные записи и сотрудники и два раза — справочники (запас до 25 небольшой).

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.CareerFramework/CareerDirectionEndpoints.cs | Expected refused.Json!["detail"]!.GetValue<string>() to be the same string, but they differ at index 0: | Competency.Tests.CatalogDeletionTests.Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices | fixed | 2725176 |
| D-2 | 1 | web/src/features/career/usePublishEntry.tsx | AssertionError: expected null not to be null | approving an entry under the list of what is published with it (AC-110…AC-112) > explains a refusal the server gives at the moment of approval, with the way to the track's positions | fixed | 4483101 |

Пояснения (вне строк таблицы): D-1 — константа `InUseMessage` («Направление используют треки, компетенции или матрицы — удалить нельзя») не обновлена под AC-37 (сущность называется «область»), `CareerMail` и остальной текст уже используют «область». D-2 — `RefusalContent` вставляет `<Link>` в содержимое `modal.error`, а модальное окно из `App.useApp()` в `Providers` отрисовывается вне `RouterProvider` (структура приложения такая же, как в тесте), поэтому при отказе «нет действующей должности» с известным треком `Link` падает на `useContext(...) is null` и экран остаётся пустым; проверено: замена `<Link>` на `<a>` в копии делает весь файл зелёным (13 из 13).

## Manual verification

| AC | Steps | Expected observation |
|---|---|---|
| — | автоматизируется всё проверяемое; ручной проверки не требуется | — |
