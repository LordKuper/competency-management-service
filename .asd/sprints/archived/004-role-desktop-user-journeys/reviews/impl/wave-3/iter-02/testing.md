[REVIEW-impl-testing]: APPROVE

# Review — testing

- **Phase**: impl-review
- **Iteration**: wave-3/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

Порог итерации — medium. Замечаний на уровне порога и выше нет.

## Coverage (internal reviewers only)

Что сверено по diff итерации (тесты рядом с кодом читались целиком, прогонов нет — у ревьюера нет оболочки):

- `D-3` (BlockModal, AC-61): тест красный на `a1b6ed8` с именем из runner в `Defects`; правка dev — `getTitleCheckboxProps: () => ({ disabled: isSaving })` (`BlockModal.tsx:384`). Строка `none` честна: регрессионный тест записи 8 покрывает риск. Родственного дефекта нет: других таблиц с `rowSelection` две, вторая (`BuilderCompetenciesStep.tsx:431`) запирает строки явным `disabled`, и шапка следует за ними.
- Impacted-набор записи 9 — «14 файлов»: сходится с `Grep` по тестам, называющим `catalogApi`, `BlockModal` или `BlocksTab` (14). Удаление неиспользуемого экспорта поведение не меняет, safety valve не нужен.
- `approveDraft`: упоминаний нет ни в `web/src`, ни в `docs/`, ни в памяти агентов (остались только артефакты спринта). Удаление теста `approves a draft %s` обосновано: путь собирается в `changeCatalogStatus`, его маршрут для каждого сегмента и действия закреплён таблицей `SEGMENTS x ACTIONS` (`satisfies Record<CatalogSegment, true>`, `catalogRoutes.test.ts:22-35`), а `SEGMENT_OF_KIND` — замкнутый `Record<CatalogKind, PublishableSegment>`; удалённый тест проверял только существование маршрута. Согласование черновика через стол закреплено `catalogueTables.test.tsx:257`. Тест в области изменений спринта, согласие пользователя не требуется.
- Запись 8, арифметика: 11 красных до правок = 2+1+1+1+1+1+3+1 по файлам; число `runs` в каждой строке равно числу названных мутантов; все семь форм с `disabled={isSubmitting}` в `web/src/features/career` (`NameModal`, `LevelModal`, `ScaleModal`, `CompetencyModal`, `BlockModal`, `TrackModal`, `MatrixCreateModal`) имеют тест запирания в покое, в полёте и после отказа. Запись 7 (сегмент): все девять замечаний `TST-1…TST-9` имеют строки и доказательства мутациями; пересчёт `runs` по спискам мутантов сходится.
- Проверенные утверждения строк: `AppModal` объединяет `isTouched || isUnsaved` (`AppModal.tsx:42`), `AppModalLeave.test.tsx` закрепляет крест, Escape и «помечено вызывающим без правок» — довод «Esc и затемнение не повторяются» верен, а тест `TrackModal` закрепляет ровно ложное срабатывание на нетронутом окне; фокус на «Отмена» закреплён `useConfirmDeletion.test.tsx:70-85`; `MAX_PAGE_SIZE` берётся из источника, не списком вручную; `openHistory` без остаточных вызовов.
- Проверки отсутствия после положительного перехода (`usePublishEntry.test.tsx` 403 и 404, `UnitHrModal.test.tsx`, `MatrixPage.test.tsx`, `TrackModal.test.tsx`) синхронизированы на отрисованном состоянии, не только на числе вызовов `fakeApi`.
- `none` для раскладки шага мастера в `MatrixEditor` (тестировщик передал решение ревью): принимается. Имя класса `matrix-step-columns--wide` совпадает в `MatrixEditor.tsx:619` и `matrix.css:97`; тот же приём `Grid.useBreakpoint().lg` без теста уже стоит в `UnitTree.tsx:88` и `AppShell.tsx:104`; ни один AC не фиксирует раскладку по ширине. Ручная строка не нужна: `Manual verification` остаётся пустой, результатов ручной проверки для отчёта нет.
- Ниже порога, правки не требуется: сегмент `test-plan.entry-07.md` называет «контрольный прогон пяти файлов — 91 из 91», а мутации записи шли по шести файлам (`UnitHrModal.test.tsx` — шестой); сегмент не правится.
- Не перепроверено без оболочки: красные/зелёные прогоны и счёт `142 успешно` в записи 9 приняты по записи, подтверждены чтением тестов и арифметикой выше.

```json
{"manifest_digest":"83b223d82b309c2511a0b312a4122307c81bde5e8f6b11d5a6942a9451e2b511","findings":[],"files":[{"i":"web/src/app/AppShell.test.tsx","s":"checked"},{"i":"web/src/features/career/BlockModal.test.tsx","s":"checked"},{"i":"web/src/features/career/CompetencyModal.test.tsx","s":"checked"},{"i":"web/src/features/career/LevelModal.test.tsx","s":"checked"},{"i":"web/src/features/career/MatrixCreateModal.test.tsx","s":"checked"},{"i":"web/src/features/career/MatrixPage.test.tsx","s":"checked"},{"i":"web/src/features/career/NameModal.test.tsx","s":"checked"},{"i":"web/src/features/career/ScaleModal.test.tsx","s":"checked"},{"i":"web/src/features/career/TrackModal.test.tsx","s":"checked"},{"i":"web/src/features/career/catalogRoutes.test.ts","s":"checked"},{"i":"web/src/features/career/catalogueTables.test.tsx","s":"checked"},{"i":"web/src/features/career/columnFilterWiring.test.tsx","s":"checked"},{"i":"web/src/features/career/usePublishEntry.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/UnitHrModal.test.tsx","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-01.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-02.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-03.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-04.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-05.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-06.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-07.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-08.md","s":"checked"}],"rules":[{"i":"Rule-set conformance","s":"pass"},{"i":"Coverage","s":"pass"},{"i":"Edge cases","s":"pass"},{"i":"Manual verification (last resort)","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[]}
```

## Verdict
APPROVE

## Next action
Роль Testing в волне 3 закрыта: завершённые решения записей 7–9 приняты, замечаний на уровне порога нет. Оркестратор фиксирует защёлку APPROVE и дожидается остальных рецензентов; после выполнения состава волны — следующая волна или терминальный полный прогон.
