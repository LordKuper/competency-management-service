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

## Risk → check decisions

Дельта записи 4 — `git diff f72d32e...HEAD`: правки `impl` по замечаниям ревью iter-02 (`0de4ecc` — external 1, `2f24826` — external 2), документы (`1826d60`, `e42aa03`, `ba40df4`) и тест тестировщика ревью-фикса (`a51e623`, его строка — в `test-plan.entry-03.md`). Изменений в `web/`, `openapi/` и конфигурации сборки нет: набор web не затронут. Safety valve применён: правка лежит в общем `CatalogStore.ViewerAsync`, который вызывают конечные точки каталога и матриц, поэтому набор backend запущен целиком.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `CatalogStore.ViewerAsync` — маршрут чтения считается и для HR, пропускается только администратор (external 1) | change: security — вышестоящий согласующий с ролью HR, не ответственный HR подразделения матрицы, получал 404 на черновой трек и уровни версии на своём шаге; доступ HR вне маршрута расширяться не должен | contract | add | `MatrixAccessTests.Ac18_TheDraftTrackAndLevelsOfAVersionInWork_AreReadByTheSuperiorApproverWhoHoldsTheHrRole_…`: руководитель отдела с ролью HR читает черновой трек и оба уровня версии команды (200) и видит их в списках `tracks?q=` и `levels?trackId=`; HR соседней команды (ответственный HR другого подразделения, не на маршруте) получает 404 и пустые списки. Правка затрагивает одиночное чтение, список и маршрут, мутант на каждый (ниже) |
| `CatalogPublication.CheckAsync` возвращает статусы как прочитаны, `PublicationPlan.Visible` решает по ним (external 2) | change: security — помеха-уровень вне состава версии с занятым именем показывалась вышестоящему согласующему, который этот уровень не читает | contract | add | `PublicationCascadeTests.Ac109_Ac110_ABlockerOfALevelOutsideTheVersion_…`: два черновых уровня одного трека с одним именем (разные авторы, один вне состава версии); вышестоящий согласующий и в предпросмотре версии, и в отказе шага, который публикует (согласование с автоматическим шагом HR), видит одну помеху (уровень состава) и `moreCount = 1`; ответственный HR и администратор видят обе, `moreCount = 0`. Предпросмотр и отказ идут через два вызова `Visible`, мутант на каждый |
| Активация перенесена: `CatalogPublication.Apply` и `CatalogEndpoints.PublishAsync` делают `Active` сами, `CheckAsync` активирует лишь на время проверок (external 2) | change: public contract — корень и каскад сохраняются не `Active`, нет строки истории версий компетенции, возвращение неактивной записи не действует, отказ или предпросмотр меняют данные | integration | keep | Закрыто существующими тестами, новый не нужен: утверждение с каскадом — `PublicationCascadeTests.Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_…`, `Ac109_Ac113_…`, `Ac107_Ac114_ApprovingABlock_…` (строки истории «Active», «Draft»); публикация версии — `PublicationCascadeTests.Ac106_Ac109_Ac110_…`, `MatrixWorkflowTests.Ac9_Publication_MakesTheDraftsOfTheVersionActive_AndAddsTheirVersionToTheHistory`; возвращение — `CatalogRightsTests.Ac9_Deactivation_AndReactivation_…` (каждый вид), `TrackAndLevelTests.Ac11_ReactivatingATrackDeactivatedAsADraft_ApprovesItsDraftLevelsAndJournalsEach`; отказ ничего не меняет — `MatrixWorkflowTests.Ac9_Publication_IsRefusedWhenADraftOf…` (шкала остаётся черновиком), `TrackAndLevelTests.Ac11_ReactivatingATrackDeactivatedAsADraft_IsRefusedWhenTwoOf…` (трек неактивен, уровни черновики). Отказ и предпросмотр ничего не сохраняют и без этих тестов: транзакция записи закрывается без фиксации. Проверка «живы ли они»: мутации новых строк активации в копии `git archive HEAD`, `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj` с `--filter-class` по `PublicationCascadeTests`, `CatalogDraftTests`, `MatrixWorkflowTests`, `TrackAndLevelTests`, `CatalogRightsTests`, `CompetencyTests`, `CompetencyBlockTests`, `LevelPositionTests` (130 тестов): `M3A` (`PublishAsync` не делает корень `Active`) → exit 2, 22 красных, среди них `CompetencyTests.Ac12_EverySavedChange_AddsAVersionToTheHistory_ListedLatestFirst` (`Expected versions.Select(version => version!["status"]!.GetValue<string>()) to be equal to {"Inactive", "Active", "Draft", "Draft"}, but {"Inactive", "Draft", "Draft", "Draft"} differs at index 1.`), `PublicationCascadeTests.Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_…`, `CatalogDraftTests.Ac9_AnApprovedOrDeletedDraft_LeavesTheList`; `M3B` (`Apply` не делает каскад `Active`) → exit 2, 12 красных, среди них `TrackAndLevelTests.Ac11_ReactivatingATrackDeactivatedAsADraft_ApprovesItsDraftLevelsAndJournalsEach` (`Expected 200 OK but got 404 NotFound`), `PublicationCascadeTests.Ac106_Ac109_Ac110_PublishingAVersion_…`, `MatrixWorkflowTests.Ac9_Publication_MakesTheDraftsOfTheVersionActive_AndAddsTheirVersionToTheHistory`, `PublicationCascadeTests.Ac107_Ac114_ApprovingABlock_…`. Потолок: возврат статуса корня в `CheckAsync` через API не наблюдается (читающий предпросмотр записи читает её сам, поэтому помеха корня видна в любом статусе), мутант не строился |
| Запросы маршрута у HR в чтениях каталога (`ViewerAsync` больше не возвращает HR сразу) | change: perf — лишние SQL-команды чтений каталога | — | none | Счётчик команд закреплён одним тестом, `DeskQueryCountTests`; он считает стол администратора, а стол `CatalogStore.ViewerAsync` не вызывает (источник стола строит `CatalogViewer` из снимка оргструктуры), поэтому HR-стол с 22 командами из записи 3 и его замер остаются в силе. Бюджет спринта (не больше 25 команд) относится к столу, для чтений каталога бюджета нет, и тест на него закрепил бы реализацию. Для HR добавляется постоянное число команд: запрос версий в работе у автора и участника и, если такие версии есть, запросы блоков и компетенций; обход поддеревьев платит только HR, руководящий подразделениями, — как любой руководитель |
| `DeskQueryCountTests` — прогрев перед `large` (TST-1, `a51e623`) | уже с доказательством (строка в `test-plan.entry-03.md`) | — | keep | не перепроверялась и не переписывалась; прогон ниже подтверждает её зелёной |
| Комментарии и документы (`CatalogPublication`, `PublicationPlan`, `CatalogStore`, `docs/architecture/career-framework.md`, `tech-reference`) | нет поведения | — | none | комментарии и тексты не исполняются; утверждение документа о странице очистки справочников (`unusedEntriesQuery` читает `GET /unused-entries` с `kind`) сверено с `web/src/features/career/catalogApi.ts` и `CatalogCleanupPage.tsx` |

## Removed tests

Записью 4 не удалён ни один тест; ревью-фикс не оставил строк на удаление.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Fail-first: `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-method "*TheDraftTrackAndLevelsOfAVersionInWork_AreReadByTheSuperiorApproverWhoHoldsTheHrRole*" --filter-method "*ABlockerOfALevelOutsideTheVersion*"` в копии `git archive 4a20587 src` (родитель `0de4ecc`, первого исправления ревью iter-02) с тестами рабочего дерева → exit 2, итог 2: 2 красных. Мутации — в копии `git archive HEAD` с теми же тестами, `dotnet test --project … --filter-method <имя теста>`; контрольный прогон без мутаций 2 из 2, exit 0.

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/MatrixAccessTests.cs` (`Ac18_TheDraftTrackAndLevelsOfAVersionInWork_AreReadByTheSuperiorApproverWhoHoldsTheHrRole_…`) | до правки: `Expected (superior.Client.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}")).Status to be HttpStatusCode.OK {value: 200} because the superior approver reads the track of the version, but found HttpStatusCode.NotFound {value: 404}.` Мутации (каждая exit 2, 1 из 1): `T1A` (`ViewerAsync` снова возвращает HR сразу) → та же строка; `T1B` (черновой трек читает любой HR) → `Expected (outsider.GetAsync($"/api/v1/career/tracks/{kit.Track.Id}")).Status to be HttpStatusCode.NotFound {value: 404} because an HR of another unit is not on the route, but found HttpStatusCode.OK {value: 200}.`; `T1C` (то же для уровня) → `Expected (outsider.GetAsync($"/api/v1/career/levels/{levelId}")).Status to be HttpStatusCode.NotFound {value: 404}, but found HttpStatusCode.OK {value: 200}.`; `T1D` (уровни версии не входят в маршрут) → `Expected (superior.Client.GetAsync($"/api/v1/career/levels/{levelId}")).Status to be HttpStatusCode.OK {value: 200}, but found HttpStatusCode.NotFound {value: 404}.`; `T1E` (список не отсекает нечитаемое) → `Expected (outsider.GetAsync($"/api/v1/career/tracks?q={Uri.EscapeDataString(trackName)}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds() to be empty, but found at least one item`; `T1F` (список не учитывает маршрут) → `Expected (superior.Client.GetAsync($"/api/v1/career/tracks?q={Uri.EscapeDataString(trackName)}&pageSize=200")).Expect(HttpStatusCode.OK).ItemIds() to be equal to {…}, but found empty collection.`; runs: 6 |
| `tests/Competency.Tests/PublicationCascadeTests.cs` (`Ac109_Ac110_ABlockerOfALevelOutsideTheVersion_…`) | до правки: `Expected Blockers(bySuperior) to be equal to {"Level/NameTaken/<id>"}, but {"Level/NameTaken/<id>", "Level/NameTaken/<id>"} contains 1 item(s) too many.` Мутации (каждая exit 2, 1 из 1): `T2A` (статусы как прочитаны не возвращаются) → та же строка; `T2B` (отказ называет все помехи, предпросмотр прежний) → `Expected Blockers(refused.Json!) to be equal to {"Level/NameTaken/<id>"}, but {"Level/NameTaken/<id>", "Level/NameTaken/<id>"} contains 1 item(s) too many.`; `T2C` (`moreCount` предпросмотра всегда 0) → `Expected bySuperior["moreCount"]!.GetValue<int>() to be 1 because the level outside the version is counted but not named, but found 0 (difference of -1).`; `T2D` (отказ не считает скрытые) → `Expected refused.Json!["moreCount"]!.GetValue<int>() to be 1, but found 0 (difference of -1).`; runs: 4 |

## Suite run

Command (запись 4): `dotnet test --solution Competency.slnx --artifacts-path <scratch>` — весь набор backend. Web не запускался: в дельте нет файлов `web/`, `openapi/` и конфигурации сборки, а тесты web работают с подменой сети. Safety valve для backend применён: правка лежит в общем `CatalogStore.ViewerAsync`, поэтому набор не сужался.
Scope: full (backend); impacted-набор вырождается в полный.
Result: pass — 680 тестов: 680 успешно, 0 упало, 0 пропущено, exit 0, 2 мин 20 с. Это 678 прежних и 2 добавленных записью. Сырой ход: до стратегии (678) 678 успешно, 0 упало, exit 0, 2 мин 21 с, на дереве `5978625`; после авторства (680) 680 успешно, 0 упало, на дереве `be95745`. Новых кодовых дефектов нет.
Lint / build: pass — `npm --prefix web run lint` (biome ci + tsc) exit 0; `dotnet build Competency.slnx --tl:off` — предупреждений 0, ошибок 0 (`TreatWarningsAsErrors`); `npm --prefix web run build` не запускался: в дельте нет файлов `web/`.
HEAD: be95745 (прогон измеряет дерево этого коммита; после него менялись только артефакты спринта)

Замер стола на сиде `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, 1 000 подразделений, 348 учётных записей, 625 версий матриц; сборка `Debug`, один процесс, 60 замеров после 5 прогревочных; число SQL-команд — команды приложения, без `DISCARD ALL` сброса соединения Npgsql):

| Роль | `/desk/count` p50 / p95 мс | `/desk` p50 / p95 мс | SQL-команд | Элементов действий | Крупнейшая группа |
|---|---|---|---|---|---|
| администратор | 75 / 91 | 57 / 64 | 22 | 813 | 1077 (UnusedCatalogEntries) |
| HR с закреплением на корне | 53 / 61 | 53 / 63 | 22 | 275 | 1077 (UnusedCatalogEntries) |
| руководитель верхнего подразделения | 33 / 42 | 34 / 41 | 19 | 1 | 6 |
| сотрудник | 19 / 27 | 20 / 25 | 11 | 0 | 1 |

Бюджеты соблюдены (≤ 300 мс, ≤ 600 мс, ≤ 25 SQL-команд, наибольшая группа 1077 < 2000). Сырой счёт строк журнала PostgreSQL вдвое больше (44 / 44 / 38 / 22) из-за `DISCARD ALL` после каждой команды; для администратора три раза читаются учётные записи и сотрудники и два раза — справочники (запас до 25 небольшой).

Замер сделан в записи 3 и не повторялся: код стола в дельте записи 4 не менялся, а `CatalogStore.ViewerAsync`, правка которого есть в дельте, стол не вызывает.

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
