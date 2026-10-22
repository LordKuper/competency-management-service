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

## Risk → check decisions

Проверки выбраны после того, как код существует, по реальному `git diff --stat 956eeeb HEAD`. Backend — HTTP-тесты на реальном хосте API против общего контейнера PostgreSQL (так устроен весь набор); чистые правила стола — модульные тесты; SPA — vitest с подменой сети `fakeApi` (e2e по правилу проекта нет).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `Competency.Desk/DeskRules.Merge` (поглощение, порядок причин, группы, порядок элементов) | change: public contract — стол показывает не то, что нужно, или в неверном порядке | unit | add | чистая функция; таблица поглощений, порядок причин и группа каждого вида `DeskKind` проверяются без хоста (`DeskRulesTests`) |
| Источники стола оргструктуры и учётных записей, администратор (`OrgStructureDeskSource`, `UserManagementDeskSource`) | change: security — источники видны только своим ролям, стол только о себе | component/contract | add | находки зависят от данных нескольких модулей; проверяется ответ `/desk`, `/desk/items`, `/desk/count` под каждой ролью (`DeskAdministratorTests`) |
| Источник career-framework: версии и матрицы, треки, уровни, должности, справочники, предпосылки | change: security — неопубликованное попадает только к тем, кто его читает | component/contract | add | права читателя считаются в БД и снимке оргструктуры; нужны реальные данные (`DeskMatrixTests`, `DeskTrackTests`, `DeskCatalogTests`, `DeskPrerequisiteTests`) |
| Постраничность и потолок `GET /desk/items` (`limit` ≤ 2000), счётчик | change: perf — вычисление на каждый запрос | component/contract | add | граница 2000 и страницы проверяются на 2001 вставленной SQL должности (`DeskPagingTests`, `DeskCeilingTests`) |
| Бюджет стола: p95 `/desk/count` ≤ 300 мс, `/desk` ≤ 600 мс, ≤ 25 SQL-команд | change: perf | замер на сиде 10 000 (вне набора) | none (замер записан ниже) | тест со временем недетерминирован и запрещён правилами; замер сделан одноразовым скриптом (вне репозитория) на `tests/perf/seed-10k.sql`, итог — в «Suite run» |
| Связь «должность — уровень», готовность трека | change: public contract; lock ordering | component/contract + гонка | add | правила привязки и снятия последней должности; гонка двух снятий держит блокировку каталога (`LevelPositionTests`, `CareerLockTests`) |
| Блокировки справочников: новые методы связей | change: lock ordering | component | add | две строки в теориях `CareerLockTests` (`level-position-link`, `level-position-unlink`): запрос ждёт эксклюзивной блокировки каталога |
| Общее правило публикации с каскадом (`CatalogPublication`, `PublicationPlan`) | change: workflow state machine; public contract; security | component/contract | add | предпросмотр, метка `cascadeTag`, 412 `publication`, 409 `PublicationRefusal`, журнал на каждую сущность (`PublicationCascadeTests`) |
| Методы чтения сценариев: полнота, изменения, настройка подразделения, `me/place`, неиспользуемые записи, шаг мастера | change: migration (`builder_step`); security | component/contract | add | права `setup` — только администратору, `me/place` — только о себе, шаг 2–6 (`ScenarioReadTests`, `MyPlaceTests`, `UnitSetupTests`, `UnitSetupReadTests`) |
| Письма-напоминания (`CareerMail`, `CareerMailQueue`) | change: lock ordering — отправка только после коммита, вне блокировок | component + перехват почты | add | письма читаются из общего Mailpit; сбой сервера — отдельный хост с недоступной почтой (`CareerMailTests`, `CareerMailFailureTests`) |
| Повторная проверка адреса при отправке и ёмкость очереди 1000 (`CareerMailQueue`) | change: lock ordering | — | none | отправка идёт в фоне, вход в очередь переполняется только на 1000 писем; в тестах это потребовало бы сна или подмены часов — потолок записан ниже |
| Общие блоки, матрицы должности, роль HR по сотруднику | change: public contract; migration | component/contract | add | `CompetencyBlockTests`, `PositionMatrixTests`, `HrRoleTests` |
| Авторизация и `If-Match` по каждому методу контракта | change: public contract; security | contract | add | перечни методов выводятся из `openapi/openapi.json` (`ApiContract`): новый метод без строки в таблице права или без `If-Match` валит тест |
| Термин «область» вместо «направление» (`CareerDirectionEndpoints`) | change: public contract | component/contract + component | add | текст отказа удаления закреплён и на backend (`CatalogDeletionTests`), и на SPA (`useConfirmDeletion.test.tsx`); см. D-1 |
| Возврат после входа только внутри приложения (`returnTo.ts`, `LoginPage`) | change: security — open redirect | unit + component | add | `returnTo.test.ts` (чужой источник, `//`, `\`, управляющие символы), `authFlow.test.tsx` (возврат и сообщение при отказе) |
| Рабочий стол в SPA: страница, счётчик в меню, сценарные карточки, тексты причин | change: public contract (рендер ответа `/desk`) | component | add | `DeskPage`, `DeskCounter` (нет опроса), `DeskScenarios`, `deskTexts` — по каждому виду и причине |
| Защита несохранённых правок (`leaveGuard`, `AppModal`) | change: workflow (AC-61) | component | add | `leaveGuard.test.tsx`, `AppModalLeave.test.tsx` — вопрос при уходе, удержание при сохранении, сброс отметки |
| Утверждение записи со списком черновиков (`usePublishEntry`) | change: workflow state machine; public contract | component | add | метка перечня, повторное подтверждение, отказ, предложение утвердить черновик (`usePublishEntry.test.tsx`); см. D-2 |
| Мастер матрицы, окна блока/компетенции/области, редактор матрицы и прочие страницы сценариев в SPA | риск «none» в плане; поведение собрано из уже проверенных хуков и серверных правил | — | none | e2e не принято правилом проекта; страницы ничего не вычисляют сверх покрытого; существующие компонентные тесты оставлены; потолок записан ниже |
| Документы (`docs/**`, `.asd/project/**`), UX-макеты, память dev | нет поведения | — | none | проверять нечем; сверка терминов — ниже |
| Контракт `1.0.0`, `InitialCreate`, `check:api` | change: public contract; migration | contract (существующие `ContractTests`) | keep | `ContractTests` читает контракт через `ApiContract`; `npm --prefix web run check:api` — exit 0 |

### Примечания к решениям

- **Выжившие мутанты, признанные эквивалентными** (тесты не усиливались; отличия в ответе на данных, которые создаёт API, при разборе не найдено): `T6` и `MP7` (`Status == Active` → `!= Inactive` для трека отличается только на черновом треке, которого в этом месте не бывает); `T6b` (неактивные уровни в счёте готовности); `P2` (`IsActive` у HR-учётной записи); `HR4` (`IsActive` у кандидата в HR); `ML3` (уровень в письме о черновиках); web `W1`, `W2`, `W5` — слои защиты `returnTo` перекрывают друг друга, пары `W1+W5`, `W2+W5` убиты.
- **Потолки, оставленные без проверки:** повторная проверка адреса при отправке и ёмкость очереди писем (см. выше); страницы-сценарии SPA целиком (мастера); поведение браузера при настоящем переходе.
- **Проверка остатков терминов** (`artifact-layout.md` «Agent memory»), по удалённым в диффе строкам и терминам: в коде — `Направление используют…` в `src/Competency.CareerFramework/CareerDirectionEndpoints.cs` (D-1); в `web/src/features/career/MatrixEditor.tsx` метка «Нет требований» при том, что sprint AC-57 убрал замечания полноты «нет требований» — метка достижима только для компетенции без требования, чего после AC-57 не бывает (flag, не D-N: теста, который падает, нет). Попадания в память агентов (не правились, владелец — по `review-policy.md` «Memory-fix dispatch»): `.claude/agent-memory/asd-dev-critical/project_position-matrix-shared-block-facts.md` (строки про `NoCompetencyCategories`, `BlockScopeNoMatrixScale`, `RequirementIsScore`, заглушки `CompetencyCategoryEndpoints`/410 в контракте 3.0.0 и `MatrixContent.Blocks`), `.claude/agent-memory/asd-dev-critical/project_contract-reset-and-squash-facts.md` (строка 14: `CareerMigrationTests`, `PositionMigrationTests`, `Infrastructure/MigrationDatabase.cs` удалены), `.claude/agent-memory/asd-dev-critical/project_position-catalogue-facts.md` (строка 3: «контракт 3.0.0»), `.claude/agent-memory/asd-ux/project_ux-docs-conventions.md` (строка 15: подпись «Шаг n из N» у линейки шагов, убранная AC-187).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — (ни один тест не удалён; тесты миграций данных, категорий, типа в справочнике и мёртвых кодов полноты удалены ранее Task 58–59 в `impl`) | — | — |

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here. Мутации ставились в копию `git archive HEAD` (production-код), запуск `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-class <класс>`; web — `node node_modules/vitest/vitest.mjs run <файлы>` с подстановкой строки в production-файл и восстановлением в `finally`. «Убит» — runner вернул ненулевой код и назвал упавший тест. Прогон без мутации (control) — зелёный, число тестов в колонке `runs`.

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/DeskRulesTests.cs` | мутации R1–R13 (порядок причин, пара поглощения, повторы находки, ссылка рядом с действием, поглощение по объекту, цель первой причины, приглашение, порядок событий и идентификаторов, вид без группы): 13 из 13 убиты, exit 2, напр. R1 → `Ac154_ReasonsFollowTheOrderOfTheTable_AndTheFirstReasonDecidesTheGroup`; R2 первым прогоном выжил (порядок из одного элемента) → тест перестроен на две находки → exit 2, `Ac154_EveryKind_HasAGroupAndAPlaceInTheOrderOfReasons`; runs: 71 |
| `tests/Competency.Tests/DeskAdministratorTests.cs` | мутации A1–A15 (пустые места в должностях, пустое подразделение с детьми, неактивное в использовании, срок приглашения 1 и 3 суток, заблокированные руководитель и HR, источники для всех): 14 из 14 убиты, exit 2; runs: 18 |
| `tests/Competency.Tests/DeskTrackTests.cs` | мутации T1–T11 (уровни, трек и должность как действие или ссылка, снятие с привязок, право руководителя, только свои треки): 10 из 11 убиты, exit 2; `T6` выжил, эквивалентный (см. примечания); runs: 18 |
| `tests/Competency.Tests/DeskMatrixTests.cs` | мутации M1–M12 (черновик для не-руководителя, шаг мастера, согласующий, своя версия на согласовании, задержка 5 и 10 суток, комментарий и правки): 12 из 12 убиты, exit 2; runs: 12 |
| `tests/Competency.Tests/DeskCatalogTests.cs` | мутации C1–C9 (добавленные уровни, перемещение, правки компетенции, автор, неиспользуемые для всех): 8 из 8 убиты, exit 2; runs: 16 |
| `tests/Competency.Tests/DeskPrerequisiteTests.cs` | мутации P1–P5 (предпосылки HR, подразделения, сотрудника, видимого трека): 4 из 5 убиты, exit 2; `P2` выжил, эквивалентный; runs: 16 |
| `tests/Competency.Tests/DeskPagingTests.cs`, `DeskCeilingTests.cs` | мутации G1–G6 (первые 20, `limit` 0, фильтр группы, потолок, итог после обрезки, признак ссылки): 6 из 6 убиты, exit 2; runs: 16 |
| `tests/Competency.Tests/Infrastructure/DeskReads.cs` | вспомогательные методы чтения стола; проверяются мутациями классов выше |
| `tests/Competency.Tests/LevelPositionTests.cs` | мутации L1–L17 (право правки, доступность должности, должность с матрицей, последняя должность, неактивная должность, готовность, счёт сотрудников): 14 из 14 убиты, exit 2; `L17` первым прогоном выжил → тест усилен → exit 2, `Ac87_ATracksPositions_ListEachLevelWithItsPositionsAndHeadcount…`; runs: 23 |
| `tests/Competency.Tests/PublicationCascadeTests.cs` | мутации K1–K10 (перечень черновиков, метка, 412, 409, откат при отказе, журнал по каждой сущности): 10 из 10 убиты, exit 2; `K1` первым прогоном не применился (якорь не уникален) → повтор с уникальным якорем → exit 2, `Ac107_Ac110_Ac113_Ac114_ApprovingACompetency_Publishes…`; runs: 6 |
| `tests/Competency.Tests/CareerLockTests.cs` (строки `level-position-link`/`-unlink`, гонка `Ac98_…`) | мутации LK1–LK3 (привязка без блокировки каталога, снятие без неё, снятие под общей блокировкой): 3 из 3 убиты, exit 2 — `Ac9_ACatalogueOrMatrixChange_WaitsForAWriterOfTheCatalogues(operation: "level-position-link")` (LK1), `…(operation: "level-position-unlink")` и `Ac98_TwoRemovalsOfTheLastTwoActivePositionsOfALevel_…` (LK2), `Ac98_…` (LK3); runs: 95 |
| `tests/Competency.Tests/ScenarioReadTests.cs` | мутации SR1–SR14 (границы шага мастера 2–6 и его хранение, тип, изменения, неиспользуемые записи, архив, скрытые версии, полнота): 12 из 12 убиты, exit 2; runs: 23 |
| `tests/Competency.Tests/MyPlaceTests.cs` | мутации MP1–MP9 (матрицы чужих подразделений, текущий уровень как следующий, черновики, путь подразделения, трек): 5 из 6 убиты, exit 2; `MP7` выжил, эквивалентный; runs: 23 |
| `tests/Competency.Tests/UnitSetupTests.cs`, `UnitSetupReadTests.cs` | мутации US1–US8 (заблокированный как приглашённый, кандидаты, унаследованные должности, доступ всем, цепочка предков, готовность HR): 8 из 8 убиты, exit 2; runs: 23 |
| `tests/Competency.Tests/CareerMailTests.cs` | мутации ML1–ML13 (письмо себе, получатели, маршрут, тема, возврат, слова): 7 из 8 убиты, exit 2; `ML3` выжил, эквивалентный; runs: 23 |
| `tests/Competency.Tests/CareerMailFailureTests.cs` | мутации MF1–MF4 (потеря причины, исполнителя, названия действия и запроса в журнале `Mail.SendFailed`): 4 из 4 убиты, exit 2; runs: 23 |
| `tests/Competency.Tests/CompetencyBlockTests.cs` | мутации BL1–BL10 (права руководителя, дубли, пустой блок, черновая компетенция в действующем блоке, «для всех», удаление используемого, область): 10 из 10 убиты, exit 2; runs: 35 |
| `tests/Competency.Tests/PositionMatrixTests.cs` | мутации PM1–PM9 (основа «и то, и другое», недоступная должность, дубль матрицы должности, колонка, черновая матрица, веса): 9 из 9 убиты, exit 2; `PM8` первым прогоном не собрался (синтаксис мутации) → повтор → exit 2, `Ac196_Ac197_APositionWithAMatrixIsNotWithoutATrack_OnTheDeskAndInTheEmployeesPlace_ByTheStatusOfItsVersion`; runs: 35 |
| `tests/Competency.Tests/HrRoleTests.cs` | мутации HR1–HR6 (права HR на должности, снятие роли при закреплении, кандидаты, сессии при смене роли): 4 из 5 убиты, exit 2; `HR4` выжил, эквивалентный; runs: 35 |
| `tests/Competency.Tests/AuthorizationTests.cs` (+`Infrastructure/ApiContract.cs`) | мутации AU1–AU4 (стол открыт анониму, настройка подразделения открыта пользователям, метод контракта без строки в таблице, строка таблицы удалена): 4 из 4 убиты, exit 2; runs: 35 |
| `tests/Competency.Tests/OptimisticConcurrencyTests.cs`, `ContractTests.cs` | мутация OC1 (строка таблицы `If-Match` удалена): убита, exit 2; runs: 35 |
| `tests/Competency.Tests/CatalogDeletionTests.cs` | fail-first vs D-1: `dotnet test --solution Competency.slnx` → exit 2, `Competency.Tests.CatalogDeletionTests.Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices`; с текстом, исправленным в копии `HEAD` → exit 0; runs: 16 |
| `web/src/features/auth/returnTo.test.ts`, `web/src/app/authFlow.test.tsx` | мутации W1–W11 (`//`, `\`, управляющие символы, `DEL`, чужой источник, разбор `//`, экраны входа, исходное значение, непроверенные цель и чтение, вход без цели): 8 из 11 убиты поодиночке, exit 1; `W1`, `W2`, `W5` выжили поодиночке (перекрывающиеся слои), пары `W1+W5` и `W2+W5` убиты, exit 1, `return target after sign-in (open redirect) refuses …`; runs: 64 |
| `web/src/features/desk/DeskScenarios.test.tsx` | мутации D1–D8 (первое подразделение, привязка без сотрудника, приём без должности, привязка без трека, построение не всегда, HR подразделения, подпись, нейтральная карточка до загрузки): 8 из 8 убиты, exit 1; runs: 12 |
| `web/src/features/desk/deskTexts.test.ts` | мутации E1–E5 (экран доработки, вид черновика, первый из треков, маршруты проверки и черновика): 5 из 5 убиты, exit 1; runs: 26 |
| `web/src/features/desk/DeskCounter.test.tsx` | мутации C1–C7 (предел, «99+», ошибка, перечитывание при переходе, после сбоя, при фокусе, опрос): 7 из 7 убиты, exit 1; `C7` (опрос раз в секунду) первым прогоном выжил → поддельные таймеры включены до рендера → exit 1, `the desk counter in the menu (AC-16…AC-21) does not poll: nothing is read again while the address and the data stay`; runs: 10 |
| `web/src/features/desk/DeskPage.test.tsx` | мутации P1–P7 (ссылки как действия, «всё сделано», шаг «ещё», обрезка на первой странице, версия и признак письма, перечитывание после отказа): 7 из 7 убиты, exit 1; runs: 9 |
| `web/src/app/leaveGuard.test.tsx`, `web/src/app/AppModalLeave.test.tsx` | мутации L1–L10 (вход, то же место, сохранение, инверсия, предупреждение вкладки, окно, много окон, своё сохранение), A1–A4 (закрытие диалога с правками, отметка вызывающего, сохранение, отслеживание правки): 14 из 14 убиты, exit 1; `L3` (сохранение не удерживает переход) первым прогоном выжил → добавлен тест «only a save runs» → exit 1, `the guard against losing edits (AC-61) holds a move silently while a save runs even when nothing else is unsaved`; runs: 11 и 6 |
| `web/src/features/career/usePublishEntry.test.tsx` | мутации PE1–PE13 (11 заданных: метка перечня, «изменился» как «появился», без повторного подтверждения, пустой перечень с уведомлением, предложение для любого статуса, 404 как жалоба, успех для пустого, без перечитывания, блокеры, остаток, подпись кнопки): 11 из 11 убиты, exit 1; `PE5`, `PE11`, `PE12` проверены с подставленной обходной правкой D-2 (`<Link>` → `<a>`; без неё упавший тест D-2 красен и без мутации), для `PE5` и `PE12` добавлены тесты «asks again, saying the list changed, even when the new list is empty…» и «counts the further records…» → exit 1; тест D-2 — fail-first vs D-2: exit 1, `explains a refusal the server gives at the moment of approval, with the way to the track's positions`, с обходной правкой exit 0; runs: 13 |
| `web/src/features/career/useConfirmDeletion.test.tsx` | n/a — две строки фикстуры приведены к новому термину AC-37 («Область используют…»), поведение хука не менялось; fail-first даёт backend-тест D-1 |

## Suite run

Command: `dotnet test --solution Competency.slnx --artifacts-path <scratch>` и `npm --prefix web test` (`test` из commands.yaml; артефакты вынесены, чтобы не трогать DLL запущенного API).
Scope: full — затронуты общие `Competency.Platform` и конфигурация (safety valve «Impacted test set»).
Result: fail — backend 660 тестов: 659 успешно, 1 упал (D-1), 0 пропущено, exit 2; web 492 теста: 491 успешно, 1 упал (D-2) и 1 необработанная ошибка (`Cannot destructure property 'basename' of 'React$1.useContext(...)' as it is null.`), 0 пропущено, exit 1. До стратегии: backend 483/0, web 348/0.
Lint / build: pass — `npm --prefix web run lint` (biome ci + tsc) exit 0; `dotnet build Competency.slnx` 0 предупреждений, 0 ошибок; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0.
HEAD: 5f2ceac

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
| D-1 | 1 | src/Competency.CareerFramework/CareerDirectionEndpoints.cs | Expected refused.Json!["detail"]!.GetValue<string>() to be the same string, but they differ at index 0: | Competency.Tests.CatalogDeletionTests.Ac25_ADirectionInUse_IsRefused_NamingTheTracksCompetenciesHistoryAndMatrices | pending | |
| D-2 | 1 | web/src/features/career/usePublishEntry.tsx | AssertionError: expected null not to be null | approving an entry under the list of what is published with it (AC-110…AC-112) > explains a refusal the server gives at the moment of approval, with the way to the track's positions | pending | |

Пояснения (вне строк таблицы): D-1 — константа `InUseMessage` («Направление используют треки, компетенции или матрицы — удалить нельзя») не обновлена под AC-37 (сущность называется «область»), `CareerMail` и остальной текст уже используют «область». D-2 — `RefusalContent` вставляет `<Link>` в содержимое `modal.error`, а модальное окно из `App.useApp()` в `Providers` отрисовывается вне `RouterProvider` (структура приложения такая же, как в тесте), поэтому при отказе «нет действующей должности» с известным треком `Link` падает на `useContext(...) is null` и экран остаётся пустым; проверено: замена `<Link>` на `<a>` в копии делает весь файл зелёным (13 из 13).

## Manual verification

| AC | Steps | Expected observation |
|---|---|---|
| — | автоматизируется всё проверяемое; ручной проверки не требуется | — |
