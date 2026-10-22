[REVIEW-impl-documentation]: APPROVE

# Review — documentation

- **Phase**: impl-review
- **Iteration**: wave-1/iter-03

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

## Coverage (internal reviewers only)

```json
{"manifest_digest":"9659d1e62df8af98db6480bdb05e2378ab0a9b2fda8c8784fc8c34a86ee4e2f0","findings":[],"files":[{"i":".claude/agent-memory/asd-dev-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-dev-critical/project_publication-cascade-facts.md","s":"checked"},{"i":".claude/agent-memory/asd-dev-critical/project_role-set-hr-assignment-facts.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-testing/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-testing/feedback_review-isolation-and-log-window-tests.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogEndpoints.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogPublication.cs","s":"checked"},{"i":"src/Competency.CareerFramework/CatalogStore.cs","s":"checked"},{"i":"src/Competency.CareerFramework/PublicationPlan.cs","s":"checked"},{"i":"tests/Competency.Tests/DeskQueryCountTests.cs","s":"checked"},{"i":"tests/Competency.Tests/MatrixAccessTests.cs","s":"checked"},{"i":"tests/Competency.Tests/PublicationCascadeTests.cs","s":"checked"}],"rules":[{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[]}
```

## Verdict
APPROVE

## Next action
Ревьюер закрыт (APPROVE, floor high). Основание:
- Инкремент с головы итерации 2: `CheckAsync` временно активирует корень и черновики и возвращает прочитанные статусы, `Apply` активирует черновики, `CatalogEndpoints.PublishAsync` активирует корень перед `Apply`, `ViewerAsync` пропускает обход маршрута только для администратора. Док-комментарии `CatalogPublication.Apply`, `CatalogPublication.CheckAsync`, `CatalogStore.ViewerAsync` и параметр `PublicationPlan.Entries` совпадают с кодом на HEAD (все вызовы `Apply` и `PlanAsync` просмотрены: `CatalogEndpoints`, `MatrixWorkflow`). Комментариев внутри тел методов, ссылок на AC или спринт в комментариях и строках, маркеров `TODO(sprint-` нет; `stubs.md` пуст.
- Постоянные документы вне манифеста и вне diff этого диапазона, сверены с кодом на HEAD: `docs/architecture/career-framework.md` (строки 18–21, 33, 80–84: видимость черновых треков и уровней через маршрут, предпросмотр только чтением, видимые блокирующие до 5 и остальные в `moreCount`) и `docs/architecture/tech-reference/testcontainers-postgresql-4.15.0.md`, `postgres-image-18.6-trixie.md` (`max_connections=500`, `ConnectionLimitSetting`, один контейнер на прогон, своя БД на хост) совпадают с `TestEnvironment`, `HostFixture`, `CatalogViewer.Sees`. Расхождений нет.
- Память агентов в diff: утверждения про `CheckAsync`/`Apply`/`SeesLoaded`, `ViewerAsync` и `RouteEntryIds`, скрытый блокирующий уровень (`MatrixTrackDraftsAsync`, уровень берёт статус трека, `IsNameTakenAsync` по авторам), `ApiHost.Logs`/`LogsAfterAsync`/`EventId` 20101, `HostFixture` на класс сверены с кодом и тестами; устаревших формулировок прежнего поведения («HR пропускается», «план активирует отслеживаемые записи») в `.claude/agent-memory/**` не осталось.

Вне порога (floor high), в счёт не идёт; для владельца памяти при ближайшей правке:
- `asd-dev-critical/project_publication-cascade-facts.md`: изменённая строка `description` содержит «Sprint 004 Task 5» (шаблоны `MEMORY_HISTORY_PATTERNS` в `.asd/runtime.js` для `sprint[ -]?\d+` и `Task \d+`; `artifact-layout.md` «Agent memory», Content); строка 14 того же файла (вне diff) устарела на HEAD: `UserStatus` несёт `IsHr` и `IsGlobalAdmin`, `CatalogPublication.PlanAsync(version)` берёт `hr.IsGlobalAdmin`, `career-framework.md` строка 19 говорит то же.
- `asd-tester-critical/project_competency-test-harness.md`: «archived src is CRLF» — смесь окончаний строк (`CatalogStore.cs` CRLF, `CatalogPublication.cs`, `PublicationPlan.cs`, `CatalogEndpoints.cs` LF); правило «окончание строки файла» верно.

## Escalations
none
