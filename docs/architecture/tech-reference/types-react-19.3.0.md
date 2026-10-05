---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @types/react @ 19.3.0

Область: пакет типов `@types/react` (DefinitelyTyped, `types/react`) — типы для `react` 19.3.0 (`react-19.3.0.md`). Парный пакет — `types-react-dom-19.3.0.md`.

## Canonical source
- npm: https://www.npmjs.com/package/@types/react ; манифест 19.3.0: https://registry.npmjs.org/@types/react/19.3.0 — лицензия MIT, `dependencies: csstype ^3.2.2`, peerDependencies нет, `typeScriptVersion 5.6`, репозиторий DefinitelyTyped, каталог `types/react`
- dist-tags (2026-10-05): `latest = 19.3.0`; теги `ts5.6`–`ts5.9`, `ts6.0`, `ts7.0`, `ts7.1` → 19.3.0 (`ts5.3`–`ts5.5` → 19.2.17) (https://registry.npmjs.org/-/package/@types/react/dist-tags)
- Исходник типов (19.3.0): https://unpkg.com/@types/react@19.3.0/index.d.ts (просмотрена только часть файла — до обработчиков событий `DOMAttributes`)
- Upgrade guide React 19 (раздел «TypeScript changes»): https://react.dev/blog/2024/04/25/react-19-upgrade-guide ; кодмод миграции типов: https://github.com/eps1lon/types-react-codemod/
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — линия `@types/react` 19.x известна, но 19.3.0 новее среза знаний и вышла вместе с React 19.3.0 (2026-09-09, `react-19.3.0.md`) с новыми API (`ViewTransition`, `addTransitionType`, `ref` у `Fragment`); дату публикации самого пакета в реестре не получали (пакет без changelog — отдельного changelog у `@types/*` нет, ориентир — релиз React и upgrade guide).

## API surface used in project
- Пакет нужен только компилятору: `tsc --noEmit` и IDE; в бандл не попадает (dev-зависимость, ставится точной версией `npm i -D -E @types/react@19.3.0`). React 19 собственных типов не поставляет.
- Типизация компонентов и хуков (`FC`/явные сигнатуры, `ReactNode`, `ComponentProps`), JSX через `jsx: "react-jsx"` (`tsconfig`): типы `react/jsx-runtime` берутся из этого пакета; в `tsconfig` `types` перечислять его **не нужно** (он подключается импортом `react`, а не глобально).
- Присутствуют в 19.3.0 (по `index.d.ts`): `ViewTransition`, `addTransitionType`, `FragmentProps` с `ref`, `useActionState`, `useEffectEvent`, `Activity`; `ReactNode` включает `Promise<…>`. Проект использует из них только то, что берёт из React (`react-19.3.0.md`).

## Version-specific notes
- Версия пакета = версия `react` (линия 19.3.x): ставится парой с `@types/react-dom` (peer `@types/react ^19.3.0`).
- Минимальная версия TypeScript по манифесту — 5.6; с TypeScript 6.0.2 совместимость заявлена только минимумом (теги `ts6.0`/`ts7.0` указывают на 19.3.0); прогон `tsc --noEmit` на реальном коде — при реализации.
- В `package.json` пакет `csstype ^3.2.2` ставится транзитивно (типы CSS-свойств для `style`).

## Deprecations and breaking changes from prior version
Changelog по upgrade guide React 19 (изменения типов относительно `@types/react` 18; проект новый, но идиомы 18 не использовать):
- Глобальное пространство имён `JSX` удалено в пользу `React.JSX` — расширять через `declare module "react" { namespace JSX { … } }`.
- `useRef` требует аргумент (`useRef(undefined)`).
- Тип `ReactElement["props"]` — `unknown` вместо `any`.
- Callback `ref` не должен возвращать значения (неявный return в стрелочной функции — ошибка типов; ref cleanup).
- Улучшенный вывод у `useReducer`: без явных параметров типа либо с обоими (state и action).
- Удалённые устаревшие типы мигрируются кодмодом `npx types-react-codemod@latest preset-19`.
- 19.2 → 19.3: типы новых API (см. выше); breaking changes для кода проекта не выявлены (полный diff типов не сверялся).

## Project conventions
- Версия `@types/react` и `@types/react-dom` — 19.3.0, обновляются вместе с `react`/`react-dom` через спринт; одновременно лежат в `package-lock.json`.
- Для `forwardRef` типы не помечены устаревшими, но `ref` — обычный prop (React 19): в новом коде `forwardRef` не использовать (`react-19.3.0.md`).
- Расширение JSX (custom elements) — только через `React.JSX` (module augmentation), не через глобальный `JSX`.

## Known issues and workarounds
- Дубликаты `@types/react` в дереве (несколько версий разных мажоров) ломают типы JSX: в lock-файле должна быть одна версия 19.3.0; проверять `npm ls @types/react`.
- Не проверялось: взаимодействие типов 19.3.0 с `antd` 6.6.5 и `@tanstack/react-query` 5.104.1 на реальном коде (peer `react >=18` у antd; собственные типы пакетов).
