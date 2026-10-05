# jsdom @ 30.1.2

## Canonical source
- Official docs / README: https://github.com/jsdom/jsdom (README)
- Release notes: https://github.com/jsdom/jsdom/releases (30.0.0 — 2026-07-27; 30.1.0 — 2026-09-17; 30.1.1 — 2026-09-22; 30.1.2 — 2026-10-04)
- Пакет: https://www.npmjs.com/package/jsdom/v/30.1.2 (MIT; metadata: https://registry.npmjs.org/jsdom/30.1.2); dist-tag `latest` = 30.1.2
- Last verified: 2026-10-05

## API surface used in project
- Прямое создание `new JSDOM(...)` в проекте не используется: jsdom подключается как окружение Vitest (`test.environment: 'jsdom'`, `vitest-5.0.3.md`). Для справки (README): `new JSDOM('<!DOCTYPE html><p>Hello</p>', { url, pretendToBeVisual, runScripts })`; `pretendToBeVisual: true` включает `requestAnimationFrame` и `document.hidden === false`; `runScripts: "dangerously"` исполняет скрипты (выключено по умолчанию), `"outside-only"` — безопасно только глобалы окна; `resources: "usable"` — загрузка подресурсов.
- Peer: `canvas ^3.2.3` (optional) — в проекте не ставится; canvas-поддержка jsdom требует отдельной установки пакета `canvas` (README).
- Зависимости 30.1.2 (основные): `parse5 ^8.0.1`, `undici ^8.11.2`, `css-tree ^3.2.1`, `whatwg-url ^17.2.0`, `tough-cookie ^6.0.2`, `saxes ^6.0.0`, `lru-cache ^11.5.3`, `@asamuzakjp/dom-selector ^9.2.4`, `@asamuzakjp/css-color ^7.1.3`, `decimal.js ^10.6.0` и др.
- Не реализовано (README): навигация (смена глобального объекта при клике по ссылке) и layout (вычисление геометрии элементов).

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — мажор 30 заметно новее известной автору линии (≤27); changelog 28.0.0 – 30.1.2 прочитан по GitHub Releases.
- Требуемая версия Node: `^22.22.2 || ^24.15.0 || >=26.0.0` (30.0.0). Проект: Node 24.21.0 (сборочная стадия) — выполнено.
- 30.1.2 опубликован за сутки до верификации стека (2026-10-04): обновления патч-уровня (поддержка Unicode 18.0.0 в IDN, уменьшение размера пакета и памяти, исправления медленного построения DOM и наследования custom properties).
- 30.1.0 (2026-09-17): ускорение построения DOM, мутаций дерева, range-операций, `getComputedStyle()` и сериализации CSS. 30.0.0: добавлены `CSS.escape()`, `CSS.supports()`, `background-position-x/y`, `getComputedStyle()` приводит длины к пикселям.

## Deprecations and breaking changes from prior version
- 29.x → 30.0.0: поднят минимум Node (см. выше) — единственное breaking change.
- 28.x → 29.0.0: минимум Node v22 — 22.13.0+; переписана реализация CSSOM (`css-tree`, webidl2js-обёртки): изменены разбор, сериализация и поведение `getComputedStyle()` / `CSSStyleDeclaration`.
- 27.x → 28.0.0: переработана кастомизация загрузки ресурсов (новый API описан в README — перед использованием прежних механизмов, в т.ч. `ResourceLoader`, сверяться с README); `<iframe>`/`<frame>`/`<img>` теперь шлют `load` (не `error`) при не-OK HTTP-ответах; известная регрессия — WebSocket больше не ограничиваются одним соединением на origin (upstream-баг undici).

## Project conventions
- Единственная роль — DOM-окружение unit/component-тестов; проверять поведение компонентов, а не вёрстку: geometry-зависимое (виртуальный скролл, позиционирование всплывающих окон, `getBoundingClientRect`) в jsdom недоступно — e2e (Playwright) исключён из стека, пока нет e2e-критерия.
- Vitest 5.0.3 подставляет Node-реализации `fetch` / `Response` / `Headers` / `AbortController` / `URLSearchParams` поверх jsdom — тесты сетевого слоя (`openapi-fetch`) идут через них (`vitest-5.0.3.md`).
- Пакет `canvas` в стек не входит — добавление только через Complication Approval.

## Known issues and workarounds
- Совместимость пары Vitest 5.0.3 ↔ jsdom 30.1.2 не заявлена явным диапазоном (peer `jsdom: *`); код окружения Vitest содержит ветки для jsdom <28 и ≥28 — подтверждать первым прогоном тестов.
- Нет layout: тесты, зависящие от размеров элементов, получают нули — переписывать на проверку состояния/атрибутов.
