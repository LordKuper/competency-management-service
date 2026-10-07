# Testcontainers.PostgreSql @ 4.15.0

## Canonical source
- Official docs: https://dotnet.testcontainers.org/ ; модуль: https://dotnet.testcontainers.org/modules/postgres/ ; создание контейнеров: https://dotnet.testcontainers.org/api/create_docker_container/ ; конфигурация: https://dotnet.testcontainers.org/custom_configuration/
- Release notes: https://github.com/testcontainers/testcontainers-dotnet/releases (4.15.0 — 2026-09-07)
- Пакет: https://www.nuget.org/packages/Testcontainers.PostgreSql/4.15.0 (MIT; nuspec: https://api.nuget.org/v3-flatcontainer/testcontainers.postgresql/4.15.0/testcontainers.postgresql.nuspec)
- Исходники модуля: https://github.com/testcontainers/testcontainers-dotnet/tree/4.15.0/src/Testcontainers.PostgreSql
- Last verified: 2026-10-05

## API surface used in project
- `new PostgreSqlBuilder("postgres:18.6-trixie").Build()` — **образ передаётся явно** (конструктор с `string image` или `IImage`); example:
  `var pg = new PostgreSqlBuilder("postgres:18.6-trixie").Build(); await pg.StartAsync(); var cs = pg.GetConnectionString();`
- Значения по умолчанию: БД `postgres`, пользователь `postgres`, пароль `postgres`, порт `5432`; переопределение — `WithDatabase`, `WithUsername`, `WithPassword` (ставят `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD`); `WithSsl(cert, key[, ca])`.
- `GetConnectionString()` возвращает строку Npgsql (`Host`, `Port` — отображённый публичный порт, `Database`, `Username`, `Password`). `ExecScriptAsync(string, ct)` выполняет SQL-скрипт через `psql` в контейнере.
- Ожидание готовности — встроенная стратегия на `pg_isready --host localhost --dbname <db> --username <user>` (если в образе нет `pg_isready` — `NotSupportedException`); `Build()` требует непустой пароль.
- Порт хоста не фиксировать: docs требуют случайный хост-порт (`WithPortBinding(port, true)` + `GetMappedPublicPort(port)`); `GetConnectionString()` уже использует отображённый публичный порт.
- Пакет тянет `Testcontainers` 4.15.0 → Docker.DotNet.Enhanced 4.3.3 (+ .X509), Microsoft.Extensions.Logging.Abstractions 8.0.3, SSH.NET 2026.0.0, SharpZipLib 1.4.2 (группы net8.0/9.0/10.0); TFM пакета: net8.0, net9.0, net10.0, netstandard2.0/2.1.

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — между известной автору линией 4.x и 4.15.0 несколько минорных релизов с breaking changes (4.10, 4.12); release notes 4.10–4.15 прочитаны.
- Модуль запускает postgres с `fsync=off`, `full_page_writes=off`, `synchronous_commit=off` — тесты не проверяют поведение durability.
- Требуется Docker-API-совместимый runtime (на Linux — Docker, на Windows/macOS — Docker Desktop); Podman и удалённые Docker-хосты заявлены как «может работать, не тестируется активно». Linux-контейнеры поддерживаются на всех ОС.
- Для платформы образа доступен `DockerImage` с явной платформой (например `linux/amd64`) — целевая архитектура проекта linux/amd64.
- Настройки среды: `DOCKER_HOST`, `DOCKER_CONTEXT`, `DOCKER_API_VERSION`, `TESTCONTAINERS_HOST_OVERRIDE`, `TESTCONTAINERS_RYUK_DISABLED`, `TESTCONTAINERS_HUB_IMAGE_NAME_PREFIX` (подмена префикса Docker Hub, с 4.13.0), файл `~/.testcontainers.properties`. Cleanup — Ryuk (Resource Reaper).
- Поведение 4.15.0: повторы при временных ошибках pull образа; отключение от вывода контейнера перед остановкой/удалением.

## Deprecations and breaking changes from prior version
- 4.10.0 (2026-01-01): создание билдера **требует явного образа** (в 4.15.0 беспараметрический `PostgreSqlBuilder()` помечен `[Obsolete]` в исходниках); `Testcontainers.Xunit` требует явного образа; модуль EventStoreDb удалён; Docker Engine API закреплён на 1.44 (для Docker v29 — переопределение до 1.52, см. `DOCKER_API_VERSION`); образ «по умолчанию» (на странице модуля — `postgres:15.1`) использовать нельзя — пинить версию явно.
- 4.12.0 (2026-05-19): Docker.DotNet 3.131.1 → 4.0.2 (breaking); в 4.15.0 зависимость — Docker.DotNet.Enhanced 4.3.3.
- 4.11.0: breaking только для CosmosDb (не используется). 4.14.0: исправлена уязвимость SSH.NET через обновление зависимостей.

## Project conventions
- Образ тестов совпадает с боевым: `postgres:18.6-trixie` (`postgres-image-18.6-trixie.md`); версия тега берётся из одного места и обновляется вместе с образом `db`.
- Контейнер поднимается на сборочном/CI-хосте **вне контура** (Docker daemon + доступ в интернет для pull образа, stack.html); в контуре тесты не запускаются.
- Контейнер — один на коллекцию/сборку через fixture xunit (`xunit.v3-4.0.1.md`, `AssemblyFixture` / `ICollectionFixture`), строка подключения из `GetConnectionString()` подставляется в конфигурацию приложения/DbContext.
- Как сделано в `tests/Competency.Tests`: один `PostgreSqlContainer` на прогон (`TestEnvironment`, `AssemblyFixture`); каждому запускаемому хосту API — своя пустая база (`CREATE DATABASE`), миграции и первого администратора применяет сам хост при старте. API стартует дочерним процессом из сборки `Competency.Api` (настоящие Kestrel и middleware), а не через `WebApplicationFactory`; строка подключения передаётся переменной `ConnectionStrings__Default` с `GSS Encryption Mode=Disable`.
- Не мокать БД: доступ к данным (FTS `russian`, `pg_trgm`, `SKIP LOCKED`, миграции) проверяется на реальном PostgreSQL 18.6.

## Known issues and workarounds
- Явная совместимость модуля 4.15.0 с образами PostgreSQL 18 в документации не заявлена — проверять первым интеграционным тестом (в 18-образе PGDATA другой, но модуль том не монтирует).
- Страница модуля в документации показывает `postgres:15.1` — это пример, не значение по умолчанию для 4.15.0.
- `pg_trgm` / локаль кластера в тестовом контейнере следуют образу (`LANG=en_US.utf8`); поведение русской сортировки и поиска проверять тем же тестом, что и на `db`.
