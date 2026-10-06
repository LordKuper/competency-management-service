# .NET SDK — образ `mcr.microsoft.com/dotnet/sdk` @ 10.0.401 (noble)

Область: образ build-стадии backend (restore/publish). Язык C# 14, `global.json`, NuGet, SDK 10 breaking changes — `csharp-14.md`; `dotnet-ef` — `dotnet-ef-10.0.12.md`; Docker-стадии — `docker-multi-stage-dockerfile-1.md`; итоговый образ — `dotnet-aspnet-image-10.0.12.md`.

## Canonical source
- Official docs (образ): https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md ; Dockerfile: https://github.com/dotnet/dotnet-docker/blob/main/src/sdk/10.0/noble/amd64/Dockerfile
- Release 10.0.12 (SDK 10.0.401 и 10.0.112): https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md ; метаданные: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- Breaking changes .NET 10 (SDK, контейнеры): https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0
- Last verified: 2026-10-06 (раздел «Проверено на Docker» — запуском, MS-1; остальное — 2026-10-05, по документации)

## API surface used in project
- Образ: `mcr.microsoft.com/dotnet/sdk:10.0.401` (теги `10.0.401`, `10.0.401-noble`, `10.0.401-noble-amd64`, `10.0`, `10.0-noble`); закрепляется по digest. Назначение (README): «development process (developing, building and testing applications)» — в проекте только build-стадия, в итоговый образ не попадает.
- Устройство (Dockerfile): `FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble-amd64` + SDK 10.0.401 (из `dotnet-sdk-10.0.401-linux-x64.tar.gz` с проверкой sha512) + PowerShell 7.6.6; пакеты `curl`, `git`, `libatomic1`, `wget`; в SDK входит `dnx`. Пользователь не задан (root).
- Переменные образа: `DOTNET_NOLOGO=true`, `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`, `DOTNET_SDK_VERSION=10.0.401`, `DOTNET_USE_POLLING_FILE_WATCHER=true`, `NUGET_XMLDOC_MODE=skip`.
- Роль в Dockerfile: стадия `FROM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:<digest> AS build` → `dotnet restore` (по lock-файлам, stack.html) → `dotnet publish` (framework-dependent, runtime pack не нужен) → артефакт копируется в итоговую стадию `aspnet:10.0.12-noble`.
- `dotnet test` в режиме MTP на SDK ≥10 включается в `global.json` (`xunit.v3-4.0.1.md`).

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — SDK 10.0.401 (feature band 4xx) и поведение образов 10 новее среза знаний; release notes, README и Dockerfile прочитаны.
- SDK 10.0.401 и runtime 10.0.12 — пара одного релиза (2026-09-08): базовый слой SDK-образа — `aspnet:10.0.12-noble-amd64`; в ту же пару ставятся тег `aspnet` итогового образа и SDK-тег (при следующем патче обновлять оба).
- Теги SDK 10.0: Ubuntu 24.04 (`noble`), Alpine, Azure Linux, Ubuntu 26.04 (`resolute`); chiseled-тегов для SDK 10.0 в README нет. Debian-образов для .NET 10 нет (`dotnet-aspnet-image-10.0.12.md`).
- Образ содержит `git`, `curl`, `wget` и PowerShell — это крупный образ для сборки; в итоговый образ ничего из него, кроме результата `publish`, не переносится.

## Deprecations and breaking changes from prior version
- По умолчанию теги .NET на Ubuntu (не Debian) — для SDK то же (`…/sdk:10.0` = Ubuntu 24.04).
- Изменения SDK 10 из списка Microsoft, затрагивающие CI/сборку (подробности и действия — `csharp-14.md`): `dotnet restore` аудирует транзитивные пакеты; NU1510 для прямых ссылок, срезанных NuGet; `PackageReference` без версии — ошибка; `dotnet new sln` создаёт `.slnx`; `dotnet` CLI пишет не относящиеся к команде данные в stderr; `--interactive` по умолчанию `true` в пользовательских сценариях; NuGet-аудит не допускает небезопасные HTTP-источники.

## Project conventions
- Образ получается и используется на сборочном хосте вне контура (с интернетом): restore по lock-файлам, внутренние зеркала NuGet не нужны (stack.html); образ SDK в контур не доставляется.
- Версия SDK в образе совпадает с `global.json` (`10.0.401`, `csharp-14.md`) и с парой `aspnet:10.0.12-noble`; цикл патчей .NET — ежемесячно (stack.html).
- Целевая платформа linux/amd64; на хосте другой архитектуры — `--platform linux/amd64`.
- Перед переносом готовых образов — сканирование и SBOM (stack.html).

## Known issues and workarounds
- Образ SDK работает от root (в Dockerfile `USER` не задан); непривилегированный пользователь включается только в итоговой стадии (`dotnet-aspnet-image-10.0.12.md`).
- `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` — dev-сертификат в образе не создаётся; для сборки HTTPS не требуется.

## Проверено на Docker (Task 10, 2026-10-06, MS-1)
- Образ `mcr.microsoft.com/dotnet/sdk:10.0.401`: digest на 2026-10-06 — `sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317` (manifest list, linux/amd64), 1,30 ГБ; внутри Ubuntu 24.04.5 LTS, `dotnet --version` = `10.0.401`, SDK `10.0.401`, рантаймы `Microsoft.NETCore.App 10.0.12` и `Microsoft.AspNetCore.App 10.0.12` (пара с `aspnet:10.0.12-noble` подтверждена).
- Build-стадия `Dockerfile` (копирование `*.csproj` и `packages.lock.json` → `dotnet restore` → `COPY src/` → `dotnet publish -c Release --no-restore -p:UseAppHost=false -p:OpenApiGenerateDocumentsOnBuild=false`) отработала в образе без БД и без доступа к чему-либо, кроме NuGet: restore ≈ 22 с, publish ≈ 7 с. `ContinuousIntegrationBuild=true` (ENV стадии) включает locked-режим restore из `Directory.Build.props`: lock-файлы в контейнере совпали с проектами (ошибок `NU1004` нет). `-p:OpenApiGenerateDocumentsOnBuild=false` выключает генерацию `openapi.json` во время сборки (она запускает приложение); в стадии каталога `openapi/` нет, контракт берётся из репозитория.
- В publish-выводе нет `Microsoft.EntityFrameworkCore.Design`, Roslyn и `ApiDescription.Server` (`PrivateAssets="all"` работает), `Competency.Api.deps.json` их не упоминает.
