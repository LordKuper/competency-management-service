---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 003-positions-career-competency

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | f902116b8e4f2d31ce8ce80c6b580b232b85508c | full change surface |
| 2 | 165dc5420ce614ad26d4e615a7919a68b85ce338 | delta since entry 1 |
| 3 | 637c04f50dea52a8503f5a92524de8eb429ff850 | delta since entry 2 |
| 4 | e10a343048d0504ad36453b6e7ac12c3c68b6f12 | delta since entry 3 |
| 5 | 6aa63a8db9b1b99e10c27a6f880763b5f08d5535 | delta since entry 4 |
| 6 | 5d5b60e743f80082a91a77ad83dbb86d898b6649 | delta since entry 5 |
| 7 | d4e23a9fce9780ef476b8bb49473a0759d21dd0e | delta since entry 6 |
| 8 | dca881533c960a5701e5943675ed2c75fd7de144 | delta since entry 7 |

Строки повествования записи 1 — в `test-plan.entry-01.md`, записи 2 — в `test-plan.entry-02.md`, записи 3 — в `test-plan.entry-03.md`, записи 4 — в `test-plan.entry-04.md`, записи 5 — в `test-plan.entry-05.md`, записи 6 — в `test-plan.entry-06.md`, записи 7 — в `test-plan.entry-07.md`.

## Risk → check decisions

Дельта записи 8 — исправления impl-review волны 3, итерация 4 (сравнение с `d4e23a9`, только `web/`): `2344490` — комментарий в `RowActions.tsx` (пример в документации `menuLabel`, поведения нет); `542155f` — блоки строк справочников и пустых таблиц перенесены из `RowActions.test.tsx` в `catalogueTables.test.tsx`, утверждения не менялись. Нового существенного риска нет. Набор: оба затронутых файла тестов; предохранитель «Impacted test set» не сработал (общая инфраструктура — `theme.ts`, `EntryActions`, настройка тестов — не менялась; бэкенд не менялся).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Комментарий в `RowActions.tsx` (`2344490`) и перенос тестов между файлами (`542155f`) | нет: поведение не менялось, утверждения те же; потеря перенесённого теста (число тестов web до и после совпадает: 369) | existing (`RowActions.test.tsx`, `catalogueTables.test.tsx`) | none | Новые тесты ничего бы не защитили: перенос покрыт теми же утверждениями, число тестов сохранено |

## Removed tests

Удалений нет: ни один тест не потерял предмет.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Новых тестов нет (решение `none` выше).

| Test | Regression proof |
|---|---|

## Suite run

- Command: impacted — `npx vitest run src/features/career/RowActions.test.tsx src/features/career/catalogueTables.test.tsx` (из `web/`); затем полный `vitest run` web (половина `test` из `commands.yaml`); бэкенд не запускался: дельта не содержит файлов `src`, `tests`, `openapi`
- Scope: impacted (запись 8, до и после стратегии) + полный web
- Result: pass — impacted: exit 0, 2 файла, 27/27; полный web: exit 0, 39 файлов, 369/369 (число не изменилось)
- Lint / build: не запускались (изменён комментарий и перенос тестов; `2344490` и `542155f` проходили lint в review-fix)
- HEAD: 2c474cb

## Defects

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Competency.CareerFramework/CatalogStore.cs | Expected 200 OK but got 500 InternalServerError: {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"An error occurred while processing your request.","status":500,"traceId":"00-80cd25aedec348bb0d15ef853467e7a7-e56c7cc0646eeb8d-00"} | Competency.Tests.TrackAndLevelTests.Ac11_LevelOrder_FollowsTheListedLevels_LeavesTheUnlistedOnesInTheirOrderAtTheEnd_AndChangesTheTracksVersion | fixed | 3ed0b25 |
| D-2 | 1 | src/Competency.CareerFramework/CareerFrameworkEntityConfiguration.cs | Expected revising.Status to be HttpStatusCode.OK {value: 200} because {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"An error occurred while processing your request.","status":500,"traceId":"00-00f081003e2ba59481f13d4480ac9a4c-26423ac8c9bb98d4-00"}, but found HttpStatusCode.InternalServerError {value: 500}. | Competency.Tests.MatrixWorkflowTests.Ac16_AcceptAndRevise_MakesTheReturnedVersionADraftAgain_KeepingTheApproversChanges | fixed | 23c909a |
| D-3 | 1 | src/Competency.CareerFramework/MatrixEndpoints.cs | Expected stale.Status to be HttpStatusCode.PreconditionFailed {value: 412} because {"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"An error occurred while processing your request.","status":500,"traceId":"00-07de4405d00725155b006eae87c674b1-3a48c5ea1b5ad2bf-00"}, but found HttpStatusCode.InternalServerError {value: 500}. | Competency.Tests.CatalogDeletionTests.Ac17_DeletingAMatrix_WithAStaleVersion_IsRefusedWith412_AndKeepsTheMatrixAndItsHistory | fixed | a2ec2a5 |
| D-4 | 1 | src/Competency.CareerFramework/CatalogEndpoints.cs | Expected refused.Status to be HttpStatusCode.Conflict {value: 409} because {"id":"0803b4ac-c2fd-4e2e-942b-ece4cf678c9e","name":"Трек-23737ceb","directionId":"1e45094c-50a7-48b3-a674-d79752df42f7","status":"Active","ownerOrgUnitId":"46f96333-b709-4962-93ec-67152ede1c64","ownerOrgUnitName":"Отдел-e79556aa","version":1}, but found HttpStatusCode.OK {value: 200}. | Competency.Tests.TrackAndLevelTests.Ac11_ApprovingATrack_IsRefusedWhenTwoOfItsDraftLevelsOfDifferentAuthorsHaveOneName_AndChangesNothing | fixed | 7c5912a |
| D-5 | 1 | .claude/agent-memory/asd-dev-critical/project_career-catalogue-facts.md | leftover-term check, строки 3 и 11: «next levels a uuid[]» и «That is why `CareerLevel.NextLevelIds` is a primitive `List<Guid>` (`uuid[]`, no FK; levels are never deleted)» — механизм `next_level_ids` удалён из уровня | leftover-term check: NextLevelIds | fixed | 04864f3 |
| D-6 | 1 | .claude/agent-memory/asd-dev-critical/project_employee-name-split-facts.md | leftover-term check, строка 17: «Form `employee` gives `#employee_lastName\|firstName\|middleName\|position\|orgUnitId`» — поле формы теперь `positionId` | leftover-term check: employee_position | fixed | 04864f3 |

Причины по журналу сервера и коду (подсказка для test-fix; в сравнении на stalemate не участвует):

- D-1: `PUT /api/v1/career/tracks/{id}/level-order` — `InvalidOperationException` из `Multigraph.ThrowCycle` (`CommandBatchPreparer.TopologicalSort`) в `CatalogStore.SaveAsync`: EF видит циклическую зависимость обновлений `position` под уникальным индексом `ux_career_levels_track_position` (по стеку, текст исключения не логируется); `MoveLevelsAsync` обходит индекс сырым `UPDATE`, но EF этого не знает.
- D-2: `POST /api/v1/career/matrix-versions/{id}/accept-and-revise` — `22001` в `MatrixWorkflow.TransitAsync`: колонка `matrix_version_decisions.kind` — `varchar(16)`, значение `AcceptedAndRevising` — 19 символов (`EnumMaxLength = 16` в конфигурации, колонка создана миграцией `20261009102128_CompetencyMatrices`; проверочное ограничение значение допускает).
- D-3: `DELETE /api/v1/career/matrices/{id}` с устаревшим `If-Match` — `23001` (`restrict_violation`) в `MatrixEndpoints.DeleteAsync`: удаление строки версии с устаревшей версией не затрагивает ни одной строки, а следующее в той же пачке удаление матрицы нарушает внешний ключ раньше, чем EF прочтёт число затронутых строк. С актуальной версией тот же запрос даёт 204 (проверено).
- D-4: `POST /api/v1/career/tracks/{id}/approve` — `ApproveLevelsAsync` проверяет название каждого уровня запросом к базе, где оба одноимённых черновых уровня ещё черновые, поэтому оба проходят проверку и становятся действующими вместе.
- D-5, D-6: память `asd-dev-critical` правит оркестратор (`review-policy.md` "Autofix vs escalation").

## Manual verification

| AC | Steps | Expected observation |
|---|---|---|
| AC-19 | Открыть матрицу с версией, возвращённой согласующим с правками, на ширине desktop и tablet (около 1024 px); прокрутить таблицу вправо | Первый столбец «Компетенции» остаётся на месте; оценки показаны чипами `rating-chip`; строки и ячейки, изменённые согласующим, выделены фоном `warning-bg` с иконкой и текстом «Изменено согласующим»; ничто не обрезается |
| AC-11 | В окне трека с тремя уровнями перетащить строку мышью с третьего места на первое; затем то же клавишами (пробел, стрелка вверх, пробел) и отменить перенос клавишей Escape; сохранить и открыть окно заново | Строка следует за указателем, остальные расступаются; Escape отменяет перенос и не закрывает окно; сохранённый порядок совпадает с показанным; подсказка над списком читается целиком |
| AC-24 | В таблице «Матрицы» открыть фильтр «Подразделение» при длинных названиях подразделений, ввести часть названия, выбрать подразделение | Названия видны полностью (список шире столбца); поиск сужает дерево; выбор сразу применяет фильтр и закрывает выпадающий список; клик по дереву не закрывает его раньше |
| AC-19 | В браузере открыть окна разных видов: форму с полями, форму без полей, подтверждение удаления, подтверждение увольнения | Фокус в первом поле; в окне без полей — на основной кнопке; в подтверждении необратимого действия — на «Отмена»; Enter в таком подтверждении ничего не удаляет |
| AC-24 | В таблицах «Матрицы» и «Компетенции» открыть фильтры «Направление», «Трек», «Категория», «Шкала»; ввести часть названия и выбрать запись из списка | Список открывается внутри окна фильтра и не закрывает его раньше выбора; поиск идёт по набранному тексту; названия видны полностью; выбор сразу применяет фильтр и закрывает окно |
| AC-19 | В браузере открыть таблицы справочников (шкалы, компетенции, категории) и раскрыть «…» в строке; в шапке открыть меню «Администрирование» | Меню «…» — обычный светлый выпадающий список (`dropdown-item`), «Удалить» и «Деактивировать» в нём красные; у шкалы и компетенции видны два действия; подменю «Администрирование» тёмное, в цвет шапки (`surface-inverse`), без светлой подложки, пункты читаются; пустые таблицы показывают «Шкал пока нет» и «Треков пока нет» с картинкой |

result: pass — п.1 матрица с правками согласующего, пользователь 2026-10-12
result: pass — п.2 перетаскивание уровней, пользователь 2026-10-12
result: pass — п.3 фильтр-дерево подразделений, пользователь 2026-10-12
result: pass — п.4 фокус в окнах, пользователь 2026-10-12
result: pass — п.5 фильтры-справочники с серверным поиском, пользователь 2026-10-12
result: pass — п.6 меню «…» строки, подменю «Администрирование», пустые таблицы, пользователь 2026-10-12
