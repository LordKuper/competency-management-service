# Vitest @ 5.0.3

## Canonical source
- Official docs: https://vitest.dev/ ; миграция: https://vitest.dev/guide/migration ; окружения: https://vitest.dev/guide/environment ; `globals`: https://vitest.dev/config/globals
- Release notes: https://github.com/vitest-dev/vitest/releases (5.0.0 — 2026-09-03; 5.0.1 — 2026-09-15; 5.0.2 — 2026-09-25; 5.0.3 — 2026-09-30)
- Пакет: https://www.npmjs.com/package/vitest/v/5.0.3 (MIT; metadata: https://registry.npmjs.org/vitest/5.0.3); dist-tag `latest` = 5.0.3
- Last verified: 2026-10-05

## API surface used in project
- Unit/component-тесты frontend в окружении jsdom: example (`vitest.config.ts`, фрагмент): `test: { environment: 'jsdom', globals: true, setupFiles: ['./vitest-setup.ts'] }`; `vitest-setup.ts`: `import '@testing-library/jest-dom/vitest'`.
- Окружение на файл: комментарий `// @vitest-environment jsdom` в самом начале файла (до импортов); настройка — `environmentOptions`.
- Моки: `vi.fn`, `vi.mock` (hoisted), `vi.useFakeTimers`, `expect.poll`; конфигурация моков — `clearMocks` / `mockReset` / `restoreMocks`.
- Peer: `vite ^6.4.0 || ^7.0.0 || ^8.0.0` (обязателен), `jsdom` `*` (optional), `@types/node ^22.0.0 || >=24.0.0` (optional); Node `^22.12.0 || ^24.0.0 || >=26.0.0`. Проект: Vite 8.3.2, Node 24.21.0, jsdom 30.1.2 — в диапазонах.
- Окружение jsdom в 5.0.3 создаёт `JSDOM` с `url: 'http://localhost:3000'`, `pretendToBeVisual: true`, `runScripts: 'dangerously'`, `cookieJar: false`; `fetch`, `Response`, `Headers`, `AbortController`, `AbortSignal`, `URLSearchParams` берутся из Node (не из jsdom); в коде есть ветки для jsdom <28 и ≥28 (исходники `packages/vitest/src/integrations/env/jsdom.ts`).

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — мажор 5 новее известной автору линии 4.x; migration guide 5.0 и release notes 5.0.0–5.0.3 прочитаны.
- Требования 5.0: Vite ≥6.4.0, Node ≥22.12.0.
- 5.0.0 — новое: `createReport` API, отчёты можно сливать, `vi.when()` (API мока), улучшения v8-coverage, трассировка в browser mode, пересмотр производительности. Browser mode и coverage в стек не входят.
- 5.0.1–5.0.3: в release notes заявлены только исправления (изоляция тестов, jsdom, pool, cache, expect, UI); breaking changes не заявлено.
- Артефакты (отчёты, blob, html, json, junit) теперь складываются в каталог `.vitest/` — добавить в `.gitignore`.

## Deprecations and breaking changes from prior version
- 4.x → 5.0 (по migration guide): `clearMocks` включён по умолчанию (история вызовов очищается перед каждым тестом; вернуть — `clearMocks: false`); `vi.mock()` / `vi.unmock()` / `vi.hoisted()` внутри функций/блоков теперь бросают ошибку (только верхний уровень); `-t` сопоставляет полное имя теста через `' > '` вместо пробела (`vitest -t 'math > adds'`); `toThrow("")` совпадает с любым сообщением (пустое — `/^$/`); `expect.poll()` отклоняется по таймауту; не дожидающиеся (unawaited) `.resolves` / `.rejects` / `toMatchFileSnapshot` теперь проваливают тест (раньше — авто-await с предупреждением); заголовки тестов форматируются `pretty-format` (вместо `loupe`); удалены `test.sequential` / `describe.sequential` (→ `{ concurrent: false }`); присваивания `globalThis` / `window` в DOM-окружении проникают в jsdom/happy-dom; `VITEST_POOL_ID` / `VITEST_WORKER_ID` начинаются с 1; конфиги не ищутся в родительских каталогах; `test.projects` наследуют корневые опции Vite, если не `extends: false`; типы кастомных матчеров — `Matchers<R, T>` (вместо `Assertion<T>`).
- Пакеты `@vitest/runner`, `@vitest/ws-client` — deprecated (только security-фиксы); удалены deprecated-импорты `vitest/coverage`, `vitest/reporters`, `vitest/environments`, `vitest/snapshot`, `vitest/runners`, `vitest/suite`, `vitest/mocker`.
- Vitest UI требует токен в URL; API бенчмарков переписан; `browser.api` → `api` (не используется в проекте).

## Project conventions
- Окружение — jsdom (stack.html); coverage-провайдер, `@vitest/ui`, browser mode, happy-dom — не в стеке: добавление через Complication Approval.
- `globals`: по умолчанию `false`; авто-cleanup `@testing-library/react` работает только при глобальном `afterEach` (документация Vitest). Рекомендация проекта: `globals: true` + `"types": ["vitest/globals", "@testing-library/jest-dom"]` в `tsconfig` — либо `globals: false` и явный `afterEach(cleanup)` в setup-файле; выбор фиксируется в `vitest.config.ts`.
- Подготовка тестов: общий setup-файл подключает `@testing-library/jest-dom/vitest`; собственный `render` с провайдерами приложения — в `test-utils` (см. `testing-library-react-16.3.3.md`).
- Нативные optional-бинарники в `package-lock.json` (Vite/Rolldown и др.) — требование сборочного хоста (stack.html).

## Known issues and workarounds
- Пара Vitest 5.0.3 ↔ jsdom 30.1.2: в peer-зависимостях `jsdom: *`, проверенный диапазон не объявлен; код окружения знает ветки jsdom <28 / ≥28 — совместимость 30.x подтверждать первым прогоном тестов (spike).
- Тесты, рассчитанные на прежние умолчания (`clearMocks`, нестрогие async-assertions, `vi.mock` внутри функций), падают после миграции — править тесты, а не отключать правила.
