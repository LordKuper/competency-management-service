---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Visual Studio: профили многопроектного запуска (.slnLaunch) и проект Docker Compose (.dcproj) @ 17.11+

## Canonical source
- Профили запуска: https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects
- Docker Compose в Visual Studio: https://learn.microsoft.com/en-us/visualstudio/containers/tutorial-multicontainer
- User Secrets: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0 (ms.date 2026-05-13); порядок источников конфигурации: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0 (ms.date 2026-09-18)
- Last verified: 2026-10-08 (страницы Learn о профилях и Compose обновлены 2026-02-25 и 2026-04-02; User Secrets и конфигурация — 2026-10-08)

## API surface used in project
- `Competency.slnLaunch` (корень репозитория, JSON рядом с решением): профили `Everything` (БД без отладчика, `Competency.Api` под отладчиком, Vite из `web/web.esproj` с браузером) и `API` (БД и Api); у каждого проекта `Path`, `Action` (`Start`, `StartWithoutDebugging`) и `DebugTarget`. Профиль с флагом «Share Profile» хранится в файле для системы контроля версий, личный — в пользовательском.
- `deploy/dev/docker-compose.dcproj` (`Sdk="Microsoft.Docker.Sdk"`) поверх `deploy/dev/docker-compose.yml`: свойства `DockerTargetOS=Linux`, `DockerLaunchAction=None`, `DockerServiceName=db`, `DockerComposeProjectName=competency-dev`; сервисы `db` (PostgreSQL) и `mailpit` (перехватчик почты, `mailpit-1.31.4.md`). Visual Studio поднимает все сервисы файла (`docker compose … up -d` без списка сервисов), `DockerServiceName` выбирает только сервис для действия запуска. `DependencyAwareStart` (VS 17.13+) не используется.
- User Secrets: `UserSecretsId` в `src/Competency.Api/Competency.Api.csproj`; «Manage User Secrets» в контекстном меню проекта или `dotnet user-secrets set|remove|clear … --project src/Competency.Api`; файл `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`. `WebApplication.CreateBuilder` подключает их только в среде `Development`, после `appsettings.json` и `appsettings.{ENVIRONMENT}.json`; переменные окружения (в том числе `environmentVariables` из `launchSettings.json`) их перекрывают. Secret Manager не шифрует значения — только для разработки.

## Version-specific notes
- Многопроектные профили запуска — Visual Studio 2022 17.11 и новее; в 2022 включаются как функция предварительного просмотра (Tools → Options → Environment → Preview Features → «Enable Multi-Project Launch Profiles»), в Visual Studio 2026 — All Settings → Preview Features.
- Для проекта Docker Compose нужны Docker Desktop (в 2026 допускается и Podman Desktop) и рабочая нагрузка Visual Studio с инструментами контейнеров; в сценарии Compose документация предлагает выбирать `docker-compose` единственным запускаемым проектом, а подмножества сервисов задавать профилями Compose, не окном свойств решения. Проект включает `.dcproj` в многопроектный профиль `.slnLaunch` как `StartWithoutDebugging` — такой способ документация для Compose не описывает; профили в Visual Studio запускал и правил пользователь (decisions-log 2026-10-06), запуск всех сервисов файла подтверждён smoke-проверкой пользователя в спринте 002; по первичному источнику не проверено.
- Формат решения `.slnx` требует Visual Studio 2022 17.14+ или 2026 — по `deploy/dev/README.md`, по первичному источнику не перепроверялось.
- `Microsoft.Docker.Sdk` — компонент Visual Studio (инструменты контейнеров), не NuGet-пакет проекта; лицензия — Visual Studio.

## Deprecations and breaking changes from prior version
- Не выявлено в проверенных страницах.

## Project conventions
- Только локальная разработка; исключение из «минимум зависимостей» принято пользователем 2026-10-06 (`stack.html`). `dotnet build`/`dotnet test`, Docker-образ и CI от этих проектов не зависят: для CLI `.dcproj` пуст.
- БД входит в оба профиля: Api применяет миграции при старте и без БД не работает. Visual Studio не ждёт готовности PostgreSQL, поэтому в `Development` Api до минуты повторяет подключение (`MigrateDatabaseAsync(waitForDatabase)`).
- Порты только на `127.0.0.1`: БД 15432 (Windows резервирует диапазоны, куда попадает 5432), Mailpit SMTP 11025 и веб 18025; данные БД — в именованном томе `competency-dev-pgdata` строго в `/var/lib/postgresql`, у Mailpit тома нет; пароли в `docker-compose.yml` и `launchSettings.json` — только для рабочей станции.
- В `launchSettings.json` — только `ConnectionStrings__Default`, `Bootstrap__*` и `ASPNETCORE_ENVIRONMENT`; значения SMTP для Mailpit и `App:PublicBaseUrl` — в `src/Competency.Api/appsettings.Development.json`, чтобы их могли переопределить User Secrets. Личные параметры SMTP (например, песочница Mailtrap) — только User Secrets, вне репозитория; команды — `deploy/dev/README.md`.

## Known issues and workarounds
- Остановка отладки завершает Api и Vite, но контейнеры БД и Mailpit могут остаться запущенными: остановить из окна Containers либо `docker compose -f deploy/dev/docker-compose.yml down` (письма Mailpit пропадают; `down -v` удаляет и данные БД).
- Переменная окружения с тем же ключом, что и в User Secrets (например, `Smtp__Host` в профиле запуска), молча перекрывает секрет.
