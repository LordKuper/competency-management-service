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

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `usePublishEntry` вызывает `useNavigate()` (исправление D-2); хук подключён в модальных окнах и страницах стола | change: workflow — окно, показанное вне маршрутизатора, не должно падать; окно, показанное в нём, должно вести по ссылке | component | keep | существующий `usePublishEntry.test.tsx` закрепляет отказ в окне вне маршрутизатора и подпись ссылки; два теста без маршрутизатора (`ScaleModal`, `CompetencyModal`) исправлены как дефект теста (см. «Added tests») |
| `InUseMessage` «Область используют…» (исправление D-1) | change: public contract — текст отказа | component/contract | keep | `CatalogDeletionTests.Ac25_…` закрепляет текст; зелёный |
| Порядок проверок передачи на согласование: `If-Match` раньше полноты документа (AC-17, TST-1) | change: public contract — устаревшая версия отвечает 409 вместо 412 | contract | keep | тест `Ac17_AStaleVersion_IsRefusedWith412_BeforeTheHandOverCheckJudgesTheDocument` сохранял полный документ и не мог упасть по заявленному свойству; теперь документ неполный (как в `Ac15_Return_WithChangesThatLeaveTheVersionIncomplete`), мутация порядка убита |
| Допустимость типа компетенции в документе версии (`MatrixContent.ValueProblem`, AC-55, TST-2) | change: public contract — число вне перечисления попало бы в jsonb | contract | add | два случая в `Ac14_Document_IsRefusedWhenItIsNotWellFormed`: `competencies[0].type = 999` и тип 999 внутри общего блока |
| Поле «Примеры подтверждений» компетенции (`evidenceExamples`, AC-33 справочника, AC-56 плана, TST-3) | change: public contract — сохранение, возврат, история версий, «не передано — прежнее», «пусто — очистка», предел 4000 | contract | add | `Ac12_EvidenceExamples_AreKept_ReturnedAndWrittenToTheHistory_AbsentKeepsThemAndBlankClearsThem` и пары 4001/4000 в `Ac12_Competency_IsValidated` |
| Приглашённая учётная запись с ролью HR не считается HR-учётной записью и кандидатом (`UserManagementDeskSource`, `UserDirectory.HasHrCandidateAsync`, TST-4) | change: security — предпосылка «есть HR» ложна, пока HR не принял приглашение | component/contract | add | шаг «в системе только приглашённый HR» в `Ac204_…` (нет `HrAccount`) и в `UnitSetupTests` (остаётся `NoHrCandidates`); мутанты `P2` и `HR4` из записи 1 не эквивалентны и пересчитаны как убитые |
| Число SQL-команд `GET /desk` и `GET /desk/count` (Task 3, Task 9, TST-6) | change: perf — запрос на строку или лишний источник | integration | add | число команд — детерминированное свойство: хост пишет команды EF в журнал (`Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information`), конец запроса отмечает строка `Request finished … - 200` с уникальным `probe`; число на малом и на большем наборе подразделений одинаково и не больше 25 (`DeskQueryCountTests`). Время запроса остаётся замером (решение `none` записи 1) |
| Снятие роли HR при закреплении и закрепление при роли HR (AC-62, `custom-coding-rules.md` «Multi-row invariant locking», TST-5) | change: lock ordering — инвариант на нескольких строках | component + гонка | add | `Ac62_AnAssignmentRacingTheRemovalOfTheHrRole_IsDecidedByWhichRequestIsFirst`: тестовое соединение держит блокировку дерева, оба запроса ждут в обоих порядках |
| Базовая шкала начальных данных (`InitialCreate`, AC-25 справочника, TST-7) | change: migration — потеря или порча сида при схлопывании миграций перед 1.0 | contract | add | `BaseScaleSeedTests` на отдельном хосте: пять точек с дословными текстами, шкала по умолчанию и действующая, в журнале нет строк по её идентификатору |
| Перенос ролей в Identity и закрепление HR по сотруднику (Task 77/80, миграция `IdentityRolesAndHrByEmployee`, AC-63, TST-10) | change: authorization, migration — потеря роли или закрепления на существующей базе | component (миграция на реальной БД) | add | `IdentityRolesMigrationTests`: строки вставляются на предыдущей миграции, после перехода проверяются роли (в том числе администратор с закреплением получает обе), закрепление по сотруднику, отброшенное закрепление без сотрудника, отсутствие колонки `role`. Удаляется вместе с миграциями при схлопывании перед 1.0 |
| Журнал смены ролей (`AccountRoles.SetRolesAsync`, Task 77/80, TST-10) | change: authorization — смена ролей должна оставлять ровно одну запись и сохраняться вместе с ней | component/contract | add | `Ac15_RoleChange_IsJournaledAsOneUpdateOfTheRoles_WithBothSetsInMemberOrder` и `Ac15_RoleChangeAndItsJournalRow_AreSavedTogetherOrNotAtAll` |
| Тесты, удалённые в `impl` до записи 1 (TST-8) | нет поведения | — | none | перечень и причины — в «Removed tests» |

Предыдущая запись рассматривалась по записи 1; её строки — в `test-plan.entry-01.md`. Для остальных вызывающих хук (`BlockModal`, `NameModal`, `DraftsSection`, `CatalogParts`, `BuilderSubmitStep`, `CascadeTable`, `CatalogCleanupPage`, `DecisionError`, `DraftsQueuePage`, `ReviewPage`, `ReworkPage`, `useVersionTransition`) новых тестов не нужно: их тесты уже рендерят под маршрутизатором и зелёные (`vitest run` 492 из 492).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| `CareerMigrationTests` (класс, 203 строки) и помощник `Infrastructure/MigrationDatabase` | тесты переноса данных старых схем вместе с самими миграциями; до 1.0 перенос данных делает только последняя миграция, остальные схлопываются в `InitialCreate` (AC-58, AC-63); поведения больше нет. Удалены коммитом `29ac3bc` в `impl`, записи 1 это не отражено | да; замена — `BaseScaleSeedTests` (сид, TST-7) и `IdentityRolesMigrationTests` (перенос в Identity) |
| `PositionMigrationTests` (класс, 121 строка) | перенос данных каталога должностей старых схем; поведения больше нет по той же причине (AC-58); коммит `29ac3bc` | да; замены нет |
| `CatalogDeletionTests.Ac23_ACategory_IsDeletedUnlessCompetenciesOrTheirHistoryUseIt`, `Ac23_Categories_StartWithTheGeneralOnes_ReadableByEveryone` | категории компетенций упразднены (AC-51); поведения больше нет; коммит `29ac3bc` | да; замены нет |
| `CompetencyTests.Ac12_Competency_RefusesANumberThatNamesNoType_OnCreationAndOnChange` | тип удалён из справочника компетенций, остался в документе версии (AC-55); коммит `29ac3bc` | да; замена — два случая в `MatrixCreationTests.Ac14_Document_IsRefusedWhenItIsNotWellFormed` (TST-2) |
| `AuthorizationTests.Ac7_UserRole_IsLimitedToTheKnownValuesInTheDatabaseItself` | колонки `users.role` и её проверочного ограничения нет, роли — строки Identity (AC-62, AC-63); коммит `d94007b`; допустимость значений проверяет `HrRoleTests.Ac62_TheRolesOfAnAccount_AreASet_WithSeveralMembers_AndOnlyKnownRoles` | да; замена — этот тест |
| `MatrixWorkflowTests.Ac9_TheDraftCategoryOfAUsedCompetency_IsReadByTheRouteWhileInWork_AndMadeActiveAndJournaledByPublication`, `Ac9_Publication_IsRefusedWhenTheDraftCategoryOfAUsedCompetencyWouldTakeANameAnActiveCategoryHasNow` | черновая категория используемой компетенции исчезла вместе с категориями (AC-51); коммит `29ac3bc` | да; замены нет |
| `CareerMigrationTests.Ac12_Catalogues_StartWithTheTwelveCategoriesAndTheBaseScaleOfTheSource…` (в составе удалённого класса) | единственная проверка начальных данных: двенадцать категорий упразднены, базовая шкала 0–4 и «начальные данные не пишутся в журнал» остались требованием (AC-25) | да; замена — `BaseScaleSeedTests` |

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| `web/src/features/career/ScaleModal.test.tsx`, `CompetencyModal.test.tsx` | исправление дефекта теста (не новый тест): окна отрисованы без `MemoryRouter`, а `usePublishEntry` теперь читает маршрутизатор; fail-first: `node node_modules/vitest/vitest.mjs run src/features/career src/features/desk` → exit 1, 10 упавших, напр. `ScaleModal.test.tsx > scale form (AC-13) > asks for a name, a score and a value` (`useNavigate() may be used only in the context of a <Router> component.`); после обёртки в `MemoryRouter` — exit 0; runs: 275 |
| `tests/Competency.Tests/MatrixWorkflowTests.cs` (`Ac17_AStaleVersion_IsRefusedWith412_BeforeTheHandOverCheckJudgesTheDocument`) | мутация: `ifMatch.ApplyTo` перенесён в `MatrixWorkflow.TransitAsync` после проверок действия (проверка полноты раньше `If-Match`): `dotnet test --solution Competency.slnx --filter-class "*MatrixWorkflowTests"` → exit 2, упал `Ac17_AStaleVersion_IsRefusedWith412_BeforeTheHandOverCheckJudgesTheDocument` (1 из 35); без мутации — 35 из 35, exit 0; runs: 1 |
| `tests/Competency.Tests/MatrixCreationTests.cs` (два случая в `Ac14_Document_IsRefusedWhenItIsNotWellFormed`) | мутации в `MatrixContent.ValueProblem`: убрана проверка `Enum.IsDefined` у `competencies[].type`; убрана у `sharedBlocks[].competencyTypes[].type`; убраны обе — каждая: `dotnet test --solution Competency.slnx --filter-class "*MatrixCreationTests"` → exit 2, упал `Ac14_Document_IsRefusedWhenItIsNotWellFormed` (1 из 11); без мутаций — 11 из 11; runs: 3 |
| `tests/Competency.Tests/CompetencyTests.cs` (`Ac12_EvidenceExamples_AreKept_ReturnedAndWrittenToTheHistory_AbsentKeepsThemAndBlankClearsThem`, 4001/4000 в `Ac12_Competency_IsValidated`) | мутации: `if (EvidenceExamples is not null)` → `if (true)` в `CompetencyRequest.ApplyTo`; убрана проверка длины; пустой текст не сводится к «нет» (`OptionalText` убран); снимок версии без примеров: `dotnet test --solution Competency.slnx --filter-class "*.CompetencyTests"` → exit 2, упали `Ac12_EvidenceExamples_AreKept_…` (три мутации) и `Ac12_Competency_IsValidated` (проверка длины), по одному из 9; без мутаций — 9 из 9; runs: 4 |
| `tests/Competency.Tests/DeskPrerequisiteTests.cs`, `UnitSetupTests.cs` (шаг приглашённого HR) | мутанты `P2` (`UserManagementDeskSource`: `IsActive` у HR-учётной записи убран) и `HR4` (`UserDirectory.HasHrCandidateAsync`: условие `PasswordHash != null` убрано), выжившие в записи 1 и названные эквивалентными, не эквивалентны и теперь убиты: `dotnet test --solution Competency.slnx --filter-class "*DeskPrerequisiteTests"` → exit 2, упал `Ac204_Ac205_Ac209_Ac210_Ac218_PrerequisitesAppearAndGoWithTheData_InTheOrderOfTheirDeclaration` (P2); `--filter-class "*UnitSetupTests"` → exit 2, упал `Ac81_Ac82_Ac84_TheRowsOfAUnit_FollowItsHeadItsHrItsAccountsAndItsPositions_AsEachGapIsClosed` (HR4); без мутаций оба зелёные; runs: 2 |
| `tests/Competency.Tests/CareerLockTests.cs` (`Ac62_AnAssignmentRacingTheRemovalOfTheHrRole_IsDecidedByWhichRequestIsFirst`) | мутация: `UserEndpoints.UpdateAsync` берёт обычную транзакцию вместо `BeginExclusiveAsync` (блокировка дерева снята): `dotnet test --solution Competency.slnx --filter-class "*CareerLockTests"` → exit 2, упали оба порядка — `(assignmentFirst: True, assignmentStatus: NoContent, removalStatus: Conflict)` и `(assignmentFirst: False, assignmentStatus: BadRequest, removalStatus: OK)` (2 из 97); без мутации — 97 из 97, exit 0; runs: 1 |
| `tests/Competency.Tests/DeskQueryCountTests.cs` | мутация: в `UserManagementDeskSource.FindAsync` добавлен запрос на каждое подразделение с руководителем: `dotnet test --solution Competency.slnx --filter-class "*DeskQueryCountTests"` → exit 2, упали `Ac4_TheSqlCommandsOfADeskRequest_AreFewAndTheSameOnSmallAndOnLargerData(route: "/api/v1/desk")` и `(route: "/api/v1/desk/count")` (2 из 2; runner: `Expected large to be 18 … but found 30`, `Expected small to be between 1 and 25, but found 31`); без мутации — 2 из 2, exit 0, 18 команд на запрос; runs: 1 |
| `tests/Competency.Tests/BaseScaleSeedTests.cs` | мутации `InitialCreate`: точка в конце текста балла 2; шкала не по умолчанию; в сид добавлена строка `audit_events`: `dotnet test --solution Competency.slnx --filter-class "*BaseScaleSeedTests"` → exit 2, упали `Ac25_TheBaseScale_IsActiveAndTheDefault_WithTheFivePointsOfTheSource` (текст, по умолчанию) и `Ac25_TheCreationOfTheBaseScale_LeavesNoAuditRecord` (журнал), по одному из 2; без мутаций — 2 из 2; runs: 3 |
| `tests/Competency.Tests/IdentityRolesMigrationTests.cs` | мутации миграции `IdentityRolesAndHrByEmployee`: роль HR не выдаётся администратору с закреплением (`AND u.role <> 'GlobalAdmin'`); закрепление не переносится на сотрудника (`UPDATE org_unit_hr …` заменён на `SELECT 1`); колонка `role` остаётся; выбор роли HR без `DISTINCT`: `dotnet test --solution Competency.slnx --filter-class "*IdentityRolesMigrationTests"` → exit 2, упал `Ac63_RolesAndHrAssignments_PassToIdentityRolesAndEmployees_WhenTheDatabaseMovesToTheNextMigration` (1 из 1) на каждой из четырёх; без мутаций — зелёный; runs: 4 |
| `tests/Competency.Tests/AuditTests.cs` (`Ac15_RoleChange_IsJournaledAsOneUpdateOfTheRoles_WithBothSetsInMemberOrder`, `Ac15_RoleChangeAndItsJournalRow_AreSavedTogetherOrNotAtAll`) | мутации: роли новой записи не упорядочены (`AccountRoles.SetRolesAsync`) → упал первый тест; запись журнала не ставится → упали оба; транзакция фиксируется до сохранения ролей (`UserEndpoints.UpdateAsync`) → упали оба: `dotnet test --solution Competency.slnx --filter-class "*.AuditTests"` → exit 2, 1/2/2 из 12; без мутаций — 12 из 12; runs: 3 |

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
