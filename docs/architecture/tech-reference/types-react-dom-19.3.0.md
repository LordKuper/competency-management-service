---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @types/react-dom @ 19.3.0

Область: пакет типов `@types/react-dom` (DefinitelyTyped, `types/react-dom`) — типы для `react-dom` 19.3.0 (`react-19.3.0.md`). Парный пакет — `types-react-19.3.0.md`.

## Canonical source
- npm: https://www.npmjs.com/package/@types/react-dom ; манифест 19.3.0: https://registry.npmjs.org/@types/react-dom/19.3.0 — лицензия MIT, зависимостей нет, **peer `@types/react ^19.3.0`**, `typeScriptVersion 5.6`; `package.json`: https://unpkg.com/@types/react-dom@19.3.0/package.json
- dist-tags (2026-10-05): `latest = 19.3.0`; теги `ts5.6`–`ts5.9`, `ts6.0`, `ts7.0`, `ts7.1` → 19.3.0 (`ts5.2`–`ts5.5` → 19.2.3) (https://registry.npmjs.org/-/package/@types/react-dom/dist-tags)
- Upgrade guide React 19 (раздел «TypeScript changes»): https://react.dev/blog/2024/04/25/react-19-upgrade-guide
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — линия 19.x известна, но 19.3.0 новее среза знаний (React 19.3.0 вышел 2026-09-09, `react-19.3.0.md`); у `@types/*` собственного changelog нет — ориентир: релиз React и upgrade guide; дату публикации пакета в реестре не получали.

## API surface used in project
- Точки входа пакета (`exports`): `.`, `./client`, `./server` (+ `server.browser|bun|edge|node`), `./static` (+ `static.browser|edge|node`), `./canary`, `./experimental`, `./test-utils`. Проект использует только `react-dom/client` (`createRoot`); `react-dom/server` и `static` — не используются (SPA без SSR).
- Dev-зависимость, ставится точной версией `npm i -D -E @types/react-dom@19.3.0` вместе с `@types/react@19.3.0`; в бандл не попадает.

## Version-specific notes
- Peer `@types/react ^19.3.0`: ставить пару одной линией (npm разрешит без ERESOLVE при 19.3.0 обоих).
- Минимальная версия TypeScript — 5.6 (манифест); с TypeScript 6.0.2 — по тегу `ts6.0`, проверка на реальном коде — при реализации.

## Deprecations and breaking changes from prior version
- Относительно 18: `ReactDOM.render`/`hydrate` удалены вместе с типами — только `createRoot`/`hydrateRoot` из `react-dom/client`; типы удалённых API убраны (upgrade guide React 19; миграция — `types-react-codemod`).
- 19.2 → 19.3: изменений, влияющих на проект, не выявлено (полный diff типов не сверялся; пакет без changelog).

## Project conventions
- Версия — равна `react-dom` и `@types/react` (19.3.0); обновление — только через спринт.
- В `tsconfig` глобально не перечисляется: пакет подтягивается импортом `react-dom/client`.

## Known issues and workarounds
- Несоответствие версий `@types/react` и `@types/react-dom` (разные минорные линии) — ошибки типов компонентов/`createRoot`; проверять `npm ls @types/react @types/react-dom`.
- Не проверялось на реальном коде SPA (проект ещё не создан).
