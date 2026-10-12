[REVIEW-impl-combined]: APPROVE

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-2/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Coverage (internal reviewers only)

```json
{"manifest_digest":"42d6d546c8dc5c33aec66ea839cef124ef6d0dd0d26a2f6a13cb531950a02611","findings":[],"files":[{"i":".claude/agent-memory/asd-dev-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-dev-critical/project_scripted-insert-doc-orphan.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-combined/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-combined/feedback_wave-review-probes.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogDraftEndpoints.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogEndpoints.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogStore.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CompetencyCategoryEndpoints.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CompetencyRequest.cs","s":"checked"},{"i":"src/Competency.CareerFramework/MatrixCatalog.cs","s":"checked"},{"i":"src/Competency.CareerFramework/MatrixContent.cs","s":"checked"},{"i":"src/Competency.CareerFramework/MatrixEndpoints.cs","s":"checked"},{"i":"src/Competency.CareerFramework/MatrixRoute.cs","s":"checked"},{"i":"src/Competency.CareerFramework/MatrixWorkflow.cs","s":"checked"},{"i":"src/Competency.OrgStructure/EmployeeDirectory.cs","s":"checked"},{"i":"src/Competency.OrgStructure/IEmployeeDirectory.cs","s":"checked"},{"i":"src/Competency.Platform/IfMatch.cs","s":"checked"},{"i":"src/Competency.UserManagement/IUserDirectory.cs","s":"checked"},{"i":"src/Competency.UserManagement/UserDirectory.cs","s":"checked"},{"i":"tests/Competency.Tests/CareerLockTests.cs","s":"checked"},{"i":"tests/Competency.Tests/CompetencyTests.cs","s":"checked"},{"i":"tests/Competency.Tests/Infrastructure/CareerScenarios.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixAccessTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixCreationTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixWorkflowTests.cs","s":"checked"},{"i":"tests/Competency.Tests/TrackAndLevelTests.cs","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"},{"i":"Overall quality","s":"reviewed"}]}
```

Manifest и ledger: `combined.manifest.json` в этой папке итерации; инкрементальный diff `1e1097b84c2bb496.diff` (review-fix и impl-test entry 4).

Основа вердикта. Все находки wave-2/iter-01 закрыты, ниже — как это проверено.

- Combined F1: `Member.EmployeeId`, `CareerOrg.Host`, `Statuses` удалены вместе с лишним `using`.
- Combined F2: `CatalogStore.ViewerAsync` не считает `RouteEntryIds` для администратора и HR. Это сверено с `CatalogViewer.Sees`: HR и администратор видят все черновики без подразделения. Черновые уровни бывают только у черновых треков: `CareerLevelEndpoints` берёт статус уровня у трека, а матрица требует действующий трек. `SuperiorOfAsync` теперь обходит только поддеревья, которыми пользователь руководит, и кэширует руководителя каждого подразделения.
- Combined F3: добавлены пакетные `FindUnitsAsync` и `FindManyAsync`.
- Combined F4: появился `ReadableAsync` — один запрос по решениям и кэш вышестоящего согласующего по подразделению. Логика равна `CanReadAsync` с `isHistory: false`; навигация `Matrix` подгружается через `AutoInclude`.
- Combined F5: повторная активация трека согласует его черновые уровни. Для Active→Active и для трека, который был действующим, черновых уровней нет, так что «повторная установка статуса ничего не меняет» по-прежнему верно.
- Combined F7: комментарии в телах методов перенесены в `<summary>`.
- External 1: `returnedWithChanges` теперь считается по `VariantNumber` последнего решения `Returned`.
- External 2: в `ApproveAsync` снова проверяется полнота, под блокировкой перехода. Без правок согласующий по-прежнему может вернуть версию с комментарием, поэтому тупика нет.
- External 3: черновые категории активируются при публикации, с проверкой названия и записью в журнал — это принято в decisions-log 2026-10-12.
- External 4: `IfMatch.ApplyTo` бросает исключение сразу. Проверены все 29 вызовов. Локальный `catch` в `AppUserStore` это исключение не видит: оно возникает раньше `SaveChanges`. `ProblemExceptionHandler` отдаёт тот же голый 412, что раньше давал `Rejections.From` для `ConcurrencyFailure`. То, что 412 теперь срабатывает раньше 409/400, принято в decisions-log.
- External 5: перед переходом версия ищется через `FirstOrDefaultAsync`, на это есть тест гонки удаления и перехода.
- External 6: добавлена проверка `Enum.IsDefined`. Новые тексты валидации записаны в decisions-log. В plan «Тексты интерфейса» таких текстов валидации нет, как и у существующего «Вес не может быть отрицательным».

Комментариев в телах методов, AC-id в коде и `TODO(sprint` в diff нет. Ручная проверка: все 4 строки — pass (decisions-log, smoke check). Результаты тестов (backend 487/487, web 303/303, entry 4) взяты из записи тестера и не пересчитывались: у ревьюера нет оболочки.

Ниже порога (low, не считаются):
- `ReadableAsync` для руководителя по-прежнему вызывает `SuperiorApproverAsync` (подъём по предкам) по одному разу на каждое подразделение страницы, даже вне его поддеревьев. Обход можно ограничить подразделениями из `UnitsBelowHeadedAsync`.
- Пример в собственной памяти ревьюера («черновая категория остаётся черновой при публикации — не дефект») противоречил решению 2026-10-12. Ревьюер исправил его в своей памяти, `feedback_wave-review-probes.md`.

## Verdict
APPROVE

## Next action
Волна 2 закрыта со стороны combined; дальше — вердикт external wave-2/iter-02 и переход к волне 3.

## Escalations (optional)
