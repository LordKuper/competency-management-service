---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

## 2026-10-05 — Workflow выбран: lite

- **Decision**: Спринт 001 идёт по workflow `lite` (scope → audit → plan → impl ⇄ impl-test → impl-review → design-promote → retro → pr), зафиксирован в `state.json.workflow`.
- **Rationale**: Явный выбор пользователя на жёстком gate `workflow choice`; рекомендация была `standard`.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/state.json`

- 2026-10-05 — retro intake skipped: no archived sprint with retrospective, candidates `[]`
- 2026-10-05 — audit: frozen `true` (greenfield scope, behaviour and contract impact)

## 2026-10-05 — Scope принят: каркас + user-management + org-structure

- **Decision**: Спринт включает каркас проекта, подсистему `user-management` и подсистему `org-structure` (AC-1..14). User→Employee 1:1 необязательно; две роли — глобальный админ (без привязки) и пользователь; штатная единица хранит должность текстом (Career Framework вне спринта).
- **Rationale**: Пользователи привязываются к оргштатке, поэтому подсистемы поставляются вместе (решение пользователя на scope gate); предложение выделить каркас в отдельный спринт отклонено.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`

## 2026-10-05 — Audit: ответы по C-1…C-4, Q-1, Q-2 и поправка scope

- **Decision**: Схема БД применяется приложением при старте. Ставок, штатных единиц, назначений и типов подразделений нет; у сотрудника поля «подразделение» и «должность» (текст); иерархия подразделений — строгое дерево. Роль «пользователь» видит дерево и ФИО, e-mail, подразделение, должность. События входа журналируются, журнал выделен в подсистему `audit` (AC-15). Это поправка scope: изменены AC-5, 8, 9, 10, 11, 12, 13, 14, добавлен AC-15.
- **Rationale**: Явные ответы пользователя на вопросы audit; упрощение модели оргштатки. Допущения вне ответов: статусы сотрудника «работает / не работает» блокируют вход; UI покрывает все мутации подразделений и сотрудников; журнал читает администратор через API и экран.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/audit.md`

- 2026-10-05 — subsystem boundary: `audit` added to sprint scope by user ("Журналирование"); registry write deferred to design-promote

## 2026-10-05 — Audit gate принят

- **Decision**: `audit.md` и поправленный `sprint.md` (AC-1…AC-15) приняты вместе с допущениями: статус сотрудника «работает / не работает», UI всех мутаций, журнал читает администратор через API и экран, `audit` — третья подсистема.
- **Rationale**: Явное согласие пользователя на audit gate; переход к plan (workflow `lite`, фаза design отсутствует).
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/audit.md`, `.asd/sprints/001-project-init-org-structure/sprint.md`

- 2026-10-05 — settings: `project.diagram_tool` none → mermaid via /asd-init (user request); sprint `documents.c4` stays frozen `false`, diagram from next sprint

## 2026-10-05 — plan.md принят

- **Decision**: `plan.md` (Task 1–10, 7 волн) принят. Архитектурные решения (дерево — adjacency list + рекурсивные запросы под advisory-lock; Identity-core + cookie, роль — колонка; Guid v4; Version + ETag/If-Match; CSRF через SameSite=Strict + Origin; журнал с триггером неизменяемости; plain YAML манифесты) приняты на plan gate; `global.json` на SDK 10.0.400.
- **Rationale**: В workflow `lite` нет design, поэтому решения принимаются на plan gate. По просьбе пользователя ручной шаг (Docker Desktop) отложен до Task 10 — Task 1–9 выполняются без Docker и PostgreSQL.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/plan.md`

- 2026-10-05 — stubs: no open stubs in scope
- 2026-10-05 — route Task 1: critical, dispatch HEAD c8a5aad
- 2026-10-05 — route Task 2, Task 3: critical, dispatch HEAD 9425a2c

## 2026-10-05 — Волны 1–2: flagged choices devs и commands.yaml

- **Decision**: Заполнен `.asd/project/commands.yaml` (build = dotnet + npm build, lint = `npm --prefix web run lint`, test = `dotnet test --solution` + `npm test`, run, custom gen-api/check-api/ef-*). Flagged choices Task 1–3 приняты как не противоречащие plan: openapi в `openapi/openapi.json`, миграции в `src/Competency.Platform/Migrations`, `IEntityConfigurationContributor`, guard только на `GetDocument.Insider`, `[Audited]` одним атрибутом (класс — opt-in, свойство — allow-list), `Version` через `EntityStampingInterceptor`, `IfMatch` как handler-параметр, CSRF без сравнения схемы Origin, `withIfMatch` не реализован (ETag возвращает `unwrap`), `standaloneRoutes` для экрана входа, `skipLibCheck: true`.
- **Rationale**: Каждый выбор в пределах plan и persistent docs, без новых зависимостей. Открыто до impl assessment: лицензия MPL-2.0 у `lightningcss` (жёсткая build-time зависимость Vite 8.3.2, не входит в допустимый список stack.html) — решение за пользователем; бэкенд-lint отсутствует (TreatWarningsAsErrors), `npm test` падает до появления тестов (не входит в impl gate).
- **Affected docs**: `.asd/project/commands.yaml`, `docs/architecture/tech-reference/openapi-typescript-7.13.0.md`
- 2026-10-05 — route Task 4: critical, dispatch HEAD 3f92a44

## 2026-10-05 — Волна 3: flagged choices Task 4 (audit)

- **Decision**: Приняты: таблица `audit_events` (snake_case), действия `<EntityType>.Created|Updated|Deleted`, `IAuditWriter` в собственном scope/контексте (события входа переживают откат вызывающего), allow-list только атрибутами, лимиты страниц 50/200 в коде. Для Task 5 добавлены: `JsonNumberHandling.Strict` в Platform (чтобы int в клиенте был `number`) и `InternalsVisibleTo Competency.Tests` в Platform, Audit, OrgStructure (Task 6 — для UserManagement и Api); Task 6 ограничивает длину пользовательского `AuditEntry.Actor`.
- **Rationale**: Решения в пределах plan и AC-5/AC-15; `InternalsVisibleTo` нужен impl-test как реальному вызывающему. Поведение триггера, jsonb и применение миграции на реальном PostgreSQL не проверены (нет Docker) — Task 10 и impl-test прогоняют их первыми.
- **Affected docs**: `src/Competency.Audit/`, `src/Competency.Platform/Migrations/`
- 2026-10-05 — route Task 5: critical, dispatch HEAD 451f39a

## 2026-10-05 — Волна 4: flagged choices Task 5 (org-structure)

- **Decision**: Приняты: одна схема `EmployeeResponse` с необязательными `personnelNumber`/`isActive`/`version` (для роли User их нет); роль User видит только активные подразделения и работающих сотрудников; один глобальный `pg_advisory_xact_lock(5001001)` на все мутации дерева (апгрейд — блокировка по поддереву); размещение только в активное подразделение; 400 для недопустимых ссылок, 409 problem+json для конфликтов правил; отдельные POST `/move`, `/activate`, `/deactivate`; `normalized_email` для уникальности; `pg_trgm` и `tsvector russian` через миграцию; `JsonNumberHandling.Strict` и `InternalsVisibleTo` сделаны.
- **Rationale**: В пределах AC-9…AC-12 и plan Overview, без новых зависимостей. Не проверено без PostgreSQL: advisory lock, рекурсивные CTE, `pg_trgm`/`russian` FTS, уникальные индексы, генерируемые колонки — Task 10 и impl-test прогоняют миграцию первой.
- **Affected docs**: `src/Competency.OrgStructure/`, `src/Competency.Platform/Migrations/`
- 2026-10-05 — route Task 6: critical, dispatch HEAD 4dac05e

## 2026-10-05 — Волна 5: flagged choices Task 6 (user-management) и пересмотр волн frontend

- **Decision**: Приняты: `AppDbContext` остаётся `DbContext`, Identity отображён одной таблицей `users` (`AddUserStore<AppUserStore>`, без таблиц ролей/claims/логинов/токенов/passkey); собственный `OnValidatePrincipal` вместо `SecurityStampValidator`; актор неуспешного входа — id известного пользователя либо `unknown`, введённый текст в журнал не попадает; привязка журналируется через `[Audited] EmployeeId`, пароль — явными событиями `Auth.PasswordChanged`/`AppUser.PasswordReset`; bootstrap срабатывает при отсутствии АКТИВНОГО администратора; FK `users.employee_id` по имени сущности. Волны plan переразбиты: Task 7 — волна 6, Task 8 и 9 — волна 7, Task 10 — волна 8.
- **Rationale**: Отклонение от «схема Identity по умолчанию» обосновано (Platform не может ссылаться на модули, остальные таблицы не используются); не проверено без PostgreSQL: `FOR UPDATE`, CHECK и уникальные индексы, DDL `users`. Task 8 и 9 зависят от хука текущего пользователя и контракта навигации Task 7, поэтому не могут идти параллельно с ним; это не изменение scope.
- **Affected docs**: `src/Competency.UserManagement/`, `.asd/sprints/001-project-init-org-structure/plan.md`
- 2026-10-05 — route Task 7: critical, dispatch HEAD 2935e40

## 2026-10-05 — Волна 6: flagged choices Task 7 (вход, оболочка, пользователи)

- **Decision**: Приняты: навигация в Header (горизонтальное меню) вместо Sider; контракт фич `routes`/`standaloneRoutes`/`navItems` и `AdminOnly`; хуки `useCurrentUser`/`useIsAdmin`; `If-Match` строится из `version` как `"N"`; тексты ошибок по статусу на русском; подсказка пароля из `passwordPolicy.ts`. Кандидаты в `design-md-delta` для design-promote (hard gate): экран входа, карточка пользователя, employee picker, Avatar/inverse-кнопка в header, `reading-max-width`, `focus-ring-inverse`.
- **Rationale**: В пределах AC-6/7/8/13 и существующих токенов DESIGN.md; новых зависимостей нет. Tablet-раскладка визуально не проверена (нет браузера).
- **Affected docs**: `web/src/app/`, `web/src/features/auth/`, `web/src/features/users/`
- 2026-10-05 — route Task 8: critical; Task 9: standard; dispatch HEAD 740f17d

## 2026-10-05 — Волна 7: flagged choices Task 8 и 9 (UI оргструктуры, журнал)

- **Decision**: Приняты: маршруты `/org-structure`, `/org-structure/employees[/new|/:id]` (только `/new` за `AdminOnly`), `/audit` (за `AdminOnly`); карточка сотрудника — форма при наличии `version` в ответе, иначе факты; поиск дерева на клиенте; `EmployeePicker` и `ifMatchOf` импортируются из `features/users` (кандидат на вынос в `web/src/api`); даты — нативный `Input type="date"`, время журнала — `Intl.DateTimeFormat`; отказ деактивации — постоянный `modal.error`. Кандидаты в `design-md-delta`: Tree, TreeSelect, Checkbox, карточка подразделения, теги активности.
- **Rationale**: В пределах AC-9…AC-13, AC-15 и существующих токенов; новых зависимостей нет.
- **Affected docs**: `web/src/features/org-structure/`, `web/src/features/audit/`
- 2026-10-05 — route Task 10: critical, dispatch HEAD 7d7a0b3

## 2026-10-05 — Волна 8: Task 10 (упаковка), manual step MS-1

- **Decision**: Подзадачи 1–3 Task 10 приняты; подзадача 4 (проверка на Docker) отложена как `BLOCKED: MS-1` (установка Docker Desktop, действие пользователя, автономно невозможно). Приняты flagged choices: Secret-шаблон в `deploy/secret.template.yaml` (вне `deploy/k8s/`), ARG-теги вместо digest-заглушек в Dockerfile, `OpenApiGenerateDocumentsOnBuild=false` при publish, pod hardening (seccomp, drop ALL), startupProbe, `app` подключается под `POSTGRES_USER` (открытый пункт). Лицензия MPL-2.0 `lightningcss` (build-time, не в образе) остаётся на решение пользователя при impl assessment.
- **Rationale**: MS-1 валидирован как необходимый: установка Docker Desktop требует прав и участия пользователя. Офлайн проверены: locked-restore + publish без Design/Roslyn/ApiDescription.Server, `npm ci` + build, linux-записи в lock, парсинг и перекрёстная проверка 8 YAML-документов.
- **Affected docs**: `Dockerfile`, `deploy/`, `.asd/sprints/001-project-init-org-structure/manual-steps.md`

## 2026-10-06 — MS-1 подтверждён, лицензия MPL-2.0 принята как исключение

- **Decision**: MS-1 выполнен пользователем (Docker Desktop 4.94.0, Server linux/amd64); оркестратор проверил `docker version` и `docker run --rm hello-world` (`Hello from Docker!`); docker.exe лежит в `C:\Users\Michieru\AppData\Local\Programs\DockerDesktop\resources\bin` и не в PATH сеанса — команды выполняются с этим каталогом в PATH. Пользователь принял MPL-2.0 у `lightningcss` (build-time зависимость Vite 8.3.2, не в образе и бандле) как исключение из списка лицензий `stack.html`; запись в `stack.html` — на design-promote.
- **Rationale**: Явные ответы пользователя на manual-steps gate; исключение узкое (build-time, слабый file-level copyleft).
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/manual-steps.md`, `docs/architecture/stack.html` (design-promote)
- 2026-10-06 — route Task 10 (deferred Docker verification): critical, dispatch HEAD d0662ea

## 2026-10-06 — Impl assessment: ручная проверка пользователем, дефект dev-прокси

- **Decision**: При ручном запуске вход давал 403: dev-прокси Vite 8 подменяет `Host` на адрес backend, и CSRF-проверка Origin/Host отклоняет запрос. Исправлено в `web/vite.config.ts` (`configure` передаёт исходный `Host` браузера, коммит 0e10628); backend и CSRF-проверка не менялись. Flagged choice 11 Task 3 («Host остаётся хостом Vite») признан ошибочным и заменён этим исправлением.
- **Rationale**: Дефект dev-окружения, найден до принятия impl assessment; проверено на отдельном Vite (вход 200, cross-origin 403, `/auth/me` 200). Порт 5432 на машине пользователя зарезервирован Windows (диапазон 5335–5434) — для локальной БД использовать 15432.
- **Affected docs**: `web/vite.config.ts`

## 2026-10-06 — Поправка scope: AC-16 запуск из Visual Studio по F5, Task 11 (волна 9)

- **Decision**: Добавлен AC-16: профиль многопроектного запуска Visual Studio («Все сервисы») поднимает БД PostgreSQL (Docker Compose-проект), Vite (JavaScript-проект `.esproj`) и `Competency.Api` под отладчиком. Новые dev-only MSBuild-SDK (`Microsoft.VisualStudio.JavaScript.Sdk`, Docker Compose SDK) принимаются как исключение из «минимум зависимостей»; в образ не попадают; запись в `stack.html` — на design-promote. `dotnet build`/`dotnet test` и Docker-образ не должны зависеть от VS-проектов.
- **Rationale**: Явный запрос пользователя; первоначальное предложение (скрипты) отвергнуто после проверки документации Microsoft: VS 17.11+/2026 умеет многопроектный запуск штатно. Поправка — `new or changed scope` (hard gate), подтверждена пользователем.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/plan.md`
- 2026-10-06 — route Task 11: critical, dispatch HEAD d3466b4

## 2026-10-06 — Волна 9: flagged choices Task 11 (запуск из Visual Studio, AC-16)

- **Decision**: Приняты: `web/web.esproj` (JavaScript SDK 1.0.5984942, закреплён, npm install/build из MSBuild отключены), compose-проект БД (`competency-dev`, том `competency-dev-pgdata`, порт 15432), `Competency.slnLaunch` с профилями «Все сервисы» и «Только API» (оба включают БД), профиль `Competency.Api` в `launchSettings.json` (dev-only значения), ожидание БД до 60 с только в Development (`MigrateDatabaseAsync(waitForDatabase)`), `DependencyAwareStart` не используется, Edge как браузер профиля. `commands.yaml` не меняется: `dotnet build/test Competency.slnx` остаются чистыми (0 предупреждений), `.dcproj`/`.esproj` для CLI — no-op; Docker-образ не затронут.
- **Rationale**: В пределах AC-16; новые dev-only MSBuild-SDK приняты пользователем. Не проверено без IDE: появление профиля в списке запуска, привязка `DebugTarget`, открытие браузера, поведение контейнера после остановки отладки — проверит пользователь по F5. Компоненты VS (Docker Tools, JavaScript/TypeScript, Node.js) установлены (ранее ошибочно сообщалось об их отсутствии).
- **Affected docs**: `web/web.esproj`, `deploy/dev/`, `Competency.slnx`, `Competency.slnLaunch`, `src/Competency.Api/Properties/launchSettings.json`

## 2026-10-06 — Ручная проверка UI: название «Калибр», логотип, правки интерфейса

- **Decision**: Название продукта — «Калибр» (подзаголовок «Компетенции и карьерный рост»), выбрано пользователем; логотип пользователя (`D:\Downloads\Logo.png`) размещён в `web/public/brand/` (уменьшенные копии 64 и 256 px, оригинал 567 КБ не поставляется), в шапке — на светлой плитке (тёмный низ логотипа сливается с тёмной шапкой, контраст 1.44:1), на экране входа и как favicon. Исправлена подсветка меню пользователя в шапке (первая правка не сработала из-за специфичности селектора antd (0,4,0); вторая переопределяет CSS-переменные кнопки, проверено по computed styles в браузере: белый текст, фон — белый 12%). Фильтры списка пользователей расширены (перекрытия значка сброса и стрелки воспроизвести не удалось, правка раскладочная). Кандидаты в `design-md-delta`: hover/active/focus на инверсной поверхности, плитка логотипа, название бренда.
- **Rationale**: Явные замечания пользователя при ручной проверке на impl assessment; бренд выбран пользователем. Профили `Competency.slnLaunch` пользователь переименовал в VS (Everything/API, Chrome) — правка пользователя, не коммитилась.
- **Affected docs**: `web/src/app/`, `web/src/features/auth/`, `web/public/brand/`, `web/index.html`

## 2026-10-06 — Поправка scope: AC-17 раздел «Оргструктура» как иерархия карточек, Task 12 (волна 10)

- **Decision**: Раздел «Оргструктура» переделывается в иерархию карточек двух видов (подразделение, сотрудник) с вертикальной вложенностью и связями; в карточке подразделения сначала дочерние подразделения, затем сотрудники. Выбор пользователя: вертикальная вложенность (не горизонтальный оргчарт), свёрнуто по умолчанию с подгрузкой сотрудников страницами при раскрытии, действия на карточках, правая панель деталей и `antd Tree` убираются. AC-13 сужен (раскладка оргструктуры вынесена в AC-17). Backend не меняется.
- **Rationale**: Явный запрос пользователя после ручной проверки; изменение критерия — `new or changed scope` (hard gate), подтверждено ответами пользователя. Масштаб 10 000+ сотрудников требует ленивой подгрузки.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/plan.md`
- 2026-10-06 — route Task 12: critical, dispatch HEAD 967160c

## 2026-10-06 — Волна 10: flagged choices Task 12 (иерархия карточек, AC-17)

- **Decision**: Принято: решение пользователя «все сотрудники подразделения сразу» (без «Показать ещё»): один `useQuery` на подразделение собирает страницы по 200 (лимит API) параллельно; счётчик сотрудников и руководитель только на раскрытых карточках; карточка сотрудника — простая разметка, не antd Card (стоимость 2000 карточек: 330 мс против 950 мс); при поиске предки показывают только совпавшие дочерние подразделения без загрузки сотрудников; `EmployeeList` потерял осиротевший prop `unitId` (отдельный коммит defd0e8); `useUnitActivation` возвращает стабильный callback; после «Перенести» карточка не прокручивается к новому месту. Кандидаты в `design-md-delta`: иерархия карточек с коннекторами, карточка подразделения, компактная карточка сотрудника, элемент раскрытия, подсветка поиска, выделенная карточка.
- **Rationale**: В пределах AC-17; новых зависимостей нет. Замеры на синтетике (1 000 подразделений, 10 000 сотрудников, замоканный API): первая карточка ≈ 1 с, подразделение из 450 сотрудников — 3 запроса, 450 карточек за 82 мс, из 2 000 — 10 запросов, 484 мс. Известный риск: одно подразделение с 10 000 сотрудников ≈ 110 000 узлов DOM (≈ 2 с), windowing нет; `invalidateOrgStructure` перезапрашивает списки всех раскрытых подразделений.
- **Affected docs**: `web/src/features/org-structure/`, `web/src/styles.css`

## 2026-10-06 — Поправка scope: у подразделений нет дат действия (AC-9), убрана раскрывающаяся информация о подразделении, Task 13 (волна 11)

- **Decision**: По решению пользователя у подразделений нет `valid_from`/`valid_to` (AC-9 сужен; отступление от `docs/Постановка.md` §4 «даты действия»); с карточки подразделения убирается кнопка «i» и блок «Путь и сводка» (эндпойнты `/path` и `/summary` остаются по AC-11). Удаление колонок — корректирующей миграцией, а не правкой применённой миграции `OrgStructure` (она уже применена на dev-БД пользователя).
- **Rationale**: Явный запрос пользователя при ручной проверке; изменение критерия и публичного контракта — `new or changed scope` и `public contract change` (hard gates), подтверждено пользователем.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/plan.md`
- 2026-10-06 — route Task 13: critical, dispatch HEAD 9b7fef4

## 2026-10-06 — Волна 11: Task 13 (без дат действия подразделений, AC-9)

- **Decision**: Приняты: контракт и UI в одном коммите (`d39efba`), так как `schema.d.ts` без UI и UI без `schema.d.ts` не проходят `tsc` (`code-style.md` §19); корректирующая миграция `RemoveOrgUnitValidity` (Down возвращает колонки пустыми и CHECK `valid_to >= valid_from`, данные Down теряет); запросы, всё ещё присылающие `validFrom`/`validTo`, молча игнорируются (поведение `System.Text.Json`); `docs/Постановка.md:150` и spike-заметки `postgres-image-18.6-trixie.md` упоминают даты — обновить на design-promote.
- **Rationale**: В пределах AC-9 после поправки; миграция проверена на scratch-PostgreSQL (данные сохраняются, Up/Down/Up чисто, CHECK восстанавливается), API smoke и аудит без полей дат.
- **Affected docs**: `src/Competency.OrgStructure/`, `src/Competency.Platform/Migrations/20261006153445_RemoveOrgUnitValidity.cs`, `openapi/openapi.json`, `web/src/features/org-structure/`

## 2026-10-06 — Поправка scope: одинаковая информация на карточке подразделения (AC-17), Task 14 (волна 12)

- **Decision**: Карточка подразделения показывает одну и ту же информацию (название, статус, число дочерних подразделений, число сотрудников, руководитель) в свёрнутом и раскрытом виде. Данные приходят в узлах `GET /api/v1/org-units/tree` (имя руководителя и число прямых сотрудников, зависящее от роли: пользователь — только работающие, администратор — все), без запросов на каждую карточку. Это расширение публичного контракта (добавляются поля).
- **Rationale**: Явный запрос пользователя при ручной проверке (`new or changed scope`, `public contract change`). Раньше число сотрудников и руководитель были только у раскрытых карточек (ленивая загрузка).
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/plan.md`
- 2026-10-06 — route Task 14: critical, dispatch HEAD 510a8c6

## 2026-10-06 — Поправка scope: e-mail у учётной записи, вход по e-mail, без табельного номера и страницы сотрудников, Task 15 и 16 (волны 13–14)

- **Decision**: (1) Отдельной страницы сотрудников нет: сотрудник открывается и правится в модальном окне с карточки; кнопка «Добавить сотрудника» на странице оргструктуры и в карточке подразделения (подразделение подставляется). (2) Вместо «Добавить корневое подразделение» — «Добавить подразделение» с необязательным родителем (подставляется при добавлении из карточки). (3) Табельный номер удаляется целиком. (4) E-mail — обязательный уникальный (без учёта регистра) атрибут учётной записи, а не сотрудника; вход по e-mail (имени пользователя отдельно нет); e-mail администратора и пароль — параметры развёртывания (`Bootstrap__AdminEmail`/`Bootstrap__AdminPassword`). У сотрудника e-mail нет; в карточке показывается e-mail его учётной записи, если она есть. Данные табельных номеров и e-mail сотрудников в локальной БД при миграции теряются (подтверждено пользователем).
- **Rationale**: Явные запросы и ответы пользователя при ручной проверке; изменение критериев AC-6, 7, 10, 11, 12, 13, 17 и публичного контракта (`new or changed scope`, `public contract change`). Исправляет модель: e-mail принадлежит аккаунту, а не записи оргструктуры.
- **Affected docs**: `.asd/sprints/001-project-init-org-structure/sprint.md`, `.asd/sprints/001-project-init-org-structure/plan.md`

## 2026-10-06 — Волна 12: Task 14 принят

- **Decision**: Task 14 принят: узлы `GET /api/v1/org-units/tree` содержат `headName` и `employeeCount` (отдельный тип `OrgUnitTreeNodeResponse`, один агрегат без N+1, видимость по роли как в списке сотрудников: пользователь — только работающие, администратор — все); карточка показывает одну и ту же информацию в свёрнутом и раскрытом виде. Проверено на scratch-PostgreSQL (1 000 подразделений, 10 000 сотрудников): число сотрудников и имя руководителя совпадают со списком для обеих ролей, p95 `/org-units/tree` ≈ 12 мс.
- **Rationale**: В пределах AC-17; без изменения схемы. Debug-сборка решения блокируется запущенной сессией Visual Studio пользователя (dll в `src/Competency.Api/bin/Debug`), devs собирают `-c Release`; перед impl-review Debug-сборку нужно повторить.
- **Affected docs**: `src/Competency.OrgStructure/`, `web/src/features/org-structure/`

- 2026-10-06 — route Task 15: critical

## 2026-10-06 — Волна 13: Task 15 принят (e-mail у учётной записи, вход по e-mail, без табельного номера)

- **Decision**: Принято: одна корректирующая миграция `EmailOnUserAccount` (backfill существующих учётных записей `<user_name>@local.invalid`, `Bootstrap__AdminEmail` существующему администратору не применяется — без скрытых правок; Down возвращает колонки сотрудников с placeholder-значениями, табельные номера и e-mail не восстанавливаются); `UserName` = `Email` через `AppUser.SetEmail` (поля Identity остаются внутренними); e-mail сотрудника в ответах — e-mail привязанной учётной записи (заблокированная учётка не скрывается, без учётной записи `null`), чтение через keyless-сущность `EmployeeAccount` (`ToSqlQuery` по таблице `users`) и коррелированный подзапрос; формат e-mail проверяется на логине (старое имя `admin` → 400); `q` по сотрудникам ищет по ФИО и должности; `Bootstrap__AdminUserName` удалён из кода и манифестов. Известное ограничение: при гонке дублей e-mail 409 от уникального индекса приходит без русского текста и `errors.email` (проверка до сохранения даёт русский текст); можно добавить `catch (DbUpdateException)` позже.
- **Rationale**: В пределах AC-6, 7, 8, 10, 11, 12 после поправки; проверено на scratch-PostgreSQL (миграция Up/Down/Up на данных, уникальность без учёта регистра, смоук API, 10 000 сотрудников без регрессии производительности). Frontend минимально обновлён механически в пределах коммитов для зелёного lint; полноценный UI — Task 16.
- **Affected docs**: `src/Competency.UserManagement/`, `src/Competency.OrgStructure/`, `src/Competency.Platform/Migrations/`, `deploy/`

- 2026-10-06 — route Task 16: critical
