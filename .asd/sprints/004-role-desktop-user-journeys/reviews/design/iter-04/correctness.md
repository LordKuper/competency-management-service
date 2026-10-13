[REVIEW-design-correctness]: CONCERNS

# Review — correctness

- **Phase**: design-review
- **Iteration**: 4

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | adr.html ADR-4 «Совпадение показанного и выполненного» (стр. 658–662), ADR-6 «Изменения поведения» (стр. 912); ux-spec.html «Изменяемые существующие экраны» (стр. 2468), m-states примечание «412 «перечень изменился»» (стр. ~2371); prd.html AC-34.6/AC-34.7 | Исправление iter-03 сделало тег `?cascade` обязательным для каждого публикующего перехода версии (`approve` на шаге HR публикует всегда; `submit`/`approve` шага 1/`accept-changes` — при автосогласовании HR). Но в SPA остаётся существующая страница матрицы с теми же переходами: `web/src/features/career/MatrixPage.tsx:56-92` (`TRANSITION_BUTTONS`: «Отправить на согласование», «Согласовать», «Принять правки»), вызов `matrixApi.ts:193` без тега. Ни один черновик эту страницу не меняет: ux-spec перечисляет изменяемые существующие экраны без неё, а 3б применяется только в «Построить матрицу», «Доработать», «Согласовать версию», «Утвердить черновые записи» и в разделах. По ADR-4 «Согласовать» на этой странице у HR на шаге 2 всегда получает 412 без тега. Общий обработчик (`web/src/app/apiErrors.ts:43` → `STALE_MESSAGE`) показывает «Данные изменены другим пользователем», перечитывание ничего не меняет, повтор снова даёт 412: согласовать версию с этой страницы нельзя. При этом m-review «Внести правки» сам ведёт согласующего на эту страницу (ux-spec стр. 1461, U-11). До iter-03 тег был необязательным, и страница публиковала без перечня (пробел AC-34.7); теперь это неработающий путь. Утверждение ADR-6 «единственный клиент — своя SPA», на котором держится отказ от 4.0.0, не учитывает, что эта часть SPA ломается. | Решить судьбу публикующих переходов на странице матрицы и записать решение в ux-spec (в «Изменяемые существующие экраны» и в список экранов 3б), в ADR-4 (кто передаёт тег) и в PRD AC-34.6 (где показывается перечень). Вариант (а): убрать с `MatrixPage` кнопки `submit`/`approve`/`accept-changes` и вести на «Согласовать версию», «Доработать» и шаг 6 мастера. Вариант (б): провести эти кнопки через предпросмотр, окно с перечнем и тег, а также через обработку 412 с полем `publication` (3б). |

## Coverage (internal reviewers only)

Manifest: `.asd/sprints/004-role-desktop-user-journeys/reviews/design/iter-04/correctness.manifest.json`; diff: `77e3a376b1271e01.diff`. Сверено с кодом: `MatrixWorkflow.cs` (Submit/Approve/AcceptChanges → ToHrAsync/PublishAsync; Rollback и AcceptAndRevise не публикуют), `CompetencyEndpoints.ReferenceProblemAsync`, `CareerTrackEndpoints` (у трека только переименование, направление не меняется), `MatrixEndpoints.ReferenceProblemAsync` (направление матрицы только действующее, трек того же направления), `MatrixCatalog.DraftsAsync`/`Fits`, `CatalogEndpoints` approve/activate с `If-Match`, SPA `MatrixPage.tsx`, `catalogApi.ts`, `apiErrors.ts`. Отчёты прежних итераций не открывались.

```json
{"manifest_digest":"97a6a33426e3e09f9f623c821ebd7a30e0c56929236847758d7af02434e2d6d0","findings":["1"],"files":[{"i":".asd/sprints/004-role-desktop-user-journeys/design/prd.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html","s":"checked"},{"i":".asd/sprints/004-role-desktop-user-journeys/design/adr.html","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"finding","f":"1"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-design-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"reviewed"},{"i":"Bugs [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Security [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Contracts [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"Best practices [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"AC coverage trace [impl-review]","s":"n/a","p":"outside phase gate"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"}]}
```

## Verdict
CONCERNS: 1

## Next action
asd-ux, asd-architect, asd-ba: внести во все три черновика ответ пользователя на вопрос #1 (что делать с публикующими переходами существующей страницы матрицы), затем iter-05.

## Escalations (optional)
- question: 1 — Публикующие переходы на существующей странице матрицы (`MatrixPage`: «Отправить на согласование», «Согласовать», «Принять правки») при обязательном теге всегда получают 412. Как поступить?; options: (а) убрать эти кнопки со страницы матрицы и вести на новые экраны («Согласовать версию», «Доработать», шаг 6 мастера) / (б) оставить кнопки и провести их через предпросмотр, окно с перечнем, тег и состояние 3б
