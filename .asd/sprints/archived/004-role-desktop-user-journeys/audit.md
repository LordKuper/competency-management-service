---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

Аудит — asd-architect, HEAD `42fa2fd`. Отсутствующий необязательный раздел означает пустой набор находок, а не невыполненную проверку (`.asd/rules/sprint-lifecycle.md` «Audit phase»). Разделов «Related open stubs» и «Documentation migration plan» нет: таблица `.asd/project/stubs.md` пуста, маркеров TODO/FIXME в `src` и `web/src` нет; документов вне формата ASD в затронутых областях нет (`docs/Постановка.md` — справочный источник, §31–§34 вне скоупа).

## Scope reference
[sprint.md](./sprint.md)

## Touched areas

Рабочий стол, навигация, счётчик (AC-1…AC-3, AC-16, AC-17) — SPA:
- `web/src/app/LandingPage.tsx`: сейчас — редирект в первый раздел роли (`navItemsFor(role)[0]`); станет «Рабочим столом».
- `web/src/app/router.tsx`: index-маршрут `LandingPage`; фичи подключаются по соглашению `features/<name>/routes.ts`; обработчик 401 уводит на `/login` без запоминания цели.
- `web/src/app/featureContract.ts`: `NavItem.group` — только `"admin"`, признака счётчика нет.
- `web/src/app/AppShell.tsx`: `menuItems` строит одну подгруппу «Администрирование»; шапка — место счётчика (AC-3). `AppShell.test.tsx` закрепляет «Оргструктуру» первым пунктом.
- `web/src/features/auth/LoginPage.tsx:28`: после входа всегда `navigate("/")` — ссылка из письма (AC-15) теряет цель.
- `web/src/api/queryClient.ts`: общий кэш TanStack — канал обновления счётчика.
- Новая фича `web/src/features/<desktop>`: стол и экраны сценариев AC-8…AC-14.

Ожидаемые действия и сценарии карьерной модели (AC-4, AC-5, часть AC-6, AC-8…AC-11, AC-14, AC-15) — `career-framework`:
- `src/Competency.CareerFramework/ProcessRoles.cs` (`HeadedUnitsAsync`, `HrUnitsAsync`, `ResponsibleHrAsync`, `HeadAccountAsync`), `CatalogViewer.cs` (`Keeps`/`CanManage`).
- `MatrixRoute.cs` (`AwaitingAsync`, `TurnAsync`, `ReadableAsync`, `CanReadAsync`), `MatrixWorkflow.cs` (`StateAsync`/`ActionsOf`, `TransitAsync` — письма по переходам после `CommitAsync`).
- `MatrixApprovalEndpoints.cs` (`/approvals`), `CatalogDraftEndpoints.cs` (`/drafts`), `MatrixEndpoints.cs` (`CreateAsync`, `ReferenceProblemAsync` — трек и направление `Active`), `MatrixCatalog.cs` (`CompletenessProblemsAsync`, `DraftsAsync`, `IsUsable`), `MatrixChange.cs` (`Between`), `MatrixVersionEndpoints.cs` (`/variants/{n}/diff`), `CatalogEndpoints.cs` (создание черновика — повод письма HR), `CatalogStore.cs` (`MatricesUsing`, `UnitNamesAsync`, `UserNamesAsync`), `OrgUnitHrEndpoints.cs`, `CareerFrameworkModule.cs`; `web/src/features/career/*`.

Оргструктура (AC-6 «без руководителя», AC-12…AC-14) — `org-structure`:
- `src/Competency.OrgStructure/OrgUnit.cs` (`HeadEmployeeId`), `OrgUnitEndpoints.cs` (`/tree`), `EmployeeEndpoints.cs`, `OrgUnitPositionEndpoints.cs` (`quick-add`).
- `IEmployeeDirectory.cs`: нет метода «все подразделения» или «без руководителя».
- `EmployeeAccount.cs` / `OrgStructureEntityConfiguration.cs:17`: keyless-запрос отдаёт только `email`, `is_blocked`, без признака «приглашён».
- `web/src/features/org-structure/*` (`UnitCard`, `UnitHrModal`, `UnitPositionsModal`, `EmployeeForm`/`EmployeeModal`, `PositionSelect`).

Учётные записи (AC-6 «приглашение», AC-12, AC-13, получатели AC-15) — `user-management`:
- `src/Competency.UserManagement/AppUser.cs` (`IsInvited`, `LinkExpiresAt`, `LinkIssuedAt`, `LinkSecurityStamp`, `HasValidLink`); `AccountLinkOptions.cs`, `src/Competency.Api/appsettings.json:37-41` (`InvitationLifetime` = 7 дней).
- `UserEndpoints.cs` (`POST /users`, `resend-invitation`, `mailSent`); `UserListQuery.cs` — нет фильтра по `employeeId` и сроку ссылки.
- `IUserDirectory.cs`, `UserStatus.cs` (`IsActive` — фильтр получателей писем); `AccountMail.cs` — образец письма и `Mail.SendFailed`.
- `web/src/features/users/*` (`UserForm`, `UserModal`, `EmployeePicker`, `useSendUserMail`).

Платформа, журнал, контракт (AC-15, AC-18):
- `src/Competency.Platform/MailSender.cs` (синхронно, соединение на письмо, `false` при ошибке), `SmtpOptions.Timeout` = 15 с, `AppOptions.PublicBaseUrl`, `IAuditWriter.WriteAsync`.
- `Competency.Audit`: новые виды письма в `Mail.SendFailed`.
- `src/Competency.Api/Program.cs:10` (`ContractVersion = "3.0.0"`), `openapi/openapi.json`, `web/src/api/schema.d.ts`.

Тесты: `tests/Competency.Tests/Infrastructure/` (`MailCatcher`, `CareerScenarios`, `MatrixKit`, `ApiHost`); скрипта сида 10 000+ сотрудников нет.

Правила и память (AC-19…AC-23):
- `.asd/project/custom-coding-rules.md` — правил AC-19, AC-20 нет; `.asd/project/custom-common-rules.md` — правил AC-21, AC-23 нет.
- `.claude/agent-memory/asd-dev-critical/`: 23 файла упоминают scratch/CDP/реальный backend без пометки «только impl-test / tester»; рецепты как минимум в `project_migration-scratch-verification-recipe.md`, `project_scratch-process-control.md`, `project_scratch-host-without-postgres.md` («before impl-test»), `project_docker-spike-recipe.md`, `project_web-cdp-real-backend-smoke-facts.md`, `project_full-verification-cycle-facts.md`, `project_vs-debug-build-lock-and-release-config.md`, `project_typed-problem-extension-facts.md` и их строки в `MEMORY.md`.

## Existing docs found
- [subsystems.md](../../../docs/architecture/subsystems.md): четыре подсистемы — `org-structure`, `user-management`, `audit`, `career-framework`; Platform и хост не подсистемы; в диаграмме почту отправляет только `user-management → platform-core` («MailSender»).
- [career-framework.md](../../../docs/architecture/career-framework.md): роли процесса «вычисляются в момент запроса … и нигде не хранятся»; `/approvals` — версии, ждущие согласования текущим пользователем; `/drafts` — черновики, ожидающие его утверждения; ответ версии несёт `actions`; «опубликованных портов нет».
- [org-structure.md](../../../docs/architecture/org-structure.md): методы `IEmployeeDirectory`; блокировка дерева 5001001 берётся первой; `GET /org-units/tree` — плоский список с именем руководителя одним запросом; руководитель — работающий сотрудник подразделения.
- [user-management.md](../../../docs/architecture/user-management.md): «Приглашён» = `password_hash IS NULL`; ссылка годна, пока не истекла и stamp не менялся (её отменяют блокировка, смена роли, привязки или пароля; смена e-mail очищает); `InvitationLifetime` 7 дней; письма по действиям администратора — синхронно после фиксации, не дольше `Smtp:Timeout`; порт `IUserDirectory`, `UserStatus.IsActive`.
- [audit.md](../../../docs/architecture/audit.md): `Mail.SendFailed` через `WriteAsync`, причина — вид письма (`Invitation` или `PasswordReset`), без адреса; `WriteAsync` переживает откат вызывающего.
- [stack.html](../../../docs/architecture/stack.html) «Архитектурные принципы»: модульный монолит, CareerFramework → OrgStructure, UserManagement без обратных ссылок; «Почта: MailSender … вызывать после фиксации транзакции, не под блокировкой»; порядок блокировок дерево → справочники 5003001 → строка матрицы → администраторы → учётная запись; «короткие личные списки … отдаются целиком»; «Сервер — единственный источник истины … SPA берёт actions из ответа»; «Очереди и outbox … когда понадобятся».
- [Требования career-framework](../../../docs/product/requirements/career-framework.html): AC-14, AC-19 — черновик без подразделения утверждает любой HR; AC-36 — «направление и трек должны быть действующими»; AC-44 — автор версии — руководитель подразделения матрицы; AC-49…AC-58 — маршрут; AC-50 — без HR отправить нельзя; AC-61 — кто читает неопубликованную версию; non-goal «уведомления о согласовании».
- [Требования user-management](../../../docs/product/requirements/user-management.html): владеет шапкой и навигацией; AC-31 — «Оргструктура» и «Карьерная модель» видны всем, остальное — в «Администрирование»; non-goals «прочие уведомления, рассылки», «гарантированная доставка писем (outbox)».
- [Требования org-structure](../../../docs/product/requirements/org-structure.html): AC-29 — администратор действует с карточек; роли процесса не меняют видимость оргструктуры.
- [Требования audit](../../../docs/product/requirements/audit.html) AC-15: журналируется неудача отправки письма с указанием вида (приглашение или сброс пароля).
- [concept.html](../../../docs/product/concept.html): целевые пользователи — сотрудники, руководители, HR; видение со столом согласуется.
- [app-shell.html](../../../docs/ux/app-shell.html): корень → первый доступный раздел роли; «Оргструктура» — первый пункт; меню не сжимается в «…».
- [DESIGN.md](../../../docs/ux/DESIGN.md): «Shell and navigation» — горизонтальное меню, боковой панели нет; `rounded-full` для count badges; `accent` — только заливка на `surface-inverse`/`text-primary`; Steps описан для стадий волн; не больше двух действий в строке; пустое состояние отвечает «что, почему, что дальше».
- [design-system.html](../../../docs/ux/design-system.html), [accessibility.html](../../../docs/ux/accessibility.html): AA обеспечивается на уровне токенов — пара цветов бейджа счётчика должна быть токенизирована.
- [career-framework.html (UX)](../../../docs/ux/career-framework.html): вкладка «На согласовании» — «Ждут моего согласования» и черновики на утверждение.
- [org-structure.html (UX)](../../../docs/ux/org-structure.html): карточки подразделений и сотрудников, действия администратора, HR и перечень должностей из карточки.
- [user-management.html (UX)](../../../docs/ux/user-management.html): список «Пользователи», приглашение и повторное приглашение.
- tech-reference: antd 6.6.5, react-router 8.4.0, TanStack Query 5.104.1, MailKit 4.18.1, Mailpit 1.31.4 — нужные API покрыты, новых технологий скоуп не требует.
- [Постановка](../../../docs/Постановка.md) (не канон): §31 — каналы `IN_APP`/`EMAIL`/`MESSENGER`; §32–§34 — dashboard'ы по волнам.
- [sprint.md спринта 003](../archived/003-positions-career-competency/sprint.md): AC-9…AC-18, на них ссылаются AC-5 и AC-8.

## Contradictions
- C-1. sprint.md AC-8 («нужного трека нет — создать черновой трек с уровнями, не уходя из сценария»; «правила и права — AC-9…AC-18 спринта 003») vs канон career-framework AC-36 и `MatrixEndpoints.ReferenceProblemAsync` (матрица только по `Active` треку `Active` направления; `MatrixCatalog.IsUsable` допускает черновые уровни, компетенции, шкалу, категории, но не трек): сценарий упрётся в 400 до утверждения трека HR; winner=unsettled → user: версию матрицы можно строить и отправлять на согласование по черновому треку своего подразделения, но опубликовать (согласование HR) нельзя, пока трек не стал действующим; AC-8 уточнён. Требование «направление действующее» не меняется.
- C-2. sprint.md AC-2 («каждое действие … с давностью») vs данные: `EntityBase` хранит только `CreatedAt`; нет момента смены руководителя, закрепления HR, появления «нет опубликованной матрицы»; история — только в `audit_events` (читает только администратор). Давность есть у событийных видов (`matrix_version_decisions.at`, `CreatedAt` черновика, `link_issued_at`/`link_expires_at`); winner=unsettled → user: давность только у событийных видов; AC-2 уточнён.
- C-3. sprint.md AC-2 («переход прямо в экран сценария, где действие выполняется») vs AC-6 «приглашение не принято»: ни один сценарий AC-8…AC-14 не делает повторное приглашение произвольной учётной записи; winner=unsettled → user: кнопка «Отправить приглашение повторно» прямо в строке стола (существующий `resend-invitation`, итог по `mailSent`); AC-6 уточнён.
- C-4. sprint.md AC-20 («после 412 перечитывает запись и повторяет с новой версией») vs stack.html «Платформенное ядро» (устаревший ETag → 412) и sprint 003 AC-17 («а не молчаливую перезапись»): буквальный автоматический повтор перезапишет чужую правку; источник ретро — F9 `reviews/impl/wave-3/iter-01/external.md` (повторные сохранения слали устаревший `If-Match`), образец — `web/src/app/useEditBase.ts`; winner=stack.html (канон): правило формулируется «перечитывает запись, показывает её пользователю, следующее сохранение отправляет новую версию; автоматического повтора нет».
- C-5. docs/Постановка.md §31, §32–§34 vs sprint.md (счётчик и письмо; волны, dashboard'ы, push, мессенджер вне скоупа); winner=sprint.md (решения пользователя, `decisions-log.001.md` 2026-10-12).

## Existing implementation found
- `ProcessRoles.cs`: роли вычисляются в момент запроса; руководитель — активное подразделение, глава — работающий сотрудник с `IsActive` учётной записью; HR — ближайшее закрепление; `ResponsibleHrAsync` → null, если закреплённая учётная запись не может действовать.
- `MatrixRoute.AwaitingAsync`/`TurnAsync`: версии `OnReview`, где ход пользователя, с шагом (Superior/Hr); ушедший согласующий шага 1 передаёт ход шагу 2 — основа «ждёт согласования» AC-4 (шаг 1), AC-5 (шаг 2).
- `MatrixApprovalEndpoints.cs`: `/approvals` с фильтрами `orgUnitId`, `step`; `submittedAt` — давность.
- `CatalogDraftEndpoints.cs`: `/drafts` — ровно правило AC-5/AC-6 (администратор — всё; HR — черновики без подразделения и черновые треки своих подразделений; уровни не показываются); автор, подразделение, `createdAt`.
- `MatrixWorkflow.StateAsync`/`ActionsOf`: в `Draft`/`Returned` ход у текущего руководителя подразделения матрицы (не у `AuthorId`); у `Returned` с правками три действия, без — два — логика AC-4 «возвращена»/«не отправлен» и AC-9.
- `MatrixWorkflow.SubmitAsync`: без ответственного HR — 409; «нет HR» у администратора (AC-6) закрывает и версии без согласующего шага 2.
- `MatrixCatalog.CompletenessProblemsAsync` — тексты полноты (AC-8, AC-10), сейчас только внутри; `MatrixCatalog.DraftsAsync` — черновые записи, которые станут действующими (AC-10 для HR).
- `MatrixChange.Between`, `MatrixContent.Empty` — основа разницы с опубликованной версией (AC-10); `/variants/{n}/diff` — правки согласующего (AC-9); переходы `submit`, `approve`, `return`, `accept-changes`, `accept-and-revise`, `rollback-changes`.
- `MatrixEndpoints.ReferenceProblemAsync`: допустимый трек — свой или предка, `Active`, с `Active` направлением — то же условие для «нет опубликованной матрицы при действующем треке» (AC-4).
- `CatalogStore.MatricesUsing(entry)` (AC-11); `DeletionBlockers` — образец «видимые + `moreCount`»; `OrgUnitHrEndpoints` — свой и унаследованный HR (AC-12, AC-14); `ProcessRoleEndpoints` (`GET /career/me`).
- org-structure: `/org-units/tree` (с `headName`), `HeadEmployeeId`, `POST /employees`, `POST /org-units/{id}/positions/quick-add` (AC-13), `/org-units/{id}/positions`.
- user-management: `AppUser.IsInvited`, колонки ссылки, `HasValidLink` (AC-6 — данные есть, наружу не выставлены); `POST /users`, `resend-invitation` (AC-12, AC-13); `UserStatus.IsActive` (AC-15); `AccountMail` — образец письма.
- Platform: `MailSender` (singleton, после фиксации), `AppOptions.PublicBaseUrl`, `IAuditWriter`.
- SPA: регистрация фич по соглашению, `navItemsFor(role)`; career — `ApprovalsSection`, `DraftsSection`, `MatrixPage`, `MatrixEditor`, `MatrixChanges`, `MatrixHistory`, `MatrixCreateModal`, `TrackModal`/`TrackLevels`, `CompetencyModal`, `CatalogSelect`; org-structure — `UnitCard`, `UnitHrModal`, `UnitPositionsModal`, `EmployeeForm`, `PositionSelect`, `OrgUnitSelect`; users — `UserForm`, `EmployeePicker`, `useSendUserMail`; app — `useEditBase` (412), `useFormSubmit`, `destructiveConfirm`.
- Тесты: `MailCatcher` (Mailpit), `CareerScenarios`, `MatrixKit`.

## Gaps
- Ожидаемые действия на сервере: endpoint'а «мои ожидаемые действия» и счётчика нет (готовы только `/approvals`, `/drafts`). Не хватает: версий `Draft`/`Returned` подразделений пользователя (ход сейчас — по одной версии в `StateAsync`); подразделений без опубликованной матрицы при действующем треке своего или предка; подразделений без ответственного HR по дереву (`ResponsibleHrAsync` — по одному); действующих подразделений без руководителя (`IEmployeeDirectory` не перечисляет); приглашённых незаблокированных учётных записей с истёкшей, отменённой, очищенной или истекающей ссылкой (`IUserDirectory` и `/users` срок и stamp не отдают); объединения без повторов для нескольких ролей; причины и давности по видам.
- Счётчик: ответ количества и бейдж в шапке; `NavItem` без признака счётчика, `AppShell` без бейджа.
- SPA: стол как index-маршрут; вторая группа меню (`featureContract`, `AppShell.menuItems` знают только `"admin"`); маршруты экранов AC-8…AC-14; возврат на цель после входа (ссылки из писем).
- Read-endpoint'ы карьерной модели: проверка полноты без действия (AC-8, AC-10; сейчас — только 409 на переходах); разница «версия в работе ↔ действующая опубликованная» (AC-10); очередь черновиков с матрицами, которые их используют (AC-11; `MatricesUsing` — по одной записи и без фильтра читаемости).
- Экран подразделения (AC-12): состояние учётной записи руководителя одним чтением не получить (`EmployeeAccount` не знает «приглашён», у `/users` нет фильтра по `employeeId`).
- AC-8 «с того же шага»: шаг нигде не хранится; до создания матрицы черновика версии нет.
- Письма AC-15: в `career-framework` нет почтового кода (тексты, получатели, ссылки); `Mail.SendFailed` знает только `Invitation`/`PasswordReset`; тексты утверждаются в plan.
- Документы к обновлению на design-promote: `docs/ux/app-shell.html` (первый пункт, корень); `docs/product/requirements/user-management.html` (AC-31, non-goal «прочие уведомления»); `docs/product/requirements/career-framework.html` (non-goal «уведомления о согласовании»); `docs/product/requirements/audit.html` AC-15 и `docs/architecture/audit.md` (виды письма); `docs/architecture/stack.html` («Почта», «короткие личные списки», зависимости модулей); `docs/architecture/subsystems.md` (связь `career-framework → platform-core` «Отправляет письма», узел новой подсистемы, если будет); `career-framework.md`, `org-structure.md`, `user-management.md` (новые endpoint'ы и порты); `docs/ux/DESIGN.md` через `design-md-delta.yaml` (бейдж счётчика на пункте шапки; Steps — шаговый экран AC-8); OpenAPI `ContractVersion` 3.0.0 → 3.1.0 (аддитивно, AC-18).
- Тесты: `web/src/app/AppShell.test.tsx` — под новый состав меню.
- Perf: скрипта сида 10 000+ нет; по `custom-coding-rules.md` его создаёт первая Task с perf-риском (вычисление стола и счётчика).
- AC-19…AC-23: правил нет; рецепты в памяти `asd-dev-critical` без пометки.
- External dependency gaps: нет — antd 6.6.5 (`Badge`, `Steps`, `Empty`), React Router 8.4.0, TanStack Query 5.104.1, MailKit 4.18.1, Mailpit 1.31.4 уже в стеке.
- Migration gaps: при вычислении на лету миграций данных нет; контракт API — аддитивно (3.0.0 → 3.1.0). Условно: новые метки времени для давности (C-2) — колонки без обратного заполнения; шаг сценария AC-8 на сервере — колонка; порог «скоро истечёт» настройкой — ключ `AccountLinks:*` в `appsettings.json`, `deploy/k8s/configmap.yaml`, `deploy/README.md`.

## Risks
- Цена вычисления на каждый запрос: `MatrixRoute.AwaitingAsync` вызывает `TurnAsync` на каждую версию (3–8 запросов); `ProcessRoles.HrUnitsAsync` — рекурсивный запрос поддерева на закрепление; «нет HR» и «нет матрицы» читают цепочку предков на подразделение; HR на корне при 10 000+ сотрудников — сотни запросов на переход: impact=задержка навигации и нагрузка на БД, mitigation=наборные запросы (дерево и закрепления одним запросом, расчёт в памяти), дешёвый ответ счётчика, клиентский кэш с инвалидацией, Task с perf-риском и бюджетом на сиде 10 000+.
- Неограниченные списки («без руководителя», «без HR», справочные пункты HR на корне — сотни строк; stack.html разрешает целиком только короткие личные списки): impact=тяжёлый ответ, нечитаемый стол, mitigation=по виду — число и первые N с переходом в отфильтрованный вторичный раздел, либо серверные страницы.
- Синхронная отправка писем (`MailSender` до 15 с на письмо, соединение на каждое; у письма о черновике несколько получателей): impact=при недоступном SMTP `submit`/`approve`/`return` и создание черновика тянутся 15–45 с, mitigation (design/ADR)=фоновая очередь в памяти по образцу `PasswordResetQueue` (потеря при перезапуске уже принималась, outbox — non-goal) или синхронно с ограничением получателей.
- Письмо под блокировкой (`MatrixWorkflow.TransitAsync` держит блокировки дерева, справочников, строки матрицы до `CommitAsync`; создание записи — под `CatalogStore.BeginWriteAsync`): impact=блокировки висят на время SMTP, mitigation=получателей вычислять и слать после `CommitAsync`, тест отправки вне транзакции.
- Утечка доступа (черновик без подразделения утверждает любой HR, но чужие версии в работе ему не видны — `MatrixRoute.CanReadAsync`, AC-61; список матриц черновика (AC-11) и письма могут раскрыть неопубликованное): impact=нарушение AC-16, mitigation=фильтр `MatrixRoute.ReadableAsync` по образцу `DeletionBlockers`, «мой» endpoint без id пользователя, в письме — только читаемое получателем.
- Расхождение стола с правами сервера (дублирование правил хода и утверждения): impact=стол показывает действие, которое сервер отклонит, или прячет доступное, mitigation=те же функции `AwaitingAsync`, `StateAsync`/`ActionsOf`, фильтр `/drafts`, `ProcessRoles`.
- Порядок блокировок в составных сценариях AC-12/AC-13 (одну транзакцию через три модуля не собрать без нарушения направления зависимостей): impact=40P01, mitigation=сценарий по шагам вызывает существующие endpoint'ы, итог AC-13 показывает частичный результат.
- Поток писем о черновиках (несколько черновых компетенций подряд в AC-8 — письмо на каждую, дайджестов нет): impact=шум, mitigation=решение по получателям (неоднозначность A-4).
- Устаревший счётчик (действия других видны при переходе или фокусе): impact=низкий, допущено AC-3, mitigation=нет.
- Open redirect при возврате после входа: impact=фишинг через ссылку, mitigation=только относительный путь приложения (один ведущий `/`), иначе стол.
- AC-22 — правка памяти `asd-dev-critical` другим агентом нарушает `.asd/rules/artifact-layout.md` «Agent memory»: impact=нарушение правила, mitigation=Task на `asd-dev-critical`, правки точечные (AC-23), без id спринта, `MEMORY.md` согласованно.
- Расползание скоупа по C-1 (матрица по черновому треку меняет канон AC-36 и контракт): impact=рост скоупа, mitigation=решение пользователя до gate audit.

## Subsystems map
- org-structure: «действующее подразделение без руководителя» (AC-6), данные AC-12/AC-13/AC-14 (руководитель, перечень должностей, создание сотрудника, `quick-add`); при агрегации вне модуля — расширение `IEmployeeDirectory`.
- user-management: приглашения со сроком и состоянием ссылки (AC-6), учётные записи AC-12/AC-13, фильтр получателей AC-15 (`IsActive`); требования модуля владеют шапкой и навигацией (AC-1, AC-3); при агрегации вне модуля — расширение `IUserDirectory`.
- career-framework: основная часть — AC-4, AC-5, «черновики» и «без HR» AC-6 (модуль владеет `org_unit_hr`), AC-8…AC-11, матрицы и HR для AC-14, письма AC-15 (`MailSender` из Platform).
- audit: новые виды письма в `Mail.SendFailed`; чтения не меняются.
- Где может жить агрегация ожидаемых действий (решение design; новая подсистема — hard gate на design-promote): (а) каждый модуль считает свои виды, общий контракт в Platform, хост собирает один endpoint — направление зависимостей сохраняется, будущие виды подключаются так же, нужна Complication Approval за интерфейс; (б) всё в `career-framework` — меньше проводки, но расширения `IEmployeeDirectory`/`IUserDirectory` под нужды администратора и размывание цели модуля; (в) новая подсистема (например, `workspace`) — очевидный владелец стола, но `career-framework` придётся открыть порты, hard gate, реестр и диаграмма; (г) без серверной агрегации — endpoint «мои действия» в каждом модуле, SPA суммирует, 3 запроса на переход.

## Решения по противоречиям и неоднозначностям

Противоречия C-1…C-3 — решения пользователя 2026-10-12 (см. «Contradictions»); C-4, C-5 — по канону, без gate.

Неоднозначности — authority/preference, решение пользователя 2026-10-12 «принять все рекомендации»:
- A-4 (получатели письма о черновике, AC-15): ответственные HR всех подразделений, которыми автор руководит сейчас (для трека — HR подразделения трека), без повторов, одно письмо на запись; уровни писем не порождают; нет HR или автор больше не руководит — письма нет.
- A-6 (AC-4 «не отправлен»): любая версия `Draft` сразу — пункт «Продолжить построение матрицы».
- A-7 (AC-6 «скоро истечёт»): константа — меньше 2 суток до `LinkExpiresAt` при сроке 7 дней; «истекла» включает ссылку, отменённую сменой stamp, и очищенную сменой e-mail.
- A-8 (AC-14): только опубликованные матрицы своего подразделения, иначе понятное пустое состояние.
- A-9 (счётчик AC-3): считает только действия, которые пользователь может выполнить сам; справочные пункты (AC-5 «подразделение без матрицы» у HR) — в списке отдельной группой, вне счётчика.
- A-11 (AC-15): письма только по событиям согласования (`submit`, согласование шага 1, `return`, `accept-changes`) и созданию черновика; изменения оргструктуры отражаются только на столе и в счётчике.
- A-12 (AC-15): письмо не уходит, если получатель — тот, кто совершил действие.
- A-13 (AC-2, AC-4, AC-5): «нет опубликованной матрицы» не показывается, если у подразделения есть версия в работе; один объект — один пункт при нескольких ролях.
- A-15 (AC-21): правило применяется уже в этом спринте — макеты новых экранов показываются на gate plan вместе с текстами.

Неоднозначности, решённые по источнику (оркестратор):
- A-5 («автор» в AC-4, AC-9, AC-15): текущий руководитель подразделения матрицы — `docs/product/requirements/career-framework.html` AC-44, `MatrixWorkflow.StateAsync`.
- A-10 (обновление счётчика AC-3): перечитывание после каждой успешной мутации и при смене адреса, плюс обновление TanStack Query при возврате фокуса; без опроса по таймеру и push — AC-3, `tech-reference/tanstack-react-query-5.104.1.md`.
- A-14 (AC-6 «без ответственного HR»): функционально — закрепление за учётной записью, которая не может действовать, считается отсутствием HR, причина указывается — `ProcessRoles.ResponsibleHrAsync`, канон AC-50.
