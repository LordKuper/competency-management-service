---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

## Overview

Спринт `lite`: критерии — `sprint.md` AC-1…AC-12, AC-14…AC-16 (AC-13 не используется); контекст — `audit.md` и его решения Q-1…Q-4.

Порядок работ:
1. Правила проекта из ретро (Task 1) идут первыми и одни в волне: AC-12 меняет глубину проверок в impl, по которой работают все следующие Task.
2. Вынос общих типов в `Competency.Platform` (Task 2, AC-16) отдельным коммитом до любых изменений контракта, чтобы «`openapi.json` не меняется» проверялся пустым diff. Параллельно — почтовый транспорт, публичный адрес и dev-перехватчик (Task 3).
3. Backend `user-management`: приглашения (Task 4), затем сброс пароля и удаление задания пароля администратором (Task 5). Оба меняют одни и те же файлы — последовательно.
4. Frontend (Task 6) — после того как контракт Task 4–5 сгенерирован в `openapi.json`.

Решения, обязательные для всех Task (источник — `audit.md`, решения пользователя 2026-10-08):
- Ссылка — случайный токен ≥ 256 бит; в `users` хранятся только его SHA-256, срок действия, время выдачи и security stamp на момент выдачи (одна действующая ссылка на запись; назначение — из состояния записи). `DataProtectorTokenProvider` и провайдеры токенов Identity не используются (Q4 `stack.html`). Колонки ссылки не `[Audited]`.
- Токен передаётся во фрагменте ссылки (`<PublicBaseUrl>/…#token=…`), тратится только POST-запросом; GET токен не тратит.
- «Приглашён» = `password_hash IS NULL`; в ответ и фильтр — новое поле, `isBlocked` не меняется.
- «Активный администратор» = `GlobalAdmin`, не заблокирован, зарегистрирован (`password_hash IS NOT NULL`) — во всех местах, где признак записан.
- Письма по действиям администратора отправляются синхронно после фиксации транзакции, с коротким таймаутом из конфигурации; анонимный «Не помню пароль» — фоновая очередь в памяти (`Channel` + `BackgroundService`, без outbox), ответ всегда одинаковый и нейтральный, отказ — только в журнале (`Mail.SendFailed`) и логе.
- Письмо никогда не отправляется внутри транзакции или под блокировкой.
- Недействительная ссылка — 400 ProblemDetails с общим текстом (не 401).
- `info.version` документа OpenAPI — `2.0.0`, путь `/api/v1` прежний.

Порядок блокировок (правило AC-14; общий порядок из `docs/architecture/user-management.md`: дерево → строки активных администраторов → строка учётной записи):
- Создание приглашения — как сейчас (блокировка дерева, затем строка записи); письмо — после фиксации, вне блокировок.
- Повторное приглашение, ссылка сброса от администратора, фоновая выдача ссылки сброса — только строка учётной записи `FOR UPDATE` (позиция 3), с перечитыванием; письмо — после фиксации.
- Принятие приглашения и сброс по ссылке — только строка учётной записи `FOR UPDATE` (позиция 3), с перечитыванием и проверкой хэша, срока и stamp; пока строка удерживается, других запросов не выполняется.
- Блокировка, смена роли, привязки и e-mail — существующий порядок; смена e-mail очищает ссылку в той же записи строки.
- Правило последнего администратора — существующая блокировка строк активных администраторов (позиция 2) с новым признаком «зарегистрирован».

## Definition of Done
Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.
Дополнения спринта: после Task 2 `git diff -- openapi/openapi.json` пуст; в логах приложения нет токенов, ссылок из писем, тел писем и учётных данных SMTP; `deploy/README.md` описывает переход для удалённого `reset-password` и новые ключи конфигурации.

### Task 1: Правила проекта из ретро
Material risk: none
- [x] AC-12: заменить раздел «Verification depth in impl» в `.asd/project/custom-coding-rules.md` текстом строки `001-project-init-org-structure#P-1` (`.asd/project/retro-backlog.md`) с уточнением AC-12: скрипт наполнения 10 000+ сотрудников создаёт первая Task с объявленным perf-риском и далее он переиспользуется; Playwright и e2e не используются
- [x] AC-14: добавить в `.asd/project/custom-coding-rules.md` правило `#P-3` (блокировка каждого многострочного инварианта объявляется в plan с местом в общем порядке блокировок и получает детерминированный тест гонки в impl-test)
- [x] AC-16: добавить в `.asd/project/custom-coding-rules.md` правило `#P-5` (общие для модулей вспомогательные типы живут в `Competency.Platform`)
- [x] AC-15: добавить в `.asd/project/custom-common-rules.md` правило `#P-4` (запрет разрушающих команд Docker для dev-стека пользователя и томов с фиксированным именем; временные прогоны — только собственные именованные тома, удаление по точному имени)

### Task 2: Общие вспомогательные типы в Competency.Platform
Material risk: artifact: cross-module refactor via `git diff -- openapi/openapi.json` пуст и `npm --prefix web run check:api`
- [x] AC-16: перенести `PageResponse<T>` из `Competency.OrgStructure` и `Competency.UserManagement` в `Competency.Platform` (`public`, XML-комментарии — `GenerateDocumentationFile` + `TreatWarningsAsErrors`); имена схем `PageResponseOf…` не меняются
- [x] AC-16: `AuditPageResponse` свести к общему типу без переименования схемы `AuditPageResponse` в `openapi.json` (тонкий тип или сопоставление имени схемы — выбор dev по меньшему коду; критерий — пустой diff `openapi.json`)
- [x] AC-16: перенести `Rejections.Conflict`/`Invalid` (заголовок «Операция отклонена») в `Competency.Platform`; модульные `UnitProblemAsync`, `EmployeeProblemAsync`, `From(IdentityResult)` остаются в модулях
- [x] AC-16: общие пределы страниц (`DefaultPageSize` 50, `MaxPageSize` 200, `MaxPage`, прежние тексты ошибок) и проверка `page`/`pageSize` — в `Competency.Platform`; вызовы в `AuditQuery.cs`, `ListQueries.cs`, `UserListQuery.cs`
- [x] AC-16: общее экранирование LIKE и предел длины поиска 200 — в `Competency.Platform`; вызовы в `TextSearch.cs` и `UserListQuery.cs` (обрезка пробелов в `UserListQuery` сохраняется)
- [x] AC-16: удалить модульные копии; `dotnet build`, `check:api` чисты, `git diff -- openapi/openapi.json` пуст
Tech reference: aspnetcore-10.0.12, microsoft-aspnetcore-openapi-10.0.12, efcore-10.0.12.

### Task 3: Почтовый транспорт, публичный адрес и dev-перехватчик
Material risk: change: security — секреты SMTP, режим TLS и проверка конфигурации при старте, не ломающая build-time генерацию OpenAPI
- [x] AC-1: подключить MailKit 4.18.1 в `Competency.Platform` (точная версия, обновить `packages.lock.json` всех проектов); один конкретный класс отправки без интерфейса: хост, порт, режим TLS (`SecureSocketOptions`, по умолчанию `StartTls`), необязательные логин и пароль (без логина `AuthenticateAsync` не вызывается — иначе MailKit бросает `NotSupportedException` на сервере без AUTH), адрес отправителя, таймаут (по умолчанию 15 с) из секции конфигурации `Smtp`; `ProtocolLogger` не используется; в лог при отказе — только тип исключения, без адресов и содержимого
- [x] AC-2: настройка публичного адреса приложения (`App:PublicBaseUrl`) с проверкой абсолютного http(s) URL при старте
- [x] AC-1,2: проверка конфигурации `Smtp` и `App:PublicBaseUrl` при старте — только вне tooling-запуска (`GetDocument.Insider`), как миграции и bootstrap
- [x] AC-3: сервис Mailpit `axllent/mailpit:v1.31.4` в `deploy/dev/docker-compose.yml`: SMTP и веб-интерфейс только на `127.0.0.1`, порты переопределяются переменными окружения по образцу `DEV_DB_PORT`, порт 1110 не публикуется, без тома и `MP_DATABASE`, `MP_ALLOWED_HOSTS=localhost` (защита от DNS rebinding), healthcheck образа; проверить, что профиль F5 «Everything» поднимает и этот сервис (`.dcproj` с `DockerServiceName=db`)
- [x] AC-3: `src/Competency.Api/Properties/launchSettings.json` — `Smtp__*` на Mailpit (`None`, без аутентификации) и `App__PublicBaseUrl` = адрес Vite (`http://localhost:5173`) в профилях
- [x] AC-1,3: `deploy/k8s/configmap.yaml` (хост, порт, режим TLS, отправитель, таймаут, публичный адрес), `deploy/secret.template.yaml` и `deploy/k8s/app.yaml` (`Smtp__UserName`, `Smtp__Password` через `secretKeyRef`, `optional: true`)
- [x] AC-3: `deploy/dev/README.md` (перехватчик, адрес веб-интерфейса) и `deploy/README.md` (новые ключи; доверие внутреннему CA relay — шаг развёртывания)
Tech reference: mailkit-4.18.1, mailpit-1.31.4, aspnetcore-10.0.12, microsoft-aspnetcore-openapi-10.0.12, visual-studio-multiproject-launch-17.11.

### Task 4: Регистрация по приглашению (backend)
Material risk: change: authentication — новое состояние «приглашён», одноразовая ссылка и правило последнего администратора
Material risk: change: migration — новые колонки ссылки в `users`
Material risk: change: public contract — создание без пароля, новые методы и поля `/api/v1/users` и `/api/v1/auth`
- [x] AC-4,5: миграция `users`: хэш ссылки, срок, время выдачи, stamp на момент выдачи (nullable, уникальный частичный индекс по хэшу); данные не переносятся; `has-pending-model-changes` чист
- [x] AC-4,5,6: хранилище ссылки в `user-management`: выдача (случайный токен ≥ 256 бит, хранится только SHA-256), проверка (хэш, срок, совпадение stamp), погашение; смена e-mail записи очищает ссылку; колонки ссылки не `[Audited]`
- [x] AC-4,9: `POST /api/v1/users` без пароля: запись создаётся приглашённой, письмо-приглашение — после фиксации, синхронно, с результатом отправки в ответе (аддитивное поле); `CreateUserRequest` без `password`
- [x] AC-5: анонимный `POST /api/v1/auth/accept-invitation` (токен, пароль по `Identity:Password`): строка записи `FOR UPDATE`, проверка ссылки, установка хэша, снятие lockout, погашение ссылки, смена stamp; недействительная ссылка — 400 с общим текстом; ограничение частоты — политика Task 5 подключается в Task 5
- [x] AC-6: `POST /api/v1/users/{id}/resend-invitation` (администратор, `If-Match`): только для приглашённой незаблокированной записи (иначе 409), новая ссылка заменяет прежнюю, письмо — после фиксации, результат отправки в ответе
- [x] AC-6: `UserResponse` получает признак «приглашён»; `ListUsers` — фильтр по нему
- [x] AC-4,5: вход приглашённой записи — тот же 401, время выравнивается `SpendHashingTime`, своя причина в `Auth.LoginFailed`
- [x] AC-4: признак «активный администратор» дополнить «зарегистрирован» в `ActiveAdministrators.LockAsync`, `IsActiveAdministrator`, `HasOtherAsync`, `AdminBootstrapper`, `EmployeeAccounts.IsLastActiveAdministratorAsync`
- [x] AC-2: письмо-приглашение — русские тема и текст со ссылкой от `App:PublicBaseUrl`, токен во фрагменте; строковые константы в `user-management`
- [x] AC-10: события `AppUser.InvitationSent` (причина — первичная/повторная), `Auth.RegistrationCompleted`, `Mail.SendFailed` (тип письма, без адреса); изменения — `Stage`, отправка и отказ — `WriteAsync` после фиксации; без токенов, ссылок, адресов
- [x] AC-4…6: перегенерировать `openapi/openapi.json` и `web/src/api/schema.d.ts`
Tech reference: microsoft-aspnetcore-identity-entityframeworkcore-10.0.12, aspnetcore-10.0.12, efcore-10.0.12, npgsql-entityframeworkcore-postgresql-10.0.3, dotnet-ef-10.0.12, mailkit-4.18.1, microsoft-aspnetcore-openapi-10.0.12.

### Task 5: Сброс пароля по ссылке и удаление задания пароля администратором (backend)
Material risk: change: authentication — сброс по ссылке, обрыв сессий, перечисление учётных записей
Material risk: change: security — анонимные методы, ограничения частоты, фоновая очередь
Material risk: change: public contract — удаление `reset-password`, новые методы, `info.version` 2.0.0
- [x] AC-7: анонимный `POST /api/v1/auth/forgot-password` (e-mail): немедленный одинаковый ответ 202 при любом e-mail; запрос кладётся в фоновую очередь в памяти (`Channel` + `BackgroundService`); обработчик: строка записи `FOR UPDATE`, ссылка выдаётся только активной зарегистрированной записи (не заблокирована, есть пароль, привязанный сотрудник работает) и не чаще настраиваемого интервала по времени выдачи (тихий пропуск); письмо — после фиксации; для несуществующих адресов ничего не хранится
- [x] AC-7: политика ограничения частоты по адресу (`RateLimiting:PasswordReset`, по образцу `login`) для `forgot-password`, `reset-password` по токену и `accept-invitation`
- [x] AC-8: анонимный `POST /api/v1/auth/reset-password` (токен, новый пароль): строка записи `FOR UPDATE`, проверка ссылки, политика пароля, установка хэша, снятие lockout за перебор (не блокировки администратором), погашение ссылки, смена stamp (все сессии обрываются); уволенный сотрудник войти по-прежнему не может; недействительная ссылка — 400 с общим текстом
- [x] AC-9: удалить `POST /api/v1/users/{id}/reset-password` и `ResetPasswordRequest`; добавить `POST /api/v1/users/{id}/send-password-reset` (администратор, `If-Match`): только для активной зарегистрированной записи (иначе 409), пароль не меняется, письмо — то же, что в AC-7, синхронно после фиксации, результат отправки в ответе
- [x] AC-8: смена собственного пароля погашает действующую ссылку сброса
- [x] AC-10: события `Auth.PasswordResetRequested` (актор — администратор или сама запись), `Auth.PasswordResetCompleted`, `Mail.SendFailed`; в фоне — необязательный request id в `AuditEntry` (аддитивно), без `HttpContext`
- [x] AC-2: письмо сброса — русские тема и текст, ссылка от `App:PublicBaseUrl`, токен во фрагменте
- [x] AC-9: `info.version` документа OpenAPI — `2.0.0`; перегенерировать `openapi/openapi.json` и `web/src/api/schema.d.ts`
- [x] AC-9: `deploy/README.md`, раздел «Обновление существующего развёртывания»: переход для удалённого метода и поля `password`, записи с адресами `@local.invalid` письма не получат — сначала заменить адрес
Tech reference: microsoft-aspnetcore-identity-entityframeworkcore-10.0.12, aspnetcore-10.0.12, efcore-10.0.12, mailkit-4.18.1, microsoft-aspnetcore-openapi-10.0.12.

### Task 6: Экраны регистрации и сброса, управление пользователями (frontend)
Material risk: change: authentication — анонимные экраны со ссылкой-токеном во фрагменте и общий обработчик 401
- [x] AC-11: общая раскладка карточки экрана входа вынесена из `LoginPage.tsx` и переиспользуется; на экране входа — ссылка «Не помню пароль»
- [x] AC-7,11: экран «Не помню пароль» (`standaloneRoutes`): ввод e-mail, всегда один нейтральный результат «Если учётная запись существует, письмо отправлено»
- [x] AC-5,8,11: экраны «Задание пароля» (приглашение) и «Новый пароль» (сброс): токен читается из фрагмента, адресная строка очищается (`history.replaceState`), токен отправляется POST; недействительная ссылка — понятное сообщение и переход к «Не помню пароль»; после успеха — переход на вход с сообщением
- [x] AC-11: `<meta name="referrer" content="no-referrer">` в `web/index.html`
- [x] AC-4,11: модальное окно создания без поля пароля; удалить `ResetPasswordModal.tsx`
- [x] AC-6,9,11: меню строки: «Отправить приглашение повторно» (приглашённые), «Отправить ссылку для сброса пароля» (активные зарегистрированные) вместо «Сбросить пароль»; после действия — сообщение «письмо отправлено» или «письмо не отправлено»
- [x] AC-6,11: `UserStatusTag` — состояние «Приглашён» (в т. ч. вместе с «Заблокирован»); фильтр «Состояние» с вариантом «Приглашённые»
- [x] AC-11: адаптивность desktop/tablet, токены `docs/ux/DESIGN.md`; `lint`, `build`, `check:api` чисты
Tech reference: react-19.3.0, react-router-8.4.0, antd-6.6.5, tanstack-react-query-5.104.1, openapi-fetch-0.17.0, openapi-typescript-7.13.0, typescript-6.0.2, vite-8.3.2.

### Task 7: Личные параметры SMTP через User Secrets
Material risk: none
- [x] AC-17: перенести `Smtp__*` и `App__PublicBaseUrl` из `environmentVariables` профиля в `src/Competency.Api/Properties/launchSettings.json` в новый `src/Competency.Api/appsettings.Development.json` (`Smtp:*` на Mailpit `127.0.0.1:11025`, `None`, без аутентификации; `App:PublicBaseUrl` `http://localhost:5173`); `ConnectionStrings__Default`, `Bootstrap__*`, `ASPNETCORE_ENVIRONMENT` остаются в `launchSettings.json`
- [x] AC-17: убедиться, что user secrets подключаются в `Development` (`UserSecretsId` уже есть в `Competency.Api.csproj`, `WebApplication.CreateBuilder` добавляет их для entry assembly) и что значения из user secrets переопределяют `appsettings.Development.json`; tooling-запуск OpenAPI и тестовый хост (`ApiHost`, задаёт `Smtp__*` переменными окружения) не затронуты
- [x] AC-17: `deploy/dev/docker-compose.yml` — сервису Mailpit `MP_SMTP_DISABLE_RDNS: "true"` (без него приветствие SMTP приходит через ~8–10 с из-за обратного DNS на Docker Desktop — наблюдение impl-test entry 1, `test-plan.md`), чтобы письма по умолчанию не упирались в таймаут 15 с
- [x] AC-17: `deploy/dev/README.md` — раздел о собственной SMTP-песочнице: команды `dotnet user-secrets set "Smtp:…" … --project src/Competency.Api` для Host, Port, SecureSocketOptions, UserName, Password (на примере Mailtrap Sandbox `sandbox.smtp.mailtrap.io:587`, `StartTls`), «Manage User Secrets» в Visual Studio, возврат к Mailpit (`dotnet user-secrets clear` или удаление ключей `Smtp:*`); секреты не попадают в репозиторий
Tech reference: aspnetcore-10.0.12, mailkit-4.18.1, mailpit-1.31.4, visual-studio-multiproject-launch-17.11.

### Task 8: Правки текстов и экрана входа по smoke-проверке
Material risk: none
- [x] AC-18: `src/Competency.UserManagement/AccountMail.cs` — убрать из письма-приглашения строку «Если вы не ждали этого письма, просто удалите его.» (письмо сброса не меняется)
- [x] AC-18: `web/src/features/auth/AuthCard.tsx` — рядом с логотипом название системы `PRODUCT_NAME` («Калибр», `web/src/app/productName.ts`), токены `docs/ux/DESIGN.md`, адаптивность desktop/tablet сохраняется
- [x] AC-18: `web/src/features/auth/passwordPolicy.ts` — убрать из `PASSWORD_HINT` фразу «Если пароль не подойдёт, сервис укажет, чего в нём не хватает.»
- [x] AC-18: `web/src/features/auth/LinkPasswordPage.tsx` — убрать из вступления экрана «Задание пароля» фразу «Затем войдите в систему по своему e-mail и этому паролю.»
- [x] AC-18: `web/src/features/users/UserForm.tsx` — убрать подсказку `extra` у поля сотрудника
Tech reference: react-19.3.0, antd-6.6.5, aspnetcore-10.0.12.

### Task 9: Крупное название у логотипа и срок приглашения 7 дней
Material risk: none
- [x] AC-18: `web/src/features/auth/AuthCard.tsx` — название «Калибр» крупным шрифтом, соразмерным логотипу 80 px (токены antd / `docs/ux/DESIGN.md`; на ширине ~820 px не переносится некрасиво и не выходит за карточку)
- [x] AC-4: срок ссылки-приглашения по умолчанию — 7 дней: `AccountLinks:InvitationLifetime` = `7.00:00:00` в `src/Competency.Api/appsettings.json` и значение по умолчанию в `deploy/README.md`; прочие упоминания 72 часов/`3.00:00:00` в коде и `deploy/**` (grep)
Tech reference: react-19.3.0, antd-6.6.5, aspnetcore-10.0.12.

### Task 10: Без подзаголовка на экранах без сессии
Material risk: none
- [x] AC-18: убрать подзаголовок «Компетенции и карьерный рост» (`PRODUCT_TAGLINE`, `web/src/app/productName.ts`) с экранов без сессии; константу удалить, если других вызовов не остаётся
Tech reference: react-19.3.0, antd-6.6.5.

### Task 11: Минимальная длина пароля в подсказке из API
Material risk: none
- [x] AC-19: анонимный `GET /api/v1/auth/password-policy` (`.AllowAnonymous()`, вне группы, объявляющей 401) → `{ minLength }` из `IdentityOptions.Password.RequiredLength` (`IOptions<IdentityOptions>`); перегенерировать `openapi/openapi.json` и `web/src/api/schema.d.ts` (аддитивно, `info.version` остаётся 2.0.0)
- [x] AC-19: `web/src/features/auth/passwordPolicy.ts` и его вызовы (`LinkPasswordPage.tsx`, `ChangePasswordModal.tsx`) — подсказка вида «Не короче N символов; заглавные и строчные буквы, цифры и специальные символы.» с N из запроса политики (React Query, долгий `staleTime`); пока значение не загружено или запрос не удался — подсказка без числа
Tech reference: aspnetcore-10.0.12, microsoft-aspnetcore-identity-entityframeworkcore-10.0.12, microsoft-aspnetcore-openapi-10.0.12, react-19.3.0, tanstack-react-query-5.104.1, openapi-fetch-0.17.0.

## Risks
- Тесты: около 48 вызовов `Scenarios.CreateUserAsync` создают пользователей с паролем, тесты `reset-password` в `AuditTests` — всё это перестаёт работать после Task 4–5 и правится в impl-test (сборка не ломается: тесты обращаются к API через HTTP).
- Перехват писем в интеграционных тестах (Mailpit через ядро Testcontainers или заглушка SMTP) выбирает impl-test; Mailpit в CI — ещё один образ.
- Тесты гонок по правилу AC-14 для impl-test: принятие приглашения против повторной отправки и против блокировки; двойное использование одного токена; сброс против смены собственного пароля; правило последнего администратора при приглашённых администраторах.
- UX новых экранов не описан до impl (`lite`, P-2 отложена): раскладка — переиспользование карточки входа; `docs/ux/user-management.html` обновляется на design-promote.

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | 1 |
| 2 | 2, 3 |
| 3 | 4 |
| 4 | 5 |
| 5 | 6 |
| 6 | 7 |
| 7 | 8 |
| 8 | 9 |
| 9 | 10 |
| 10 | 11 |

- Task 1 один в волне 1: AC-12 меняет глубину проверок, по которой dev выполняет все следующие Task.
- Task 2 и 3 не пересекаются по файлам: Task 2 — новые файлы общих типов в `Competency.Platform` и `.cs`-файлы модулей; Task 3 — почтовые файлы `Competency.Platform`, `Competency.Platform.csproj`, все `packages.lock.json`, `Competency.Api`, `deploy/**`. Task 2 не меняет `.csproj` и lock-файлы.
- Task 4 зависит от Task 2 (контракт до изменений проверен пустым diff) и Task 3 (отправка писем, публичный адрес).
- Task 5 зависит от Task 4 (хранилище ссылки, письма, те же `AuthEndpoints.cs`/`UserEndpoints.cs`).
- Task 6 зависит от контракта Task 4–5 (`web/src/api/schema.d.ts`).
- Task 7 (поправка скоупа 2026-10-08, AC-17) — новая последняя волна 6: меняет `launchSettings.json`, `deploy/dev/docker-compose.yml` и `deploy/dev/README.md` после Task 3.
- Task 8 (поправка скоупа 2026-10-08, AC-18, по итогам smoke-проверки impl-test entry 2) — новая последняя волна 7.
- Task 9 (поправка 2026-10-08 по smoke-проверке impl-test entry 3: размер названия, срок приглашения) — новая последняя волна 8.
- Task 10 (поправка 2026-10-08 по повторной smoke-проверке AC-18: убрать подзаголовок) — новая последняя волна 9.
- Task 11 (поправка 2026-10-08, AC-19: длина пароля в подсказке из API) — новая последняя волна 10.

## Out of scope
- Скрипт наполнения 10 000+ сотрудников (решение пользователя 2026-10-08, audit Q-4).
- Outbox и автоматические повторы отправки писем (решение пользователя 2026-10-08, audit Q-2).
- Разбор заголовков `X-Forwarded-*` для лимитов за прокси (открытый Q8 `stack.html`).
