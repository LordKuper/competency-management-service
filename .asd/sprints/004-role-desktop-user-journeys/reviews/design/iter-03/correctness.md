[REVIEW-design-correctness]: CONCERNS

# Review — correctness

- **Phase**: design-review
- **Iteration**: 3

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | .asd/sprints/004-role-desktop-user-journeys/design/prd.html:609 (AC-34.10); adr.html:608 (Context), :632-637 («Создание записи хранителем (AC-34.1)»), :900 (ADR-6, изменения поведения); код: src/Competency.CareerFramework/CompetencyEndpoints.cs:177 → `ReferenceProblemAsync`:240-264 → CatalogStore.cs:124-129 (`IsUsableAsync`) | AC-34.10 и ADR-4 закрывают состояние «действующая запись ссылается на черновик» только для `POST`. Соседний путь записи, `PUT` действующей компетенции, создаёт то же состояние и остаётся открытым. `UpdateAsync` проверяет новую категорию, направление или шкалу через `IsUsableAsync(…, competency.AuthorId)`, а она разрешает черновики автора компетенции. Пример из сценариев этого спринта. Руководитель H создаёт в мастере компетенцию и черновую категорию K2. Компетенция становится действующей с публикацией версии. Затем HR (`CanEdit` действующей = `Keeps`, CatalogViewer.cs:85) правит её и выбирает K2. Сервер принимает правку, и действующая компетенция ссылается на черновик без утверждения, без перечня и без каскада. Это противоречит AC-34.1 (must) и цели AC-34.10. Дальше ADR-4:625 («Каскад идёт транзитивно и только по черновым сущностям») не проходит через действующую компетенцию. Тогда версия с ней публикуется, а черновое направление или черновая шкала этой компетенции так и остаются черновиками. Это нарушает AC-34.2 («прямо или через другие»). Сейчас `MatrixCatalog.DraftsAsync`:163-166 подбирает хотя бы черновые категории действующих компетенций. Этот путь встречается чаще, чем закрытый: хранитель сам черновиков категорий, направлений и шкал не создаёт (`StatusOfNew` → Active), а черновики руководителей в сценариях спринта появляются постоянно. | Расширить правило на правку. В PRD AC-34.10 (или новом AC-34.11) записать: правка действующей записи не может назначить ей черновую ссылку (AC-34.3); такая правка отклоняется 400 с ошибкой поля, запись остаётся как была. В ADR-4 пункт «Создание записи хранителем» переименовать в «Запись хранителем» и добавить правило для `PUT` действующей записи. Проверка — в общем месте, в `ReferenceProblemAsync`: у действующей записи новая ссылка должна быть действующей, а не «черновик автора». В ADR-6:900 добавить строку о `PUT` компетенций. Отдельно уточнить в ADR-4:625, что замыкание версии проходит и через действующие компетенции к их черновым ссылкам. Иначе публикация версии потеряет нынешнее поведение `DraftsAsync`. |

## Coverage (internal reviewers only)

Manifest: `.asd/sprints/004-role-desktop-user-journeys/reviews/design/iter-03/correctness.manifest.json`. Diff: `9c34a9f38df333d3.diff`. Фаза сохраняет проверенный ledger рядом с этим отчётом.

```json
{"manifest_digest":"a92511fded3e386b2f44b71636e4ce3a74eff9dfb03ca98ac1d60752cb2a8988","findings":["1"],"files":[{"i":".asd/sprints/004-role-desktop-user-journeys/design/prd.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/adr.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/design-md-delta.yaml","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"finding","f":"1"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-design-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"reviewed"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"}]}
```

Порог — high: находки low и medium отброшены. Что проверено:

- **Перечень `publishingActions` (AC-34.4, ADR-4:655).** Перечень сверен с `MatrixWorkflow.cs`. `ApproveAsync`:365-367 и `AcceptChangesAsync`:411-413 ведут через `AdvanceToHrAsync` в `ToHrAsync`:436-445. Там автосогласование и публикация, если ответственный HR — автор последнего варианта автора. Пропуск шага 1 (:343-344) покрыт `submit`. Перечень хранится в одном месте. AC-8.14, 9.4, 10.4, 10.8, 34.7, примечания m-rework/m-review и подписи макетов ссылаются на него и его не повторяют.
- **Создание черновиком (AC-34.10).** Совпадает с решением пользователя (decisions-log.md:42 (2)). AC-34.4 больше не называет «создание сразу действующей». ADR-4 lede, Alternatives и ADR-6:900 согласованы. Строка AC-34.10 есть в таблице покрытия ux. Направление, категория и шкала ссылок не имеют (CareerDirection.cs:9, CompetencyCategory.cs:9), поэтому правило фактически касается только компетенции. Это безвредно и ниже порога.
- **Тексты усечения (m-desk-3, каталог).** Разведены по `isReference`. Ни один справочный текст не обещает, что список уменьшится от действий. Формулировка ADR-2 о «потолке» согласована с ux.
- **design-md-delta.yaml.** `new_value` у `step-current` теперь равен карте DESIGN.md:436-441, `colors.accent` равен DESIGN.md:12. Поле `doc_patch` выходит за шаблон: решение есть в decisions-log.md:44, формат подтверждается на design-promote.
- **Токены нового макета.** В окне «Результат создания» только `var(--ds-*)`. «Не сейчас» и «Утвердить вместе с 2 записями» соответствуют U-21. Фокус и отмена описаны.
- **Отклонение от процедуры.** В начале проверки прочитан отчёт прошлой итерации `reviews/design/iter-02/correctness.md`, а `review-policy.md` «Clean-context review iteration» это запрещает. Находка 1 выведена из `.diff`, драфтов и кода (`CompetencyEndpoints`, `CatalogStore`, `CatalogViewer`, `MatrixCatalog`). Прошлую находку она не повторяет и на неё не опирается.

## Verdict
CONCERNS: 1

## Next action
Исправления в цикле: asd-ba — PRD AC-34.10 или новый AC-34.11 (#1); asd-architect — ADR-4 «Запись хранителем», ссылки каскада :625 и ADR-6:900 (#1). ux-spec не меняется: правка отклоняется существующей ошибкой поля. Затем design-review, итерация 4.

## Escalations (optional)
- question: 1 — Что делать с правкой действующей записи, которая назначает ей черновую ссылку?; options: a) отклонять 400 с ошибкой поля (по умолчанию: опубликованная запись не теряет статус) / b) переводить запись в черновик по образцу AC-34.10 (действующая компетенция выпадет из опубликованных матриц до утверждения)
