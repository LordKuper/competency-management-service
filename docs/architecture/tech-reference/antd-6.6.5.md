---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# antd (Ant Design) @ 6.6.5 (+ @ant-design/icons)

## Canonical source
- Official docs: https://ant.design/docs/react/introduce ; компоненты: https://ant.design/components/overview
- Миграция v5→v6: https://ant.design/docs/react/migration-v6 ; changelog: https://ant.design/changelog
- Тема/CSS-переменные/zero-runtime: https://ant.design/docs/react/customize-theme ; релиз 6.0: https://dev.to/zombiej/ant-design-60-is-released-bfa
- CSP: https://ant.design/components/config-provider (prop `csp`), https://ant.design/docs/react/faq
- npm: https://www.npmjs.com/package/antd (манифест 6.6.5: peer `react >=18.0.0`, `react-dom >=18.0.0`; 47 dependencies, среди них `@ant-design/icons ^6.3.4`, `dayjs ^1.11.11`, `@ant-design/cssinjs ^2.1.2`, `clsx ^2.1.1`, пакеты `@rc-component/*`; `sideEffects: ["*.css"]`)
- Last verified: 2026-10-05 (dist-tag `latest = 6.6.5`, релиз 2026-09-20)

## API surface used in project
- `ConfigProvider`: `locale={ru_RU}` (`import ru_RU from 'antd/locale/ru_RU'`), `theme` (token/algorithm/components/cssVar/hashed), `csp={{ nonce }}`, `getPopupContainer`.
- `App` и `App.useApp()` — `message`/`notification`/`modal` с контекстом ConfigProvider (статические `message.info`, `Modal.confirm` его не видят).
- `Table`, `Form` (+ `Form.Item`, `Form.List`; `rules` — только UX, истина на сервере, см. stack.html), `DatePicker`/`RangePicker` (на `dayjs`), `Tree`, `TreeSelect`, `Upload`.
- Иконки: именованные импорты из `@ant-design/icons` (SVG-компоненты; зависимость `@ant-design/icons-svg`). **Не использовать** `createFromIconfontCN` (внешний `scriptUrl`).
- Шрифты: умолчание — системный стек (Apple System, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica Neue, Arial, Noto Sans); веб-шрифты антд не подгружает — по документации темы.

## Version-specific notes
- v6 (релиз 6.0.0): React ≥18; режим CSS-переменных включён по умолчанию (IE не поддерживается); `@ant-design/icons` ≥6 (несовместим с antd 5); патч `@ant-design/v5-patch-for-react-19` не нужен.
- Доставка стилей по умолчанию — CSS-in-JS в рантайме (`@ant-design/cssinjs`), стили вставляются в DOM динамически; `hashed: true`. Режимы, документированные antd:
  - **CSP**: `<ConfigProvider csp={{ nonce: '…' }}>` (FAQ: «fix dynamic styles while using CSP»). Для статического `index.html`, раздаваемого Kestrel, nonce на запрос недоступен без шаблонизации ответа — выбор CSP-политики (nonce / `style-src 'unsafe-inline'` / zero-runtime) — вопрос design.
  - **zero-runtime** (с 6.0.0): `<ConfigProvider theme={{ zeroRuntime: true }}>` + `import 'antd/dist/antd.css'` (без hashed-классов) либо `@ant-design/static-style-extract` для выборочных стилей. Исключает рантайм-генерацию стилей.
- Браузеры: только современные (CSS variables); сборка Vite 8 по умолчанию целится в Chrome/Edge 111+, Firefox 114+, Safari 16.4+ — минимальные браузеры контура в стеке не заданы.
- Локаль: `antd/locale/ru_RU` содержит русские `Form.defaultValidateMessages` и локали pickers; файл **не** импортирует `dayjs/locale/ru` — по FAQ локаль dayjs подключается отдельно (`import 'dayjs/locale/ru'`, затем `dayjs.locale('ru')`), и не должно быть дублей dayjs (`npm ls dayjs`).
- Из changelog 6.x, затрагивающее используемые компоненты: Form — `defaultValidateMessages` на 8 языках, `labelAlign` в ConfigProvider (6.4.0); Table — `column` в ConfigProvider, `scrollTo` с alignment (6.4.0), `expandable.forceRender` (6.6.0); DatePicker — `onClear` (6.5.0); Upload — `classNames/styles.trigger` (6.3.0); Tree — хук `useTree`, `scrollTo({ autoExpand })` (6.6.0); исправление ESM-сборки в строгих окружениях (6.4.4).

## Deprecations and breaking changes from prior version
v5 → v6 (проект новый; v5-идиомы не использовать):
- `Button.Group`, `Input.Group` → `Space.Compact`; `Dropdown.Button` → `Space.Compact` + `Dropdown` + `Button`; `BackTop` → `FloatButton.BackTop`;
- `dropdownXxx` у Dropdown/Select → `popupXxx`; `headStyle`/`bodyStyle` → `styles.header`/`styles.body`; `direction`/`type` → `orientation`; `Position` → `Placement`; размер `"default"` → `"medium"` (Avatar, Badge, Spin, Steps);
- `children` → `items` (Menu, Anchor, Breadcrumb, Descriptions, Timeline, Tabs); `Collapse.Panel disabled` → `collapsible="disabled"`;
- `Form.onFinish` не включает незарегистрированные элементы `Form.List`; `getFieldsValue({ strict: true })` не нужен;
- размытие маски Modal/Drawer отключено по умолчанию (было включено в 6.0.0–6.2.x);
- в 6.x: `List` объявлен deprecated в пользу `Listy` (6.6.2); `collapsibleIcon` у Splitter → `collapsible.icon` (6.4.0); устаревшие `*Props` типы → `GetProps` (6.6.4).

## Project conventions
- Один пакет закрывает таблицы, формы, даты, i18n — без react-hook-form/zod и отдельного data-grid (stack.html); добавлять только при доказанной нехватке.
- Никаких CDN: ни скриптов антд, ни иконочных шрифтов; иконки — только из бандла.
- Глобальные уведомления — через `App.useApp()`, не статические методы.
- `@ant-design/icons` и `dayjs` приходят транзитивно через antd (версии `^6.3.4`/`^1.11.11` в манифесте antd 6.6.5); в stack.html отдельно не закреплены — фактические версии фиксирует `package-lock.json`.

## Known issues and workarounds
- Не проверено (spike до реализации): (1) содержимое `antd/dist/antd.css` на внешние `url(...)`/`@font-face` — файл не анализировался; (2) вставляет ли `@ant-design/icons` собственные `<style>` и нужен ли ему nonce при строгом CSP; (3) поведение при CSP без `'unsafe-inline'` в режиме по умолчанию.
- Кастомные стили на внутренние DOM-узлы компонентов могут ломаться при обновлении мажора (DOM v6 оптимизирован) — стилизовать через `theme`/`classNames`/`styles`.
