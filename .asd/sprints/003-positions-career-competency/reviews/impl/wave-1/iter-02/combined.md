[REVIEW-impl-combined]: APPROVE

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-1/iter-02

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Coverage (internal reviewers only)

Манифест: `.asd/sprints/003-positions-career-competency/reviews/impl/wave-1/iter-02/combined.manifest.json`, digest `d48bdf9e872a790c1dda47f3786b8a3c7c52aff7aa79e034e4468f7a57dab450`. Ledger: 7 файлов, 29 правил, 16 секций. Каждый id манифеста закрыт; n/a стоит только с предикатами, которые разрешает манифест. Порог серьёзности — medium.

Что проверено по замечаниям iter-01:
- F1 исправлено. Константы `BeforeCareer` больше нет (`MigrationDatabase.cs`). Grep по `src` и `tests` не нашёл ни одного обращения к ней, так что сборка не сломалась. Каждая из оставшихся констант (`BeforePositions`, `Positions`, `Catalogues`, `Matrices`, `BeforeOwnership`, `Ownership`, `Salary`, `Levels`) используется в `PositionMigrationTests` или `CareerMigrationTests`.
- F2 исправлено. В пункте памяти `asd-dev-critical/project_position-catalogue-facts.md:11` литерала с диапазоном больше нет. Вместо него стоит ссылка на константу `Whitespace` и объяснение, что второй аргумент `btrim` — набор символов, а не регулярное выражение. Сверено с кодом: в `20261009090759_PositionCatalogue.cs:18` кодовые точки U+2000…U+200A перечислены по одной.
- F3 исправлено. Повторы удалены из doc класса `EmployeeEndpoints`, из doc `EmployeeRequest.PositionId` и из doc класса `OrgUnitPositionEndpoints`. Каждое правило теперь записано в одном месте. Проверка должности при её задании или переводе и сохранение прежней должности описаны в `Employee.PositionId` (Employee.cs:33-36). Наследование перечня от вышестоящих подразделений и то, что исключение из перечня удаляет строку, описаны в `OrgUnitPosition` (OrgUnitPosition.cs:5-8). После удаления повторов ни один факт не потерялся.
- Изменения в памяти `asd-reviewer-combined` (`MEMORY.md`, `feedback_wave-review-probes.md`) не противоречат коду и друг другу. В добавленных строках нет идентификаторов спринтов и задач.
- В добавленных строках диффа нет комментариев в теле функций, `AC-N` и `TODO`. Код меняется только в doc-комментариях и в одной тестовой константе, так что поведение, контракты и производительность не затронуты.

```json
{"manifest_digest":"d48bdf9e872a790c1dda47f3786b8a3c7c52aff7aa79e034e4468f7a57dab450","findings":[],"files":[{"i":".claude/agent-memory/asd-dev-critical/project_position-catalogue-facts.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-combined/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-combined/feedback_wave-review-probes.md","s":"checked"},{"i":"src/Competency.OrgStructure/EmployeeEndpoints.cs","s":"checked"},{"i":"src/Competency.OrgStructure/EmployeeRequest.cs","s":"checked"},{"i":"src/Competency.OrgStructure/OrgUnitPositionEndpoints.cs","s":"checked"},{"i":"tests/Competency.Tests/Infrastructure/MigrationDatabase.cs","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"},{"i":"Overall quality","s":"reviewed"}]}
```

## Verdict
APPROVE

## Next action
Исправлять ничего не нужно: все три замечания iter-01 (F1, F2, F3) закрыты. Ревью волны 1 можно закрывать после того, как оркестратор сведёт вердикты остальных ревьюеров.

## Escalations (optional)
- нет
