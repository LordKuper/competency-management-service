[REVIEW-impl-testing]: APPROVE

# Review — testing

- **Phase**: impl-review
- **Iteration**: wave-2/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

## Coverage (internal reviewers only)

Реестр покрытия — одним блоком ниже, привязан к digest манифеста. Что сверено по существу:

- Строки `add` записи 6 (13 тестовых файлов): каждый заявленный тест есть в диффе, поведение в нём утверждается, а не только названо. Ячейки «Regression proof» названы по упавшему тесту и тексту ошибки runner, мутации выполнены в копии `git archive HEAD web`, рабочее дерево не мутировалось (выборочно сверены `LinkButton.tsx`, `QueueLayout.tsx`, `StepRuler.tsx`, `deskTexts.ts`, `scenario.css` — мутаций на диске нет).
- Арифметика fail-first сходится: 44 новых и 6 прежних теста дают 50; восемь стражей (`LinkButton` 1, `MyPlacePage` 1, `AddBlockModal` 1, `DraftsQueuePage` 1, `ConnectUnitPage` 3, `OrgCleanupPage` 1) зелёные до правки, красных 36 — как в записи. Итог набора: 632 прежних плюс 44 равно 676, файлов 55 плюс 10 новых равно 65.
- Каждый коммит дельты `552407b..7a75da6` (список из reflog) отнесён к строке таблицы; не отнесённых нет.
- Строки `none` проверены на честность: `.queue-row-selected` в `scenario.css` действительно стоит после `.queue-row`, `.queue-row:hover` выигрывает по специфичности; удалённые имена (`cancelLabel`, `clearError`, `foreignReads`, `directionOf`) в коде и памяти агентов не встречаются; подсказка `BuilderTrackStep` — статичная строка под существующим условием `unit`. Ручная строка для косметики каскада не нужна: риск вида, не поведения, владелец проверки — ревью соответствия UI.
- Выжившие мутанты (`T2`/`T2b` — обработчик `onOpenChange` в `TrackPositionsPage`, `K2` — множество занятых в `attach`, `G9` — сброс признака правок в форме должности) записаны с причиной «недостижимо или равнозначно библиотеке». Теста они не требуют; судьба соответствующего кода — вопрос ревью эффективности, не тестирования.
- Строки ревью-фикса `TST-W2-1…8` (сегмент `test-plan.entry-05.md`): таблицы `Record<DeskKind, string \| null>` вместо ручных `it.each` (удаление в пределах изменений, причина валидна и записана в «Removed tests»), положительный переход в `DeskCounter`, по случаю на источник роли в `DeskPage`, ошибка 412 в `userMail` — равноценный парный тест к уже существующему в `DeskPage`.
- Синхронизация проверок отсутствия: каждая стоит после положительного перехода (`findByRole` следующей записи, исчезновение «3», `dialogGone`, повторное чтение), а не после счётчика вызовов fakeApi; единственное исключение (`NOT_ATTACHED` в `BuilderCompetenciesStep`) не несёт нагрузки — защиту держит утверждение о теле `PUT /content`.
- Комментариев внутри тел функций и ссылок на AC или спринт в комментариях в просмотренных файлах нет; три подряд прогона 13 файлов по 50 из 50.
- `Manual verification` пуста («—»): автоматизируется всё проверяемое, результата ручной проверки к оценке нет, эскалации по ней нет.

```json
{"manifest_digest":"3ec3a7dddedbeec206d480ec4fde05495403df4caf85ac1d5bcd475a04d05f8b","findings":[],"files":[{"i":"web/src/features/desk/AddBlockModal.test.tsx","s":"checked"},{"i":"web/src/features/desk/BuilderCompetenciesStep.test.tsx","s":"checked"},{"i":"web/src/features/desk/BuilderSubmitStep.test.tsx","s":"checked"},{"i":"web/src/features/desk/ConnectUnitPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/DeskCounter.test.tsx","s":"checked"},{"i":"web/src/features/desk/DeskPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/DeskScenarios.test.tsx","s":"checked"},{"i":"web/src/features/desk/DraftsQueuePage.test.tsx","s":"checked"},{"i":"web/src/features/desk/LinkButton.test.tsx","s":"checked"},{"i":"web/src/features/desk/MatrixBuilderPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/MyPlacePage.test.tsx","s":"checked"},{"i":"web/src/features/desk/OnboardEmployeePage.test.tsx","s":"checked"},{"i":"web/src/features/desk/OrgCleanupPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/ReviewPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/ReworkPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/TrackPositionsPage.test.tsx","s":"checked"},{"i":"web/src/features/desk/builderContent.test.ts","s":"checked"},{"i":"web/src/features/desk/deskTexts.test.ts","s":"checked"},{"i":"web/src/features/users/UserModal.test.tsx","s":"checked"},{"i":"web/src/features/users/roles.test.ts","s":"checked"},{"i":"web/src/features/users/userMail.test.tsx","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-01.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-02.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-03.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-04.md","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/test-plan.entry-05.md","s":"checked"}],"rules":[{"i":"Rule-set conformance","s":"pass"},{"i":"Coverage","s":"pass"},{"i":"Edge cases","s":"pass"},{"i":"Manual verification (last resort)","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[]}
```

## Verdict
APPROVE

## Next action
Тестировщик закрыт: исправлений по тестам и по `test-plan.md` не требуется. Переход к следующему рецензенту состава волны.
