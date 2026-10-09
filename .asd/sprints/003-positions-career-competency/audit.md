---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

An absent optional section below means an empty finding set for that section, never an unperformed check (`.asd/rules/sprint-lifecycle.md` "Audit phase"). Omit any optional section entirely when it has no findings — never emit a placeholder row.

Аудит выполнен asd-architect на HEAD `603928f` (ветка `sprint/003-positions-career-competency`, workflow `lite`). Раздела «Related open stubs» нет: `.asd/project/stubs.md` открытых заглушек не содержит, маркеров TODO/FIXME в `src`, `web/src`, `tests`, `deploy` нет.

## Scope reference

[sprint.md](./sprint.md)

## Touched areas

**Справочник должностей (AC-1…AC-6), `org-structure`:**
- `src/Competency.OrgStructure/Employee.cs:38` — `Position` (строка, `[Audited]`) → ссылка `PositionId`; `SearchVector` (стр. 54) сейчас индексирует должность.
- `src/Competency.OrgStructure/EmployeeRequest.cs:17,35,48` — `position` (текст ≤200, trim) → `positionId`.
- `src/Competency.OrgStructure/EmployeeResponse.cs:24,40,62` — обе проекции (`FullProjection`, `ProjectionFor`) получают `positionId`, название — через join.
- `src/Competency.OrgStructure/EmployeeEndpoints.cs` — `CreateAsync` (133–156), `UpdateAsync` (174–205): проверка «активна и в перечне подразделения или его предков» под блокировкой дерева, повторно при переводе; `Matching` (343–349): поиск по должности через справочник.
- `src/Competency.OrgStructure/OrgStructureEntityConfiguration.cs:48,54` — колонка `position` и generated `search_vector`; новые таблицы справочника и перечня, FK `employees.position_id` без каскада, уникальность названия без учёта регистра.
- `src/Competency.OrgStructure/OrgTree.cs` — `AncestorIds` (включая сам узел) для проверки доступности; при разделяемой блокировке (R-6) — её метод рядом с `BeginExclusiveAsync`.
- Новые endpoints: `/api/v1/positions`, `/api/v1/org-units/{id}/positions` (собственный и унаследованный перечень), подсчёт затронутых сотрудников; `Rejections.cs`, `OrgStructureModule.cs`.
- `src/Competency.OrgStructure/IEmployeeDirectory.cs`, `EmployeeDirectory.cs` — расширение порта для карьерной модели (см. «Subsystems map»).
- `src/Competency.Platform/Migrations/`, `AppDbContextModelSnapshot.cs` — миграция справочника с переносом данных (AC-5); миграции карьерной модели с начальными данными (AC-12, AC-13).
- `src/Competency.Api/Program.cs:9` — `ContractVersion = "2.0.0"` → `"3.0.0"`; `MapCareerFrameworkEndpoints`. `ApplicationServices.cs` — `AddCareerFrameworkModule` после OrgStructure и UserManagement (межмодульные FK по имени сущности требуют порядка, `UserManagementEntityConfiguration.cs:21-23,59`). `Competency.Api.csproj` — новая ProjectReference.
- Перегенерация `openapi/openapi.json` (`info.version` 2.0.0; схемы с `position` ~3151–3223) и `web/src/api/schema.d.ts`.

**Роли процесса (AC-7, AC-8):**
- Источник «Руководителя подразделения»: `OrgUnit.cs:28` (`HeadEmployeeId`), `OrgUnitEndpoints.cs:265-270,388-397` (руководитель — работающий сотрудник этого подразделения), `EmployeeEndpoints.cs:333-341` (перевод снимает руководство прежним), `EmployeeImpact.cs:27,66` (увольнение/удаление снимает руководство всеми).
- `src/Competency.UserManagement/` — опубликованного порта чтения учётной записи нет, нужен новый (регистрация в `UserManagementModule.cs`).
- `SessionAuthentication.cs:67-85` — на каждом запросе обрывает сессию заблокированной записи и уволенного сотрудника; для текущего пользователя AC-7 выполняется без доработок.

**Карьерная модель (AC-9…AC-18):**
- Новый проект `src/Competency.CareerFramework/` (csproj, `packages.lock.json`, модуль, сущности, endpoints); добавить в `Competency.slnx`, `tests/Competency.Tests/Competency.Tests.csproj` и **`Dockerfile:27-32`** (`COPY` перечисляет csproj и lock-файлы перед `dotnet restore` — без строки сборка образа упадёт).
- `src/Competency.Audit/` — правок кода не ожидается: `[Audited]` журналирует интерцептор (составной ключ поддержан, `AuditSaveChangesInterceptor.cs:87-90`), явные события переходов — `IAuditWriter.Stage`.

**Frontend (AC-19):**
- `web/src/features/org-structure/`: `EmployeeForm.tsx:65` (Input → Select с поиском и «Добавить должность»), `EmployeeModal.tsx:186`, `orgStructureApi.ts`, `EmployeeCard.tsx:111`, `UnitCard.tsx`, `UnitFormModal.tsx` (вход на «экран подразделения»).
- `web/src/features/users/EmployeePicker.tsx:61` — подпись «подразделение, должность».
- Новые фичи по конвенции `features/<name>/routes.ts` (`web/src/app/features.ts:4-6`): справочник должностей; карьерная модель (справочники, матрицы, «Ждут моего согласования», черновые записи у HR).
- `web/src/app/featureContract.ts:21` — `NavItem.roles` знает только `UserRole`; для ролей процесса нужен серверный признак.
- Тестовые данные с текстовой `position`: `web/src/test/fakeApi.ts`, `EmployeeCards.test.tsx:18`, `editDialogs.test.tsx:85`, `OrgStructurePage.test.tsx:53`, `orgTree.test.ts:37`.

**Тесты backend:** `tests/Competency.Tests/Infrastructure/Scenarios.cs:39-42` (`CreateEmployeeAsync(position: "Инженер")`), `ContractTests.cs:34` (`"2.0.0"`), `EmployeeSearchTests.cs:17-36` (поиск по должности), текстовая `position` в телах запросов: `EmployeeDepartureTests.cs:105,125`, `OptimisticConcurrencyTests.cs:28,150,157`, `OrgTreeTests.cs:109`.

**Ретро (AC-20, AC-21):** `.asd/project/custom-common-rules.md`, `.asd/project/custom-coding-rules.md` — проектные файлы, не инфраструктура workflow; `code-style.md` §7, §8 существуют.

**Развёртывание:** `deploy/README.md` §4 «Поведение при старте» — переход на контракт 3.0.0 и миграция должностей по образцу абзаца о 2.0.0 (стр. 93).

## Existing docs found

- [Реестр подсистем](../../../docs/architecture/subsystems.md) — `org-structure`, `user-management`, `audit`; org-structure «справочник сотрудников (подразделение, должность, статус…)»; mermaid-диаграмма (`IEmployeeDirectory`, `IEmployeeAccounts`, `[Audited]`); подсистемы карьерной модели нет.
- [org-structure.md](../../../docs/architecture/org-structure.md) — «должность текстом»; модуль зависит только от Platform; блокировка дерева `pg_advisory_xact_lock(5001001)` — одна на все изменения подразделений, сотрудников и зависящих от сотрудника учётных записей, берётся первой; руководитель — только работающий сотрудник этого подразделения (400; прежние несоответствия молча не исправляются); недоступная запись — 404, мутация — 403; `search_vector` — generated `tsvector`; `EmployeeAccount` — единственное чтение чужих данных.
- [user-management.md](../../../docs/architecture/user-management.md) — наружу только `Add…`, `Map…`, `EnsureBootstrapAdminAsync`, порта чтения учётных записей нет; «активный администратор — не заблокирован и зарегистрирован»; порядок блокировок дерево → строки администраторов → строка записи; переход на контракт 2.0.0 описан абзацем в Key paths.
- [audit.md](../../../docs/architecture/audit.md) — allow-list `[Audited]`; события `Created|Updated|Deleted` в той же транзакции; `ExecuteUpdate`/`ExecuteDelete` над аудируемыми сущностями не применяются; `Stage`, `WriteAsync`.
- [stack.html](../../../docs/architecture/stack.html) «Архитектурные принципы» — Audit, OrgStructure, UserManagement ссылаются только на Platform, кроме UserManagement → OrgStructure; наружу только `Add…Module`, `Map…Endpoints` и опубликованные порты; If-Match 428/400/412; уникальность и FK — 409; **«физически удаляется только сотрудник»**; инварианты — блокировками в одном порядке; встроенные средства вместо сторонних библиотек.
- Tech-reference: [sql-postgresql-18.6.md](../../../docs/architecture/tech-reference/sql-postgresql-18.6.md) (FTS `russian`, generated-колонки), [npgsql-entityframeworkcore-postgresql-10.0.3.md](../../../docs/architecture/tech-reference/npgsql-entityframeworkcore-postgresql-10.0.3.md) (`HasGeneratedTsVectorColumn`, `jsonb` через `ToJson`), [efcore-10.0.12.md](../../../docs/architecture/tech-reference/efcore-10.0.12.md), [antd-6.6.5.md](../../../docs/architecture/tech-reference/antd-6.6.5.md) (`Table`, `Form`, `TreeSelect`; у Select `dropdownXxx` → `popupXxx`). Advisory-блокировки в tech-reference не описаны.
- [Требования org-structure](../../../docs/product/requirements/org-structure.html) — G2 «должностью (текстом)»; AC-8 «ранее сохранённые данные молча не исправляются»; AC-10 «должность (текст, обязательная, не длиннее 200)»; AC-14 снятие с руководства всех подразделений; AC-20 поиск «по ФИО и должности, по подстроке… и полнотекстово»; AC-22 изменения только у администратора; AC-23 конкурентность; AC-24 P95 < 2 с на 10 000 сотрудников; «Вне рамок»: справочник должностей и Career Framework, роли с областью действия.
- [Требования user-management](../../../docs/product/requirements/user-management.html) — AC-12 две роли; AC-14 защита от IDOR; AC-31 навигация («Пользователи» и «Журнал» — администратор, «Оргструктура» — все); «Вне рамок»: роли сверх двух, scope-based access, row-level и field-level безопасность.
- [Требования audit](../../../docs/product/requirements/audit.html) — AC-3 журналируются «пользователи, подразделения и сотрудники»; AC-4 запись в одной транзакции; AC-11 журнал читает только администратор.
- [Концепция](../../../docs/product/concept.html) — «Открытость критериев: карьерные треки, должности, требования, компетенции, шкала и методика доступны сотруднику заранее»; HR и методологи управляют методологией. Согласуется с AC-18.
- [UX org-structure](../../../docs/ux/org-structure.html) — одна страница с иерархией карточек, без отдельной страницы сотрудников, правой панели и компонента-дерева; должность в форме — обязательный текст; поиск по ФИО и должности; при 412 — сообщение и актуальные данные.
- [UX user-management](../../../docs/ux/user-management.html) — выбор сотрудника показывает подразделение и должность.
- [UX app-shell](../../../docs/ux/app-shell.html) — горизонтальное меню в шапке; состав меню по двум ролям.
- [UX audit](../../../docs/ux/audit.html) — фильтр «Сущность» свободным текстом, новые типы правок не требуют.
- [DESIGN.md](../../../docs/ux/DESIGN.md) — раздел «Competency matrix» (строки — компетенции, липкий первый столбец, токены `matrix-*`, `rating-chip-*`); теги `tag-neutral`, `tag-info`, `tag-success`; компонент `Steps`; без боковой панели; «a change worth noticing is marked with an icon or text». Токена «изменённое значение» для правок согласующего нет.
- [accessibility.html](../../../docs/ux/accessibility.html) — общие правила, требований к редактору матрицы сверх DESIGN.md нет.
- [Постановка](../../../docs/Постановка.md) — неканонична (нет в path map). Относятся к scope: §3.1, §4, §5.1–5.3, §6.1–6.4, §7.1–7.4, §8.6–8.7, §9, §21, §22, §29, §30, §39–§41 (`Position`, `CareerTrack`, `/api/v1/positions` в смысле карьерного уровня), §46.
- [deploy/README.md](../../../deploy/README.md) §4 — абзацы о миграциях данных и переходе на контракт 2.0.0 — образец для 3.0.0.
- [custom-coding-rules.md](../../../.asd/project/custom-coding-rules.md) — «Multi-row invariant locking» (блокировка, место в глобальном порядке, гоночный тест), «Shared helper types» (общие типы в Platform); правил AC-20, AC-21 нет. [custom-common-rules.md](../../../.asd/project/custom-common-rules.md) — только «Docker safety».
- Архивный audit спринта 001, план миграции п. 5: «`/positions` за Career Framework» — путь тогда предназначался для карьерных уровней.

## Contradictions (optional)

- **C-1** — `sprint.md` AC-2 vs AC-3: деактивация должности «не меняет должность у сотрудников, которые её уже занимают», но при изменении сотрудника «принимается только активная должность» (исключение сделано только для перечня). Сотрудника с деактивированной должностью нельзя поправить даже в ФИО, не сменив должность. Winner: решение пользователя — вариант (a), AC-3 уточнён.
- **C-2** — `sprint.md` AC-9 vs AC-16/AC-18: черновая запись видна только автору, ответственному HR и администратору, а согласующий шага 1 видит и правит версию, состоящую из таких записей. Winner: решение пользователя — черновые записи версии видны для чтения всем, кто видит версию; AC-9 уточнён.
- **C-3** — внутри AC-9: «HR видит все записи» vs «черновая запись видна… ответственному HR». Winner: решение пользователя — HR видит действующие и неактивные записи всех подразделений, черновые — только своих; AC-9 уточнён.
- Постановка §7.4 (настраиваемый маршрут) vs AC-16 (фиксированный) — обе стороны неканоничны, уже решено пользователем: winner `sprint.md` (decisions-log.001 «scope answers: roles and matrix approval»; Out of scope).
- Постановка §8–§9 (десять ролей, RBAC, scope, RLS) vs AC-7/AC-8/AC-18 — winner `sprint.md` (то же решение; роли процесса не роли учётной записи).
- Постановка §5.3, §39–§41 («Должность» = карьерный уровень) vs `sprint.md` (справочник должностей отделён) — winner `sprint.md` (decisions-log.001 «scope answers: positions catalog, career framework, roles»); последствие для именования — R-17.

Устаревшие формулировки канонических документов, прямо отменённые принятым scope (decisions-log.001 «scope accepted»), — не противоречия, а документы к обновлению на design-promote («Gaps»): org-structure.html (G2, AC-10, «Вне рамок»), user-management.html («Вне рамок», AC-31), audit.html (AC-3).

## Existing implementation found

- `Employee.Position` — обязательный текст ≤200 с `[Audited]`; `EmployeeRequest.Validate` и `Trimmed`; изменения сотрудника — под блокировкой дерева; справочника нет.
- `employees.search_vector` — generated `tsvector('russian')` по last/first/middle/position; `Matching` — `ILIKE` по `full_name` либо FTS. Должность ищется только полнотекстово (подстрокой — нет, хотя org-structure.html AC-20 говорит о подстроке); AC-5 «как прежде» = сохранить текущее поведение.
- Прецедент миграции `20261007064349_EmployeeNameInThreeFields.cs:73-116` — пересоздаёт `search_vector` и индексы, преобразует данные без событий журнала.
- Блокировка дерева — `OrgTree.BeginExclusiveAsync` (ключ 5001001, только эксклюзивно), открыта другим модулям через `IEmployeeDirectory.BeginExclusiveAsync`.
- `AncestorIds`/`DescendantIds` — рекурсивный CTE (неупорядоченный); `PathAsync` упорядочивает путь в памяти.
- Правило руководителя проверяется при смене руководителя, активации подразделения, переводе и увольнении/удалении; в БД — только FK, без уникального индекса по `head_employee_id` и без проверки «из этого подразделения».
- Чтение чужих данных — `EmployeeAccount`, keyless-запрос по `users` (`employee_id`, `email`, `is_blocked`).
- Сессия: `OnValidatePrincipal` проверяет блокировку, security stamp, работает ли сотрудник; `CurrentUserResponse` отдаёт SPA `employeeId`; `ICurrentActor` — `UserId`, `Role`, `RequestId`.
- Platform: `EntityBase` (Guid v4, `Version`, `CreatedAt`); `EntityStampingInterceptor` повышает `Version` только изменённому `IVersioned`; `IfMatch.ApplyTo`; `ProblemExceptionHandler` — 412 на конфликт версий, 409 на `23505`/`23503`; общие `ListRequest`, `PageResponse<T>`, `Rejection`.
- Межмодульный FK по имени сущности (`"Competency.OrgStructure.Employee"`) — образец для связей карьерной модели с `org_units` и `users`.
- Web: конвенция фич, `AdminOnly`, статичный `NavItem.roles`, `useEditBase` и `ifMatch.ts` для 412. Токены матрицы в DESIGN.md есть, компонента матрицы в коде нет.
- Карьерной модели в коде нет (`career|competenc|matrix` по `src` — пусто).

## Gaps

- **Справочник должностей:** нет сущностей справочника и перечня, endpoints, проверки AC-3, подсчёта AC-2, транзакции AC-4 («создать или взять существующую + добавить в перечень»); нужен явный ответ с полем при дубле названия вместо общего 409; индекс по `employees.position_id`.
- **Экрана подразделения нет:** UX org-structure — одна страница без панели деталей, а AC-2 и AC-8 требуют места для перечня (собственного и унаследованного с источником) и HR. UX-решение — в plan, по правилу AC-20 с текстами на утверждение.
- **Поиск по должности:** generated `search_vector` не может ссылаться на другую таблицу — свой `tsvector` (или trigram) у справочника и подзапрос `position_id IN (…)`.
- **Ответ API:** рекомендуется оставить `position` (название) и добавить `positionId` — ломается только вход, что AC-6 допускает.
- **Роли процесса:** нет порта чтения учётной записи в user-management; нет порта подразделения, предков и руководителей (`IEmployeeDirectory` умеет только сотрудника и блокировку); нет хранения закреплений HR и вычисления ответственного HR; нет endpoint «мои роли процесса» для меню и действий SPA.
- **Карьерная модель:** модуль целиком — записи со статусом и подразделением-владельцем (AC-9), версии компетенций (AC-12), шкалы, матрицы, версии, варианты, решения, проверка полноты, маршрут. Нужен **снимок версии на момент публикации** (AC-17): уровни и треки не версионируются, их переименование иначе изменит опубликованную версию задним числом. Диапазон оклада (AC-11) скрыт во всех проекциях.
- **If-Match версии матрицы:** `EntityStampingInterceptor` не повышает `Version` версии при правке дочерних требований — либо явно «касаться» корня, либо хранить содержимое версии одним `jsonb`-документом (варианты — копии, правки — разница документов).
- **Проверка полноты (AC-15) после правок согласующего:** «принять правки без доработки» на шаге HR опубликовало бы непроверенный вариант — повторять проверку при возврате с правками и при принятии; заодно перепроверять статус записей и неизменность шкалы.
- **Начальные данные:** 12 категорий §6.1 и базовая шкала §6.3 — миграцией с фиксированными Guid, без событий журнала.
- **Frontend:** все экраны AC-19 — Select с поиском и `popupRender`, редактор матрицы на токенах `matrix-*`, показ правок согласующего существующими средствами (`warning-bg` + иконка/текст); новый токен DESIGN.md — жёсткий gate.
- **Документы к обновлению на design-promote:** `docs/architecture/org-structure.md`, `user-management.md`, `audit.md`, `subsystems.md` (+ новый `career-framework.md`), `stack.html` (ссылки модулей, порты, порядок блокировок, принцип физического удаления); требования `org-structure.html`, `user-management.html`, `audit.html` (+ новый `career-framework.html`); UX `org-structure.html`, `user-management.html`, `app-shell.html` (+ новый `career-framework.html`); `deploy/README.md` §4.
- **External dependency gaps:** нет — antd 6.6.5, EF Core/Npgsql (`jsonb`, частичные индексы, generated `tsvector`), PostgreSQL 18.6 (advisory-блокировки, рекурсивные CTE) покрывают scope; при использовании `pg_advisory_xact_lock_shared` или частичных уникальных индексов — дописать в `sql-postgresql-18.6.md`.
- **Migration gaps:**
  - `employees.position` (текст) → `positions`, перечни подразделений, `employees.position_id` (FK RESTRICT, NOT NULL): слияние после btrim без учёта регистра; каждая пара «подразделение сотрудника — должность» в перечень, включая уволенных и неактивные подразделения; порядок: таблицы → заполнение → `position_id` → NOT NULL → снять индексы и `search_vector` → удалить `position` → вернуть `search_vector` без должности; Down восстанавливает текст.
  - Контракт `/api/v1/employees` 2.0.0 → 3.0.0: вход `position` → `positionId`, ответ + `positionId`; `ContractTests.cs:34`, `schema.d.ts`.
  - Журнал: прежние `Employee.Updated` хранят `position` текстом, новые — `positionId` (как `orgUnitId`).
  - Начальные данные AC-12, AC-13.

## Risks

- **R-1** — порядок шагов миграции AC-5. Impact: EF удалит `position` до переноса — потеря данных. Mitigation: ручной порядок SQL; тест миграции на данных (регистр, пробелы, уволенные, неактивные подразделения); Down.
- **R-2** — нормализация названий в миграции и приложении (`lower(btrim())` vs `ToUpperInvariant`; `lower()` для кириллицы зависит от ctype кластера). Mitigation: одна нормализация — уникальный индекс по `lower(name)` (имена хранятся обрезанными), сравнение в SQL через `lower()`; тест с «Ё/ё».
- **R-3** — регрессия поиска по должности. Mitigation: подзапрос по `positions.search_vector`; `EmployeeSearchTests` byPosition остаётся зелёным; при объявленном perf-риске Task первой создаёт seed на 10 000 сотрудников.
- **R-4** — блокировка дерева и перечни: без неё сотрудник сохранится с одновременно исключённой должностью. Mitigation: изменения справочника, перечней и закреплений HR — под эксклюзивной блокировкой 5001001 первой; чтение для выбора — без блокировки; между созданием должности (AC-4) и сохранением сотрудника возможен 400 с ошибкой поля.
- **R-5** — смена руководителя или HR во время согласования: версия может зависнуть в ON_REVIEW и блокировать новую версию (действия «отозвать» нет). Mitigation: согласующего вычислять в момент действия, пропуск шага перепроверять в момент действия, в истории — фактический исполнитель; зависание — решение пользователя (A-6b).
- **R-6** — TOCTOU проверки ролей процесса. Mitigation: переходы версии берут блокировку дерева в разделяемом режиме (`pg_advisory_xact_lock_shared(5001001)`) первой, затем строку матрицы `FOR UPDATE`; блокировка учётных записей, смена руководителя, закрепление HR — эксклюзивно; правки DRAFT защищает If-Match. Объявить в plan, гоночные тесты.
- **R-7** — инварианты матриц («одна опубликованная и одна в работе», «одна матрица на трек у подразделения», «один HR на подразделение»): публикация одним `SaveChanges` может нарушить неоткладываемый частичный уникальный индекс. Mitigation: частичные уникальные индексы как страховка; архивировать отдельным `SaveChanges` в той же транзакции под `FOR UPDATE` матрицы; гоночные тесты.
- **R-8** — утечка диапазона оклада (снимки, варианты, разница правок, история, общая OpenAPI-схема). Mitigation: проекции по роли (как `EmployeeResponse.ProjectionFor`), снимки без оклада или фильтрация при чтении, тест на каждый путь чтения; журнал (только администратор) — допустим.
- **R-9** — правило «руководитель из этого подразделения» действует с 2026-10-07 и в БД не закреплено, старые несоответствия не исправлялись. Mitigation: «подразделения под руководством сотрудника» — список (как `EmployeeImpact`).
- **R-10** — в lite новая подсистема утверждается на design-promote, после impl. Mitigation: получить решение о «Subsystem proposal» на plan, записать в decisions-log; design-promote подтверждает.
- **R-11** — объём спринта (деление отклонено). Mitigation: волны по зависимостям — должности → роли и HR → справочники → матрицы и маршрут → экраны; каждая тестируется отдельно.
- **R-12** — меню по ролям процесса (`NavItem.roles` знает только `UserRole`). Mitigation: endpoint ролей процесса текущего пользователя; права проверяет сервер.
- **R-13** — многословный UI (редактор, решения, тексты ошибок AC-16). Mitigation: правило AC-20 применить уже в plan этого спринта.
- **R-14** — канонический принцип `stack.html` «физически удаляется только сотрудник» vs удаление строк перечня (AC-2) и снятие закрепления HR (AC-8). Mitigation: мягкое исключение строк либо поправка принципа на design-promote с явным решением в plan.
- **R-15** — уникальность названий при невидимых черновиках: отказ раскрывает чужой черновик. Mitigation: область уникальности (A-13).
- **R-16** — объём снимков и вариантов `jsonb` — незначителен; не журналировать документ целиком, для переходов — явные события.
- **R-17** — пересечение имён: в Постановке `Position` и `/api/v1/positions` — карьерный уровень. Mitigation: `/api/v1/positions` — справочник, `/api/v1/career-levels` — карьерные уровни; глоссарий в `custom-common-rules.md` или `<id>.md`.
- **R-18** — меню растёт на планшете (antd уводит пункты в «…»). Mitigation: учесть в UX, обновить состав меню в app-shell.html.

## Subsystems map (optional, decomposition enabled)

- **org-structure** — справочник должностей и перечни подразделений (AC-1…AC-6), ссылка сотрудника на должность, поиск; источник роли «Руководитель подразделения» (AC-7); расширение порта `IEmployeeDirectory` для карьерной модели: факты о подразделении (существует, активно, `parentId`, `headEmployeeId`), упорядоченная цепочка предков с руководителями, подразделения под руководством сотрудника (список, R-9), `BeginSharedAsync` (R-6).
- **user-management** — новый опубликованный порт `IUserDirectory` (`FindAsync(userId)`, `FindByEmployeeIdsAsync`: e-mail, привязка к сотруднику, блокировка, приглашена ли) для AC-7, AC-8, AC-16; роли учётной записи не меняются; AC-31 и «Вне рамок» требований — к обновлению.
- **audit** — новые типы сущностей и события: справочник и перечни (AC-6), закрепления HR (AC-8), сущности и переходы версий (AC-17); код не меняется, обновляются требования AC-3 и `audit.md`.
- **career-framework** (предлагается; добавление подсистемы — жёсткий gate, решение рекомендуется получить на plan, R-10):
  - Название «Карьерная модель»; роль — профессиональные направления, карьерные треки и уровни, каталог компетенций с версиями, шкалы, версионируемые матрицы компетенций подразделений с маршрутом согласования; закрепление HR за подразделениями и вычисление ролей процесса (руководитель подразделения, ответственный HR).
  - Модуль `Competency.CareerFramework`; ключевые пути `src/Competency.CareerFramework/`, миграции в общей линии `src/Competency.Platform/Migrations/`, `web/src/features/career/`, `docs/architecture/career-framework.md`, `docs/product/requirements/career-framework.html`, `docs/ux/career-framework.html`.
  - Ссылки: Platform, OrgStructure, UserManagement (обратных нет, циклов нет); правило ссылок `stack.html` дополняется «CareerFramework → OrgStructure, UserManagement». БД — межмодульные FK на `org_units` и `users` по имени сущности, контрибьютор регистрируется последним.
  - **Закрепление HR (AC-8) — в `career-framework`** (рекомендация): роль HR существует только для процесса карьерной модели и ссылается на учётную запись; org-structure не получает зависимость от `users` и не меняет контракт `/org-units`; экран подразделения собирает данные из двух API; изменения закрепления — под эксклюзивной блокировкой дерева через `IEmployeeDirectory.BeginExclusiveAsync`. Альтернатива `org_units.hr_user_id` — If-Match, блокировка и журнал подразделения бесплатно, но FK OrgStructure → `AppUser` невозможен по порядку контрибьюторов, нужен обратный порт, ломается контракт `/org-units`.
  - Диаграмма: `Container(career-framework, …)` и связи → org-structure (`IEmployeeDirectory`), → user-management (`IUserDirectory`), → audit, → postgres.
- **Справочник должностей (AC-1…AC-6)** — в `org-structure`: таблицы `positions` и перечень подразделения, endpoints `/api/v1/positions`, `/api/v1/org-units/{id}/positions`, всё под блокировкой дерева.

## Documentation migration plan

| # | Source (path/URL) | Format | Proposed target in `docs/` | Type | Notes |
|---|---|---|---|---|---|
| 1 | `docs/Постановка.md` §5.1–5.3, §6.1–6.4, §7.1–7.3 | md | `docs/product/requirements/career-framework.html` | migrated | только принятое подмножество; «должность / карьерный уровень» → «карьерный уровень», без связи со справочником должностей |
| 2 | `docs/Постановка.md` §7.4 | md | `docs/product/requirements/career-framework.html` | migrated | только фиксированный маршрут AC-16; настраиваемый — «Вне рамок» |
| 3 | `docs/Постановка.md` §21, §22, §6.2 | md | `docs/product/requirements/career-framework.html` | migrated | только хранение формулы, порогов, блокирующих правил; расчёт — «Вне рамок» |
| 4 | `docs/Постановка.md` §3.1, §30, §46 | md | `docs/product/requirements/career-framework.html` | migrated | открытость действующих записей и опубликованных версий; If-Match/412 |
| 5 | `docs/Постановка.md` §6.1 (категории), §6.3 (тексты шкалы) | md | данные миграции и перечень в `career-framework.html` | migrated | тексты дословно |
| 6 | `docs/Постановка.md` §39–§41 (`Position`, `/positions`) | md | глоссарий в `docs/architecture/career-framework.md` и `org-structure.md` | migrated | `positions` — справочник должностей; `career-levels` — карьерные уровни |

## Criterion check

| AC | Выполним | Согласован | Примечание |
|---|---|---|---|
| AC-1 | да | да | уникальный индекс по выражению; явный 409 с полем |
| AC-2 | да | нет — C-1 | «экрана подразделения» нет; удаление строк перечня — R-14; область подсчёта — A-3 |
| AC-3 | да | нет — C-1 | перевод проверяется под блокировкой дерева |
| AC-4 | да | частично | неактивная должность с тем же названием — A-2 |
| AC-5 | да | да (без учёта регистра) | поиск через справочник (R-3); написание — A-1 |
| AC-6 | да | да | `info.version` = 2.0.0 (`Program.cs:9`, `openapi.json:4`) → 3.0.0 — корректный мажорный; ответ совместим |
| AC-7 | да | да | правило руководителя в БД не закреплено (R-9); «действующая учётная запись» — A-4 |
| AC-8 | да | да | место хранения — Subsystems map; заблокированный HR — A-5; видимость HR — A-17 |
| AC-9 | да | нет — C-2, C-3 | действующие записи чужих подразделений — A-12 |
| AC-10 | да | да | область уникальности — A-13; новая версия при неактивном треке — A-18 |
| AC-11 | да | да | оклад общих уровней — только администратор (из AC-9); утечка — R-8 |
| AC-12 | да | частично | шкала компетенции vs шкала версии — A-10; направление компетенции — A-11 |
| AC-13 | да | да | тексты — Постановка §6.3 |
| AC-14 | да | частично | «блоки компетенций» — A-9 |
| AC-15 | да | да («в работе» = DRAFT, ON_REVIEW, «возвращена с правками»; APPROVED — только в истории) | полнота после правок согласующего — Gaps; «править» — A-16 |
| AC-16 | да | да | автор ≠ HR выполнимо; автор и согласующий шага 1 руководят разными подразделениями; HR = согласующий шага 1 — A-7; возврат с одним комментарием — A-8; маршрут — A-6 |
| AC-17 | да | да | агрегатный If-Match и снимок публикации — Gaps |
| AC-18 | да | частично | 403 vs принятый 404 — A-15; архивные версии и история — A-14 |
| AC-19 | да | да | экран подразделения, меню по ролям процесса, без новых токенов DESIGN.md |
| AC-20 | да | да | применить к plan этого спринта (R-13) |
| AC-21 | да | да | `code-style.md` §7, §8 существуют |

## Ambiguities

Source-resolvable (ответ из источника, решения пользователя не требуют):
- **A-3** — «занимающие должность» при исключении из перечня (AC-2) = сотрудники поддерева подразделения с этой должностью (AC-3: доступность = перечень своего подразделения и всех вышестоящих).
- **A-6a** — согласующий вычисляется в момент действия (AC-7: смена руководителя сразу меняет права).
- **A-12** — действующие записи чужого подразделения можно использовать в своих матрицах (AC-9 ограничивает только черновики; Постановка §5.3).
- **A-16** — в статусе «возвращена с правками» автор выполняет решения AC-16, а не правит содержимое (AC-15: правка только в DRAFT).
- **A-18** — новую версию существующей матрицы при неактивном треке создать можно (AC-10: недоступен только «для новых матриц»).
- **A-19** — опубликованная версия хранит снимок на момент публикации (AC-17, Постановка §7.3, §30).

Authority/preference-only — решения пользователя: см. «Решения по противоречиям и неоднозначностям» ниже (A-1, A-2, A-4, A-5, A-6b, A-7, A-8, A-9, A-10, A-11, A-13, A-14, A-15, A-17).

## Решения по противоречиям и неоднозначностям

Решение пользователя 2026-10-09 на audit: приняты все рекомендации (decisions-log «audit contradictions and ambiguities»).

- **C-1** — если должность и подразделение сотрудника не менялись, не проверяются ни активность должности, ни перечень (AC-3).
- **C-2** — черновые записи, используемые версией матрицы, видны для чтения всем, кто видит эту версию (AC-9).
- **C-3** — любой HR видит действующие и неактивные записи всех подразделений, черновые — только подразделений, за которые отвечает (AC-9).
- **A-1** — при слиянии сохраняется самое частое написание; при равенстве — у самого раннего сотрудника.
- **A-2** — «Добавить должность» с названием неактивной должности возвращает её в работу в той же транзакции с событием журнала.
- **A-4** — «действующая учётная запись» = не заблокирована и зарегистрирована.
- **A-5** — заблокированный закреплённый HR: ответственного HR нет, отправка на согласование отклоняется с понятной ошибкой, администратор перезакрепляет.
- **A-6b** — пропуск шага перепроверяется в момент действия; новых действий (отзыв) в scope не добавляется.
- **A-7** — если ответственный HR одновременно согласующий шага 1, он согласует оба шага по отдельности (если HR — автор, шаг HR засчитывается сразу — поправка AC-16).
- **A-8** — «Принять правки без доработки» доступно только при возврате с правками, не при возврате с одним комментарием.
- **A-9** — блоки компетенций — именованные блоки внутри версии; каждая компетенция версии ровно в одном блоке; коэффициенты блоков уровня в сумме 100 %; у блока признак «обязательный» для порогов.
- **A-10** — версия принимает только компетенции своей шкалы; требуемый уровень — балл этой шкалы.
- **A-11** — в матрицу включаются компетенции направления матрицы или «общие».
- **A-13** — уникальность названий — среди нечерновых записей и черновиков своего подразделения; повторная проверка при утверждении черновика.
- **A-14** — архивные версии видят все аутентифицированные; историю (варианты, комментарии) — участники маршрута, ответственный HR и администратор.
- **A-15** — 404 на чтение недоступного, 403 на изменение видимого (принятый в проекте шаблон).
- **A-17** — закреплённого HR видят все аутентифицированные: ФИО сотрудника или e-mail, если учётная запись не привязана.
