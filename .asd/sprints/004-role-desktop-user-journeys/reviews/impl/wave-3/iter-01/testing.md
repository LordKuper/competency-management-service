[REVIEW-impl-testing]: CONCERNS

# Review — testing

- **Phase**: impl-review
- **Iteration**: wave-3/iter-01

Границы проверки: оболочка без shell, поэтому мутационные и fail-first прогоны волны 3 не перезапускались. Они сверены структурно: названия тестов `leaveGuard.test.tsx`, `AppModalLeave.test.tsx`, `authFlow.test.tsx`, `usePublishEntry.test.tsx` совпадают со строками мутаций записи 1 (L3, A1–A4, W1–W11, PE1–PE13); арифметика записи 7 сходится (676 + 9 добавленных = 685; 4 + 2 + 1 + 1 + 1 = 9). Ручной проверки `test-plan.md` не требует (всё автоматизировано, Playwright правилом проекта не используется) — результата ручной проверки нет и ожидать нечего.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| TST-1 | medium | .asd/sprints/004-role-desktop-user-journeys/test-plan.md:40 (Removed tests); web/src/features/career/catalogueTables.test.tsx:120; web/src/features/career/MatrixPage.test.tsx:135 | В волне 3 удалено около 20 web-тестов, а в `Removed tests` и `Risk → check decisions` для них нет ни одной строки (записи 2 и 3 вели такие строки для backend): MatrixPage.test.tsx — «adds a named block…», «says why a block that still has competencies cannot be deleted…», «offers the competencies a matrix of this version may use…» и три переписанных на ссылки стола; catalogueTables.test.tsx — восемь тестов `CategoriesTab` и строки `tracks of a direction` и `categories` в `TABLES`; columnFilterWiring.test.tsx — случаи «categories by status», «competencies by category», «competencies by type»; matrixContent.test.ts — четыре теста блоков и порогов и тест UUID `addBlock`. Причины правдоподобны (AC-51, AC-55, блоки перенесены в мастер), но `test-plan.md` — единственный дом причины удаления. Изменённые утверждения AppShell.test.tsx (`navItemsFor`, группа «Справочники», вход HR), MatrixPage.test.tsx (ссылки `Submit`/`Approve`/`AcceptChanges` на экраны стола, запирание действий на время сохранения) и UnitHrModal.test.tsx (назначение по `employeeId`, `hrCandidate=true`) не имеют ни строки решения, ни следа fail-first или мутации; строка записи 1 с `none` для «редактора матрицы» этому противоречит | Добавить строки в `Removed tests` (тест, причина, в области изменений, замена) и строки решения с доказательством (мутация либо fail-first) для названных файлов; замены для потерянного поведения — см. TST-2, TST-3 |
| TST-2 | medium | web/src/features/career/catalogueTables.test.tsx:120; web/src/features/career/columnFilters.ts:61 | AC-24: выбор размера страницы (`showSizeChanger`, `onChange` в `pageProps`) проверялся единственным тестом `the page size of a catalogue table` через `CategoriesTab`; он удалён вместе с вкладкой, и ни один оставшийся тест не выбирает размер (`columnFilters.test.ts` не упоминает `pageProps`). Мутант `onChange`, теряющий `pageSize`, остаётся зелёным на пяти таблицах: области, шкалы, компетенции, матрицы, блоки | Перенести тест на `ScalesTab` или `CompetenciesTab` (константы `PAGE_SIZE` и `TOTAL` уже в файле): со страницы 2 выбрать 50 на странице → последний запрос `page: "1", pageSize: "50"` |
| TST-3 | medium | web/src/features/career/catalogueTables.test.tsx:403; web/src/features/career/CatalogParts.tsx:108 | AC-19, AC-25: правила действий строки (`EntryActions`: «Изменить» только при `entry.canEdit`, «Удалить» только при `entry.canDelete`, без действий нет меню «…», «Деактивировать» уходит с `If-Match` версии строки, «Удалить» из меню спрашивает с фокусом на «Отмена» и удаляет с версией строки) проверялись через `CategoriesTab`, эти тесты удалены. Оставшиеся строки (`:406-445`) собраны из `editable()` с `canEdit` и `canDelete` равными `true`: отрицательной ветки флагов, случая «только удаление», пустого меню и отправки версии из меню не проверяет ни один тест (`useConfirmDeletion.test.tsx` покрывает хук, не проводку строки). Мутант `entry.canDelete` → `true` (`CatalogParts.tsx:143`) выживает | Повторить на `ScalesTab`/`CompetenciesTab`: три строки (обе возможности, только удаление, ни одной) → списки видимых действий и меню; «Деактивировать» из меню → `If-Match: "3"`; подтверждение удаления с фокусом на «Отмена» и `DELETE` с версией строки |
| TST-4 | medium | web/src/features/career/catalogueTables.test.tsx:36; web/src/features/career/columnFilterWiring.test.tsx:94 | AC-24: новая вкладка «Общие блоки» (`BlocksTab.tsx`) не названа ни одним тестом репозитория — ни в `TABLES` (чтение страниц с сервера), ни в `CASES` (проводка фильтров колонок). У неё собственный фильтр «Действует для» (`ScopeFilter`, `BlocksTab.tsx:116-181`, параметры `scopeUnitId` и `forAll`), а `columnFilterWiring` существует ровно для проверки имён параметров, которые читает сервер. Оба перечня написаны вручную и пропустили вкладку, добавленную в этом спринте; опечатка в имени параметра останется зелёной | Добавить блоки в `TABLES` и два случая (`scopeUnitId`, `forAll=true`) в `CASES` |
| TST-5 | medium | web/src/app/AppShell.test.tsx:92; web/src/app/AdminOnly.tsx:14 | Роль HR (AC-19): маршрут `/positions` открыт администратору и HR (`AdminOrHrOnly`, `positions/routes.ts:29`), но тесты `:93-110` открывают его только администратору и пользователю без ролей; для HR закреплена лишь запись меню (`:85-88`). Мутант `hasAccess = isAdmin` (без `isHr`) остаётся зелёным, и HR получит пункт «Должности», ведущий на «Страницу не найдена» | Тест: `openAs(["Hr"], "/positions")` → заголовок «Должности» и запрос списка должностей |
| TST-6 | medium | web/src/features/career/MatrixPage.test.tsx:115; web/src/features/career/MatrixPage.tsx:171 | Правило проекта «Mutating UI flows» и AC-61: страница подключена к общей защите (`useLeaveGuard(edits)`, выбор другой версии идёт через `confirmLeave` на `:280`, редактор сообщает правки на `MatrixEditor.tsx:216-221`), но тест рендерит её без `LeaveGuardProvider` и нигде не ждёт вопроса «Перейти без сохранения?». Сама защита проверена в `leaveGuard.test.tsx`, проводка на страницах — только в `OrgCleanupPage.test.tsx`. Мутанты «убрать `confirmLeave` у переключателя версий» и «убрать `onEditsChange` редактора» выживают. Название теста «keeps the link to the desk scenario open with unsaved edits, since leaving goes through the confirmation» шире утверждения: в этой обвязке ссылка просто ведёт без вопроса | Один тест под `LeaveGuardProvider`: набрать порог, выбрать другую версию → вопрос, «Остаться» сохраняет версию и значение, после сохранения вопроса нет; название теста ссылки сузить до того, что он проверяет |
| TST-7 | low | web/src/features/career/catalogueTables.test.tsx:464; web/src/features/career/DirectionsTracksTab.tsx:115 | Граница «много» на изменённом пути: вложенная таблица треков вместо постраничного чтения делает один запрос `pageSize: MAX_PAGE_SIZE` с `pagination={false}` и показывает `TruncationNote` (`:162,168`). Строка `tracks of a direction` из `TABLES` удалена, замены нет: ни запрос с `pageSize=200`, ни текст «Показаны первые N из M — уточните поиск.» (`web/src/app/TruncationNote.tsx:11-15`) не проверяются ни одним тестом репозитория | Один тест рядом с `:464`: `GET /tracks` с `total` больше числа строк → примечание на экране, запрос несёт `pageSize=200` |
| TST-8 | low | web/src/features/career/usePublishEntry.test.tsx:107; web/src/features/career/usePublishEntry.test.tsx:465 | Отсутствие проверяется раньше положительного перехода: в первом тесте утверждения отсутствия окна подтверждения и `.ant-message-notice` (`:107-108`) стоят до `waitFor(isCatalogStale)` (`:109`), а в `it.each([403, 404])` (`:465-476`) синхронизация идёт по числу вызовов `fakeApi`, то есть по входу в `fetch`, а не по доставленному ответу. Мутации PE4 и PE6 в записи 1 названы убитыми, так что на практике проверка срабатывает, но держится на порядке задач, а не на синхронизации | В первом тесте перенести утверждения отсутствия после `isCatalogStale`; во втором дождаться положительного следа обработки ответа (например, сброса кэша каталога) и только потом утверждать отсутствие |
| TST-9 | low | web/src/features/career/MatrixPage.test.tsx:135 | Вспомогательная `openHistory` ветвится по тому, что отрисовано (прямая кнопка «История» либо пункт меню «Действия с версией»), и три теста истории проходят с любым из двух видов. Перенос «История» между видимой кнопкой и меню остаётся незамеченным | Утверждать один ожидаемый вид для каждого состояния версии (либо два явных варианта вызова) вместо проверки наличия |

## Coverage

Манифест: `.asd/sprints/004-role-desktop-user-journeys/reviews/impl/wave-3/iter-01/testing.manifest.json` (digest 479612d1…ebb6c); строки `n/a` отсутствуют, потому что для файлов и правил манифест не допускает ни одного предиката.

```json
{"manifest_digest":"479612d1387a342cf764a765974d21a182e5f3335c80119b7eb6d2f3553ebb6c",
"findings":["TST-1","TST-2","TST-3","TST-4","TST-5","TST-6","TST-7","TST-8","TST-9"],
"files":[
{"i":"web/src/app/AppModalLeave.test.tsx","s":"checked"},
{"i":"web/src/app/AppShell.test.tsx","s":"checked"},
{"i":"web/src/app/UserMenu.test.tsx","s":"checked"},
{"i":"web/src/app/authFlow.test.tsx","s":"checked"},
{"i":"web/src/app/editDialogs.test.tsx","s":"checked"},
{"i":"web/src/app/leaveGuard.test.tsx","s":"checked"},
{"i":"web/src/features/career/CareerPage.test.tsx","s":"checked"},
{"i":"web/src/features/career/CompetencyModal.test.tsx","s":"checked"},
{"i":"web/src/features/career/MatrixCreateModal.test.tsx","s":"checked"},
{"i":"web/src/features/career/MatrixPage.test.tsx","s":"checked"},
{"i":"web/src/features/career/ScaleModal.test.tsx","s":"checked"},
{"i":"web/src/features/career/TrackLevels.test.tsx","s":"checked"},
{"i":"web/src/features/career/TrackModal.test.tsx","s":"checked"},
{"i":"web/src/features/career/careerApi.test.ts","s":"checked"},
{"i":"web/src/features/career/catalogRoutes.test.ts","s":"checked"},
{"i":"web/src/features/career/catalogueTables.test.tsx","s":"checked"},
{"i":"web/src/features/career/columnFilterWiring.test.tsx","s":"checked"},
{"i":"web/src/features/career/matrixContent.test.ts","s":"checked"},
{"i":"web/src/features/career/useCatalogRights.test.tsx","s":"checked"},
{"i":"web/src/features/career/useConfirmDeletion.test.tsx","s":"checked"},
{"i":"web/src/features/career/usePublishEntry.test.tsx","s":"checked"},
{"i":"web/src/features/org-structure/EmployeeImpactList.test.tsx","s":"checked"},
{"i":"web/src/features/org-structure/OrgStructurePage.test.tsx","s":"checked"},
{"i":"web/src/features/org-structure/UnitCard.test.tsx","s":"checked"},
{"i":"web/src/features/org-structure/UnitHrModal.test.tsx","s":"checked"},
{"i":"web/src/features/org-structure/UnitPositionsModal.test.tsx","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-01.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-02.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-03.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-04.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-05.md","s":"checked"},
{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-06.md","s":"checked"}],
"rules":[
{"i":"Rule-set conformance","s":"finding","f":"TST-1"},
{"i":"Coverage","s":"finding","f":"TST-4"},
{"i":"Edge cases","s":"finding","f":"TST-7"},
{"i":"Manual verification (last resort)","s":"pass"},
{"i":".asd/project/custom-common-rules.md","s":"pass"},
{"i":".asd/project/custom-coding-rules.md","s":"finding","f":"TST-6"}],
"sections":[]}
```

## Verdict
CONCERNS: 9

## Next action
Тестировщик ревью-фикса (все девять замечаний лежат в тестах и `test-plan.md`; шесть замечаний уровня medium, поэтому маршрут — review-fix, а не исправление «на месте» для low): TST-1 — строки в `test-plan.md`; TST-2–TST-6 — тесты на оставшихся вкладках и странице матрицы с доказательством (мутация либо fail-first с командой, кодом выхода и именем упавшего теста); TST-7–TST-9 — точечные правки тестов. Код продукта не меняется.
