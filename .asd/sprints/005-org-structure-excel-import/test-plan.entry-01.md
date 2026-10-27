# Test plan — sprint 005-org-structure-excel-import, entry 1

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Парсер xlsx (`OrgImportParser`, `XlsxArchiveGuard`) | DTD/внешняя сущность, ZIP-бомба, мусор вместо книги, склейка строк (ё/регистр/пробелы), пропуск уровня | unit (InternalsVisibleTo) + component через API | add | Чистая функция над байтами: быстрая проверка границ; на API — 413/Expect-continue и «файл не принят» с номером строки и колонки. Покрывает риск плана «XML DTD запрещён» |
| `POST /org-import/preview` | Предпросмотр что-то меняет; 13 групп плана считаются неверно; тёзки; последний администратор | component/contract (реальный PostgreSQL) | add | Сцена `ImportScene` даёт все 13 групп с точными значениями; повторный предпросмотр даёт тот же отпечаток; «предпросмотр ничего не меняет» проверено по БД |
| `POST /org-import/apply` | Неверный итог, неидемпотентность, нет аудита, решения по умолчанию, нарушение правил, подмена плана | component/contract | add | Счётчики и состояние БД после применения; повторный импорт ничего не меняет; запись аудита `OrgImport.Applied`; 409 с `planChanged` отличается от 409 нарушения правила; плохие решения/руководители — 400 с полем |
| Отпечаток плана | Применение плана, которого пользователь не видел | component (5 видов изменения) | add | Теория: другие данные, другой файл, новое подразделение, которого нет в файле, мусор, пустой файл |
| Гонки импорта | Два применения одного плана; правка между предпросмотром и применением; потеря единственного администратора | component, детерминированная синхронизация (блокировки БД, без sleep) | add | Дерево держится advisory-блокировкой, администраторы — строками; очередь блокировок FIFO даёт повторяемый порядок |
| Доступ (HR/admin/head/member) | HR не может импортировать или правит чужое; лишнее право у руководителя | component | add/keep | `GlobalAdminOrHr`: 200 админу и HR, 403 руководителю и сотруднику, 401 без входа; `AuthorizationTests` расширен новыми маршрутами |
| Руководитель подразделения при импорте (`ImportHeads`) | Руководитель без назначения в подразделении; освобождение; совмещение сохраняет руководство | unit + component | add | Кандидаты, подсказка, выбор, сохранение текущего, снятие, 400 на неверный выбор, отказ для уволенного |
| Назначения и совмещения сотрудника | Главное и дополнительные назначения, юрлица назначения, `legalEntityIds`/`assignments` обязательны | component | add/adapt | `EmployeeAssignmentTests`; прежние тесты сотрудников переведены на `Scenarios.EmployeeBody` |
| `POST /employees/{id}/move` | Перенос не того назначения; руководитель остаётся без назначения; 412/409 | component | add | `EmployeeMoveTests`, строки в `OptimisticConcurrencyTests` |
| `DELETE /org-units/{id}` | Удаление при препятствиях; нет перечня `obstacles` | component | add | `UnitDeletionTests`: 409 со списком препятствий |
| Справочник юрлиц (`/legal-entities`, `quick-add`) | Дубли по регистру/ё, права, версии | component | add | `LegalEntityTests` |
| Collation `ru-x-icu` и порядок | Порядок ё между е и ж, кавычки первыми; индексы не в том порядке | component на реальной БД | add | `RussianCollationTests`: порядок БД и `TextCollation.Comparer` совпадают в проверенных парах |
| HR в оргструктуре | HR получил права правки дерева, но не получил админских (`hrCandidate`, назначение HR в подразделение, `/users`, `/audit`) | component | add | `HrOrgStructureTests` |
| Мастер импорта в SPA (`OrgImportPage`, `importPlan`, `importHeads`) | Неверные решения/руководители в форме применения; блокировка шага; последний администратор; смена плана; уход со страницы | component (jsdom + `fakeApi`) + unit | add | Форма проверяется по сырому multipart: решения и руководители побайтно |
| `MoveEmployeeModal`, `useUnitDeletion`, `useConfirmUnitLosesHead`, карточки, `orgTree` | Перенос с предупреждением; подтверждение снятия с руководства; список препятствий | component/unit | add | Предупреждение «должность будет добавлена в перечень», отказ без запроса, перенос по подтверждению |
| `EmployeeForm` / `AssignmentFields` / `LegalEntitySelect` | Совмещения теряются или портятся при сохранении; «Сделать основным» | component | add | `EmployeeAssignments.test.tsx`; найден код-дефект D-1 |
| `editDialogs`, `DeskScenarios`, `OnboardEmployeePage`, `OrgStructurePage` | Новые обязательные поля формы, справочник юрлиц | component | adapt | Данные и обработчики приведены к новой схеме; таймаут диалога сотрудника поднят до `LONG_FLOW_TIMEOUT` (форма стала тяжелее) |
| AC-24 производительность импорта | Деградация на большом файле | не автоматизируется как ассерт | none | Ассерты по времени нестабильны на нагруженной машине; цифры замеров разработчика ниже, wall-clock не проверяется |
| Старые имена `isAdmin`→`canEdit`, `ru-x-icu`, правило руководителя | Остатки устаревшего термина | static (grep по репозиторию и памяти агентов) | done | см. «Остатки терминов» |

Остатки терминов (проверка по репозиторию, включая `.claude/agent-memory/**`):

- Код: `isAdmin`/`IsAdmin` остался только в проверках прав каталога карьеры и `useIsAdmin` в SPA — это право администратора, а не прежнее право правки; `canEdit` в схеме API присутствует. Устаревших употреблений не найдено.
- Память агентов (не правится, сообщается владельцу):
  - `.claude/agent-memory/asd-dev-critical/project_pg-behaviour-facts.md`, строка про сортировку: «no ICU column collation is needed» противоречит миграции `RussianCollation` (`ru-x-icu`).
  - `.claude/agent-memory/asd-dev-critical/project_org-head-rule-facts.md`: правило руководителя «только из своего подразделения», проверка «только при смене руководителя», перенос снимает руководство только в покидаемом подразделении — описывают поведение до sprint 005 (теперь руководитель обязан иметь назначение в подразделении, в том числе дополнительное).
  - Остальные упоминания (`asd-tester-critical/project_competency-test-harness.md`: `Intl.Collator("ru")` в SPA) актуальны: сортировка в SPA осталась на `Intl.Collator`.

AC-24 (замеры разработчика, не ассерты): предпросмотр 1,9–2,9 с, применение 6,0–8,6 с; с руководителями предпросмотр 3,3–3,6 с, применение 8,1–9,8 с. Время в тестах не проверяется.

Тесты вне области изменений, предлагаемые к удалению: не найдено. Нестабильные под нагрузкой тесты вне области спринта (не менялись, по одному проходят): `web/src/features/auth/linkScreens.test.tsx` («reads the token from the address fragment …», таймаут 5 с) и `web/src/features/career/catalogueTables.test.tsx` («deletes a matrix against the version in work …»). Предложение: поднять им таймаут или разобрать ожидание; без одобрения не правились.

Замечание по блокировкам: `await accounts.LockAdministratorsAsync` в `ImportDecisions.BrokenRuleAsync` избыточен при блокировке дерева, которую импорт берёт раньше (мутация «убрать вызов» не ловится ни одним тестом; поведенчески все писатели администраторов сериализуются деревом). Дефектом не заведено.

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | Тестов, не заслуживающих места, не найдено: устаревшие проверки адаптированы на месте, а не удалены | yes |

## Added tests

Дотнет: 682 → 838 (+156), web: 707 → 782 (+75).

| Test | Regression proof |
|---|---|
| `OrgImportApplyTests` — теория «план изменился» (5 видов) и гонки `OrgImportRaceTests` | mutation (проверка отпечатка отключена): `dotnet test --project tests/Competency.Tests --filter-class "*OrgImportApplyTests" --filter-class "*OrgImportRaceTests"` → exit 2, `OrgImportApplyTests.Ac20_Apply_OfAPlanThatNoLongerHolds_IsRefusedWith409PlanChanged_AndChangesNothing(kind: "changed-data")`; runs: 1 |
| `OrgImportRaceTests` (блокировка дерева в применении) | mutation (`BeginExclusiveAsync` → обычная транзакция): `dotnet test … --filter-class "*OrgImportRaceTests"` → exit 2, `Ac20_TwoAppliesOfOnePlan_ApplyItOnce_AndTheSecondIsToldThePlanChanged`, `Ac20_AnEditThatCommitsWhileTheApplyWaitsForTheTreeLock_…`, `Ac20_AnApplyAndAnEditOfAnEmployeeItMoves_…`; runs: 1 |
| `OrgImportTests.Ac9_Preview_IsOpenToAdministratorsAndHr_AndRefusedToHeadsAndMembers`, `OrgImportApplyTests.Ac9_Apply_IsRefusedToHeadsAndMembers_AndChangesNothing` | mutation (`GlobalAdminOrHr` → `RequireAuthorization()`): `dotnet test … --filter-class "*OrgImportTests" --filter-class "*OrgImportApplyTests"` → exit 2, оба теста; runs: 1 |
| `OrgImportParserTests.Ac10_AZipThatInflatesBeyondTheLimit_…`, `OrgImportTests.Ac10_Preview_AZipThatInflatesBeyondTheLimit_…` | mutation (лимит распаковки отключён): `dotnet test … --filter-class "*OrgImportTests" --filter-class "*OrgImportParserTests"` → exit 2, оба теста; runs: 1 |
| `importHeads.test.ts` («keeps the current head who stays over the only candidate», «keeps a current head the file does not name only while …») | mutation (`importHeads.ts`: условие сохранения текущего руководителя отключено): `npx vitest run src/features/org-import` → exit 1, оба теста; runs: 1 |
| `EmployeeAssignments.test.tsx` «swaps a secondary assignment …» | fail-first vs D-1: `npx vitest run src/features/org-structure/EmployeeAssignments.test.tsx` → exit 1, тест падает на текущем коде; runs: 3 (при перечне должностей, содержащем обе должности в обоих подразделениях, тест проходит — причина подтверждена) |
| Остальные новые тесты (`OrgImport*`, `EmployeeAssignmentTests`, `EmployeeMoveTests`, `UnitDeletionTests`, `LegalEntityTests`, `RussianCollationTests`, `HrOrgStructureTests`; web: `OrgImportPage`, `importPlan`, `MoveEmployeeModal`, `useUnitDeletion`, `useConfirmUnitLosesHead`, `EmployeeAssignments`, дополнения в `EmployeeCards`, `UnitCard`, `orgTree`, `ApiError`, `apiErrors`) | n/a — проверяют поведение, прошедшее ревью; доказательство прохождения — полный прогон ниже |
