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
- Last verified: 2026-10-05

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
