---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

<!--
Format rules (parser-critical): see .asd/templates/t_plan.md and .asd/rules/sprint-lifecycle.md "Plan file format".
-->

## Overview

Спринт `standard`. Критерии — PRD спринта `design/prd.html` (AC-N.k; N — номер критерия `sprint.md` AC-1…AC-34), продвинутые в постоянные требования `docs/product/requirements/desk.html`, `career-framework.html`, `user-management.html`, `org-structure.html`, `audit.html`. Решения — постоянные документы после design-promote: `docs/architecture/desk.md`, `career-framework.md`, `org-structure.md`, `user-management.md`, `audit.md`, `stack.html`; UX — `docs/ux/desk.html`, `app-shell.html`, `career-framework.html`, `user-management.html`, `DESIGN.md`. Решения пользователя — `decisions-log.001.md`…`decisions-log.005.md`.

Порядок: сначала сервер по слоям (связь «должность — уровень» и контракт 3.1.0 → общее правило публикации → методы чтения сценариев → письма и модуль `desk` с источниками модулей → источник карьерной модели и замер на сиде 10 000), затем SPA (каркас и стол, раздел «Карьерная модель», экраны сценариев). Правила проекта и память dev (AC-19…AC-23) — первой волной.

Тексты интерфейса и писем — каталоги в постоянных UX-документах (`docs/ux/desk.html` «Каталог текстов», макеты `docs/ux/app-shell.html`, `career-framework.html`, `user-management.html`). Отдельного утверждения текстов и макетов на gate plan нет: AC-21, AC-15.12 и AC-17.3 отозваны пользователем 2026-10-13, правило «User-visible texts approval» удаляется (Task 1).

Решения плана, которые нужно подтвердить на gate:
- П-1. Матрица по черновому треку своего подразделения (AC-8.13) разрешена независимо от автора трека: трек теперь всегда создаётся черновиком (в том числе хранителем), и правило career-framework «черновая запись используется только в матрицах подразделений её автора» для трека уступает AC-8.13 (иначе матрицу по треку, созданному HR, нельзя начать до его утверждения).
- П-2. Доступность «Доработать», мастера и ссылок страницы матрицы определяется только `actions` версии с сервера (чей ход, `MatrixWorkflow.StateAsync`), а не ролью в SPA: руководитель, сменивший прежнего автора, получает путь к отправке, прежний автор — нет (замечание design-review iter-05).

## Definition of Done
Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.
Дополнительно для спринта: замер `GET /api/v1/desk/count` (p95 ≤ 300 мс), `GET /api/v1/desk` (p95 ≤ 600 мс), не больше 25 SQL-команд на запрос на сиде `tests/perf/seed-10k.sql` для администратора, HR с закреплением на корне, руководителя верхнего подразделения и сотрудника записан в `test-plan.md`; `info.version` OpenAPI — `3.1.0`.

### Task 1: Правила проекта из ретроспектив
Material risk: none
- [x] AC-19 (`003-positions-career-competency#A-6`): в `.asd/project/custom-coding-rules.md` раздел с правилом: перед коммитом UI dev сверяет каждый изменённый экран с таблицей компонентов `docs/ux/DESIGN.md` — не больше двух видимых действий строки, свой текст пустого состояния, токены всплывающих меню темы
- [x] AC-20 (`003-positions-career-competency#P-2`): в `.asd/project/custom-coding-rules.md` раздел с правилом: каждый изменяющий UI-поток блокирует правку на время сохранения, не даёт переходов при несохранённых правках, а после 412 перечитывает запись, показывает её пользователю, и следующее сохранение отправляет новую версию; автоматического повтора нет
- [x] удалить из `.asd/project/custom-common-rules.md` раздел «User-visible texts approval» (решение пользователя 2026-10-13, AC-21 отозван); в `docs/architecture/tech-reference/dnd-kit-6.3.1.md` ссылку «итоговые тексты — в plan.md «Тексты интерфейса»» заменить на «итоговые тексты — в UX-документе раздела»
- [x] AC-23 (`002-mail-invite-password-reset#A-2`): в `.asd/project/custom-common-rules.md` раздел с правилом: агенты меняют существующие файлы своей памяти точечной правкой и никогда не перезаписывают файл памяти целиком

### Task 2: Рецепты проверки в памяти dev (исполнитель — asd-dev-critical, владелец памяти)
Material risk: none
- [x] AC-22 (`003-positions-career-competency#A-4`): в `.claude/agent-memory/asd-dev-critical/` рецепты scratch-БД, запуска API и проверки на реальном backend (`project_migration-scratch-verification-recipe.md`, `project_scratch-process-control.md`, `project_scratch-host-without-postgres.md`, `project_docker-spike-recipe.md`, `project_web-cdp-real-backend-smoke-facts.md`, `project_full-verification-cycle-facts.md`, `project_vs-debug-build-lock-and-release-config.md`, `project_typed-problem-extension-facts.md`) пометить «только impl-test / tester» или удалить; в impl проверка ограничена сборкой, линтером и контрактом; строки `MEMORY.md` согласовать; правки точечные (AC-23), без id спринта

### Task 3: Снимок оргструктуры для источников стола
Material risk: change: lock ordering — снимок читается одним набором запросов вне и внутри разделяемой блокировки дерева
- [x] `IEmployeeDirectory.FindOrgSnapshotAsync` и `OrgSnapshot` с методами-правилами (`org-structure.md`): дерево действующих подразделений, руководители, работающие сотрудники, перечни должностей — постоянное число запросов; `EmployeeStatus` получает `OrgUnitId`, `PositionId` (AC-16, AC-26…AC-28, AC-4…AC-6)
- [x] реализация в `EmployeeDirectory.cs` без изменения публичного API `/api/v1`

### Task 4: Связь «должность — уровень», готовность трека, контракт 3.1.0
Material risk: change: migration — новая таблица `career_level_positions` без заполнения, `ContractVersion` 3.1.0
Material risk: change: public contract — методы связей, `POST /tracks` всегда черновик, проблема полноты `LevelWithoutActivePosition`
Material risk: change: lock ordering — связи под блокировкой справочников 5003001 после разделяемой блокировки дерева, гонки снятия последней должности
- [x] `CareerLevelPosition.cs`, конфигурация EF, миграция `CareerLevelPositions` (только схема, `Down` удаляет таблицу); ключ `(level_id, position_id)`, уникальный `(track_id, position_id)`, FK `RESTRICT` (AC-24.1…AC-24.10)
- [x] `GET /tracks/{id}/positions`, `GET /tracks/{id}/available-positions`, `POST /levels/{id}/positions`, `DELETE /levels/{id}/positions/{positionId}` с `If-Match` уровня, правами правки трека и событиями журнала `CareerLevelPosition` (AC-24, AC-31 сервер; `career-framework.md` «Методы связей»)
- [x] одна функция готовности трека (`career-framework.md` «Готовность трека»): утверждение и «Вернуть в работу» трека, запрет снятия последней действующей должности уровня действующего трека, проблема полноты `LevelWithoutActivePosition` при отправке, согласовании и принятии правок (AC-25.1…AC-25.7)
- [x] `POST /tracks` всегда создаёт черновик; удаление уровня или трека удаляет их связи (`career-framework.md` «Переход на контракт API 3.1.0»)
- [x] `ContractVersion` = `3.1.0` в `src/Competency.Api/Program.cs`; перегенерация `openapi/openapi.json` и `web/src/api/schema.d.ts` (`gen-api`, `check-api`)

### Task 5: Общее правило публикации с каскадом
Material risk: change: workflow state machine — каскадная публикация записей и версий, публикующие действия, автосогласование HR
Material risk: change: public contract — `PublicationRefusal`, предпросмотр, `?cascade` и 412 с `publication`, `POST` черновиком, `PUT` 400
Material risk: change: security — право ответственного HR при публикации версии, видимость перечня
- [x] одна функция публикации с транзитивным замыканием черновых ссылок (версия → трек и уровни, компетенции, шкала, через действующие компетенции — их черновые ссылки; трек → направление, черновые уровни; компетенция → категория, направление, шкала), проверками права (`CanManage`; у версии — права ответственного HR подразделения матрицы), повтора названий, готовности трека и полноты версии; всё или ничего в транзакции действия; отказ — 409 `PublicationRefusal` (до 5 видимых + `moreCount`); журнал `.Approved` на каждую сущность со ссылкой на корень (AC-34.1…AC-34.4, AC-34.8, AC-34.9; `career-framework.md` «Публикация»)
- [x] предпросмотр `GET /matrix-versions/{id}/publication` (`publishingActions`, `entries`, `blockers`, `moreCount`, `cascadeTag`) и `GET /{directions|tracks|categories|competencies|scales}/{id}/publication` (AC-34.5, AC-34.6)
- [x] `?cascade=<tag>`: публикующее исполнение (переход версии, решённый под блокировками; `approve`/`activate` записи с непустым каскадом) без совпадающего тега — 412 с полем `publication`, ничего не меняется (AC-34.7)
- [x] запись хранителем: `POST` направления, категории, компетенции, шкалы со ссылками на черновики создаётся черновиком (`status` в 201); `PUT` действующей записи с новой черновой ссылкой — 400 с ошибкой поля (`ReferenceProblemAsync`/`CatalogStore.IsUsableAsync`) (AC-34.10, AC-34.11)
- [x] `POST /matrices` принимает черновой трек своего подразделения, независимо от автора трека (AC-8.13, решение плана П-1); публикация версии по такому треку — каскадом (AC-8.14)
- [x] перегенерация `openapi/openapi.json` и `web/src/api/schema.d.ts`

### Task 6: Методы чтения сценариев и шаг мастера
Material risk: change: migration — колонка `matrix_versions.builder_step`
Material risk: change: security — `/org-units/{id}/setup` только администратору, `/me/place` только о себе, `/matrices` записи — только читаемые версии
- [x] миграция `matrix_versions.builder_step` (`smallint`, NULL, без журнала); `PUT …/content?builderStep=2..6`, `MatrixVersionResponse.builderStep` (AC-8.11)
- [x] `GET /matrix-versions/{id}/completeness` (коды проблем, та же функция, что у переходов) и `GET /matrix-versions/{id}/changes` (разница с действующей опубликованной версией) (AC-8.9, AC-10.2, AC-10.3)
- [x] `GET /{directions|tracks|levels|categories|competencies|scales}/{id}/matrices` — только читаемые (`MatrixRoute.ReadableAsync`); `readiness` у `/drafts` («утверждать рано») (AC-11.2, AC-11.3, AC-27.2)
- [x] `GET /unused-entries?kind=` — неиспользуемые записи каталога, HR и администратор (AC-27.5…AC-27.8, AC-32)
- [x] `GET /org-units/{id}/setup` (руководитель, HR свой/унаследованный, учётные записи с состоянием и `version`, перечень, строки «готово / не хватает / ждёт») — `IUserDirectory`: `UserStatus.Version`, состояние приглашения (AC-12.1…AC-12.5)
- [x] `GET /me/place` (подразделение, должность, руководитель, ответственный HR, треки должности, текущий и следующие уровни с требованиями из `snapshot` опубликованной матрицы своего подразделения, `noTrack`) (AC-14.1…AC-14.12, AC-29)
- [x] перегенерация `openapi/openapi.json` и `web/src/api/schema.d.ts`

### Task 7: Письма-напоминания
Material risk: change: lock ordering — получатели и отправка только после `CommitAsync`, вне блокировок
- [x] `CareerMailQueue` (ограниченный `Channel` на 1 000, фоновый обработчик, отбрасывание с предупреждением в лог) и получатели по таблице `career-framework.md` (`submit`, согласование шага 1, `accept-changes`, `return`, черновая запись руководителя); не тому, кто совершил действие; проверка `UserStatus.IsActive` и работающего сотрудника при отправке (AC-15.1…AC-15.12)
- [x] тексты писем — константы по `docs/ux/desk.html` (письма m-mail, «Каталог текстов»); ссылки `App:PublicBaseUrl` + `/desk/review/{versionId}`, `/desk/rework/{versionId}`, `/desk/drafts/{kind}/{entryId}` (AC-15)
- [x] `Mail.SendFailed` с видами `MatrixAwaitingApproval`, `MatrixReturned`, `DraftAwaitingApproval` (`audit.md`; требования audit AC-15)

### Task 8: Модуль `desk` и источники оргструктуры и учётных записей
Material risk: change: public contract — новые методы `/api/v1/desk*`
Material risk: change: security — стол только о себе, источники ограничивают видимость ролями
- [x] контракт в `src/Competency.Platform/`: `IDeskSource`, `DeskFinding`, `DeskKind` (по члену на вид AC-4…AC-7, AC-26…AC-29), `DeskObjectType` (`desk.md`)
- [x] модуль `src/Competency.Desk/` (проект в `Competency.slnx`, регистрация в хосте): слияние находок в шесть шагов, таблицы поглощения и порядка причин (PRD таблицы 1–2, AC-30), группы, счётчик; `GET /api/v1/desk`, `GET /api/v1/desk/items?group=&limit=` (потолок 2 000), `GET /api/v1/desk/count`; предупреждение в лог при вычислении дольше 1 с (AC-2, AC-3, AC-16, AC-30)
- [x] источник org-structure (только администратору): подразделение без руководителя, пустой перечень при сотрудниках, должность сотрудника вне перечня, неактивная должность у работающих, должность без перечней и сотрудников, пустое подразделение (AC-6.2, AC-28.1…AC-28.4, AC-28.7)
- [x] источник user-management (только администратору): приглашение не принято (истекла, отменена, очищена, меньше 2 суток), учётная запись «пользователь» без привязки, руководитель или HR без действующей учётной записи (AC-6.5…AC-6.8, AC-28.5, AC-28.6)
- [x] перегенерация `openapi/openapi.json` и `web/src/api/schema.d.ts`

### Task 9: Источник стола карьерной модели и замер на сиде 10 000
Material risk: change: perf — вычисление стола на каждый запрос, бюджет p95 и число SQL-команд
Material risk: change: security — неопубликованное попадает только к тем, кто его читает
- [x] источник career-framework (`career-framework.md` «Источник стола»): версии (возвращена, черновик, ждёт шага 1 / HR, ваша на согласовании, задержка шага 1 дольше 7 дней), подразделения без опубликованной матрицы и без ответственного HR (с причиной «учётная запись не может действовать»), без руководителя для HR, черновики на утверждение и черновики автора, проверки связей и треков (должность без трека с числом сотрудников, трек без должностей, уровень без должности, трек без уровней или с одним), пересмотр матрицы (уровни изменились, неактивные записи, компетенция изменилась), неиспользуемые записи для HR, должность сотрудника без трека (AC-4, AC-5, AC-6.1, AC-6.3, AC-6.4, AC-26, AC-27, AC-29); права и видимость — те же функции, что у действий (`ProcessRoles`, `MatrixRoute`, `CatalogViewer`)
- [x] наборные запросы с постоянным числом обращений (снимок оргструктуры Task 3)
- [x] сид `tests/perf/seed-10k.sql` (10 000 работающих сотрудников, дерево, закрепления, треки, матрицы) по `.asd/project/custom-coding-rules.md` «Verification depth in impl» — создаётся этой Task как первой с perf-риском

### Task 10: Каркас приложения и рабочий стол
Material risk: change: security — возврат после входа только на путь внутри приложения (open redirect)
- [x] меню: «Рабочий стол» первым со счётчиком (`accent` + `surface-inverse`, `rounded-full`, «99+», без метки при 0, до ответа и при ошибке), группа «Справочники» (`featureContract.NavItem.group`), «Администрирование» без изменений; корень → `/desk` вместо `LandingPage` (AC-1, AC-3; `docs/ux/app-shell.html`)
- [x] счётчик в клиенте — ключ `['desk']`: сброс после любой успешной мутации (`MutationCache.onSuccess` без возврата Promise), перечитывание при смене адреса и возврате фокуса (`tech-reference/tanstack-react-query-5.104.1.md`) (AC-3.4…AC-3.6)
- [x] возврат на цель после входа: `returnTo` в `client.ts` и `LoginPage.tsx`, проверка разобранного URL (тот же origin, путь с одного «/», не страницы входа), подсказка на экране входа (AC-15.13, AC-15.14; `user-management.md`)
- [x] `web/src/features/desk/`: `routes.ts`, страница стола — блоки «Ожидают вашего действия» (группы, строки-объекты с причинами, давность, «Показать ещё 20», усечение по `isReference`), «К сведению» свёрнутой группой, «Всё сделано», «Что можно сделать» по ролям сессии; «Отправить приглашение повторно» на столе с итогом и окном «Письмо не отправлено» (AC-2, AC-6.8, AC-7; `docs/ux/desk.html` m-desk-1…m-desk-3)

### Task 11: Раздел «Карьерная модель» после правил публикации
Material risk: none
- [x] страница матрицы без публикующих переходов: ссылки «Согласовать версию», «Доработать возвращённую версию», «Перейти к отправке в мастере» по `actions` версии (решение плана П-2), не больше двух видимых действий и «…» (AC-34.12; `docs/ux/career-framework.html` m-matrix)
- [x] окно трека: столбец «Должности» и окно «Должности уровня» (AC-24, AC-31; m-track-form)
- [x] утверждение и «Вернуть в работу» записи через предпросмотр: пустой перечень — сразу, иначе окно с перечнем; создание записи черновиком — окно «создана черновиком» с «Утвердить вместе с N записями»; 412 «перечень изменился» (3б) отдельно от 412 по `If-Match`; ошибка поля «Выбрана черновая запись — сначала утвердите её» (AC-34.5…AC-34.7, AC-34.10, AC-34.11; m-publish-confirm)

### Task 12: Экраны сценариев карьерной модели
Material risk: none
- [ ] «Построить матрицу компетенций» — мастер из 6 шагов: трек (свой или вышестоящего; создание чернового трека с уровнями и должностями), компетенции (создание черновой), требования, формула и пороги, проверка полноты с «что исправить», отправка с перечнем каскада; сохранение шага `builderStep`, продолжение с шага, вход `?step=3|5` из «Доработать», предвыбор `?unitId=&trackId=` (AC-8; m-wiz-1…m-wiz-4)
- [ ] «Доработать возвращённую версию»: комментарий и правки согласующего, три решения с подтверждением и перечнем каскада для публикующего «Принять правки», недоступность при замечаниях полноты (AC-9; m-rework)
- [ ] «Согласовать версию»: разница с опубликованной, полнота, перечень каскада, «Согласовать» / «Согласовать и опубликовать» / «Вернуть с правками» (AC-10; m-review)
- [ ] «Утвердить черновые записи»: очередь «список слева — запись справа», автор, подразделение, матрицы записи, «утверждать рано», «Поправить и утвердить», «Утвердить вместе с N записями», следующая запись сама (AC-11; m-drafts)
- [ ] «Связать должности с треком» и «Привести каталог в порядок» (AC-31, AC-32; m-link, m-catalog)

### Task 13: Экраны сценариев оргструктуры и «Моё место»
Material risk: none
- [ ] «Подключить подразделение»: строки «готово / не хватает / ждёт» с действием на месте (руководитель, HR, перечень, учётные записи) (AC-12; m-connect)
- [ ] «Принять сотрудника»: карточка с «Добавить должность», затем по желанию учётная запись с приглашением, итог с частичным результатом (AC-13; m-onboard)
- [ ] «Навести порядок в оргструктуре»: очередь находок администратора с действием на месте (AC-33; m-org)
- [ ] «Моё место в компании»: подразделение, должность, руководитель, HR, треки, текущий и следующие уровни с требованиями, без привязки — объяснение (AC-14, AC-29; m-myplace)

Оркестратор перед impl-test волны 6 (после Task 9): прогон замера бюджета стола на сиде — поручение asd-tester в impl-test, результат в `test-plan.md` (DoD этого плана).

## Risks
- Контракт 3.1.0 меняет поведение существующих методов (`POST /tracks`, каскад, обязательный тег, полнота с должностями): после выката отправка новых версий по существующим трекам невозможна, пока HR не свяжет должности с уровнями — принято пользователем (AC-25).
- Правки контракта ломают типы тестовых данных web и тестов backend: если gate impl красный только из-за тестов, сломанных намеренной правкой, оркестратор сразу запускает tester-цепочку их восстановления (ретро 003#A-3).
- Бюджет стола: при невыполнении — короткая память результата на пользователя со сбросом после его мутаций (`desk.md`, пересмотр); решение — в impl-review по замеру.
- Письма из очереди теряются при перезапуске процесса — принятый риск (ADR-5).

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | Task 1, Task 2, Task 3 |
| 2 | Task 4 |
| 3 | Task 5 |
| 4 | Task 6 |
| 5 | Task 7, Task 8 |
| 6 | Task 9 |
| 7 | Task 10, Task 11 |
| 8 | Task 12 |
| 9 | Task 13 |

- Task 4, 5, 6 — каждая меняет `src/Competency.CareerFramework/`, миграции и перегенерирует `openapi/openapi.json` / `web/src/api/schema.d.ts`, поэтому по одной в волне; Task 5 опирается на готовность трека Task 4, Task 6 — на полноту и публикацию Task 4–5.
- Task 7 (career-framework, Audit) и Task 8 (Platform, Desk, Api, источники org-structure и user-management, OpenAPI) не пересекаются по путям; Task 8 опирается на снимок Task 3 и `UserStatus.Version` Task 6.
- Task 9 зависит от Task 8 (`IDeskSource`, `DeskKind`) и Task 3–6 (данные и функции правил).
- Task 10 (`web/src/app/`, `web/src/features/auth/`, `web/src/api/client.ts`, `web/src/features/desk/`) и Task 11 (`web/src/features/career/`) не пересекаются; обе опираются на схему после Task 8.
- Task 12 и Task 13 добавляют маршруты в `web/src/features/desk/routes.ts`, поэтому в разных волнах; обе опираются на компоненты Task 10–11.

## Out of scope
- Отдельный экран «Пересмотреть матрицу» и «Моё подразделение» (решение пользователя 2026-10-12).
- Outbox и гарантированная доставка писем; настройка состава стола и отключение писем.
