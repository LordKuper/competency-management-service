---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# React + react-dom @ 19.3.0

## Canonical source
- Official docs: https://react.dev/ ; версии: https://react.dev/versions
- Changelog: https://github.com/facebook/react/blob/main/CHANGELOG.md ; релизы: https://github.com/facebook/react/releases
- npm: https://www.npmjs.com/package/react , https://www.npmjs.com/package/react-dom (манифесты 19.3.0 получены; `react-dom` peer `react ^19.3.0`, dependency `scheduler ^0.28.0`)
- Last verified: 2026-10-05 (dist-tag `latest = 19.3.0`)

## API surface used in project
Клиентская SPA, без SSR/RSC. Один документ на пару `react` + `react-dom` — версия у них общая и обязана совпадать.
- `createRoot` из `react-dom/client` — единственная точка монтирования; `StrictMode`.
- Function components + hooks; `lazy` + `Suspense` для разбиения по маршрутам; ошибки маршрутов — через механизм react-router.
- `ref` как обычный prop (React 19; `forwardRef` не нужен для новых компонентов).
- Не используются: `react-dom/server`, Server Components/Actions, `browser()`/`onBrowserBailout` (только SSR), React Compiler (в стек не входит).

## Version-specific notes
- Ставить `react` и `react-dom` одной точной версией 19.3.0 (peer `react ^19.3.0` у react-dom); react-router 8.4.0 требует ≥19.2.7 — условие выполнено.
- 19.3.0 (2026-09-09, по CHANGELOG и react.dev/versions): `<ViewTransition />` и `addTransitionType`; ref у `<Fragment />`; transitions теперь рендерятся независимо, а не склеиваются в один рендер (изменение поведения); DEV-предупреждение об условном вызове `use()`; интеграция Trusted Types; исправления Fast Refresh (`lazy()`, `memo()`, смена типа компонента); новый `browser()` API и `onBrowserBailout` — только для серверного рендера.
- 19.2.x (19.2.0 — 2025-10-01; 19.2.8 — 2026-07-21): `<Activity>`, `useEffectEvent`, `cacheSignal`, Performance Tracks в DevTools; формат `useId` с `_` вместо `:`.
- Ветки 19.2.x и 19.3 в CHANGELOG не содержат явных breaking changes/deprecations (кроме изменения поведения transitions выше).

## Deprecations and breaking changes from prior version
- 19.2 → 19.3: удалений нет; учитывать независимый рендер transitions (код не должен рассчитывать на объединение обновлений).
- Относительно 18 (для справки; проект новый, на 18-идиомах не пишется): `ReactDOM.render` → `createRoot`, удалены `propTypes`/`defaultProps` у function components, string refs, legacy context. Upgrade guide: https://react.dev/blog/2024/04/25/react-19-upgrade-guide

## Project conventions
- Node нужен только на этапе сборки; в образе `app` React — статические файлы из `dist/`, раздаваемые Kestrel.
- Серверное состояние — TanStack Query, не `useEffect`+`fetch`; клиентская маршрутизация — react-router (Data mode).
- Типы — пакеты `@types/react` 19.3.0 и `@types/react-dom` 19.3.0 (входят в стек, stack.html rev. 7; `types-react-19.3.0.md`, `types-react-dom-19.3.0.md`); версии обновляются вместе с `react`/`react-dom`.
- Никаких CDN-скриптов React; бандл собирается Vite.

## Known issues and workarounds
- Не выявлено. Не проверялось: взаимодействие независимого рендера transitions (19.3) с `useTransitions` по умолчанию в `RouterProvider` react-router 8 (все обновления оборачиваются в `startTransition`) — проверить на реальных экранах.
