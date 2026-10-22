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

## Risk → check decisions

Дельта записи 5 — `git diff d8c397f...HEAD`: изменение требований AC-110/AC-112 (`d329d7c`: перечень публикации называет только читаемые публикующим записи, остальные показаны числом «и ещё N»; подтверждение остаётся над всем каскадом), правка `impl` по замечанию внешнего ревью (`e692120`: `PublicationPlan.Describe(reader)` отбирает записи и помехи одной `SeesLoaded`, поле контракта `hiddenEntryCount`; web считает каскад целиком и показывает «и ещё N») и заметка памяти dev (`68c1a40`). Тесты этих коммитов не менялись. Safety valve применён: правка затрагивает общий контракт (`openapi.json`, `schema.d.ts`), общий `PublicationPlan` (оба предпросмотра) и общие web-компоненты (`usePublishEntry`, `CascadeTable`, `useVersionTransition`), поэтому набор backend и набор web запущены целиком.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `PublicationPlan.Describe(reader)` — `entries` только читаемые, остальные числом `hiddenEntryCount` (предпросмотр версии; предпросмотр записи идёт тем же методом) | change: security — вышестоящий согласующий видел идентификатор и название черновика, который читать не вправе; число скрытых неверно | contract | add | `PublicationCascadeTests.Ac110_Ac112_APreviewOfAVersion_…`: версия на шаге вышестоящего согласующего (автор — ответственный HR), в треке черновой уровень вне содержимого версии. Вышестоящий согласующий видит трек и два уровня версии, `hiddenEntryCount = 1`, идентификатора и названия скрытого уровня нет ни в одной строке тела; ответственный HR и администратор видят все четыре записи, `hiddenEntryCount = 0`; тег один у всех троих. Мутанты — ниже |
| Подтверждение над всем каскадом: `Tag`, `IsConfirmedBy` (AC-112; код не менялся, смысл — тег остаётся по полному перечню, когда перечень читателя короче) | change: workflow state machine — тег по видимой части дал бы опубликовать скрытые черновики без подтверждения того, что изменилось | contract | add | `Ac112_ApprovingUnderThePreviewOfAReaderWhoCannotReadADraft_…`: тот же вышестоящий согласующий подтверждает по тегу своего предпросмотра; скрытый уровень переименован → 412 с `publication`, версия остаётся «на согласовании», уровень — черновиком; новый предпросмотр даёт другой тег, подтверждение по нему публикует и скрытый уровень (три `CareerLevel.Approved` в журнале). Страж: кроме нового поля до правки зелёный, поэтому его держат мутанты |
| Предпросмотр записи (`GET /{segment}/{id}/publication`), читающий вправе утвердить корень | change: security | contract | add | одна проверка в существующем `Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_…`: у HR `hiddenEntryCount = 0` на маршруте записи. Что хранитель читает весь свой каскад, уже закрепляют `Entries(preview)` в `Ac107_…` и `Ac109_Ac113_…` |
| Предпросмотр записи: читающий вправе утвердить корень, но не вправе читать черновик каскада (пункт 1 списка dev) | change: security | — | none | предпосылка не достижима. Каскад записи — направление, шкала, компетенции (без подразделения) и уровни трека-корня. Утвердить корень вправе администратор, ответственный HR подразделения трека (учётная запись с ролью HR) и любой HR для записи без подразделения. `CatalogViewer.Sees` показывает черновик без подразделения каждому HR и администратору, а уровень — ответственному HR подразделения его трека. Скрытое возникает только в предпросмотре версии, где читает вышестоящий согласующий, не хранитель; этот случай — первая строка. Сузятся права чтения HR — красными станут строка выше и `Entries(...)` существующих тестов |
| `usePublishEntry`: `publishedCount`, `hiddenDraftsText`, `EntryList`, решение «спросить или утвердить сразу» (`publish`), подпись кнопки, число в вопросе и в сообщении | change: workflow state machine — публикующее действие без подтверждения, когда читаемый список пуст, а скрытые записи есть; неверное число | component | add | `usePublishEntry.test.tsx`: два теста — читаемые вместе со скрытыми (подпись «вместе с 3 записями», строка «и ещё 2 черновые записи», тег целого перечня, сообщение «опубликовано записей: 3») и только скрытые (окно открывается, до подтверждения запроса нет, списка `<ul>` нет) |
| `CascadeTable` — таблица читаемых записей и строка «и ещё N» (общая для четырёх экранов) | change: public contract — при одних скрытых таблица не показывается, при нуле скрытых строки нет | component | add | `CascadeTable.test.tsx`: три сочетания — только читаемые, читаемые и скрытые, только число |
| `useVersionTransition`: число в вопросе, `CascadeTable` в окне, число в `onDone` | change: workflow state machine | component | add | `useVersionTransition.test.tsx`: при пустом читаемом перечне хук спрашивает первым и публикует по тегу (число 2); решение под перечнем, который показал экран, выполняется сразу, число 3 |
| Экраны `DraftsQueuePage` (подпись кнопки, фраза «Других черновых записей…»), `BuilderSubmitStep` (фраза, таблица, отправка без окна), `ReviewPage` (блок «Будет опубликовано вместе с версией») | change: workflow state machine — на `DraftsQueuePage` и `BuilderSubmitStep` экран и есть подтверждение, окна нет | component | add | по два теста на экран (скрытые записи и пустой перечень), сеть `fakeApi`. Запись 1 оставила страницы сценариев целиком на `none` (потолок); здесь закреплены только изменённые блоки, остальные части страниц и `ReworkPage` по-прежнему вне проверок |
| Фикстуры предпросмотра без `hiddenEntryCount` (`usePublishEntry.test.tsx`, `catalogueTables.test.tsx`) | дефект теста: ответы строятся `Response.json` без типа, `tsc` нового поля не видит, `entries.length + undefined` — `NaN`, и пустой перечень не распознаётся | component | add | поле добавлено в обе фикстуры; до правки 9 красных (`approves at once under the tag of an empty list, …` и ещё семь тестов `usePublishEntry.test.tsx`, `approves a draft against the version its row shows, …` в `catalogueTables.test.tsx`) — дефект теста, не кода, `D-N` нет |
| Контракт: `hiddenEntryCount` в `PublicationPreviewResponse` и `MatrixPublicationResponse` (`openapi.json`, `schema.d.ts`) | change: public contract | contract | keep | `ContractTests` в наборе backend зелёные; `npm --prefix web run check:api` без расхождений |
| Документы (`docs/product`, `docs/architecture`, `docs/ux`), заметка памяти dev (`68c1a40`) | нет поведения | — | none | тексты не исполняются; заметка памяти верна — фикстуры, о которых она, исправлены строкой выше. Попаданий в память агентов нет: проверено поиском `cascadeTag`, `PublicationPreviewResponse`, `moreCount` по `.claude/agent-memory` (только память тестировщика, описания приёмов верны) |

## Removed tests

Записью 5 не удалён ни один тест; ревью-фикс не оставил строк на удаление.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Fail-first. Backend: `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-method "*Ac110_Ac112_APreviewOfAVersion*" --filter-method "*Ac112_ApprovingUnderThePreview*" --filter-method "*Ac107_Ac110_Ac113_Ac114_ApprovingACompetency*"` в копии `git archive e692120~1` (src, tests, openapi; родитель исправления `e692120`) с `PublicationCascadeTests.cs` рабочего дерева → exit 2, итог 3: 3 красных. Web: `node node_modules/vitest/vitest.mjs run <семь файлов> --reporter=verbose` в копии `git archive e692120~1 web` (node_modules — junction) с тестами рабочего дерева → exit 1, красных 10 из 13 новых тестов; три теста «пустой перечень» зелёные до правки (стражи, чтобы экран не показывал список всегда).

Мутации — в копиях `git archive HEAD` с тестами рабочего дерева. Backend: тот же `dotnet test --project … --filter-method` (три имени выше), `runs` — число мутационных прогонов; контрольный прогон без мутации 3 из 3, exit 0. Web: `node node_modules/vitest/vitest.mjs run <семь файлов> --reporter=json`; контрольный прогон 38 из 38, exit 0. «Убит» — runner вернул ненулевой код и назвал упавший тест; в ячейке — первая упавшая проверка из отчёта runner.

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/PublicationCascadeTests.cs` (`Ac110_Ac112_APreviewOfAVersion_…`) | до правки: `Expected HiddenEntryCount(full) to be 0 because whoever may read every draft sees them all, but found -1 (difference of -1).` (поля нет; в том же прогоне `Expected Entries(bySuperior) to be a collection with 3 item(s), but` и идентификатор скрытого уровня в теле). Мутации (каждая exit 2): `S1` (предпросмотр перечисляет весь каскад) → `Expected Entries(bySuperior) to be a collection with 3 item(s), but`; `S2` (`hiddenEntryCount` всегда 0) → `Expected HiddenEntryCount(bySuperior) to be 1 because the level outside the version is counted but not listed, but found 0 (difference of -1).`; `S3` (число — весь каскад) → `Expected HiddenEntryCount(full) to be 0 because whoever may read every draft sees them all, but found 4 (difference of 4).`; `S4` (ответ версии теряет число) → строка `S2`; runs: 4 |
| `tests/Competency.Tests/PublicationCascadeTests.cs` (`Ac112_ApprovingUnderThePreviewOfAReaderWhoCannotReadADraft_…`) | до правки красен одним новым полем: `Expected HiddenEntryCount(fresh) to be 1, but found -1 (difference of -2).`; 412, неизменность данных, новый тег и публикация скрытого уровня до правки зелёные — страж, его держит мутация `S5` (тег не включает последний черновик каскада — скрытый уровень), exit 2, 1 красный: `Expected stale.Status to be HttpStatusCode.PreconditionFailed {value: 412} because a change of the draft the reader cannot read changes the list they confirmed, but found HttpStatusCode.OK {value: 200}.`; `S1`–`S4` краснят его только строкой `HiddenEntryCount(fresh)`; runs: 1 |
| `tests/Competency.Tests/PublicationCascadeTests.cs` (`Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_…`, проверка `hiddenEntryCount`) | до правки: `Expected HiddenEntryCount(preview) to be 0 because a keeper reads every draft the approval publishes, but found -1 (difference of -1).` Мутации (каждая exit 2): `S3` → `… but found 2 (difference of 2).`; `S6` (предпросмотр записи описан для читателя без роли HR) → `Expected Entries(preview) to be a collection with 2 item(s), but found an empty collection.`; runs: 2 |
| `web/src/features/career/usePublishEntry.test.tsx` (`names the drafts the user may read, counts the ones they may not, …`, `asks before approving when every draft of the list is one the user may not read, …`) | до правки (exit 1): `Error: expect(element).toHaveTextContent()` («3 черновые записи») и `AssertionError: expected null not to be null` (окно не открылось, утверждение ушло сразу). Мутации (каждая exit 1): `U1` (`publishedCount` без скрытых) → 7 красных, оба новых среди них; `U2` (`publish` решает по `entries.length`) → `AssertionError: expected null not to be null`; `U3` (текст окна по читаемым) → `Error: expect(element).toHaveTextContent()`; `U4` (`execute` сообщает читаемые) → `Unable to find an element with the text: /вместе с ней опубликовано записей: 3/`; `U5` (подпись кнопки по читаемым) → `Unable to find an accessible element with the role "button" and name "Утвердить вместе с 3 записями"`; `U6` (нет строки «и ещё») → `Error: expect(element).toHaveTextContent()`; `U7` (из строки убрано «и») → 8 красных; `U8` (пустой `<ul>` при одних скрытых) → `AssertionError: expected <ul></ul> to be null`; runs: 8 |
| `web/src/features/desk/CascadeTable.test.tsx` | до правки все три красные `TypeError: entries is not iterable` (проп переименован в `drafts`, по поведению до правки проверить нечем). Мутации (каждая exit 1): `C1` (таблица при нуле читаемых) → `AssertionError: expected <table …(1)>…(2)</table> to be null`; `C2` (строка при нуле скрытых) → `AssertionError: expected <p></p> to be null`; `C3` (строка без числа) → `Unable to find an element with the text: и ещё 2 черновые записи.`; runs: 3 |
| `web/src/features/desk/useVersionTransition.test.tsx` | до правки: `Error: expect(element).toHaveTextContent()` («записей: 2») и `AssertionError: expected "vi.fn()" to be called with arguments: [ ObjectContaining {"id": "v1"}, 3 ]`. Мутации (каждая exit 1): `V1` (вопрос по читаемым) → `Error: expect(element).toHaveTextContent()`; `V2` (подтверждённое решение сообщает читаемые) → `expected "vi.fn()" to be called with arguments: [ ObjectContaining {"id": "v1"}, 2 ]`; `V3` (прямое решение) → то же с `3`; `C1` краснит и его (таблица в окне); runs: 3 |
| `web/src/features/desk/DraftsQueuePage.test.tsx` | до правки: `TestingLibraryElementError: Unable to find role="button" and name "Утвердить вместе с 2 записями"`; тест «пустой перечень» — страж, зелёный до правки. Мутации (каждая exit 1): `Q1` (счёт по читаемым) → та же строка; `Q2` (список только при читаемых) → `Unable to find an element with the text: и ещё 2 черновые записи.`; runs: 2 |
| `web/src/features/desk/BuilderSubmitStep.test.tsx` | до правки: `Unable to find an element with the text: /и вместе с ней 3 черновые записи/`; тест «нет черновых записей» — страж. Мутации (каждая exit 1): `B1` (счёт по читаемым) → та же строка; `B2` (таблица только при читаемых) → `Unable to find an element with the text: и ещё 3 черновые записи.`; `V3` краснит и проверку сообщения «опубликовано записей: 3»; runs: 2 |
| `web/src/features/desk/ReviewPage.test.tsx` | до правки: `Unable to find an element with the text: и ещё 2 черновые записи.`; тест «версия уходит одна» — страж. Мутация `R1` (блок только при читаемых) → exit 1, та же строка; runs: 1 |
| `web/src/features/career/usePublishEntry.test.tsx`, `web/src/features/career/catalogueTables.test.tsx` (фикстуры предпросмотра) | исправление дефекта теста, не новый тест. Команда `npm --prefix web test` на дереве `07dc3df`: exit 1, 9 красных из 492; первая — `approves a draft against the version its row shows, and reads no entry on the way`: `AssertionError: expected [] to have a length of 1 but got +0`; после правки обе фикстуры зелёные |

## Suite run

Command (запись 5): `dotnet test --solution Competency.slnx --artifacts-path <scratch>` — весь набор backend и `npm --prefix web test` — весь набор web. Safety valve применён: правка затрагивает общий контракт, `PublicationPlan` обоих предпросмотров и общие web-компоненты, поэтому ни один набор не сужался.
Scope: full (backend и web); impacted-набор вырождается в полный.
Result: pass — backend 682 теста: 682 успешно, 0 упало, 0 пропущено, exit 0, 2 мин 23 с; web 505 тестов в 52 файлах: 505 успешно, 0 упало, exit 0, 30 с. Это 680 и 492 прежних плюс 2 и 13 добавленных записью. Сырой ход: до стратегии на дереве `07dc3df` backend 680 из 680, exit 0, 2 мин 25 с, web 483 из 492, 9 красных, exit 1 — все девять от фикстур теста без нового поля (см. «Risk → check decisions»), кода среди них нет; после авторства на дереве `6581783` оба набора зелёные. Новых кодовых дефектов нет.
Lint / build: pass — `npm --prefix web run lint` (biome ci + tsc) exit 0; `dotnet build Competency.slnx --tl:off` — предупреждений 0, ошибок 0 (`TreatWarningsAsErrors`); `npm --prefix web run build` exit 0; `npm --prefix web run check:api` без расхождений.
HEAD: 6581783 (прогон измеряет дерево этого коммита; после него менялись только `test-plan.md`, `test-plan.entry-04.md` и память тестировщика)

Замер стола на сиде `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, 1 000 подразделений, 348 учётных записей, 625 версий матриц; сборка `Debug`, один процесс, 60 замеров после 5 прогревочных; число SQL-команд — команды приложения, без `DISCARD ALL` сброса соединения Npgsql):

| Роль | `/desk/count` p50 / p95 мс | `/desk` p50 / p95 мс | SQL-команд | Элементов действий | Крупнейшая группа |
|---|---|---|---|---|---|
| администратор | 75 / 91 | 57 / 64 | 22 | 813 | 1077 (UnusedCatalogEntries) |
| HR с закреплением на корне | 53 / 61 | 53 / 63 | 22 | 275 | 1077 (UnusedCatalogEntries) |
| руководитель верхнего подразделения | 33 / 42 | 34 / 41 | 19 | 1 | 6 |
| сотрудник | 19 / 27 | 20 / 25 | 11 | 0 | 1 |

Бюджеты соблюдены (≤ 300 мс, ≤ 600 мс, ≤ 25 SQL-команд, наибольшая группа 1077 < 2000). Сырой счёт строк журнала PostgreSQL вдвое больше (44 / 44 / 38 / 22) из-за `DISCARD ALL` после каждой команды; для администратора три раза читаются учётные записи и сотрудники и два раза — справочники (запас до 25 небольшой).

Замер сделан в записи 3 и не повторялся: код стола в дельтах записей 4 и 5 не менялся (запись 5 меняет `PublicationPlan`, ответы предпросмотров публикации и web), а `CatalogStore.ViewerAsync` из записи 4 стол не вызывает.

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
