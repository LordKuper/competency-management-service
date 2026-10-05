---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# npm (из поставки Node.js 24.21.0) @ 11.19.0

## Canonical source
- Official docs: https://docs.npmjs.com/cli/v11/ ; `npm ci`: https://docs.npmjs.com/cli/v11/commands/npm-ci ; `package.json` (overrides, engines): https://docs.npmjs.com/cli/v11/configuring-npm/package-json ; конфиг: https://docs.npmjs.com/cli/v11/using-npm/config
- Версия npm в stack.html не указана («из поставки Node 24.21.0»). 11.19.0 получена из `deps/npm/package.json` в теге `v24.21.0` репозитория nodejs/node (https://raw.githubusercontent.com/nodejs/node/v24.21.0/deps/npm/package.json; `engines.node ^20.17.0 || >=22.9.0`); страница релиза Node версию npm не называет. Для сравнения: `latest` в реестре — npm 12.2.0 (`engines.node ^22.22.2 || ^24.15.0 || >=26.0.0`) — отдельно не ставится.
- Node.js 24.21.0 (LTS «Krypton», релиз 2026-09-08; OpenSSL 3.5.8, undici 7.29.1): https://nodejs.org/en/blog/release/v24.21.0 ; образ `node:24.21.0-trixie-slim` на Docker Hub (на 2026-10-05: тег active, обновлён 2026-09-19, manifest-list digest `sha256:8ec5d7557396cfe32d21c3f9c13072355ceab22b584578ca4bb28af31120cffe`, linux/amd64 присутствует) — digest сверять при пересборке, закрепление по digest — правило stack.html.
- Last verified: 2026-10-05

## API surface used in project
- `npm ci` — установка строго по `package-lock.json`: требует lock-файл, падает при расхождении с `package.json`, удаляет `node_modules`, ничего не пишет в `package.json`/lock.
- `.npmrc`: `ignore-scripts=true` (lifecycle-скрипты зависимостей не выполняются; явные `npm run <script>` работают, pre/post-хуки пропускаются), `save-exact=true` (точные версии без `^`), `engine-strict=true` (рекомендация: отказ при несовместимом Node).
- Секция `overrides` корневого `package.json` (для peer-конфликта openapi-typescript — см. `openapi-typescript-7.13.0.md`).
- Скрипты `npm run`: lint (`biome ci`), typecheck (`tsc --noEmit`), `vite build`, test (Vitest) — состав задаёт commands.yaml.

## Version-specific notes
- Сборка вне контура, на хосте с интернетом: внутреннее зеркало npm не нужно (Q6 решён).
- npm выбирает платформенные optional-зависимости по полям `os`/`cpu`/`libc`; опции конфигурации `os`/`cpu`/`libc` позволяют принудительно ставить пакеты другой платформы (для отладки lock-файла).
- Бинарные пакеты Vite/Rolldown, lightningcss, Biome (`@rolldown/binding-linux-x64-gnu`, `lightningcss-linux-x64-gnu`, `@biomejs/cli-linux-x64`) без install-скриптов (проверено по манифестам) → совместимы с `ignore-scripts=true`.
- `npm ci` требует тех же флагов, с которыми создан lock (`--legacy-peer-deps`, `--install-links`).

## Deprecations and breaking changes from prior version
- В рамках стека deprecations не выявлено. `engines` самих пакетов проверены: react-router 8.4.0 `node >=22.22.0`, Vite 8.3.2 и @vitejs/plugin-react 6.1.2 `^20.19.0 || >=22.12.0` — Node 24.21.0 удовлетворяет.

## Project conventions
- `package-lock.json` в репозитории; на сборочном хосте — только `npm ci` с `ignore-scripts=true`; версии зависимостей — точные.
- Lock-файл обязан содержать записи платформенных бинарников для **linux-x64 (glibc)** и платформы разработки. Проверка после каждого изменения зависимостей: наличие `node_modules/@rolldown/binding-linux-x64-gnu`, `node_modules/lightningcss-linux-x64-gnu`, `node_modules/@biomejs/cli-linux-x64` в `package-lock.json`.
- Не использовать `legacy-peer-deps` глобально (отключает peer-контроль всего дерева; документация npm: «not recommended»).

## Known issues and workarounds
- npm/cli#4828 (по странице — Open, помечена как «needs review»): при перегенерации lock-файла поверх существующего `node_modules` в нём остаются только optional-бинарники текущей платформы; на другой ОС npm молча их не ставит. Обход: удалить `node_modules` и `package-lock.json`, переустановить (`npm install`) и проверить записи (см. выше); lock регенерировать на машине, где проверка проходит, либо в linux-контейнере.
- Размещать в Docker-контексте только `package.json`/`package-lock.json` для слоя зависимостей; хостовый `node_modules` исключать `.dockerignore`.
