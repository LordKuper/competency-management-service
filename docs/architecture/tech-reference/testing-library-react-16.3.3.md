# @testing-library/react @ 16.3.3

## Canonical source
- Official docs: https://testing-library.com/docs/react-testing-library/intro/ ; setup: https://testing-library.com/docs/react-testing-library/setup
- Release notes: https://github.com/testing-library/react-testing-library/releases (16.3.3 — 2026-08-27)
- Пакет: https://www.npmjs.com/package/@testing-library/react/v/16.3.3 (MIT; metadata: https://registry.npmjs.org/@testing-library/react/16.3.3); dist-tag `latest` = 16.3.3
- Last verified: 2026-10-05

## API surface used in project
- `render`, `screen`, `within`, `waitFor`, `cleanup`, `act` из `@testing-library/react`; запросы — из `@testing-library/dom` (см. `testing-library-dom-10.4.2.md`), взаимодействия — `@testing-library/user-event` (`testing-library-user-event-14.6.7.md`), матчеры — `@testing-library/jest-dom` (`testing-library-jest-dom-7.0.1.md`).
- Кастомный `render` с провайдерами — паттерн из docs; example: `const customRender = (ui, options) => render(ui, { wrapper: AllTheProviders, ...options }); export { customRender as render }` (файл `test-utils`).
- Peer-зависимости (metadata 16.3.3): `react` / `react-dom` `^18.0.0 || ^19.0.0` (проект: 19.3.0), **`@testing-library/dom ^10.0.0` — ставится явно**, `@types/react` / `@types/react-dom` `^18 || ^19` (optional); зависимость `@babel/runtime ^7.12.5`; Node ≥18.

## Version-specific notes
- Риск знаний (Phase 5): **LOW** — линия 16.x известна; патчи 16.3.1–16.3.3 прочитаны в release notes.
- 16.1.0 — поддержка React 19; 16.2.0 — поддержка обработчиков ошибок React; 16.3.2 (2026-01-19) — вывод типа `onCaughtError` в `RenderOptions` для React 19; 16.3.3 (2026-08-27) — `act()` не вызывается повторно (re-entrant) при диспетчеризации событий.
- Авто-cleanup после каждого теста регистрируется, только если среда определяет глобальный `afterEach`; отключение: `RTL_SKIP_AUTO_CLEANUP=true`, импорт `@testing-library/react/dont-cleanup-after-each` или `@testing-library/react/pure`.

## Deprecations and breaking changes from prior version
- 16.3.0 → 16.3.3: breaking changes в release notes не заявлено. `@testing-library/dom` — peer-зависимость (по metadata 16.3.3): ставить отдельно; поддержка React 19 — с 16.1.0.

## Project conventions
- Vitest по умолчанию `globals: false` → без глобального `afterEach` авто-cleanup не сработает (Vitest docs: `@testing-library/react` полагается на globals). Либо `globals: true` в `vitest.config.ts`, либо явный `afterEach(cleanup)` в setup-файле — см. `vitest-5.0.3.md`.
- Рекомендация: общий `render` оборачивает провайдеры приложения (react-query, antd `ConfigProvider` с локалью `antd/locale/ru_RU`, роутер) — один wrapper в `test-utils` (паттерн из docs RTL), тесты импортируют `render` оттуда; состав провайдеров уточняется в design.
- Запросы по приоритету Testing Library: `getByRole` (с `name`), затем `getByLabelText`, `getByText`; `getByTestId` — только если иначе нельзя. Отсутствие элемента — `queryBy*`, асинхронное появление — `findBy*`.
- Версии `@testing-library/*` закреплены точно (stack.html).

## Known issues and workarounds
- Без `globals`/`cleanup` DOM между тестами накапливается — тесты начинают видеть элементы предыдущих (см. «Project conventions»).
- Пакет рассчитан на DOM-окружение: в Vitest нужен `environment: 'jsdom'` (`jsdom-30.1.2.md`).
