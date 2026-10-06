---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

## Overview

План покрывает AC-1…AC-13 и AC-15 из [sprint.md](./sprint.md) (workflow `lite`, acceptance-criteria source — `sprint.md`). AC-14 (регистрация подсистем `org-structure`, `user-management`, `audit` в `docs/architecture/subsystems.md` и их `<id>.md`, требования и UX в `docs/`) выполняет фаза design-promote после impl-review; отдельной Task у неё нет. Входной материал для dev: [audit.md](./audit.md) (риски и spike-пункты), `docs/architecture/stack.html` и `docs/architecture/tech-reference/` (точные версии, конвенции технологий), `docs/ux/DESIGN.md`, `docs/Постановка.md` (§4, §29, §39–§46).

Раскладка репозитория:
- backend — `src/Competency.Api` (хост: `Program.cs`, сборка модулей, SPA-статика), `src/Competency.Platform` (общее ядро), модули `src/Competency.Audit`, `src/Competency.OrgStructure`, `src/Competency.UserManagement`; корень — `Competency.slnx`, `global.json`, `Directory.Build.props`, `.config/dotnet-tools.json`;
- тесты backend — `tests/Competency.Tests` (один xunit.v3-проект; тесты пишет impl-test);
- frontend — `web/` (собственные `package.json` и `package-lock.json`, не смешиваются с корневым `package.json` инструмента design.md);
- упаковка — `Dockerfile`, `.dockerignore`, `deploy/k8s/`.

Границы модулей: `Platform` не ссылается на модули; `Audit`, `OrgStructure`, `UserManagement` ссылаются только на `Platform`, кроме `UserManagement → OrgStructure` (чтение сотрудника и его статуса через интерфейс, который `OrgStructure` публикует из `internal`-реализации); `Api` ссылается на все. Типы модулей `internal`, наружу выходят только `Add<Module>Module(IServiceCollection)`, `Map<Module>Endpoints(IEndpointRouteBuilder)` и опубликованные интерфейсы. Один `AppDbContext` и одна история миграций; конфигурации сущностей поставляют модули.

Принятые решения (на plan gate; без отдельного design в `lite` они фиксируются здесь и в `decisions-log.md`, фолдятся на design-promote по fold rule):
- Хранение дерева: adjacency list (`parent_id`), поддерево, путь и сводка — `WITH RECURSIVE` через `FromSql`; все переносы и деактивации — в одной транзакции под `pg_advisory_xact_lock` на дерево (запрет циклов при конкурентных переносах).
- Схема БД: `MigrateAsync()` при старте приложения (решение пользователя), под guard'ом build-time OpenAPI; учётка БД приложения имеет DDL-права.
- Порядок сборки: типы клиента `web/src/api/schema.d.ts` генерируются из `openapi.json` и коммитятся; проверка актуальности — `openapi-typescript --check`; Node-стадия образа собирает SPA без backend.
- Identity: `AddIdentityCore` + cookie-аутентификация, собственные `/api/v1/auth/*`; `MapIdentityApi` не используется. Роль — колонка `Role` (`GlobalAdmin`/`User`), а не Identity roles. Логин — `UserName` (уникальный, без учёта регистра).
- Идентификаторы — `Guid.NewGuid()` (v4). Конкурентность: `int Version` на агрегатах, ETag = версия, `If-Match` обязателен на изменяющих запросах (отсутствует → 428, не совпал → 412); для пользователя `Version` ставится в `OriginalValue` перед сохранением.
- CSRF: cookie `SameSite=Strict`, `HttpOnly`, `Secure` по конфигурации (TLS — открытый Q8); плюс проверка `Origin`/`Sec-Fetch-Site` на изменяющих методах.
- Вход: Identity lockout (5 попыток / 15 минут, значения в конфигурации), rate limiter на `/api/v1/auth/login`, единое сообщение об ошибке; `SecurityStampValidatorOptions.ValidationInterval = 0` и проверка блокировки пользователя и статуса сотрудника на каждом запросе — блокировка обрывает действующую сессию.
- Правило последнего администратора: блокировка набора активных администраторов (`FOR UPDATE`) в той же транзакции, что и изменение.
- Журнал: таблица `audit_events` модуля `Audit`; `SaveChangesInterceptor` пишет изменения сущностей с атрибутом `[Audited]` и allow-list свойств (хэши, stamp, счётчик попыток входа не пишутся); события входа/выхода/lockout пишет явно `IAuditWriter` (интерфейс в `Platform`). Неизменяемость — DB-триггер, запрещающий UPDATE/DELETE; поскольку учётка приложения владеет схемой и имеет DDL-права, триггер защищает от ошибок кода и SQL-инъекции в DML, но не от DDL — принятый остаточный риск, фиксируется в `<id>.md` на design-promote.
- Поиск: FTS `russian` по названию подразделения и ФИО/должности; `pg_trgm` (GIN) по ФИО, e-mail, табельному номеру для подстроки и префикса. Расширения создаются миграцией; spike на неизменённом `postgres:18.6-trixie` — в Task 4.
- DataProtection-ключи: каталог на PVC `app` (путь из конфигурации), `strategy: Recreate`. Манифесты — plain YAML в `deploy/k8s/` (без Kustomize/Helm), образы по digest-плейсхолдерам, Ingress/TLS не задаются.
- Физического удаления нет: блокировка пользователя, деактивация подразделения и сотрудника; нет `ExecuteUpdate/ExecuteDelete` над аудируемыми сущностями.
- Версии SDK/Node: на машине .NET SDK 10.0.400, Node 24.15.0, npm 11.16.0; `global.json` фиксирует 10.0.400 с `rollForward: latestFeature`, образ сборки остаётся `10.0.401`; `engines` в `web/package.json` — `node >=24.15.0 <25`. Расхождение с точными версиями `stack.html` для локальной машины принимается, образы строятся строго по стеку.

Два шага вне Task'ов (оркестраторский и ручной) описаны ниже.

Orchestrator-only: после волны 2 (Task 1–3 закоммичены) main orchestrator заполняет `.asd/project/commands.yaml` (`build`, `lint`, `test`, `run`, `custom.*`) командами, созданными Task 1–3, и коммитит; без этого completion gate `impl` нечем проверять.

Manual (MS-N): установка Docker Desktop (WSL2) на dev-машину отложена до последнего возможного момента — Task 1–9 выполняются без Docker и без PostgreSQL (миграции генерируются design-time без БД, сборка не требует БД). Dev регистрирует MS-N один раз, только когда Task 10 доходит до `docker build` (последняя волна); без Docker невозможны также Testcontainers в impl-test, проверка Dockerfile и spike-пункты, зависящие от реального образа PostgreSQL (`pg_trgm`, ICU-коллации, схема Identity на `InitialCreate`, linux-записи в `web/package-lock.json`) — они переносятся в Task 10 и impl-test.

## Definition of Done
Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.
Дополнительно для спринта: сборка backend (`dotnet build`) и frontend (`npm run build`, `biome ci --error-on-warnings`, `tsc --noEmit`) проходит без ошибок и предупреждений при отсутствии PostgreSQL; `openapi-typescript --check` подтверждает актуальность `web/src/api/schema.d.ts`; spike-результаты (ERESOLVE в Task 3; `pg_trgm` в образе, схема Identity на `InitialCreate` в Task 10; Testcontainers ↔ PG 18 в impl-test) записаны в соответствующий `tech-reference/`; лицензии новых транзитивных зависимостей сверены со списком допустимых в `stack.html`.

### Task 1: Каркас backend, хост и подключение БД
Material risk: change: build-time OpenAPI запускает entry point (startup-код с БД ломает сборку), EF/Npgsql/Identity на новых версиях
- [x] AC-1,2: создать `Competency.slnx`, `global.json` (SDK 10.0.400, `rollForward: latestFeature`, `test.runner = Microsoft.Testing.Platform`), `Directory.Build.props` (`Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `WarningsNotAsErrors` для NU1901–NU1904, `RestorePackagesWithLockFile`, `RestoreLockedMode` в CI), `.config/dotnet-tools.json` (`dotnet-ef` 10.0.12), проекты `Competency.Api`, `Competency.Platform`, `Competency.Audit`, `Competency.OrgStructure`, `Competency.UserManagement` и тестовый `tests/Competency.Tests` (xunit.v3 4.0.1, NSubstitute 6.2.0, AwesomeAssertions 9.6.0, Testcontainers.PostgreSql 4.15.0; без тестов — их пишет impl-test) со ссылками по правилам границ из Overview; пакеты точными версиями из `stack.html`; `packages.lock.json`
- [x] AC-1: обновить `.gitignore` (`bin/`, `obj/`, `dist/`, `TestResults/`, `.env`, `appsettings.*.local.json`) и добавить `.gitattributes` (`eol=lf` для `Dockerfile`, `*.sh`, `*.yaml`, `package-lock.json`)
- [x] AC-2,3: `Competency.Api/Program.cs` — минимальный хост (DI/Options, JSON-логи через `AddJsonConsole`, `AddOpenApi` + `Microsoft.Extensions.ApiDescription.Server` с `OpenApiDocumentsDirectory`, документ `openapi.json`), весь startup-код с БД/секретами/миграциями под guard по entry assembly `GetDocument.Insider`; сборка без PostgreSQL
- [x] AC-3: `AppDbContext` в `Competency.Platform` (Npgsql; конфигурации сущностей поставляют модули), `IDesignTimeDbContextFactory` для `dotnet ef`, первая миграция `InitialCreate` (схема Identity добавится в Task 6, расширения PostgreSQL — в Task 4/5), `MigrateAsync()` при старте, строка подключения из конфигурации окружения
- [x] AC-3: health-пробы `/healthz/live` и `/healthz/ready` без авторизации и без деталей, readiness — собственный `IHealthCheck` на `Database.CanConnectAsync`
- [x] AC-4,1: раздача SPA статикой из `wwwroot` с `MapFallbackToFile` для не-API путей (каталог `web/dist` подключается Dockerfile'ом в Task 10; без него хост стартует)
- [x] AC-3: `DataProtection` с ключами в каталоге из конфигурации (`DataProtection:KeysPath`)
Tech reference: aspnetcore-10.0.12, kestrel-10.0.12, microsoft-aspnetcore-openapi-10.0.12, microsoft-extensions-apidescription-server-10.0.12, efcore-10.0.12, microsoft-entityframeworkcore-design-10.0.12, dotnet-ef-10.0.12, npgsql-entityframeworkcore-postgresql-10.0.3, xunit.v3-4.0.1, testcontainers-postgresql-4.15.0.

### Task 2: Сквозные механизмы платформы
Material risk: change: security-контракт (CSRF, авторизация), контракт конкурентности ETag/If-Match и формат ошибок, публичный API
- [x] AC-5: единый формат ошибок — `ProblemDetails` через `IExceptionHandler` (логирует тип и код исключения, не значения; Npgsql `Detail` не попадает в лог и ответ), статус-коды 400/401/403/404/409/412/428
- [x] AC-5: инфраструктура версионирования — интерфейс `IVersioned` (`int Version`), заголовки `ETag`/`If-Match` для эндпойнтов изменения (отсутствует → 428, не совпал → 412), хелпер установки `OriginalValue` свойства `Version`, `DbUpdateConcurrencyException` → 412; `AddOpenApiOperationTransformer` и `ProducesProblem` описывают `ETag`, `If-Match` и ошибки в `openapi.json`
- [x] AC-5: идентификаторы — `Guid.NewGuid()` как генератор значений для ключей всех модулей (общий `EntityBase` с `Id`, `Version`, `CreatedAt`)
- [x] AC-5: `request_id` — middleware (входящий `X-Request-Id` или новый), в логи и в контекст; JSON-логи без ПДн: уровень `Warning` для `Microsoft.AspNetCore.Hosting.Diagnostics` и EF-категорий, значения SQL-параметров выключены (`EnableSensitiveDataLogging` никогда)
- [x] AC-6,12: authz-политики `Authenticated` и `GlobalAdmin`, claims (`sub`, `role`, `employee_id`), хелпер текущего актора для аудита; все эндпойнты по умолчанию требуют аутентификацию (fallback policy), анонимны только health и `/api/v1/auth/login`
- [x] AC-6: защита от CSRF — middleware проверки `Origin`/`Sec-Fetch-Site` на изменяющих методах; rate limiter (built-in) для входа с именованной политикой `login`
- [x] AC-5,15: абстракция `IAuditWriter` и атрибут `[Audited]` (allow-list свойств) в `Platform`; реализация — Task 4
Tech reference: aspnetcore-10.0.12, microsoft-aspnetcore-openapi-10.0.12, efcore-10.0.12.

### Task 3: Каркас frontend
Material risk: change: openapi-typescript 7.13.0 (peer `typescript ^5`) против TypeScript 6.0.2 — ERESOLVE, спайк обязателен; генерация типов из реального OpenAPI 3.1
- [x] AC-1,2: `web/` — собственный `package.json` с точными версиями из `stack.html` (React/react-dom 19.3.0, react-router 8.4.0 в Data mode, `@tanstack/react-query` 5.104.1, antd 6.6.5 + `@ant-design/icons`, openapi-fetch 0.17.0; dev: TypeScript 6.0.2 только `npm i -D -E typescript@6.0.2`, Vite 8.3.2, `@vitejs/plugin-react` 6.1.2, Biome 2.5.15, openapi-typescript 7.13.0, Vitest 5.0.3, jsdom 30.1.2, `@testing-library/*`, `@types/react`/`react-dom` 19.3.0, `@types/node` 24.19.1), `.npmrc` (`ignore-scripts=true`, `save-exact=true`), `package-lock.json` (проверка и при необходимости регенерация linux-x64 optional-бинарников `@rolldown/binding-linux-x64-gnu`, `lightningcss-linux-x64-gnu`, `@biomejs/cli-linux-x64` отложена в Task 10, где доступен Docker)
- [x] AC-2: spike openapi-typescript: точечный `overrides` в `web/package.json`; критерии — генерация из реального `openapi.json` Task 1, `tsc --noEmit`, код выхода `--check`; провал → `QUESTION` пользователю о запасном `@hey-api/openapi-ts` (изменение стека), `legacy-peer-deps` не использовать; результат записать в `docs/architecture/tech-reference/openapi-typescript-7.13.0.md`
- [x] AC-2: скрипты `build`, `lint` (`biome ci --error-on-warnings` + `tsc --noEmit`), `test` (`vitest run`), `gen:api` / `check:api`; `tsconfig` strict (`moduleResolution: bundler`, `noUncheckedIndexedAccess`, `isolatedModules`, `types` перечислены явно), `biome.json` (сгенерированный `schema.d.ts` исключён), `vite.config.ts` (прокси `/api` на Kestrel), `index.html`
- [x] AC-2,13: `web/src/api` — `openapi-fetch` клиент с middleware (401 → единая точка перехода на экран входа, `If-Match`/`ETag` из `ETag` ответа, ошибки 4xx/5xx бросаются типизированным `ApiError` в `queryFn`), `QueryClient` (`retry` не повторяет 4xx), `web/src/api/schema.d.ts` из `openapi.json`
- [x] AC-13: оболочка приложения — `createBrowserRouter` (Data mode) с автообнаружением маршрутов фич по `import.meta.glob('../features/*/routes.ts')`, чтобы Task 7–9 не правили общие файлы; `ConfigProvider` с `ru_RU` и токенами `docs/ux/DESIGN.md`, `dayjs/locale/ru`; шрифты PT Sans/PT Sans Narrow/PT Mono (WOFF2, лицензия OFL) в `web/public/fonts`, без CDN; без `createFromIconfontCN`
Tech reference: react-19.3.0, react-router-8.4.0, tanstack-react-query-5.104.1, antd-6.6.5, openapi-fetch-0.17.0, openapi-typescript-7.13.0, typescript-6.0.2, vite-8.3.2, vitejs-plugin-react-6.1.2, biome-2.5.15, vitest-5.0.3, jsdom-30.1.2, testing-library-*.

### Task 4: Подсистема audit — журнал
Material risk: change: журнал с ПДн и секретами (хэши, stamp), триггер неизменяемости, EF-interceptor, миграция PostgreSQL-расширений
- [x] AC-15: сущность `AuditEvent` (timestamp, actor, role, action, entity_type, entity_id, old_value, new_value, reason, request_id) и миграция `AuditEvents`; DB-триггер, запрещающий UPDATE/DELETE таблицы; индексы по времени, актору, типу и идентификатору сущности
- [x] AC-15,5: реализация `IAuditWriter`; `SaveChangesInterceptor`, пишущий изменения сущностей с `[Audited]` по allow-list (старое/новое значение в одной транзакции с изменением), актор/роль/`request_id` из контекста, для bootstrap/фоновых операций актор `system`
- [x] AC-15: `GET /api/v1/audit` только `GlobalAdmin`, фильтры (период, актор, действие, тип и идентификатор сущности, `request_id`), постраничность; пользовательского изменения журнала нет
- [x] AC-15: регенерировать `web/src/api/schema.d.ts` (`gen:api`) и закоммитить
Tech reference: efcore-10.0.12, npgsql-entityframeworkcore-postgresql-10.0.3, postgresql-18.6.

### Task 5: Подсистема org-structure — backend
Material risk: change: конкурентные переносы дерева, рекурсивные запросы через `FromSql`, FTS/`pg_trgm` на русском, миграция
- [x] AC-9: сущность `OrgUnit` (название, `parent_id`, `head_employee_id`, `is_active`, `valid_from`/`valid_to`, `Version`); CRUD `/api/v1/org-units` (создание, чтение, правка с `If-Match`, перенос, деактивация/активация — без удаления); перенос и деактивация под `pg_advisory_xact_lock`, запрет циклов, отказ деактивации при активных потомках и активных сотрудниках с понятной причиной; `parent_id` допускает `NULL` (несколько корней допустимы)
- [x] AC-10: сущность `Employee` (ФИО, табельный номер уникален, рабочий e-mail уникален без учёта регистра, `is_active`, `org_unit_id`, должность текстом, `Version`); `/api/v1/employees` (создание, чтение, правка, перевод в подразделение, смена статуса с `If-Match`); перевод руководителя активного подразделения в «не работает» отклоняется; интерфейс `IEmployeeDirectory` (получить сотрудника и его статус по `Id`) для `UserManagement`
- [x] AC-11: `GET /api/v1/org-units/tree`, `/{id}/subtree`, `/{id}/path`, `/{id}/summary` (число сотрудников с учётом потомков), поиск и фильтрация подразделений и сотрудников (`q`, статус): FTS `russian` + `pg_trgm` GIN, миграция создаёт `pg_trgm`; список — постраничный
- [x] AC-12,10: две проекции чтения сотрудника — полная (`GlobalAdmin`: табельный номер, статус) и урезанная (`User`: ФИО, e-mail, подразделение, должность); мутации только `GlobalAdmin`; все сущности помечены `[Audited]`
- [x] AC-11: регенерировать `web/src/api/schema.d.ts` и закоммитить
Tech reference: efcore-10.0.12, npgsql-entityframeworkcore-postgresql-10.0.3, postgresql-18.6.

### Task 6: Подсистема user-management — backend
Material risk: change: аутентификация, пароли, lockout, сессии, правило последнего администратора, CSRF, миграция схемы Identity
- [x] AC-6: `AppUser : IdentityUser<Guid>` (`Role`, `IsBlocked`, `EmployeeId?`, `Version`), `AddIdentityCore` + cookie-аутентификация (`HttpOnly`, `SameSite=Strict`, `Secure` по конфигурации, sliding-срок из конфигурации), хэши только `PasswordHasher`; миграция схемы Identity; CHECK по роли и `employee_id` (администратор — без привязки, пользователь — с привязкой), unique partial index на `employee_id`
- [x] AC-6,15: `/api/v1/auth` — `POST login` (lockout, rate limiter `login`, единое сообщение об ошибке; блокировка пользователя и «не работает» сотрудника → отказ), `POST logout`, `GET me`, `POST change-password`; события входа успешного/неуспешного, lockout и выхода пишутся через `IAuditWriter` без паролей и ПДн
- [x] AC-6: bootstrap — при пустой таблице администраторов создаёт глобального администратора из секрета окружения (`Bootstrap:AdminUserName`, `Bootstrap:AdminPassword`), существующего не перезаписывает; под guard'ом build-time OpenAPI; запуск без секрета при отсутствии администратора — понятная ошибка старта
- [x] AC-7: `/api/v1/users` (список с поиском/фильтрацией, создание, просмотр, правка с `If-Match`, блокировка/разблокировка, сброс пароля администратором); инвариант последнего активного администратора (блокировка, смена роли, снятие) в транзакции с блокировкой набора администраторов; сессии блокированного пользователя обрываются (`UpdateSecurityStampAsync`, `ValidationInterval = 0`, проверка `IsBlocked` и статуса сотрудника через `IEmployeeDirectory` в `OnValidatePrincipal`); все мутации `[Audited]` с allow-list (без `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `AccessFailedCount`)
- [x] AC-8: привязка и отвязка пользователя к сотруднику: роль «пользователь» обязана иметь существующего сотрудника, один сотрудник — одна учётная запись; привязка журналируется
- [x] AC-12: пользовательские эндпойнты — только `GlobalAdmin`; `GET /api/v1/auth/me` — любой аутентифицированный; прямой доступ по ID без прав — 403/404 по матрице прав в `<id>.md`
- [x] AC-6,7: регенерировать `web/src/api/schema.d.ts` и закоммитить
Tech reference: microsoft-aspnetcore-identity-entityframeworkcore-10.0.12, aspnetcore-10.0.12, efcore-10.0.12.

### Task 7: Frontend — вход, оболочка и управление пользователями
Material risk: change: поток аутентификации в SPA (401, cookie, CSRF-заголовки), экран входа вне shell, компоненты вне DESIGN.md
- [x] AC-6,13: экран входа вне shell (форма, ошибки, lockout-сообщение), guard маршрутов по `GET /api/v1/auth/me`, выход, смена собственного пароля, shell приложения с навигацией по ролям
- [x] AC-7,8,13: фича `web/src/features/users` — список (поиск, фильтр, блокировка/разблокировка), карточка, создание/правка с `If-Match`, привязка к сотруднику (выбор сотрудника поиском), сброс пароля; конфликт версий (412) — понятное сообщение и перечитывание данных
- [x] AC-13: адаптивность desktop/tablet, соответствие токенам `docs/ux/DESIGN.md`; новых токенов нет, пробелы (экран входа, карточка) фиксируются как кандидаты в `design-md-delta` для design-promote (hard gate)
Tech reference: react-router-8.4.0, tanstack-react-query-5.104.1, antd-6.6.5, openapi-fetch-0.17.0.

### Task 8: Frontend — оргструктура и сотрудники
Material risk: change: компонент дерева отсутствует в DESIGN.md, UI мутаций дерева (перенос, деактивация), конфликт версий
- [x] AC-9,13: фича `web/src/features/org-structure` — дерево подразделений (antd Tree) с созданием, правкой, переносом и деактивацией; карточка подразделения (руководитель, путь, сводка); отказ деактивации показывает причину
- [x] AC-10,11,13: управление сотрудниками (список, поиск и фильтры, создание, правка, перевод, статус, должность текстом); для роли «пользователь» — только чтение урезанной проекции без табельного номера и статуса
- [x] AC-13: конфликт версий (412) — понятное сообщение и перечитывание; адаптивность desktop/tablet; только существующие токены DESIGN.md
Tech reference: antd-6.6.5, react-router-8.4.0, tanstack-react-query-5.104.1.

### Task 9: Frontend — просмотр журнала
Material risk: none
- [x] AC-15,13: фича `web/src/features/audit` — только для администратора: таблица событий с фильтрами (период, актор, действие, сущность, `request_id`) и постраничностью, без редактирования; маршрут регистрируется автообнаружением Task 3
Tech reference: antd-6.6.5, tanstack-react-query-5.104.1.

### Task 10: Упаковка и Kubernetes-манифесты
Material risk: change: непривилегированный пользователь и PVC-пути (`/var/lib/postgresql`, ключи DataProtection), порядок сборки; проверка возможна только с Docker
- [x] AC-4: `Dockerfile` (`# syntax=docker/dockerfile:1`, три стадии: `node:24.21.0-trixie-slim` → SPA, `mcr.microsoft.com/dotnet/sdk:10.0.401` → publish без `EntityFrameworkCore.Design`/`ApiDescription.Server` в publish-выводе, runtime `aspnet:10.0.12-noble`, `USER $APP_UID`, порт 8080, `--platform=linux/amd64`, образы по digest-плейсхолдерам), `.dockerignore` (`**/node_modules`, `**/bin`, `**/obj`, `.git`, `.asd`, `.claude`); PT-шрифты в образе
- [x] AC-4,3: `deploy/k8s/` plain YAML: `app` Deployment (`strategy: Recreate`, порт 8080, `runAsUser: 1654`, пробы на `/healthz/live` и `/healthz/ready`, PVC для ключей DataProtection), `db` StatefulSet (`postgres:18.6-trixie`, PVC монтируется строго в `/var/lib/postgresql`, `PGDATA` не переопределяется, `runAsUser: 999` + `fsGroup`, `/dev/shm` через emptyDir Memory, `terminationGracePeriodSeconds`), Services, ConfigMap, Secret-шаблон с плейсхолдерами (реальные значения не коммитятся), `imagePullPolicy: IfNotPresent`; Ingress/TLS не задаются
- [x] AC-4: короткая инструкция сборки и применения в `deploy/README.md` (порядок: сборка образов вне контура → перенос → `kubectl apply`), проверка манифестов — `kubectl apply --dry-run=client` как строка Manual verification (impl-test)
- [x] AC-4,3: проверка на Docker (первый момент, когда он нужен; регистрирует MS-N «установить Docker Desktop»): `docker build` образа `app`; проверка linux-x64 записей в `web/package-lock.json` и регенерация lock в linux-контейнере при их отсутствии; spike на неизменённом `postgres:18.6-trixie` — `CREATE EXTENSION pg_trgm`, ICU-коллации `ru-*`, схема Identity на `InitialCreate`; результаты записать в `docs/architecture/tech-reference/postgres-image-18.6-trixie.md`
Tech reference: docker-multi-stage-dockerfile-1, dotnet-sdk-image-10.0.401, dotnet-aspnet-image-10.0.12, postgres-image-18.6-trixie, node-24.21.0.

## Risks
- Docker не установлен на dev-машине: ручной шаг отложен до Task 10 (последняя волна); до этого момента миграции Task 4–6 (расширение `pg_trgm`, схема Identity, триггер журнала) не исполнялись против реального PostgreSQL, ошибки в них обнаруживаются поздно — Task 10 и impl-test обязаны прогнать их первыми.
- Учётка БД приложения имеет DDL-права (решение пользователя): триггер неизменяемости журнала не защищает от DDL.
- Без отдельного design (`lite`) архитектурные решения выше принимаются на plan gate; пересмотр после начала impl — через Scope amendment.

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | 1 |
| 2 | 2, 3 |
| 3 | 4 |
| 4 | 5 |
| 5 | 6 |
| 6 | 7 |
| 7 | 8, 9 |
| 8 | 10 |

- Task 2 и Task 3 зависят от Task 1 (проекты, `openapi.json`).
- Task 4, 5, 6 идут последовательно: общий `AppDbContext` и единая линия миграций (конфликт model snapshot при параллельной генерации); Task 4 первым, чтобы сущности Task 5–6 аудировались с начала.
- Task 6 зависит от Task 5 (`IEmployeeDirectory`, статус сотрудника) и Task 4 (`IAuditWriter`).
- Task 7 зависит от Task 3 и Task 6 (оболочка, backend пользователей); Task 7 владеет оболочкой (`web/src/app/**`), хуком текущего пользователя и контрактом навигации фич. Task 8 и 9 зависят от Task 7 (хук роли, навигация), идут параллельно в своих каталогах `web/src/features/<feature>/` и общих файлов не правят (волны переразбиты при impl: исходная волна 6 с Task 7, 8, 9 порождала зависимость от ещё не созданного хука).
- Task 10 зависит от Task 1 и Task 3 (волна 8) (структура репозитория, SPA-статика); находится в последней волне, чтобы собирать итоговое приложение.

## Out of scope
- Тесты (пишутся в impl-test), документы `docs/` (design-promote), диаграмма (mermaid включён в config, но для спринта 001 `documents.c4` заморожен `false`).
- Типы подразделений, штатные единицы, ставки, назначения, справочник должностей; роли §8 и scope-based access; SSO, HRIS, e-mail-уведомления, импорт/экспорт.
