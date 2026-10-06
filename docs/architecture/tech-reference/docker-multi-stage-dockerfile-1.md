---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Docker / OCI multi-stage сборка @ Dockerfile syntax 1 (`docker/dockerfile:1`)

> Версия Docker Engine/BuildKit в stack.html не задана («версия и ОС сборочного хоста определяются командой/организацией»). Имя файла несёт версию frontend-синтаксиса Dockerfile (`# syntax=docker/dockerfile:1`, стабильный канал), чтобы документ был адресуемым по `<tech>-<version>`; при выборе конкретной версии Docker документ переименовать.

## Canonical source
- Official docs: https://docs.docker.com/build/building/multi-stage/ ; Dockerfile reference: https://docs.docker.com/reference/dockerfile/
- Базовый образ Node: https://github.com/nodejs/docker-node (варианты `slim`, `trixie`; пользователь `node`); образы .NET: https://github.com/dotnet/dotnet-docker (`sdk:10.0.401`, `aspnet:10.0.12-noble` — проверены в stack.html).
- Last verified: 2026-10-06 (раздел «Проверено на сборке» — запуском, MS-1; остальное — 2026-10-05, по документации)

## API surface used in project
- `# syntax=docker/dockerfile:1` — стабильный канал frontend («latest stable version of the Dockerfile syntax»).
- `FROM <image>:<tag>@sha256:<digest> AS <stage>` — закрепление базовых образов по digest (правило stack.html); `ARG` перед `FROM` допустим в ссылке на образ.
- `FROM --platform=linux/amd64 …` — целевая архитектура linux/amd64 (Q10); фиксирует платформу, если сборочный хост — другой ОС/архитектуры.
- `COPY --from=<stage> <src> <dest>` — перенос артефактов между стадиями; `COPY --chown=user:group`; `USER $APP_UID` в итоговом образе (непривилегированный пользователь образа `aspnet`, UID 1654).
- `RUN --mount=type=cache,target=…` — кэш менеджера пакетов (npm/NuGet) между сборками.
- `docker build --target <stage>`; BuildKit собирает только стадии, от которых зависит целевая (старый builder обрабатывает все предшествующие).
- Стадии по stack.html: (1) `node:24.21.0-trixie-slim` → `npm ci` → проверки/`vite build` → `dist/`; (2) `mcr.microsoft.com/dotnet/sdk:10.0.401` → restore/publish backend; (3) итоговый `mcr.microsoft.com/dotnet/aspnet:10.0.12-noble`: publish + `dist/` из стадий; образ `db` — официальный `postgres:18.6-trixie`, не пересобирается.

## Version-specific notes
- Образ `node:24.21.0-trixie-slim`: Debian 13 (glibc), без сборочных инструментов (Python/make/g++) — нативные зависимости должны приходить готовыми бинарными пакетами (Rolldown, lightningcss, Biome — так и устроены; при `ignore-scripts=true` node-gyp-сборок не будет в любом случае). Присутствует linux/amd64 (проверено по Docker Hub, 2026-10-05).
- Канал `docker/dockerfile:1` плавающий; для воспроизводимости при необходимости закрепить конкретную версию/digest frontend — решение design.
- Сборка выполняется вне контура на машине с интернетом; в контур доставляются готовые образы (registry либо `docker save` → импорт на узлы, Q11 открыт).

## Deprecations and breaking changes from prior version
- Не применимо: версия Docker не закреплена, изменений синтаксиса `docker/dockerfile:1`, влияющих на проект, не выявлено.

## Project conventions
- Все базовые образы — по digest; digest фиксируются в Dockerfile/манифестах при каждом цикле патчей (.NET — ежемесячно, PostgreSQL — минорные релизы).
- `.dockerignore` обязателен: `**/node_modules`, `**/bin`, `**/obj`, `.git` — хостовый `node_modules` (бинарники другой ОС) и артефакты сборки не должны попадать в контекст.
- Слой зависимостей: сначала `package.json` + `package-lock.json` → `npm ci` (с `ignore-scripts=true`), затем исходники.
- В итоговый образ `app` не попадают Node, SDK, исходники; секреты — не в образе (Kubernetes Secrets).
- Перед переносом в контур — сканирование уязвимостей и SBOM для каждого образа (инструменты в stack.html не названы).

## Known issues and workarounds
- Зависимость порядка стадий. stack.html описывает «Node → SPA, .NET SDK → publish», но JSON-документ OpenAPI генерируется при сборке backend (`Microsoft.Extensions.ApiDescription.Server`; YAML при сборке .NET 10 не поддерживается), а TS-типы — из него: стадия SPA зависит от результата сборки .NET (либо JSON/сгенерированные типы лежат в репозитории и проверяются `openapi-typescript --check`). Выбор — design (открытый пункт stack.html rev. 7); от него зависит возможность параллельного исполнения стадий BuildKit. Генерация запускает entry point приложения: в SDK-стадии нет БД — старт не должен к ней обращаться.
- Выбор registry vs tar-доставки (Q11) влияет на сохранение digest после `docker save`/импорта — проверка spike в design (stack.html); здесь не верифицировалось.

## Проверено на сборке (Task 10, 2026-10-06, MS-1)

Среда: Docker 29.8.2 / Docker Desktop 4.94.0 (BuildKit, WSL2, containerd image store), хост Windows 11, linux/amd64; `docker build --progress=plain` из корня репозитория, `Dockerfile` из `7ce17c3` (+ директива `check=skip`, см. ниже).
- Сборка со значениями `ARG` по умолчанию (теги) и сборка с `--build-arg` по digest проходят. Теги на 2026-10-06 разрешились (`docker buildx imagetools inspect`) в: `node:24.21.0-trixie-slim` → `sha256:173f125896c3b47ddf056734c7ea789d04595a6a08769a8f78e0df642781fb66` (заметка `node-24.21.0.md` от 2026-10-05 называла другой digest — тег сдвинулся или Docker Hub отдал иное представление индекса; поэтому закрепление по digest обязательно); `mcr.microsoft.com/dotnet/sdk:10.0.401` → `sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317`; `mcr.microsoft.com/dotnet/aspnet:10.0.12-noble` → `sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4`; `postgres:18.6-trixie` → `sha256:fc973eb97c9fd04bfa1840e0f510719a584ccb3be8debfe6a4144637a9dfe8cf`. Реальные digest в `ARG` по умолчанию не записываются (остаются тегами); для поставки их подставляют `--build-arg` (`deploy/README.md`).
- Воспроизводимость: 38 файлов `/app` (сборка по тегам и сборка `--no-cache` по digest) совпали побайтно (`sha256sum` каждого файла), включая `Competency.*.dll` и `wwwroot/assets/index-*.js`.
- Времена (хост выше, интернет есть): первая сборка ≈ 3,5 мин, почти всё — pull образов (SDK ≈ 1,3 ГБ ≈ 190 с); `--no-cache` с уже загруженными базовыми образами — `npm ci` 22 с (205 пакетов), `dotnet restore` 22 с (по lock-файлам, `ContinuousIntegrationBuild=true`), `dotnet publish` 7 с, `npm run build` 7 с. Контекст сборки с `.dockerignore` — 1,09 МБ.
- BuildKit lint: на константу `FROM --platform=linux/amd64` (3 строки) выдаёт предупреждение `FromPlatformFlagConstDisallowed`. Константа осознанная (Q10), поэтому вторая строка Dockerfile — директива `# check=skip=FromPlatformFlagConstDisallowed`; после неё сборка без предупреждений.
- Образ `app`: linux/amd64, 354 591 369 Б; `USER 1654` (`app:app`), `ENTRYPOINT ["dotnet","Competency.Api.dll"]`, `ASPNETCORE_HTTP_PORTS=8080`, `DataProtection__KeysPath=/var/lib/competency/keys`. `/app/wwwroot` — `index.html`, `assets/`, `fonts/` (`PTSans-Regular.woff2`, `PTSans-Bold.woff2`, `PTSansNarrow-Bold.woff2`, `PTMono-Regular.woff2`, лицензии OFL, README); по HTTP проверены `/` и `/fonts/PTSans-Regular.woff2` (`text/html`, `font/woff2`); SPA-маршрут `/org-units` отдаёт `index.html`, `/api/v1/…` без входа — 401 `problem+json`, а не `index.html`.
- В `/app` (29 файлов верхнего уровня) нет `Microsoft.EntityFrameworkCore.Design`, Roslyn/`CodeAnalysis`, `ApiDescription.Server`, `Mono.TextTemplating`, и `Competency.Api.deps.json` их не упоминает; apphost не создаётся (`UseAppHost=false`); `.pdb`, XML-документация и `web.config` попадают в образ (безвредно, не убирались). Каталог ключей — владелец 1654, запись из-под uid 1654 проверена, приложение создаёт там `key-*.xml` (0600).
- `npm ci` в Linux-контейнере (`node:24.21.0-trixie-slim`, node 24.21.0, npm 11.19.0) прошёл по существующему `web/package-lock.json`: записи linux-x64 в нём есть, регенерация не понадобилась; установлены `@biomejs/cli-linux-x64`, `@rolldown/binding-linux-x64-gnu`, `lightningcss-linux-x64-gnu`; `npm run lint` (`biome ci` + typecheck) внутри стадии `spa` проходит. Стадию отдельно собирает `docker build --target spa`; `.npmrc` (`ignore-scripts=true`, `save-exact=true`, `engine-strict=true`) в образ копируется (в нём сборка работает без postinstall-скриптов).
- Запуск образа с `--cap-drop ALL --security-opt no-new-privileges` под uid 1654 — работает. Остановка (`docker stop`, SIGTERM) — 1 с, код выхода 0, в логе «Application is shutting down...».
