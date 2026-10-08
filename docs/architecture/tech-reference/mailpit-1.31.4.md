---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Mailpit (Docker-образ `axllent/mailpit`) @ 1.31.4

Область: перехватчик почты только для локальной разработки и тестов. Mailpit — SMTP-сервер, который принимает письма приложения и показывает их в веб-интерфейсе и через REST API; реальная почта не отправляется. Сервис `mailpit` dev-стека `deploy/dev/docker-compose.yml` (проект Docker Compose в Visual Studio, профили «Everything» и «API») и generic-контейнер Testcontainers 4.15.0 в интеграционных тестах (`tests/Competency.Tests/Infrastructure/MailCatcher.cs`). В production, Kubernetes-манифестах и образе `app` не используется.

## Canonical source
- Документация: https://mailpit.axllent.org/docs/ — Docker: https://mailpit.axllent.org/docs/install/docker/ ; параметры запуска: https://mailpit.axllent.org/docs/configuration/runtime-options/ ; SMTP: https://mailpit.axllent.org/docs/configuration/smtp/ ; хранение: https://mailpit.axllent.org/docs/configuration/email-storage/ ; поиск: https://mailpit.axllent.org/docs/usage/search-filters/ ; Chaos: https://mailpit.axllent.org/docs/integration/chaos/ ; API: https://mailpit.axllent.org/docs/api-v1/ (спецификация — `server/ui/api/v1/swagger.json` в репозитории на теге).
- Репозиторий: https://github.com/axllent/mailpit ; на теге v1.31.4: Dockerfile https://github.com/axllent/mailpit/blob/v1.31.4/Dockerfile , LICENSE https://github.com/axllent/mailpit/blob/v1.31.4/LICENSE — MIT («The MIT License (MIT) Copyright (c) 2022-Now() Ralph Slooten»), метка образа `org.opencontainers.image.licenses=MIT`. CHANGELOG: https://github.com/axllent/mailpit/blob/develop/CHANGELOG.md (дат в заголовках нет).
- Релиз: https://api.github.com/repos/axllent/mailpit/releases/tags/v1.31.4 — опубликован 2026-10-03T04:36:58Z, не pre-release.
- Docker Hub: https://hub.docker.com/r/axllent/mailpit ; API тегов: https://hub.docker.com/v2/repositories/axllent/mailpit/tags?page_size=10&ordering=last_updated — `v1.31.4` загружен 2026-10-03T04:52:33Z, digest индекса `sha256:b68349e3a014b90c5610bfb26b2ae36f3892d7b8cf25ee140c6c71c98d2fcf48`; на 2026-10-08 тот же digest у `latest`, `v1`, `v1.31` — последняя стабильная версия (новее только плавающий `edge`). Платформы: linux/amd64, linux/arm64, linux/386. Образ linux/amd64 (https://hub.docker.com/v2/repositories/axllent/mailpit/tags/v1.31.4/images): digest `sha256:c8e498023104710cd71a7bb1856a51f2183ff0dc1ea07675067a38ecda08428f`, 16 836 010 байт в сжатом виде, база — Alpine 3.24.2. Зеркало образов — `ghcr.io` (страница Docker в документации).
- Last verified: 2026-10-08 (документация, исходники тега v1.31.4 — в том числе `cmd/root.go` для `MP_SMTP_DISABLE_RDNS`, `MP_MAX_MESSAGES`, `MP_SMTP_AUTH_FILE`, — API Docker Hub и GitHub; образ запущен в dev-стеке и тестах спринта 002)
- Риск знаний (Phase 5): **MEDIUM** — линия 1.31 (2026-08…2026-10) новее среза знаний, `--allowed-hosts` появился в 1.31.1; поведение сверено с исходниками тега.

## API surface used in project
- Образ `axllent/mailpit:v1.31.4` (Dockerfile тега и конфигурация образа на Docker Hub): `ENTRYPOINT ["/mailpit"]`; `EXPOSE 1025/tcp 1110/tcp 8025/tcp` (SMTP, POP3, веб-интерфейс и API); `HEALTHCHECK --interval=15s --start-period=10s --start-interval=1s CMD ["/mailpit", "readyz"]`; инструкций `USER` и `VOLUME` нет — процесс работает от root контейнера, анонимный том не создаётся. В образе есть `tzdata` (часовой пояс — `TZ`), `sendmail` — символическая ссылка на `/mailpit`.
- Адреса в контейнере по умолчанию: SMTP `0.0.0.0:1025` (`MP_SMTP_BIND_ADDR`), веб-интерфейс и API `0.0.0.0:8025` (`MP_UI_BIND_ADDR`); переопределять не нужно — хост-порты задаются в `ports:`. POP3 (1110) проекту не нужен.
- SMTP по умолчанию — «без шифрования и без аутентификации» (docs SMTP). В EHLO STARTTLS объявляется только при заданном сертификате (`MP_SMTP_TLS_CERT`), AUTH — только при файле паролей (`MP_SMTP_AUTH_FILE`) или `MP_SMTP_AUTH_ACCEPT_ANY` (`internal/smtpd`). Клиент MailKit (`mailkit-4.18.1.md`): example: `await client.ConnectAsync(host, port, SecureSocketOptions.None, ct);` и **без** `AuthenticateAsync` — на сервере без AUTH MailKit бросает `NotSupportedException("The SMTP server does not support authentication.")`, поэтому при пустом имени пользователя аутентификацию пропускать. `SecureSocketOptions.StartTls` к Mailpit без сертификата тоже даёт `NotSupportedException`; `StartTlsWhenAvailable` и `Auto` продолжают без шифрования.
- Переменные окружения (`cmd/root.go`; булевы принимают `1`/`true`/`yes`, без учёта регистра):
  - `MP_SMTP_AUTH_ACCEPT_ANY` (по умолчанию `false`) — принимать любые логин и пароль «или их отсутствие»; `MP_SMTP_AUTH_ALLOW_INSECURE` (`false`) — разрешить PLAIN/LOGIN по незашифрованному соединению. Нужны вместе и только для проверки ветки с аутентификацией (пример compose на странице Docker задаёт оба).
  - `MP_MAX_MESSAGES` — хранить не больше N последних писем, по умолчанию 500, старые удаляются автоматически; `0` отключает удаление (docs email-storage); `MP_MAX_AGE` — удаление по возрасту (`36h`, `14d`).
  - `MP_SMTP_DISABLE_RDNS` (`false`) — «Disable SMTP reverse DNS lookups»; без него на Docker Desktop приветствие SMTP приходит через 8–10 с (обратный DNS адреса клиента), что упирается в `Smtp:Timeout`.
  - `MP_SMTP_AUTH_FILE` — файл паролей; простой текст — строка `user:password` на пользователя (docs passwords).
  - `MP_DATABASE` — путь к файлу БД; **не задан** → временная БД, «при выходе приложения временная база удаляется с диска» (docs email-storage).
  - `MP_ALLOWED_HOSTS` (с 1.31.1) — allow-list заголовка `Host` против DNS rebinding, по умолчанию пуст (см. «Known issues»).
  - Прочие, проекту не нужные: `MP_DISABLE_VERSION_CHECK` (`false`, проверка новой версии), `MP_SMTP_REQUIRE_STARTTLS` / `MP_SMTP_REQUIRE_TLS` (`false`), `MP_UI_AUTH_FILE` (basic auth веб-интерфейса и API).
- Готовность: `GET /livez` — 200 без тела; `GET /readyz` — 200, когда HTTP-сервер готов и хранилище отвечает, иначе 503 `Service Unavailable` (`server/handlers/k8sready.go`); оба маршрута — без basic auth и без проверки `Host` (`server/server.go`). `mailpit readyz` запрашивает `/readyz` по `MP_UI_BIND_ADDR` и `MP_WEBROOT` (HTTPS при `MP_UI_TLS_CERT`), код выхода 0 — готов, 1 — нет; `--wait` ждёт готовности (таймаут по умолчанию 30 с). Compose применяет HEALTHCHECK образа, если сервис не задаёт свой (`healthcheck` в Compose лишь переопределяет его), поэтому `depends_on: { mailpit: { condition: service_healthy } }` и `docker compose up --wait` работают без `healthcheck:` в `docker-compose.yml`.
- REST API (порт 8025, префикс `/api/v1`, по `swagger.json` тега):
  - `GET /api/v1/messages?start=0&limit=50` — список, новые первыми (по умолчанию `start=0`, `limit=50`); ответ: `total`, `unread`, `messages_count`, `messages_unread`, `start`, `tags`, `messages[]` (`ID`, `MessageID`, `From`, `To[]` из `{Name, Address}`, `Subject`, `Created`, `Snippet`, `Read`, `Size`, …).
  - `GET /api/v1/search?query=<фильтр>&start=&limit=&tz=` — поиск, тот же формат ответа, новые первыми; фильтры `to:`, `from:`, `subject:`, `message-id:`, `is:unread`, `before:`/`after:`, фраза в кавычках, исключение префиксом `-` или `!`; `query` кодировать в URL.
  - `GET /api/v1/message/{ID}` — письмо целиком: `Text`, `HTML`, `Subject`, `From`, `To[]`, `Date`, `MessageID`, `Size`; `ID` = `latest` — последнее письмо; **помечает письмо прочитанным**. Там же `/api/v1/message/{ID}/raw` (исходник) и `/headers`; `GET /view/{ID}.txt` и `/view/{ID}.html` — текстовая и HTML-часть (404, если HTML-части нет).
  - `DELETE /api/v1/messages` с телом `{"IDs": ["…"]}` — удалить выбранные; без `IDs` — удалить **все**; ответ 200 `ok`. `DELETE /api/v1/search?query=…` — удалить найденные.
  - Example (письмо приглашения в тесте): `GET /api/v1/search?query=to%3Auser%40example.com` → `messages[0].ID` → `GET /api/v1/message/{ID}` → ссылка из `Text`.
- Chaos — имитация отказа SMTP, по умолчанию выключен; в тестах проекта не используется (отказ SMTP имитирует хост API, направленный на свободный порт без сервера): `MP_ENABLE_CHAOS=true` или сразу `MP_CHAOS_TRIGGERS=Sender:451:100` (`<trigger>:<код>:<вероятность %>`); триггеры `Sender`, `Recipient`, `Authentication`, код 400–599. На лету — `PUT /api/v1/chaos` с телом `{"Sender":{"ErrorCode":451,"Probability":100}}` (опущенные триггеры сбрасываются в 0 %, без включённого Chaos — 400). Сработавший триггер отвечает `<код> Chaos <trigger> error`, например `451 Chaos recipient error`.
- Testcontainers 4.15.0 — ядро `Testcontainers`, уже транзитивная зависимость `Testcontainers.PostgreSql` (`testcontainers-postgresql-4.15.0.md`). Generic-контейнер, образ передаётся в конструктор (`new ContainerBuilder()` без образа в 4.15.0 помечен `[Obsolete]`); example:
  ```csharp
  var mailpit = new ContainerBuilder("axllent/mailpit:v1.31.4")
      .WithPortBinding(1025, true)
      .WithPortBinding(8025, true)
      .WithWaitStrategy(Wait.ForUnixContainer()
          .UntilHttpRequestIsSucceeded(r => r.ForPort(8025).ForPath("/readyz")))
      .Build();
  await mailpit.StartAsync();
  var smtpPort = mailpit.GetMappedPublicPort(1025);
  var api = new Uri($"http://{mailpit.Hostname}:{mailpit.GetMappedPublicPort(8025)}/");
  ```
  Альтернатива ожиданию `/readyz` — `UntilContainerIsHealthy()` (HEALTHCHECK образа).
  В проекте (`MailCatcher`): один контейнер на прогон (`TestEnvironment`), все хосты API получают `Smtp__*` на него (`None`, без аутентификации); дополнительно `MP_MAX_MESSAGES=0` (письма других тестов не вытесняются) и `MP_SMTP_DISABLE_RDNS=true`. Вариант с аутентификацией (`MailTransportTests`) — свой контейнер с `MP_SMTP_AUTH_FILE` (файл кладёт `WithResourceMapping`) и `MP_SMTP_AUTH_ALLOW_INSECURE=true`.

## Version-specific notes
- 1.31.4 (релиз 2026-10-03): исправлены дубликаты сообщений в обработчиках websocket (#737) и имя хоста в HELO/EHLO SMTP-клиента пересылки (#738); обновление зависимостей.
- 1.31.3 (Docker Hub 2026-09-27): не больше 500 MIME-частей на письмо; `sendmail` в образе — ссылка на Mailpit (#736).
- 1.31.2 (2026-09-19): безопасность — декодирование миниатюр ограничено одним кадром (GHSA-2vgv-6hcp-mf43); тег мажорной версии для Docker-образов (#734, отсюда `v1`); локальные части адресов снова в кавычках в JSON API (#732).
- 1.31.1 (2026-09-05): безопасность — флаг `--allowed-hosts` против DNS rebinding к API; исправлены SSRF deny-list, рассинхронизация SMTP (`drainData`), пробел в кавычках локальной части (RFC 5321).
- 1.31.0 (2026-08-22): тема deep dark; исправлена SMTP command injection через `drainData`; проверка ключей заголовков Send API.
- Даты 1.31.0–1.31.3 — время загрузки тегов на Docker Hub (CHANGELOG дат не содержит); для 1.31.4 совпадает с публикацией релиза на GitHub с разницей 16 минут.
- Образ собирается `FROM alpine:latest`: базовый слой определяется моментом сборки тега; `tag_last_pushed` у `v1.31.4` — 2026-10-03, после релиза тег не перезаливался.

## Deprecations and breaking changes from prior version
- Не применимо: первое использование в проекте. В CHANGELOG 1.30.x–1.31.4 несовместимых изменений не помечено. Новое поведение с 1.31.1 — проверка `Host` по `--allowed-hosts` (по умолчанию выключена) и предупреждение в логе при старте без `--allowed-hosts`/`--ui-auth-file`: «[http] listening on … without --ui-auth-file or --allowed-hosts; the API is reachable from any host» — в dev-стеке ожидаемо.

## Project conventions
- Только локальная разработка и тесты: сервис dev-стека `deploy/dev/docker-compose.yml` и контейнер Testcontainers. В Kubernetes-манифестах, `deploy/secret.template.yaml`, образе `app` и production-конфигурации Mailpit не появляется — там корпоративный SMTP relay (`mailkit-4.18.1.md`).
- Образ закреплён точным тегом `axllent/mailpit:v1.31.4` — не `latest`, `v1`, `v1.31`, `edge` (плавающие); один тег в compose и тестах; версия меняется только через спринт вместе с этим документом.
- Порты публикуются только на loopback и переопределяются переменной окружения по образцу `DEV_DB_PORT`: `"127.0.0.1:${DEV_MAIL_SMTP_PORT:-11025}:1025"` (SMTP) и `"127.0.0.1:${DEV_MAIL_UI_PORT:-18025}:8025"` (веб-интерфейс и API, http://localhost:18025). Порт 1110 (POP3) не публикуется.
- В dev-стеке заданы `MP_ALLOWED_HOSTS=localhost` (DNS rebinding, см. «Known issues») и `MP_SMTP_DISABLE_RDNS: "true"`.
- Без тома и без `MP_DATABASE`: письма живут во временной БД процесса и пропадают при остановке контейнера. Именованного тома нет, `down -v` у Mailpit удалять нечего — правило AC-15 о разрушающих командах Docker по-прежнему охраняет `competency-dev-pgdata`.
- Без SMTP-аутентификации и TLS: `src/Competency.Api/appsettings.Development.json` — `Smtp:Host` `127.0.0.1`, `Smtp:Port` 11025, `Smtp:SecureSocketOptions` `None`, учётные данные пустые, поэтому приложение не вызывает `AuthenticateAsync`; `App:PublicBaseUrl` — адрес Vite (http://localhost:5173), в профиле «API» без Vite ссылки из писем не открываются. Значения не в переменных окружения `launchSettings.json`: те перекрыли бы .NET User Secrets, которыми разработчик подменяет Mailpit своей SMTP-песочницей (`deploy/dev/README.md`). `MP_SMTP_AUTH_ACCEPT_ANY` и `MP_SMTP_AUTH_ALLOW_INSECURE` включать только ради проверки ветки с аутентификацией.
- Healthcheck — из образа; собственный `healthcheck:` в compose не дублировать.

## Known issues and workarounds
- DNS rebinding: при пустом `MP_ALLOWED_HOSTS` API принимает любой `Host` (`server/cors.go`), а привязка порта к 127.0.0.1 от DNS rebinding не защищает — страница в браузере разработчика теоретически может прочитать перехваченные письма, в том числе ссылки приглашения и сброса пароля dev-учёток. Смягчение — `MP_ALLOWED_HOSTS=localhost` (в dev-стеке включено): при непустом списке `localhost`, `127.0.0.1` и `::1` разрешены всегда и с любым портом, а `/livez` и `/readyz` проверку `Host` не проходят, так что healthcheck не ломается.
- Обратный DNS на Docker Desktop: без `MP_SMTP_DISABLE_RDNS` каждое SMTP-соединение ждёт приветствия 8–10 с (наблюдение impl-test спринта 002) — запросы администратора с письмом становятся медленными.
- `DELETE /api/v1/messages` без `IDs` удаляет все письма — в контейнере, общем на прогон тестов, это письма других тестов. Письма выбирать поиском по уникальному адресу получателя.
- Поиск регистронезависим только для ASCII (case folding SQLite «не имеет полной поддержки Unicode», docs search-filters) — искать по адресу или `message-id:`, а не по русской теме.
- `GET /api/v1/message/{ID}` помечает письмо прочитанным — после него фильтр `is:unread` даёт другой результат.
- Хост-порт занят или попадает в диапазон, зарезервированный Windows (как 5432, `visual-studio-multiproject-launch-17.11.md`), — переопределить переменной окружения порта.
- Процесс в контейнере работает от root (`USER` в образе нет); для dev-инструмента на loopback допустимо. Запуск от непривилегированного пользователя (`user:` в compose) документацией не описан и не проверялся.
- Проект Visual Studio `deploy/dev/docker-compose.dcproj` (`DockerServiceName=db`) поднимает все сервисы файла, в том числе `mailpit` (`docker compose … up -d` без списка сервисов) — подтверждено smoke-проверкой пользователя в спринте 002, по первичному источнику не сверено.
