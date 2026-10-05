---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Vite @ 8.3.2

## Canonical source
- Official docs: https://vite.dev/guide/ ; миграция 7→8: https://vite.dev/guide/migration
- Конфигурация: https://vite.dev/config/server-options , https://vite.dev/config/build-options ; env: https://vite.dev/guide/env-and-mode ; TS/CSS: https://vite.dev/guide/features
- Changelog: https://raw.githubusercontent.com/vitejs/vite/main/packages/vite/CHANGELOG.md
- npm: https://www.npmjs.com/package/vite (манифест 8.3.2: `engines.node ^20.19.0 || >=22.12.0`; dependencies `rolldown ~1.2.11`, `lightningcss ^1.33.0`, `postcss ^8.5.28`, `picomatch ^4.0.7`, `tinyglobby ^0.2.17`; optional `fsevents ~2.3.3`; все peer — optional: `esbuild ^0.27 || ^0.28`, `terser`, `sass`, `less`, `stylus`, `@types/node` и др.)
- Last verified: 2026-10-05 (dist-tag `latest = 8.3.2`; релиз 8.3.2 — 2026-10-01 по CHANGELOG)

## API surface used in project
- `vite.config.ts`: `defineConfig({ plugins: [react()], server: { proxy }, build })` (плагин — `vitejs-plugin-react-6.1.2.md`).
- `vite build` → статический `dist/` (копируется в образ `app` и раздаётся Kestrel); `vite` (dev-server) — только разработка; `vite preview` — не для продакшена.
- Dev-прокси на Kestrel: `server.proxy: { '/api': { target: 'http://localhost:<порт Kestrel>' } }` (строка-сокращение или объект `target`/`changeOrigin`/`rewrite`/`ws`/`secure`/`configure`); умолчания `server.port` 5173, `server.host` localhost.
- `index.html` — точка входа в корне проекта (не в `public/`).
- Env: только переменные с префиксом `VITE_` попадают в клиентский бандл (`import.meta.env`) и **вшиваются в сборку** — секретов там быть не должно.
- Типы окружения: `"types": ["vite/client"]` в tsconfig; Vite **не проверяет типы** (отдельный `tsc --noEmit`) и игнорирует `target` из tsconfig (используются `oxc.target` в dev и `build.target`).

## Version-specific notes
- Vite 8 заменил esbuild+Rollup на **Rolldown + Oxc**; минификация JS — Oxc, CSS — Lightning CSS (по умолчанию).
- Умолчание `build.target = 'baseline-widely-available'` (Baseline на 2026-01-01): Chrome/Edge 111+, Firefox 114+, Safari 16.4+. Соответствие браузеров контура — проверить.
- `build.outDir = dist`, `sourcemap` выключен, `assetsInlineLimit` 4 KiB, `emptyOutDir` включён, `build.rolldownOptions` — доступ к настройкам Rolldown.
- **Нативные optional-бинарники.** `rolldown` и `lightningcss` подтягивают платформенные пакеты: `@rolldown/binding-<платформа>@1.2.11` (в т.ч. `linux-x64-gnu`, `linux-x64-musl`, `win32-x64-msvc`, `darwin-*`) и `lightningcss-<платформа>@1.33.0`. Проверено по манифестам: `@rolldown/binding-linux-x64-gnu` и `lightningcss-linux-x64-gnu` объявляют `os: linux`, `cpu: x64`, `libc: glibc`, **без install-скриптов** — `ignore-scripts=true` их работе не мешает. Сборочный образ `node:24.21.0-trixie-slim` (Debian, glibc, linux/amd64) → нужны варианты `-gnu`; на Alpine (musl) потребовались бы `-musl`. Требование к `package-lock.json` — см. `npm-11.19.0.md`.
- Vite 8.3.x: безопасность dev-сервера — `server.fs.strict` включён, `server.fs.deny` расширен типовыми файлами (с 8.1.0), `server.allowedHosts` по умолчанию localhost/127.0.0.1/::1.

## Deprecations and breaking changes from prior version
Vite 7 → 8 (по migration guide; проект новый — новые имена опций использовать сразу):
- `esbuild` → `oxc`; `optimizeDeps.esbuildOptions` → `optimizeDeps.rolldownOptions`; `build.rollupOptions`/`worker.rollupOptions` → `rolldownOptions`; `build.minify: 'esbuild'` deprecated; `output.manualChunks` (объектная форма удалена, функциональная deprecated) → `codeSplitting`;
- удалены: `build.commonjsOptions` (no-op), `resolve.alias[].customResolver` (использовать плагин с `resolveId`), `import.meta.hot.accept()` с URL-параметром; форматы `system`/`amd` не поддерживаются;
- изменена CJS-интероперабельность (поведение зависит от типа файла импортёра, `type` в package.json, `__esModule`; откат — `legacy.inconsistentCjsInterop: true`); `require()` внешних модулей сохраняется как есть;
- нативные декораторы Oxc пока не поддерживаются; свойство-манглинг не поддерживается;
- умолчание `build.target` поднято (Chrome 107→111, Firefox 104→114, Safari 16.0→16.4).

## Project conventions
- Vite — только сборка и dev-server; продакшен-раздача — Kestrel; Node нужен лишь в build-стадии образа.
- Сборка выполняется вне контура; в образ попадает только `dist/`.
- Прокси `/api` в dev повторяет схему продакшена (один origin, cookie-сессия).
- Версия закреплена точно; `vite.config.ts` использует только имена опций Vite 8 (`rolldownOptions`, `oxc`).

## Known issues and workarounds
- Платформенные бинарники в lock-файле: если lock сгенерирован на Windows/macOS при уже существующем `node_modules`, npm мог записать только бинарники текущей платформы (issue npm/cli#4828, статус по странице — Open) → `npm ci` в linux-образе не получит `@rolldown/binding-linux-x64-gnu`. Обход — регенерация lock с нуля (удалить `node_modules` и `package-lock.json`) и проверка наличия записей `node_modules/@rolldown/binding-linux-x64-gnu`, `node_modules/lightningcss-linux-x64-gnu`, `node_modules/@biomejs/cli-linux-x64`.
- Хост-`node_modules` (win32-бинарники) не должен попадать в Docker-контекст — `.dockerignore` (см. `docker-multi-stage-dockerfile-1.md`).
