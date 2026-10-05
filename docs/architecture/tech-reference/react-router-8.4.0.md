---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# react-router @ 8.4.0

## Canonical source
- Official docs: https://reactrouter.com/ (на 2026-10-05 «Current version 8.4.0»)
- Режимы: https://reactrouter.com/start/modes ; upgrade v7→v8: https://reactrouter.com/upgrading/v7
- API: https://reactrouter.com/api/data-routers/createBrowserRouter , https://reactrouter.com/api/data-routers/RouterProvider
- Changelog: https://raw.githubusercontent.com/remix-run/react-router/main/CHANGELOG.md
- npm: https://www.npmjs.com/package/react-router (манифест 8.4.0: `engines.node >=22.22.0`, peer `react >=19.2.7`, `react-dom >=19.2.7` (optional), dependencies `cookie-es ^3.1.1`, `@remix-run/route-pattern ^0.22.1`; `exports`: `.`, `./dom`, `./internal`, `./route-pattern`)
- Last verified: 2026-10-05 (`latest = 8.4.0`)

## API surface used in project
Data mode, без SSR и без Framework mode (`@react-router/dev`, Vite-плагин — не входят в стек).
- `createBrowserRouter(routes, opts?)` из `"react-router"`; `opts`: `basename`, `dataStrategy`, `future`, `getContext`, `hydrationData`, `instrumentations`, `patchRoutesOnNavigation`, `window`. Проекту нужны `routes` и, при размещении не в корне, `basename`.
- `RouterProvider` из **`"react-router/dom"`** (включает `ReactDOM.flushSync`); props: `router` (обязателен), `flushSync`, `onError(error, info)`, `useTransitions` (`undefined` по умолчанию — все обновления оборачиваются в `React.startTransition`).
- Route object: `path`, `Component`, `loader`, `action`, `lazy`; обработка ошибок — `useRouteError`.
- Доступно в Data mode (применение — решение design): `useLoaderData`, `useActionData`, `useFetcher`, `useNavigation`, `useRevalidator`, `<Await>`, `<Form>`, middleware маршрутов + `getContext`.
- Общие: `<Link>`, `useNavigate`, `useLocation`.

```tsx
import { createBrowserRouter } from "react-router";
import { RouterProvider } from "react-router/dom";
const router = createBrowserRouter(routes);           // один раз, вне дерева React
createRoot(root).render(<RouterProvider router={router} />);
```

## Version-specific notes
- v8.0.0 (2026-06-17) — базовые требования: Node ≥22.22.0, React ≥19.2.7, ESM-only пакет; Vite 7+ требуется только Framework mode. Сборочный образ `node:24.21.0-trixie-slim` условию Node удовлетворяет.
- v8.3.0 (2026-07-22): path params кодируются по RFC 3986, а не `encodeURIComponent` — проверить URL со спецсимволами/кириллицей и построение ссылок.
- v8.4.0 (2026-09-15): меньше лишних перерисовок компонентов маршрутов (гранулярные внутренние контексты); новый матчинг маршрутов на `@remix-run/route-pattern` и `unstable_validateParams` — помечены как unstable, в проекте не использовать.
- Два runtime-зависимых пакета (`cookie-es`, `@remix-run/route-pattern` — предрелизная 0.x) — учитывать при сканировании цепочки поставок.

## Deprecations and breaking changes from prior version
v7 → v8 (по CHANGELOG 8.0.0 и upgrade-гайду):
- пакет `react-router-dom` удалён: импорт из `react-router`, DOM-вариант `RouterProvider` — из `react-router/dom`;
- middleware всегда включён (флаг `future.v8_middleware` удалён); `v8_trailingSlashAwareDataRequests`, `v8_passThroughRequests` — умолчания; `v8_splitRouteModules` → `splitRouteModules` (по умолчанию включено); `v8_viteEnvironmentApi` удалён (относятся к Framework mode / серверным запросам);
- поле `data` в `meta` удалено — `loaderData` (Framework mode);
- пакет только ESM.
Проект новый — v7-идиомы (`react-router-dom`, `future.*` флаги) не использовать.

## Project conventions
- Маршрутизатор создаётся один раз на уровне модуля, `RouterProvider` — из `react-router/dom`.
- Не использовать `react-router-dom`, `unstable_*`, Framework mode.
- Kestrel отдаёт `index.html` для глубоких ссылок SPA, но не для `/api/*` и статических файлов (подтверждается на design).
- 401 от API обрабатывает SPA (stack.html): перенаправление на экран входа — из единой точки (middleware openapi-fetch / обработчик ошибок), не из каждого компонента.
- Разделение «loader/action vs TanStack Query» для серверных данных в stack.html не зафиксировано — решение design.

## Known issues and workarounds
- Не выявлено. В документации Data mode встречаются примеры импорта `RouterProvider` из `"react-router"`; страница API для DOM-приложений рекомендует `"react-router/dom"` — использовать её.
