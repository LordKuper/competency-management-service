# @testing-library/jest-dom @ 7.0.1

## Canonical source
- Official docs / README: https://github.com/testing-library/jest-dom
- Release notes: https://github.com/testing-library/jest-dom/releases (7.0.0 — 2026-07-20; 7.0.1 — 2026-08-09)
- Пакет: https://www.npmjs.com/package/@testing-library/jest-dom/v/7.0.1 (MIT; metadata: https://registry.npmjs.org/@testing-library/jest-dom/7.0.1); dist-tag `latest` = 7.0.1
- Last verified: 2026-10-05

## API surface used in project
- Подключение к Vitest: в setup-файле `import '@testing-library/jest-dom/vitest'` (`setupFiles: ['./vitest-setup.ts']`); экспорты пакета: `.` , `./vitest`, `./matchers`, `./jest-globals`.
- TypeScript (README, Vitest): `"types": ["vitest/globals", "@testing-library/jest-dom"]` и включение setup-файла в `include` `tsconfig`.
- Матчеры (README): `toBeInTheDocument`, `toBeVisible`, `toBeDisabled` / `toBeEnabled`, `toBeRequired`, `toBeValid` / `toBeInvalid`, `toBeChecked`, `toBePartiallyChecked`, `toBePressed` / `toBePartiallyPressed`, `toBeEmptyDOMElement`, `toContainElement`, `toContainHTML`, `toHaveAccessibleName`, `toHaveAccessibleDescription`, `toHaveAccessibleErrorMessage`, `toHaveAttribute`, `toHaveClass`, `toHaveFocus`, `toHaveFormValues`, `toHaveStyle`, `toHaveTextContent`, `toHaveValue`, `toHaveDisplayValue`, `toHaveRole`, `toHaveErrorMessage`, `toHaveSelection`, `toAppearBefore` / `toAppearAfter`.
- Новые в 7.0 (появились в 6.10.0 и вошли в 7.0.0): семейства `toContainAnyBy*` (≥1 совпадение) и `toContainOneBy*` (ровно одно) для вариантов `AltText`, `DisplayValue`, `LabelText`, `PlaceholderText`, `Role`, `TestId`, `Text`, `Title`.
- Зависимости: `aria-query`, `@adobe/css-tools`, `css.escape`, `dom-accessibility-api`, `picocolors`, `redent`; peer: `@testing-library/dom >=10 <11`, `vitest >=0.32` (optional); Node ≥22.

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — мажор 7 новее известной автору линии 6.x; release notes 6.10.0, 7.0.0, 7.0.1 прочитаны.
- 7.0.1 (2026-08-09): единственное изменение — `vitest` объявлен optional peer-зависимостью (#733).
- Минимум Node 22 (проект: сборочная стадия на Node 24.21.0 — выполнено).

## Deprecations and breaking changes from prior version
- 6.x → 7.0.0 (единственные breaking changes по release notes): `@testing-library/dom` теперь **обязательный** peer (`>=10 <11`) — ставится явно; минимальная версия Node — 22.
- Функциональных ломающих изменений матчеров в release notes 7.0.0 не заявлено (новые матчеры добавлены также в 6.10.0).

## Project conventions
- Подключается только через `@testing-library/jest-dom/vitest`; глобальный `expect` Vitest расширяется в setup-файле (один раз, не в каждом тесте).
- Предпочитать семантические матчеры (`toHaveAccessibleName`, `toBeInTheDocument`) проверкам по CSS-классам; `toHaveStyle` — для видимых состояний.
- Версия закреплена точно; обновляется вместе с `@testing-library/dom` (peer-диапазон `>=10 <11`).

## Known issues and workarounds
- Установка без `@testing-library/dom` (обязательный peer с 7.0.0) даёт ошибку/предупреждение peer — добавить явную зависимость.
- Если TypeScript не видит `toBeInTheDocument` и другие матчеры — проверить `types` и `include` setup-файла в `tsconfig` (см. «API surface»).
