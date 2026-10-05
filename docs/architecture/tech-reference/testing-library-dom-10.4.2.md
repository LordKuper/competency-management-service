# @testing-library/dom @ 10.4.2

## Canonical source
- Official docs: https://testing-library.com/docs/dom-testing-library/intro/ ; запросы: https://testing-library.com/docs/queries/about
- Release notes: https://github.com/testing-library/dom-testing-library/releases (10.4.2 — 2026-09-13)
- Пакет: https://www.npmjs.com/package/@testing-library/dom/v/10.4.2 (MIT; metadata: https://registry.npmjs.org/@testing-library/dom/10.4.2); dist-tag `latest` = 10.4.2
- Last verified: 2026-10-05

## API surface used in project
- Прямой импорт нужен редко: запросы и `waitFor` реэкспортируются из `@testing-library/react` (`testing-library-react-16.3.3.md`); `@testing-library/dom` обязателен как peer-зависимость `react` (`^10.0.0`), `user-event` (`>=7.21.4`) и `jest-dom` (`>=10 <11`).
- Варианты запросов: `getBy*` (бросает при 0 или >1 совпадений), `queryBy*` (null при отсутствии; бросает при >1), `findBy*` (асинхронный), `*AllBy*`; ключевая опция `getByRole('button', { name: /submit/i })`.
- Приоритет запросов: `getByRole` → `getByLabelText` → `getByPlaceholderText` (только запасной вариант) / `getByText` / `getByDisplayValue` → `getByAltText` / `getByTitle` → `getByTestId` (последним).
- Зависимости 10.4.2: `aria-query 5.3.0`, `dom-accessibility-api ^0.5.9`, `pretty-format ^27.0.2`, `lz-string ^1.5.0`, `picocolors 1.1.1`, `@babel/runtime ^7.12.5`, `@babel/code-frame ^7.10.4`, `@types/aria-query ^5.0.1`; Node ≥18.

## Version-specific notes
- Риск знаний (Phase 5): **LOW** — линия 10.x известна; релизы 10.4.1 и 10.4.2 прочитаны.
- 10.4.1 (2025-07-27): `chalk` заменён на `picocolors`. 10.4.2 (2026-09-13): `@types/node` закреплён на версии, совместимой с TypeScript 4 (влияет только на типы). 10.3.1 откатил «Reduce caught exceptions in prettyDom», 10.4.0 вернул его.
- По зависимостям (`aria-query`, `dom-accessibility-api`) роли и доступные имена вычисляются библиотеками, а не браузером — результаты `getByRole` в jsdom следует сверять с реальной разметкой.

## Deprecations and breaking changes from prior version
- 10.4.x: breaking changes не заявлено. Важно для обновлений: `jest-dom` 7 требует `@testing-library/dom` как **обязательный** peer (`>=10 <11`).

## Project conventions
- Предпочитать семантические запросы (роль + имя, подпись), как требует приоритет выше; `data-testid` — исключение.
- Версия закреплена точно (stack.html); обновляется только вместе с `@testing-library/react` / `jest-dom` / `user-event` (совместимые peer-диапазоны).

## Known issues and workarounds
- Известных проблем для 10.4.2 в просмотренных release notes не выявлено. В jsdom нет layout (`jsdom-30.1.2.md`) — тесты не должны зависеть от геометрии элементов.
