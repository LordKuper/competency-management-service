---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

## Scope reference
[sprint.md](./sprint.md)

## Touched areas
- Репозиторий целиком greenfield: `git ls-files` вне tooling даёт только `docs/**`, `.gitignore`, `AGENTS.md`, `CLAUDE.md`, корневые `package.json`/`package-lock.json`. Всё остальное создаётся спринтом: backend (.NET 10, модули `org-structure` и `user-management`, платформенные сквозные механизмы, тест-проекты), frontend (React/Vite/antd, Vitest), упаковка (`Dockerfile`, `.dockerignore`, K8s-манифесты), корневые файлы (`global.json`, `Directory.Build.props`, `.slnx`, `.config/dotnet-tools.json`, `.gitattributes`, правки `.gitignore`).
- `.asd/project/commands.yaml`: `build`/`lint`/`test`/`run` пусты; от них зависят completion gate `impl` и `impl-test`.
- Корневые `package.json` и `package-lock.json`: единственная зависимость `@google/design.md ^0.4.0` (инструмент `designmd-lint`). Пересекается с размещением frontend: npm `overrides` читаются только из корня пакета.
- `docs/architecture/subsystems.md` и новые `docs/architecture/org-structure.md`, `docs/architecture/user-management.md` (design-promote, lite).
- `docs/product/requirements/org-structure.html`, `user-management.html` и `docs/ux/org-structure.html`, `user-management.html` (design-promote из реализации; каталога `docs/product/requirements/` ещё нет).
- `docs/architecture/stack.html` и `docs/architecture/tech-reference/*`: фиксация решений «для design/spike» и результатов spike.
- `docs/ux/DESIGN.md`: компонента Tree в нём нет; возможный token delta (hard gate на design-promote).
- `project.diagram_tool: none` — c4/mermaid не пишутся; реестр только Markdown.

## Existing docs found
- [Постановка](../../../docs/Постановка.md) (non-canonical, вне `docs/product/requirements`): §4 оргмодель; §8–§9 (10 ролей, RBAC + scope + RLS + field-level); §29 audit (поля и минимум событий включает «вход пользователя»); §30 история; §37.2 SSO; §38 домены; §39–§40 `User` и `Employee` отдельные сущности, связь не определена; §41 группы API; §42 поиск; §43 (P95 < 2 с, 10 000+ сотрудников, desktop/tablet); §44 ИБ (IDOR, перебор ID, CSRF, отделение тех. администрирования от HR); §46 optimistic locking; §49 MVP. §36 импорт/экспорт и §37.1/37.3 интеграции — вне спринта.
- [Концепция](../../../docs/product/concept.html) (draft): видение, контур без интернета, PII/HR; оргструктура и пользователи не описаны, противоречий нет.
- [Стек](../../../docs/architecture/stack.html) (approved, rev 7): точные версии; ограничения (нет интернета в рантайме, список лицензий, PII не в логах, секреты только из K8s Secrets, локальная аутентификация Q2, Kubernetes Q9, два workload'а `app`+`db`); ~15 открытых пунктов «для design/spike». Масштабирование/HA не заданы.
- [Реестр подсистем](../../../docs/architecture/subsystems.md): пустая таблица; добавления только на design-promote.
- Tech-reference в `docs/architecture/tech-reference/` — backend (aspnetcore, kestrel, csharp, openapi, apidescription-server, identity-efcore, efcore, efcore-design, dotnet-ef, npgsql), БД (postgresql, postgres-image), frontend/сборка (react, react-router, tanstack-query, antd, openapi-fetch, openapi-typescript, typescript, @types/*, vite, plugin-react, biome, node, npm), тесты (xunit.v3, nsubstitute, awesomeassertions, testcontainers, vitest, jsdom, testing-library/*), упаковка (docker-multi-stage, dotnet-sdk-image, dotnet-aspnet-image). Не используются спринтом (пакеты не добавлять): `closedxml`, `pdfsharp-migradoc`, `csvhelper`, `mailkit`.
- [DESIGN.md](../../../docs/ux/DESIGN.md): токены, antd 6 mapping, breakpoints, шрифты PT Sans/Narrow/Mono; Tree/TreeSelect, экран входа без shell и карточка единицы не специфицированы.
- [design-system.html](../../../docs/ux/design-system.html): сгенерирован из DESIGN.md, Tree нет.
- [accessibility.html](../../../docs/ux/accessibility.html): WCAG 2.2 AA только на уровне токенов; клавиатура, ARIA, страницы вне охвата (решение заказчика 2026-10-05).

## Contradictions
- Criterion check AC-1…AC-14: все критерии достижимы на заявленных host/runtime/contract-фактах, кроме C-1…C-4; оговорки по остальным помечены `[AC-N]` в Gaps и Risks.
- C-1 AC-3 («приложение стартует, применяет схему») vs `efcore-10.0.12.md` (миграции bundle + одноразовый K8s Job, app-учётка без DDL); winner=user: (a) приложение применяет схему само при старте (`MigrateAsync`), AC-3 без изменений; учётка БД приложения имеет DDL-права (принятый риск для PII/HR-контура; ответ пользователя 2026-10-05).
- C-2 AC-10 («суммарная ставка не превышает допустимую») — нет лимита и определения «занята»; winner=user: ставок нет — штатные единицы и ставки выведены из scope (ответ пользователя 2026-10-05); AC-10 переписан.
- C-3 AC-8 vs AC-10 vs AC-9 — не определены статусы занятости, какие блокируют вход, что с действующими назначениями; winner=user: отдельной сущности «назначение» нет; у сотрудника два поля — подразделение и должность (текст) (ответ пользователя 2026-10-05). Статусы занятости не заданы пользователем: принято допущение «работает / не работает», «не работает» блокирует вход (проверяется на audit gate).
- C-4 AC-9 («типы единиц настраиваемы без изменения кода») vs AC-13 (нет экрана типов; CRUD-экраны мутаций не названы); winner=user: типов подразделений нет; иерархия — строгое дерево, один родитель, без циклов (ответ пользователя 2026-10-05). Объём UI: все мутации подразделений и сотрудников (допущение, AC-13).
- Постановка §37.2/§44/§49 (SSO) vs `stack.html` Q2 + `sprint.md` (локальные учётные записи); winner=stack.html.
- Постановка §8–§9 (10 ролей, scope, RLS) vs `sprint.md` (две роли); winner=sprint.md (решение пользователя 2026-10-05). Следствие: глобальный администратор совмещает техническое администрирование и HR-данные оргструктуры (против §44/§8.9); права изолируются для будущего расширения.
- Постановка §38 (горизонтальное масштабирование, очередь, кэш) vs `stack.html` (один экземпляр, без брокера); winner=stack.html.
- Постановка §4 (профнаправления и настройки процесса единицы) vs `sprint.md` Out of scope; winner=sprint.md.
- `sprint.md` Goal («ссылка на сотрудника необязательна») vs AC-8 («пользователь обязан быть привязан»); winner=AC-8: nullable `employee_id`, обязателен при роли «пользователь», запрещён при роли администратора.
- Аудит: после поправки scope все требования журнала собраны в AC-15; AC-5 и AC-12 ссылок на журнал не содержат.
- AC-13 («соответствие accessibility.html») vs фактическое содержание `accessibility.html`; winner=accessibility.html: соответствие только на уровне токенов.

## Existing implementation found
- Исходного кода нет (greenfield). `package.json` (корень): только `@google/design.md ^0.4.0` под `designmd-*` команды `commands.yaml`; `.gitignore`: `node_modules/`, `.vs/`, `.asd/tmp/`, `.claude/settings.local.json`, без `bin/obj/dist/TestResults/.env`.

## Gaps
- Backend-каркас [AC-1,2,3]: `global.json` (SDK 10.0.401, `test.runner = Microsoft.Testing.Platform`), `Directory.Build.props` (Nullable, ImplicitUsings, `TreatWarningsAsErrors` + `WarningsNotAsErrors` NU190x, lock-файлы), `.slnx`, `.config/dotnet-tools.json` (`dotnet-ef` 10.0.12), проект запуска + модульные проекты подсистем с `internal`-границами.
- Сборка без БД [AC-2]: `AddOpenApi()` + `Microsoft.Extensions.ApiDescription.Server` (`openapi.json`), весь startup-код (DbContext, Identity, DataProtection, миграции, bootstrap admin, health) под guard `GetEntryAssembly()?.GetName().Name != "GetDocument.Insider"` либо `IDesignTimeDbContextFactory`.
- Контракт в OpenAPI [AC-2,5]: `ETag`, `If-Match` и ProblemDetails 4xx пакет сам не опишет — нужны `AddOpenApiOperationTransformer`/`ProducesProblem`; `openapi-fetch` не бросает на 4xx/5xx — `queryFn` обязаны бросать типизированную ошибку.
- Платформенное ядро [AC-5,15]: DbContext с Identity, `SaveChangesInterceptor` аудита (actor/role/request_id; для bootstrap — «system»), `int Version` + ETag/`If-Match` (поведение без `If-Match`: 428 или разрешено; 412 vs 409), ProblemDetails, `AddJsonConsole()` с фильтрами PII, health `live`/`ready`, две authZ-политики.
- Решения для plan gate до impl (lite без design; ADR-темы): (1) хранение дерева: adjacency list + `WITH RECURSIVE` / `ltree` / closure table; (2) формат K8s-манифестов: plain YAML / Kustomize / Helm; (3) доставка миграций (C-1); (4) порядок сборки/кодогенерации; (5) подключение Identity: `AddIdentityCore` + cookie vs `MapIdentityApi` (открывает анонимные `/register`, `/forgotPassword`); (6) хранение ролей; (7) один DbContext vs по подсистемам; (8) тип идентификатора `Guid` v4 vs v7; (9) `Version` vs `ConcurrencyStamp`; (10) allow-list аудируемых свойств, иммутабельность аудита на уровне БД; (11) ключи DataProtection (Secret read-only vs PVC, `strategy: Recreate`); (12) политика паролей, lockout, сессии, `Secure`/`SameSite`; (13) снято — назначений нет; (14) поля FTS `russian` vs `pg_trgm`; (15) CSP для antd, минимальные браузеры.
- Операция «удалить» [AC-7,9]: hard delete не предоставлять (блокировка пользователя, деактивация единицы/сотрудника) — зафиксировать в plan.
- Cookie-безопасность [AC-6]: CSRF (SameSite + antiforgery) закрыть в рамках AC-6; bootstrap admin создаётся только при отсутствии администратора, пароль из env/Secret никогда не перезаписывает существующий; логин-идентификатор (username vs e-mail) не определён.
- Дерево и экран входа [AC-13]: в DESIGN.md нет Tree/TreeSelect, карточки единицы, экрана входа; dev использует только существующие токены; новые — через design-promote с hard gate. Шрифты PT Sans/Narrow/Mono (WOFF2, OFL) добавить в репозиторий/образ.
- Именование: `OrganizationUnit` — `/api/v1/org-units`, сотрудники — `/api/v1/employees`, вход — `/api/v1/auth`, журнал — `/api/v1/audit`; `/positions` зарезервировано за Career Framework (должность у сотрудника — текстовое поле).
- Frontend-каркас [AC-1,2]: frontend в отдельном каталоге со своим `package.json` и lock (чтобы `overrides` и `npm ci` не тянули `@google/design.md`), `.npmrc` (`ignore-scripts=true`, `save-exact=true`, `engine-strict=true`), `typescript` только `npm i -D -E typescript@6.0.2`, `tsconfig` strict, `vite.config.ts` (прокси `/api`), `biome.json` (сгенерированный `schema.d.ts` исключить; `biome ci --error-on-warnings`), Vitest + jsdom, `openapi-fetch`, router Data mode, `ConfigProvider` с токенами DESIGN.md. Не добавлять: ClosedXML, PDFsharp-MigraDoc, CsvHelper, MailKit, Playwright, react-hook-form.
- Тест-каркас [AC-1]: `impl` не пишет тесты, а MTP (xunit.v3) и `vitest run` на пустом проекте падают; скелеты тест-проектов с минимум одним тестом и Testcontainers-фикстурой вынести в `Test-only` Task для `asd-tester`.
- Упаковка [AC-4]: `Dockerfile` (образы по digest, `--platform=linux/amd64`, `USER $APP_UID`, SPA статикой, PT-шрифты в образе), `.dockerignore`, манифесты: `app` (порт 8080, runAsUser 1654, пробы, PVC для ключей DataProtection, `strategy: Recreate`), `db` (PVC строго в `/var/lib/postgresql`, `PGDATA` не менять, `runAsUser: 999` + `fsGroup`, `/dev/shm`, `terminationGracePeriodSeconds`), ConfigMap, Secret-шаблон с плейсхолдерами.
- Гигиена: `.gitignore` (`bin/`, `obj/`, `dist/`, `TestResults/`, `.env`, `appsettings.*.local.json`), `.gitattributes` (`eol=lf` для `Dockerfile`, `*.sh`, `*.yaml`, `package-lock.json`); локальный запуск БД — `docker run postgres:18.6-trixie`.
- `commands.yaml` [AC-2]: заполнить `build`, `lint`, `test`, `run`; `test_affected` не задавать. Правка `commands.yaml` — settings-change, меняется только через `/asd-init` (plan-declared settings change, `asd-phase-impl.md` step 6); plan объявляет это шагом первой волны.
- Постфактум-документы (lite design-promote): requirements/UX/ADR/API-контракты пишутся из `plan.md` и diff; API-контракт фолдится в `openapi.json` либо `<id>.md`; решения «для design» из `stack.html` — по fold rule.
- External dependency gaps: Q8 TLS открыт — от него зависят `Secure` cookie и `X-Forwarded-*`; Ingress/TLS в манифестах не задавать, `CookieSecurePolicy` настраиваемым.
- External dependency gaps: Q11 registry vs tar-импорт открыт; манифесты ссылаются на образы по digest, `imagePullPolicy: IfNotPresent`.
- External dependency gaps: параметры кластера неизвестны (версия K8s, runtime, StorageClass/RWO, Pod Security, Ingress); Q5 (бэкапы) вне спринта.
- External dependency gaps: Docker daemon и интернет на dev/CI-хосте; наличие .NET SDK 10.0.401, Node 24.21.0/npm 11.19.0, `dotnet-ef` на dev-хосте аудитом не проверялось.
- External dependency gaps: секрет bootstrap-администратора и пароль БД — оператор создаёт в K8s Secret (кандидат на `MS-N`); локально — `dotnet user-secrets`.
- External dependency gaps: fallback `@hey-api/openapi-ts 0.99.0` не входит в «версии строго по stack.html» — его принятие = изменение стека (Complication Approval + tech-reference); возможная ссылка на `Microsoft.Extensions.Validation` — новая зависимость без tech-reference.
- External dependency gaps: непроверенные факты, закрываются spike в первых волнах: `CREATE EXTENSION pg_trgm`/`btree_gist` в `postgres:18.6-trixie`; ICU-коллации; `openapi-typescript` 7.13.0 на TS 6.0.2 (overrides + `tsc --noEmit` + `--check`); Vitest 5.0.3 ↔ jsdom 30.1.2; Testcontainers 4.15.0 ↔ PG 18; попадание `EntityFrameworkCore.Design` в publish; схема Identity по умолчанию; `LockoutEnd` с offset 0 в Npgsql.

## Risks
- openapi-typescript 7.13.0 (peer `typescript ^5.x`) vs TS 6.0.2 → ERESOLVE [AC-1,2]: impact=нет типов клиента, блок сборки frontend; mitigation=spike в первой волне с точечным `overrides`; провал → вернуть пользователю решение о fallback.
- npm lock, созданный на Windows, без linux-x64 optional-бинарников и CRLF [AC-2,4]: impact=`npm ci` в Docker падает; mitigation=проверка записей `@rolldown/binding-linux-x64-gnu`, `lightningcss-linux-x64-gnu`, `@biomejs/cli-linux-x64`, регенерация в linux-контейнере, `.gitattributes`.
- Build-time OpenAPI запускает entry point [AC-2,3]: impact=любой startup-код с БД/секретами ломает `dotnet build` без PostgreSQL; mitigation=guard по entry assembly, `--tl:off` в CI, контрольная сборка без БД.
- «Без предупреждений» [AC-2]: impact=новый NuGet-advisory (NU1901–NU1904) даёт предупреждение без изменений кода; mitigation=`WarningsNotAsErrors` для NU190x с отдельной проверкой, pinned версии, lock-файлы.
- Аудит и Identity [AC-15]: impact=interceptor запишет `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `AccessFailedCount` в old/new_value; mitigation=allow-list аудируемых свойств, тест на отсутствие хэшей в журнале.
- Append-only аудит держится соглашением [AC-15]: impact=`ExecuteUpdateAsync`/`ExecuteDeleteAsync` обходят `ChangeTracker`; mitigation=DB-триггер/права на запрет UPDATE/DELETE таблицы аудита, запрет bulk-операций над аудируемыми сущностями, тест «каждая мутация создаёт строку аудита».
- ПДн в логах [AC-5]: impact=URL с query на Information, исключения Npgsql со значениями; mitigation=уровень Warning, `IExceptionHandler` логирует тип и код, тест с sentinel-значением ПДн.
- Правило последнего администратора [AC-7]: impact=конкурентные блокировки/смены роли оставляют систему без администратора; mitigation=SERIALIZABLE-транзакция либо блокировка набора администраторов; правило охватывает блокировку, смену роли, удаление.
- Блокированный пользователь сохраняет сессию [AC-6,7,8]: impact=`ValidationInterval` по умолчанию 30 минут; mitigation=малый интервал либо проверка `IsBlocked` и статуса сотрудника в `OnValidatePrincipal`, `UpdateSecurityStampAsync`.
- Lockout как DoS на единственного администратора [AC-6]: impact=перебор блокирует admin, сообщение раскрывает существование учётки; mitigation=rate limiter на `/api/v1/auth/login`, единое сообщение об ошибке.
- Инварианты привязки и ссылочная целостность [AC-8,9,10]: impact=дубликаты учётных записей, администратор с привязкой, hard delete ломает аудит; mitigation=unique partial index на `employee_id`, CHECK по роли, FK без каскадов, статус вместо удаления.
- Перенос подразделения и циклы [AC-9]: impact=два конкурентных переноса дают цикл; mitigation=SERIALIZABLE/advisory lock либо свойства выбранной структуры дерева (строгое дерево, один родитель), тест на конкурентный перенос.
- Поиск и коллация [AC-11]: impact=`pg_trgm`/ICU в неизменённом образе не подтверждены; mitigation=`pg_trgm` первым интеграционным тестом, колоночная ICU-коллация, тестовые данные с `ё`.
- P95 < 2 с на 10 000 сотрудников [AC-11]: impact=«обычные операции» и железо не определены, нагрузочного инструмента нет, Testcontainers `fsync=off`; mitigation=список операций и окружение в plan, perf-тест на xunit вне impacted set, сид через COPY/raw SQL.
- Lite без design/design-review [workflow]: impact=~15 решений «для design» не имеют предварительного места принятия; mitigation=plan gate включает таблицу решений до impl, design-promote фолдит их по fold rule.
- Идентификатор [AC-5,12]: выбор v7 можно оспорить как «последовательный»; решение на plan gate, рекомендация v4. Прямой IDOR-риск на admin-only мутациях и `/users/{id}`; mitigation=матрица прав по эндпойнтам и негативные тесты 401/403/404 на пару роль×ресурс.
- Конфликт версий на Identity-сущности [AC-5]: `UserManager.UpdateAsync` использует `ConcurrencyStamp`, поверх него `Version` + `If-Match`; mitigation=`OriginalValue` из `If-Match`, тест на 412/409.
- Packaging без кластера [AC-4]: манифесты не проверяются запуском; mitigation=минимальные манифесты по tech-reference, `kubectl apply --dry-run=client` как строка Manual verification, без Ingress/TLS.
- Dev-зависимости в publish [AC-4]: `EntityFrameworkCore.Design` и `ApiDescription.Server` попадают в образ; mitigation=`ExcludeAssets=runtime` либо отдельный проект миграций, проверка `dotnet publish`.
- Лицензии [AC-1]: инструмента проверки в стеке нет; транзитивные лицензии и шрифты PT (OFL) не сверены; mitigation=ручная сверка lock-файлов в plan.
- Доступность [AC-13]: `accessibility.html` гарантирует только токены; mitigation=принять как решение заказчика, не добавлять неявных обязательств в ревью.

## Subsystems map
- `org-structure` (предлагается; §38 «Organization»): строгое дерево подразделений без типов, справочник сотрудников (подразделение и должность текстом); чтение дерева, поддерева, пути, поиска и сводки. Зависит от платформенного ядра и `audit`; от `user-management` не зависит.
- `user-management` (предлагается; §38 «Identity & Access»): учётные записи (ASP.NET Core Identity), две роли, вход/сессия/lockout, bootstrap-администратор, `/api/v1/users`, `/api/v1/auth`, привязка к сотруднику. Направление зависимости одно: `user-management → org-structure` (read-only: id и статус занятости сотрудника).
- `audit` (предлагается пользователем 2026-10-05, §38 «Audit»): append-only журнал §29, события входа/выхода/lockout, чтение администратором; пишут в него `org-structure` и `user-management`; зависимость `org-structure`/`user-management → audit`. Решение пользователя: журналирование — отдельная подсистема (AC-15).
- Платформенное ядро (конкурентность/ETag, ошибки, логи, health, authZ-политики) в реестр не предлагается; описывается в `stack.html` и `<id>.md`. Реестр на этом этапе не записывается: добавление подсистем — hard gate на design-promote; scope acceptance 2026-10-05 переиспользуется при неизменных границах.

## Поправка после audit (2026-10-05)

Ответы пользователя упростили модель: нет штатных единиц, ставок, назначений и типов подразделений; сотрудник имеет подразделение и должность (текст); дерево строгое. Выше удалены/переписаны строки про назначения; остальные оговорки (spike, шрифты, упаковка, риски аудита и Identity) остаются в силе. Журнал выделен в подсистему `audit` (AC-15).

## Documentation migration plan

| # | Source (path/URL) | Format | Proposed target in `docs/` | Type | Notes |
|---|---|---|---|---|---|
| 1 | `docs/Постановка.md` §4 | md | `docs/product/requirements/org-structure.html` | migrated | иерархия произвольной глубины, расширяемые типы, поля единицы; профнаправления и настройки процесса не переносить |
| 2 | `docs/Постановка.md` §39, §40 | md | `docs/product/requirements/org-structure.html`, `user-management.html`; цель подсистем в `docs/architecture/<id>.md` | migrated | `User`, `Employee`, `OrganizationUnit`; связь 1:1 необязательная — решение спринта |
| 3 | `docs/Постановка.md` §8, §9, §44 | md | `docs/product/requirements/user-management.html` | migrated | только принятое подмножество (две роли, backend-проверки, IDOR, перебор ID, CSRF) |
| 4 | `docs/Постановка.md` §29, §30 | md | `docs/product/requirements/org-structure.html` и `user-management.html`; механизм аудита — в `stack.html` | migrated | поля аудита и события в scope; история — только назначения |
| 5 | `docs/Постановка.md` §41, §42 | md | `docs/product/requirements/org-structure.html` и `user-management.html`; контракт — `openapi.json` | migrated | `/auth`, `/employees`, `/org-units`; `/positions` за Career Framework |
| 6 | `docs/Постановка.md` §43.1, §43.2, §43.4, §46 | md | `docs/product/requirements/org-structure.html` (NFR), `docs/ux/org-structure.html`, `docs/ux/user-management.html` | migrated | P95 < 2 с на 10 000 сотрудников, optimistic locking |
| 7 | `docs/Постановка.md` §5–7, §10–28, §31–35, §37, §45, §47–56 | md | не переносить в этом спринте | — | вне scope; Постановка остаётся источником требований |
