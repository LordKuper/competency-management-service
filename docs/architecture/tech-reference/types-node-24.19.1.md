---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @types/node @ 24.19.1

Область: пакет типов `@types/node` (DefinitelyTyped, `types/node`), линия 24.x — типы Node.js 24 для `vite.config.ts`, Vitest и скриптов сборки. Среда выполнения — `node-24.21.0.md`.

## Canonical source
- npm: https://www.npmjs.com/package/@types/node ; манифест 24.19.1: https://registry.npmjs.org/@types/node/24.19.1 — лицензия MIT, `dependencies: undici-types >=7.24.0 <7.24.7`, peerDependencies нет, `typeScriptVersion 5.6`, `typesVersions` для TypeScript ≤5.6 и ≤5.7 (подкаталоги `ts5.6/*`, `ts5.7/*`); `package.json`: https://unpkg.com/@types/node@24.19.1/package.json
- Выбор линии: dist-tag `latest` = **26.6.4** (линия 26), поэтому `npm i -D @types/node` без версии поставит типы Node 26 — несовместимо с рантаймом сборки Node 24. Линия 24 разрешена через https://data.jsdelivr.com/v1/packages/npm/@types/node/resolved?specifier=24 → 24.19.1; версии 24.19.2 и 24.20.0 в реестре отсутствуют (HTTP 404, 2026-10-05). Теги `ts5.0`–`ts5.9`, `ts6.0`, `ts7.0`, `ts7.1`: https://registry.npmjs.org/-/package/@types/node/dist-tags (тег `ts6.0` указывает на 26.6.4, то есть на линию 26, а не 24)
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — линия `@types/node` 24 известна, но точный патч 24.19.1 и его зависимость `undici-types` в узком диапазоне новее среза знаний; у `@types/*` собственного changelog нет — ориентир: релиз-ноты Node 24 (`node-24.21.0.md`). Дату публикации 24.19.1 в реестре не получали.

## API surface used in project
- Только design-time/сборка: типы модулей Node (`node:path`, `node:url`, `node:fs`, глобалы `process`, `__dirname`-аналоги) для `vite.config.ts`, конфигурации Vitest и скриптов; в бандл SPA не попадает.
- Dev-зависимость, точная версия: `npm i -D -E @types/node@24.19.1`.
- Опциональный peer-пакет двух инструментов стека: Vite 8.3.2 — `@types/node ^20.19.0 || >=22.12.0` (optional); Vitest 5.0.3 — `@types/node ^22.0.0 || >=24.0.0` (optional). 24.19.1 удовлетворяет обоим диапазонам; при установке пакета npm не должен показать ERESOLVE.

## Version-specific notes
- Версия типов (24.19.1) отстаёт от рантайма сборочного образа (Node 24.21.0): `@types/node` версионируется по мажору Node и не повторяет минор/патч; API, добавленные в Node 24.20+/24.21, могут не иметь типов. Ожидаемо проект использует только базовые API (`path`, `url`, `fs`) для конфигов — не критично, фиксируется как наблюдение (проверить при реализации).
- Транзитивно ставится `undici-types` с очень узким диапазоном `>=7.24.0 <7.24.7` — следить за lock-файлом при обновлении: недопустимо разрешение вне диапазона.
- Минимальный TypeScript по манифесту — 5.6; для TypeScript 6.0.2 используются типы верхнего уровня (старые `ts5.6/ts5.7` подкаталоги — только для TypeScript ≤5.7).
- В TypeScript 6 умолчание `types: []` — глобальные типы (`node`, `vite/client`) в `tsconfig` перечисляются явно (`typescript-6.0.2.md`).

## Deprecations and breaking changes from prior version
- Типы линии 24 следуют API Node 24 (сопоставление с линией 22 для проекта не проводилось — проект новый); `latest` (26.x) — не применять.
- Для патчей 24.x в рамках линии breaking changes не ожидаются (типы одной линии), но выдача отдельного changelog `@types/node` отсутствует — сверка только через diff типов при обновлении (через спринт).

## Project conventions
- Линия типов совпадает с мажором Node сборочного образа (`node:24.21.0-trixie-slim`); при переходе на Node 26 (LTS 2026-10-28, `stack.html`) `@types/node` переводится на 26.x в том же спринте.
- В `tsconfig` для **браузерного кода SPA** тип `node` не включать (иначе глобали Node «протекают» в код, исполняемый в браузере); включать только для `vite.config.ts`/скриптов и тестов — разделение tsconfig — решение реализации.
- Обновление — только через спринт, точной версией, `package-lock.json` в репозитории.

## Known issues and workarounds
- `npm i -D @types/node` без версии ставит 26.6.4 — всегда `@types/node@24.19.1` (или `-E` с точной версией).
- Не проверялось на реальных конфигурациях Vite 8 / Vitest 5 с TypeScript 6.0.2 (проект ещё не создан).
