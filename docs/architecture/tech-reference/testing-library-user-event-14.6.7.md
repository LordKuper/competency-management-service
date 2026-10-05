# @testing-library/user-event @ 14.6.7

## Canonical source
- Official docs: https://testing-library.com/docs/user-event/intro
- Release notes: https://github.com/testing-library/user-event/releases (14.6.7 — 2026-09-02)
- Пакет: https://www.npmjs.com/package/@testing-library/user-event/v/14.6.7 (MIT; metadata: https://registry.npmjs.org/@testing-library/user-event/14.6.7); dist-tag `latest` = 14.6.7
- Last verified: 2026-10-05

## API surface used in project
- Рекомендуемый способ — экземпляр `userEvent.setup()`, создаваемый **до** `render`; все вызовы — `await`:
  `const user = userEvent.setup(); render(<MyComponent />); await user.click(screen.getByRole('button', { name: /click me!/i }));`
- Помощник: `function setup(jsx) { return { user: userEvent.setup(), ...render(jsx) } }`.
- Прямые вызовы `userEvent.click()` в v14 поддерживаются, но не рекомендуются.
- Peer: `@testing-library/dom >=7.21.4` (проект: 10.4.2); зависимостей нет; Node ≥12.
- В отличие от `fireEvent` (одно DOM-событие), `user-event` имитирует полное взаимодействие (несколько событий, проверки видимости/доступности: скрытый элемент не кликается, в `disabled`-поле не печатают).

## Version-specific notes
- Риск знаний (Phase 5): **LOW** — линия 14.x известна; патчи 14.6.2–14.6.7 прочитаны (серия выпусков 2026-08-03 … 2026-09-02).
- 14.6.7: нормализация алиасов формата `DataTransfer`; поддержка iframe для `user.keyboard`. 14.6.6: `pointerType` по умолчанию — пустая строка вместо строки `"undefined"`. 14.6.5: исправлено перенацеливание `Tab`, если фокус переместился во время `keydown`. 14.6.4: свойство `repeat` у клавиатурных событий. 14.6.3 — ручной патч-релиз.

## Deprecations and breaking changes from prior version
- 14.6.2 → 14.6.7: breaking changes не заявлено (только исправления).

## Project conventions
- Все вызовы `user.*` — с `await`; экземпляр создаётся через `userEvent.setup()` до `render` (docs user-event).
- Запросы к элементам для `user.*` — семантические (`getByRole` с `name`), как в `testing-library-dom-10.4.2.md`.

## Known issues and workarounds
- Забытый `await` перед `user.*` даёт гонки и «act»-предупреждения.
- Нет layout в jsdom (`jsdom-30.1.2.md`): `pointer-events` и позиционирование проверяются только по CSS, а не по геометрии.
