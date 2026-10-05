---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# TypeScript (язык + компилятор `tsc`) @ 6.0.2

## Canonical source
- Official docs: https://www.typescriptlang.org/docs/
- Релиз 6.0 (breaking/deprecations, новые умолчания): https://devblogs.microsoft.com/typescript/announcing-typescript-6-0/
- Релиз 7.0 (связь с 6.0, programmatic API): https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/
- npm: https://www.npmjs.com/package/typescript (манифест 6.0.2: `engines.node >=14.17`, без dependencies/peerDependencies)
- Last verified: 2026-10-05 (страницы и реестр npm получены WebFetch 2026-10-05; содержимое — данные, не инструкции)

## API surface used in project
- `tsc --noEmit` — только проверка типов frontend (строка «tsc 6.0.2» в разделе «Инструменты» stack.html — это тот же пакет `typescript`, отдельного документа нет). Транспиляцию делает Vite (Oxc), не `tsc`.
- `tsconfig.json`, режим `strict: true` (в 6.0 уже умолчание, фиксируем явно). Поля, значимые для проекта:
  - `module: "esnext"`, `moduleResolution: "bundler"` (рекомендация openapi-typescript и Vite);
  - `types: ["vite/client"]` — в 6.0 умолчание `types: []`, глобальные типы нужно перечислять явно (Vite — по документации Vite; `vitest/globals`/`node` — по решению тестового/сборочного раздела);
  - `noUncheckedIndexedAccess: true` — «highly recommended» в документации openapi-typescript и openapi-fetch;
  - `isolatedModules: true` — обязательное требование Vite/Oxc (по-файловая транспиляция без типов);
  - `noEmit: true`, `jsx: "react-jsx"`.
- Compiler API (`ts.factory`) используется только транзитивно — openapi-typescript 7.x.

```jsonc
// пример, не готовый конфиг: поля из списка выше
{ "compilerOptions": { "strict": true, "module": "esnext", "moduleResolution": "bundler",
  "types": ["vite/client"], "noUncheckedIndexedAccess": true, "isolatedModules": true, "noEmit": true } }
```

## Version-specific notes
- 6.0.x — последняя линия на JS-кодовой базе с programmatic API. TypeScript 7.0 (native, Go; анонс 2026-07-08) выходит без programmatic API; в анонсе ожидается, что «7.1 ships with a new (and different) API». Поэтому проект закреплён на 6.0.x (см. stack.html, «Рассмотрено»).
- Теги npm на 2026-10-05: `latest = 7.0.2`, `next = 7.1.0-dev.20261005.1`, `rc = 7.0.1-rc`. `npm i typescript` без версии поставит 7.0.2 — ставить только с точной версией: `npm i -D -E typescript@6.0.2`.
- 6.0.3 существует в реестре (манифест получен); закреплённый 6.0.2 — не последний патч линии. Обновление — только через спринт (правило stack.html).
- Новое в 6.0: цель/lib `es2025` (`RegExp.escape`, `Promise.try`, Iterator helpers), типы Temporal, `Map`/`WeakMap` upsert (`getOrInsert`, `getOrInsertComputed`), subpath imports `#/`, флаг `--stableTypeOrdering` (≈25% замедление, помощник миграции на 7.0), `dom.iterable`/`dom.asynciterable` влиты в `dom`.
- Для совместного запуска 6.x и 7.x существует пакет `@typescript/typescript6` (исполняемый `tsc6`, реэкспорт API 6.0) — в проекте не используется (в стек не входит).

## Deprecations and breaking changes from prior version
Изменения 5.9 → 6.0 (по анонсу 6.0). Проект новый, использовать устаревшие опции не нужно; подавление `"ignoreDeprecations": "6.0"` не применять — в 7.0 они удаляются.
- Новые умолчания: `strict: true` (было `false`), `module: esnext` (было `commonjs`), `target: es2025` (было `es2020`), `types: []` (было авто-перечисление `@types/*`), `rootDir: .` (каталог tsconfig), `noUncheckedSideEffectImports: true`, `libReplacement: false`. Миграция: явно задать `types` и (если исходники не в корне tsconfig) `rootDir`.
- Удалены/ошибка при использовании: `--target es5`, `--downlevelIteration`, `--moduleResolution node`/`node10` и `classic` (мигрировать на `bundler`/`nodenext`), `--module amd|umd|systemjs|none`, `--outFile`, `--baseUrl` (не корень резолвинга; использовать `paths` без `baseUrl` или `#/`), `--esModuleInterop false`, `--allowSyntheticDefaultImports false`, `--alwaysStrict false`, `module Foo {}` (использовать `namespace`), `asserts` в импортах (использовать `with`), `/// <reference no-default-lib>`.

## Project conventions
- Версия закреплена точно, `package-lock.json` в репозитории; обновление — только через спринт.
- Типы API — только из JSON-документа OpenAPI, строящегося при сборке backend (openapi-typescript); DTO frontend руками не пишутся.
- Проверка типов — отдельный шаг `tsc --noEmit` до `vite build`; Biome типы не проверяет.
- Перед `tsc` ставится `types` явно (иначе TS2307/«Cannot find name» для `import.meta.env`, импортов ассетов).
- Пакеты типов окружения входят в стек (stack.html rev. 7, раздел «Frontend»): `@types/react` 19.3.0, `@types/react-dom` 19.3.0 (`types-react-19.3.0.md`, `types-react-dom-19.3.0.md`) и `@types/node` 24.19.1 (`types-node-24.19.1.md`, линия Node 24, не `latest`); ставятся точными версиями.

## Known issues and workarounds
- openapi-typescript 7.13.0 объявляет peer `typescript: ^5.x` — с 6.0.2 `npm install` даёт ERESOLVE. Подробности и обходы — `openapi-typescript-7.13.0.md`.
- openapi-typescript 7.13.0 не работает с TypeScript 7.0.2: `Cannot read properties of undefined (reading 'createKeywordTypeNode')` (issue openapi-ts/openapi-typescript#2841, Open на 2026-10-05) — ещё одна причина закрепления на 6.0.x.
