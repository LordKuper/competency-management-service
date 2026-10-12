---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @dnd-kit (классическая линия) @ core 6.3.1 + sortable 10.0.0 + utilities 3.2.2

## Canonical source
- Official docs: https://dndkit.com/legacy/introduction/installation (классическая линия, вкладка «Legacy»; старый адрес `docs.dndkit.com` отвечает 301 на `dndkit.com`). Страницы: sortable — https://dndkit.com/legacy/presets/sortable/overview , `useSortable` — https://dndkit.com/legacy/presets/sortable/use-sortable , клавиатура — https://dndkit.com/legacy/api-documentation/sensors/keyboard , доступность — https://dndkit.com/legacy/guides/accessibility
- Репозиторий и релизы: https://github.com/clauderic/dnd-kit (теги `@dnd-kit/<pkg>@<ver>`)
- Образец antd: демо таблицы 6.6.5 `components/table/demo/drag-sorting.tsx` и `drag-sorting-handler.tsx` (https://github.com/ant-design/ant-design/tree/6.6.5/components/table/demo); в `devDependencies` antd 6.6.5 — `@dnd-kit/core ^6.3.1`, `@dnd-kit/sortable ^10.0.0`, `@dnd-kit/utilities ^3.2.2`, `@dnd-kit/modifiers ^9.0.0`
- npm (манифесты получены; лицензия у всех — MIT; `typings: dist/index.d.ts`, CJS `main` + ESM `module`, без `exports`):
  - `@dnd-kit/core` 6.3.1 — релиз 2024-12-05; peer `react >=16.8.0`, `react-dom >=16.8.0`; deps `@dnd-kit/accessibility ^3.1.1`, `@dnd-kit/utilities ^3.2.2`, `tslib ^2.0.0`
  - `@dnd-kit/sortable` 10.0.0 — релиз 2024-12-04; peer `react >=16.8.0`, `@dnd-kit/core ^6.3.0`; deps `@dnd-kit/utilities ^3.2.2`, `tslib ^2.0.0`
  - `@dnd-kit/utilities` 3.2.2 — релиз 2023-11-06; peer `react >=16.8.0`; dep `tslib ^2.0.0`
  - транзитивно `@dnd-kit/accessibility` 3.1.1 (MIT, peer `react >=16.8.0`); install-скриптов нет (`prepublish` к установке не относится; в проекте и так `ignore-scripts=true`)
- Last verified: 2026-10-11 (dist-tags: core `latest = 6.3.1`, sortable `latest = 10.0.0`, utilities `latest = 3.2.2`, modifiers `latest = 9.0.0`; новых стабильных версий классической линии после 2024-12-05 нет)

## API surface used in project
Перестановка короткого списка (уровни трека, порядка десятка строк) в окне трека (`Modal` antd) — строки `Table` или простой список.

Установка (runtime-зависимости `web/package.json`, точные версии):
`npm i -E @dnd-kit/core@6.3.1 @dnd-kit/sortable@10.0.0 @dnd-kit/utilities@3.2.2`
(`@dnd-kit/utilities` — прямая зависимость, т.к. `CSS` импортируется из него напрямую; `@dnd-kit/modifiers` не ставить — см. `restrictToVerticalAxis` ниже.)

- `@dnd-kit/core`:
  - `DndContext` — props: `sensors`, `collisionDetection`, `modifiers`, `accessibility?: { announcements?, screenReaderInstructions?, container?: Element, restoreFocus?: boolean }`, события `onDragStart`/`onDragMove`/`onDragOver`/`onDragEnd`/`onDragCancel` (+ `onDragPending`/`onDragAbort` с 6.3.0). Без `sensors` действуют `PointerSensor` и `KeyboardSensor` с опциями по умолчанию (клавиатура двигает на 25 px, а не к соседнему элементу).
  - `useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }))`.
  - `closestCenter` — алгоритм столкновений, рекомендованный для sortable (по умолчанию `rectIntersection`, строже).
  - типы (с `verbatimModuleSyntax` — только `import type`): `DragEndEvent`, `DragStartEvent`, `UniqueIdentifier` (`string | number`), `Announcements`, `ScreenReaderInstructions`, `Modifier`.
  - `restrictToVerticalAxis` — своим модификатором в одну строку (тело совпадает с реализацией в `@dnd-kit/modifiers` 9.0.0):
    `const restrictToVerticalAxis: Modifier = ({ transform }) => ({ ...transform, x: 0 });`
- `@dnd-kit/sortable`:
  - `SortableContext items={ids} strategy={verticalListSortingStrategy}` — `items` строго в порядке отрисовки.
  - `useSortable({ id, attributes?: { role?, roleDescription?, tabIndex? }, disabled?, transition? })` → `attributes`, `listeners`, `setNodeRef`, `setActivatorNodeRef`, `transform`, `transition`, `isDragging` и др.; по умолчанию `role: 'button'`, `tabIndex: 0`, `aria-roledescription: 'sortable'`, `aria-describedby` → скрытый текст инструкции. Анимация по умолчанию 250 ms `ease`; `transition` и `transform` ставить на один и тот же узел.
  - `sortableKeyboardCoordinates` — стрелки переносят к ближайшему элементу в направлении нажатия (`closestCorners` по отфильтрованным droppable).
  - `arrayMove(array, from, to)` — возвращает новый массив.
- `@dnd-kit/utilities`: `CSS.Translate.toString(transform)` → `translate3d(Xpx, Ypx, 0)` (округление до px); `CSS.Transform.toString` = Translate + `scaleX() scaleY()`. Для строк таблицы — `CSS.Translate` (как в демо antd): масштаб строке не нужен.
- Клавиатура (`KeyboardSensor`, по `event.code`): начать — `Space`/`Enter`; двигать — стрелки; отпустить — `Space`/`Enter`/`Tab` (`Tab` добавлен в 6.3.1); отменить — `Escape`. Активатор — `onKeyDown` из `listeners`; если задан `setActivatorNodeRef`, старт только при `event.target === активатор` (нажатия Enter/Space на вложенных кнопках строки перетаскивание не запускают). После отпускания фокус возвращается на активатор (`restoreFocus` по умолчанию `true`).
- Доступность и русские тексты. Встроенные тексты только английские (6.3.1: инструкция «To pick up a draggable item, press the space bar. …», объявления «Picked up draggable item {id}.» и т. п., `aria-roledescription` «sortable») — заменять все три. Объявления получают `active.id`/`over.id` (у нас — id уровней), поэтому формулировать через название и позицию с 1, а не через id/индекс. Пример (формулировки — образец; итоговые тексты — в plan.md «Тексты интерфейса»):
  ```ts
  import type { Announcements, ScreenReaderInstructions, UniqueIdentifier } from '@dnd-kit/core';

  const screenReaderInstructions: ScreenReaderInstructions = {
    draggable: 'Чтобы переместить уровень, нажмите пробел или Enter, стрелками вверх и вниз выберите место, ' +
      'затем снова нажмите пробел или Enter. Escape — отменить.',
  };

  // levels — текущий порядок до перестановки
  const announcements = (levels: { id: string; name: string }[]): Announcements => {
    const pos = (id: UniqueIdentifier) => levels.findIndex((l) => l.id === id) + 1;
    const name = (id: UniqueIdentifier) => levels.find((l) => l.id === id)?.name ?? '';
    const n = levels.length;
    return {
      onDragStart: ({ active }) => `Уровень «${name(active.id)}» взят, позиция ${pos(active.id)} из ${n}.`,
      onDragOver: ({ active, over }) =>
        over ? `Уровень «${name(active.id)}» на позиции ${pos(over.id)} из ${n}.` : undefined,
      onDragEnd: ({ active, over }) =>
        over ? `Уровень «${name(active.id)}» перемещён на позицию ${pos(over.id)} из ${n}.` : undefined,
      onDragCancel: ({ active }) => `Перемещение отменено, уровень «${name(active.id)}» на позиции ${pos(active.id)}.`,
    };
  };
  // <DndContext accessibility={{ announcements: announcements(levels), screenReaderInstructions }} …>
  // useSortable({ id, attributes: { roleDescription: 'перемещаемый уровень' } })
  ```
  Callback возвращает `string | undefined` (`undefined` — ничего не объявлять). Скрытая инструкция и live region рендерятся на месте `DndContext` (без портала, если не задан `accessibility.container`).
- antd `Table` (по демо antd 6.6.5): свой компонент строки через `components={{ body: { row: SortableRow } }}`; antd передаёт в строку `data-row-key` (= `rowKey`), он же — `id` для `useSortable`; `DndContext` и `SortableContext` оборачивают `<Table>` снаружи. Каркас (вариант с ручкой, см. «Project conventions»):
  ```tsx
  type Handle = Pick<ReturnType<typeof useSortable>, 'attributes' | 'listeners' | 'setActivatorNodeRef'>;
  const HandleContext = createContext<Handle | null>(null);

  function SortableRow(props: React.HTMLAttributes<HTMLTableRowElement> & { 'data-row-key': string }) {
    const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } =
      useSortable({ id: props['data-row-key'], attributes: { roleDescription: '…' } });
    const handle = useMemo(() => ({ attributes, listeners, setActivatorNodeRef }), [attributes, listeners, setActivatorNodeRef]);
    const style: React.CSSProperties = { ...props.style, transform: CSS.Translate.toString(transform), transition,
      ...(isDragging ? { position: 'relative', zIndex: 1 } : {}) };
    return <HandleContext.Provider value={handle}><tr {...props} ref={setNodeRef} style={style} /></HandleContext.Provider>;
  }
  // колонка-ручка: const h = useContext(HandleContext);
  // <Button type="text" size="small" icon={<HolderOutlined />} aria-label="…" ref={h?.setActivatorNodeRef} {...h?.attributes} {...h?.listeners} />

  // onDragEnd={({ active, over }) => { if (over && active.id !== over.id) setLevels((ls) =>
  //   arrayMove(ls, ls.findIndex((l) => l.id === active.id), ls.findIndex((l) => l.id === over.id))); }}
  ```
  Простой список — то же без `components`: `useSortable` в компоненте элемента, `setNodeRef` на корень элемента.

## Version-specific notes
- Линия: классическая (`@dnd-kit/core` + `sortable` + `utilities`), не новая `@dnd-kit/react` (0.5.0 от 2026-06-11, `beta` 0.5.1-beta от 2026-09-12; peer `react ^18 || ^19`). Факты выбора: (1) новая линия — 0.x (minor может ломать API); (2) в ней открыт issue #2116 (2026-07-31, bug): в React 19 при StrictMode-повторе монтирования `DragDropProvider` остаётся с уничтоженным менеджером, перетаскивание может перестать работать — проект монтируется в `StrictMode` (react-19.3.0.md); исправление — открытый PR #2117; (3) демо таблицы antd 6.6.5 и его `devDependencies` — на классической линии этих же версий. Цена: классическая линия на сайте помечена «Legacy» («There's a new version of @dnd-kit available. We recommend you use the latest version instead.»), релизов нет с 2024-12-05 — исправлений ждать не стоит.
- React 19: peer `>=16.8.0` — `npm i` с react 19.3.0 без ERESOLVE. Исправления для StrictMode React 18 — PR #788 (2022), входят в 6.x; открытых issue классической линии про StrictMode/React 19 (поиск по репозиторию: `strictmode`, `react 19` в заголовке) не найдено. В приложении (React 19.3, `StrictMode` в `web/src/main.tsx`) перетаскивание уровней мышью и с клавиатуры проверено вручную 2026-10-12.
- 6.3.0: события `onDragPending`/`onDragAbort` (визуальные подсказки при `activationConstraint`). 6.3.1: `Tab` в кодах завершения перетаскивания. sortable 10.0.0 — major только из-за peer `@dnd-kit/core ^6.3.0`, изменений API нет.
- Стили — только инлайн через React `style` и `style.setProperty` (CSSOM); `<style>`, `insertRule`, `innerHTML`, `eval` в `core.esm.js` не найдено (файл просмотрен без последних ~4 КБ) — на CSP `style-src` не влияет.

## Deprecations and breaking changes from prior version
- Проект ставит пакет впервые; deprecated API в 6.3.1 / 10.0.0 / 3.2.2 не заявлено.
- Переход на новую линию (на будущее) — другой API: `DndContext` → `DragDropProvider`, `useSortable` из `@dnd-kit/react/sortable` (без `SortableContext`), `setNodeRef` → `ref`, `arrayMove` → `move` из `@dnd-kit/helpers`, `active`/`over` → `event.operation.source`/`target`, `onDragCancel` → `event.canceled` в `onDragEnd`. Гайд: https://dndkit.com/react/guides/migration

## Project conventions
- Ровно три пакета точными версиями (выше); `@dnd-kit/modifiers` не ставить — однострочный модификатор `restrictToVerticalAxis`. Новая линия `@dnd-kit/react` — только отдельным решением после 1.0 и закрытия #2116.
- Сенсоры: `PointerSensor` + `KeyboardSensor` с `sortableKeyboardCoordinates`; `closestCenter`; `verticalListSortingStrategy`; `restrictToVerticalAxis`.
- Активатор — ручка в первой колонке (antd `Button type="text"` с `HolderOutlined`, демо `drag-sorting-handler`): на ней `setActivatorNodeRef`, `listeners` и `attributes` (родная `<button>`, доступное имя через `aria-label`); на `<tr>` — только `setNodeRef` и `style`, семантика строки таблицы сохраняется. Тянуть мышью за всю строку (демо `drag-sorting`) — только с `setActivatorNodeRef` на той же `<tr>` и `PointerSensor` с `activationConstraint: { distance: N }`, чтобы клики по элементам строки не начинали перетаскивание; `attributes` на `<tr>` дают ей `role="button"`.
- Порядок — локальное состояние окна (`arrayMove` в `onDragEnd`, только при `over && active.id !== over.id`); на сервер уходит по кнопке «Сохранить» окна. `SortableContext items` — id уровней из того же массива, в порядке отрисовки.
- Все тексты (`screenReaderInstructions`, `announcements`, `roleDescription`, `aria-label` ручки) — русские, позиции с 1.
- `DndContext` — вне `<table>` (не через `components.body.wrapper`): его скрытые `<div>` рендерятся на месте контекста.
- `web/tsconfig.json` держит `skipLibCheck: true` — без него `tsc` падает на типах пакета (см. ниже).

## Known issues and workarounds
- Escape в `Modal` antd (по чтению кода, запуском не проверено): `KeyboardSensor` отменяет перетаскивание обработчиком `keydown` на `document` и делает только `preventDefault()`, без `stopPropagation()`; `@rc-component/dialog` 1.10.0 (antd 6.6.5) закрывает верхнее окно через `@rc-component/portal` — слушатель `keydown` на `window`, `defaultPrevented` не проверяет. Итог — Escape отменяет перенос и закрывает окно трека. Обход: в `onCancel` окна не закрывать, если `e.defaultPrevented` (событие из `onEsc` — тот же `KeyboardEvent`, на котором сенсор уже вызвал `preventDefault()`); клик по крестику/маске `defaultPrevented` не несёт. Вариант `keyboard={!dragging}` ненадёжен: между слушателями `document` и `window` выполняются микрозадачи, React может успеть перерисовать окно с `dragging = false`. Обход применён в `web/src/features/career/CatalogModal.tsx`: Escape отменяет перетаскивание, окно остаётся открытым.
- Пустая таблица: rc-table (`@rc-component/table` 1.11.x) рисует строку-заглушку `emptyText` тем же `components.body.row` (`getComponent(['body', 'row'], 'tr')`), без `data-row-key` — `useSortable` получит `id: undefined`. Обход: компонент строки без `data-row-key` отдаёт обычный `<tr {...props} />` (хук — в отдельном дочернем компоненте), либо пустое состояние без `Table`.
- Типы под `@types/react` 19: `dist/components/Accessibility/Accessibility.d.ts` возвращает глобальный `JSX.Element` → TS2503 «Cannot find namespace 'JSX'» (issue #1559 закрыт 2026-02-16, PR #1818 закрыт без слияния, релиза нет). В проекте скрыто `skipLibCheck: true`; не отключать.
- jsdom без layout (jsdom-30.1.2.md): все прямоугольники нулевые, `sortableKeyboardCoordinates` не находит соседа — жесты перетаскивания в Vitest не воспроизводятся (вывод из устройства кода, не запускалось). Тестировать: перестановку в `onDragEnd` (`arrayMove`), функции объявлений, наличие `aria-describedby`/`aria-roledescription`/`aria-label` у ручки.
- Без `DragOverlay` `transition` у перетаскиваемой строки отключён (по документации `useSortable`), остальные строки анимируются — для десятка строк достаточно; `DragOverlay` не используется (с ним нельзя рендерить тот же компонент с `useSortable` — коллизия id).
- Документация: в примерах legacy-страниц есть ошибки (accessibility — `useState` без деструктуризации; keyboard — коды `'Right'` вместо `ArrowRight`; sortable — `over.id` без проверки `over`); опираться на типы `dist/*.d.ts` и демо antd.
