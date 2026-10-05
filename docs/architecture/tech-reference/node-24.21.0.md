---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Node.js (только build-стадия) @ 24.21.0

## Canonical source
- Official docs: https://nodejs.org/docs/latest-v24.x/api/ ; релиз: https://nodejs.org/en/blog/release/v24.21.0 ; график: https://nodejs.org/en/about/previous-releases , https://endoflife.date/api/nodejs.json
- Образ: https://hub.docker.com/_/node (тег `24.21.0-trixie-slim`) ; README образов: https://github.com/nodejs/docker-node
- Last verified: 2026-10-05

## API surface used in project
Node не входит в образ `app` и в рантайм: используется только в build-стадии для `npm ci`, `tsc --noEmit`, `biome ci`, `openapi-typescript`, `vite build`, Vitest.
- Образ build-стадии: `node:24.21.0-trixie-slim` (Debian 13, glibc, linux/amd64). Docker Hub на 2026-10-05: тег `active`, обновлён 2026-09-19, manifest-list digest `sha256:8ec5d7557396cfe32d21c3f9c13072355ceab22b584578ca4bb28af31120cffe`, linux/amd64 присутствует (digest сверять при пересборке — закрепление по digest, правило stack.html).
- npm из поставки — 11.19.0 (`npm-11.19.0.md`).

## Version-specific notes
- 24.21.0 — LTS «Krypton», релиз 2026-09-08 (последняя версия цикла 24 на 2026-10-05); OpenSSL 3.5.8, undici 7.29.1; в release notes явных уязвимостей/breaking changes не отмечено.
- График (endoflife.date, nodejs.org): цикл 24 — Active LTS до 2026-10-20, затем Maintenance, EOL 2028-04-30; Node 26 станет LTS 2026-10-28 (последняя 26.10.0). Переход на 26 — через спринт.
- Совместимость: react-router 8.4.0 `engines.node >=22.22.0`; Vite 8.3.2 и @vitejs/plugin-react 6.1.2 `^20.19.0 || >=22.12.0`; npm 11.19.0 `^20.17.0 || >=22.9.0`; jsdom 30.1.2 (тесты) `^22.22.2 || ^24.15.0 || >=26` — 24.21.0 удовлетворяет всем.
- `slim` не содержит Python/make/g++: сборка нативных модулей из исходников невозможна; зависимости проекта — готовые бинарные пакеты (Rolldown, lightningcss, Biome).

## Deprecations and breaking changes from prior version
- Для проекта не применимо (новая зависимость). Deprecation в 24.21.0: флаг сборки `--enable-static` (на проект не влияет).

## Project conventions
- Только build-стадия; не использовать Node как рантайм/прокси в контуре.
- Версия образа — точная (`24.21.0-trixie-slim`) + digest; обновление — через спринт.
- Образ `node` содержит пользователя `node` (не root); в итоговый образ `app` ничего из Node не копируется, кроме статических файлов `dist/`.

## Known issues and workarounds
- Не выявлено. Хостовый `node_modules` (другая ОС) исключать из контекста сборки (`docker-multi-stage-dockerfile-1.md`).
