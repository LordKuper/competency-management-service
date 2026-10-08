[REVIEW-impl-combined]: APPROVE

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-3/iter-03

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

Замечаний нет. F1 из wave-3/iter-02 закрыт обеими частями.

## Coverage (internal reviewers only)

Manifest `.asd/sprints/001-project-init-org-structure/reviews/impl/wave-3/iter-03/combined.manifest.json`, digest `d35cc15d9c9af0ce63fe981ade35e79aac95c131692bcfd60013956fc4d8f64c`. Ledger ниже: 6 файлов, 29 правил, 16 секций, каждый id манифеста разрешён.

Закрытие F1 (iter-02):
- (а) `web/src/app/useDebouncedValue.ts`: параметр `delayMs` удалён, задержка берётся из `SEARCH_DEBOUNCE_MS`, зависимости эффекта `[value]`. Константа остаётся экспортируемой, её читает `OrgStructurePage.test.tsx:14,171`. Оба вызывающих (`useEmployeeSearch.ts:29`, `EmployeePicker.tsx:40`) звали хук без второго аргумента, поведение не изменилось. Строка doc-комментария «for the delay» остаётся верной.
- (б) `web/src/test/fakeApi.ts`: `json(body, status = 200)` удалён; `problem`, `deferred`, `fakeApi` остались и все используются (`problem` в `authFlow.test.tsx`, `editDialogs.test.tsx`; `deferred` в `authFlow.test.tsx`, `OrgStructurePage.test.tsx:132`). Вызовов `json(` в `web/src` не осталось (Grep), импорты `json` из `test/fakeApi` в трёх тестовых файлах убраны, все 14 мест переведены на `Response.json(...)`. Других импортёров `test/fakeApi` нет.

Basis (прочитано, не пересчитано): весь diff целиком (216 строк); `useDebouncedValue.ts` и `fakeApi.ts` в итоговом виде целиком; iter-02 `combined.md`; `test-plan.md` entry 8 (строки 24-64) и записи `decisions-log.md` о review-fix wave-3/iter-02; `feedback_frontend-wave-probes.md` целиком. Cross-file проверки Grep: `\bjson\(` по `web/src` (остались только `Response.json` и `.json()` у Request), `from ".*test/fakeApi"` (три импортёра, имена совпадают с экспортами), `useDebouncedValue|SEARCH_DEBOUNCE_MS` по репозиторию (два вызова без задержки, одно определение, одно чтение константы в тесте; утверждение `project_org-employee-search-facts.md:15` остаётся верным), `delayMs` и `json(body` вне истории ревью (только записи test-plan и decisions-log об удалении). Длина строк переформатированных вызовов в `editDialogs.test.tsx` и `OrgStructurePage.test.tsx` посчитана вручную, не больше 78 символов.

Файл памяти `feedback_frontend-wave-probes.md` (собственная память рецензента): добавленный блок не вводит устаревших утверждений о коде (`delayMs` и `json(status)` приведены как примеры уже удалённого, и текст это передаёт), описание в frontmatter и индекс `MEMORY.md` согласованы.

Строк ручной проверки в `test-plan.md` нет, судить нечего. Новых тестов дельта не добавляет (web 60 → 60), AC-покрытие не затронуто: поведение не менялось, `OrgStructurePage.test.tsx` по-прежнему проходит задержку набора через ту же константу.

Shell не запускался: lint (biome ci и typecheck), `vite build` и vitest (9 файлов, 60/60) взяты из `test-plan.md` entry 8 (HEAD a817235, рабочее дерево равно коммиту), не из моего прогона; последующие коммиты 995b138 и fc730dc меняют только `.asd/**`. `docs/**` diff не затрагивает, persistent-доки пишет design-promote, дрейфа нет. Проза отчёта на `language.docs` (ru).

Замечено и не засчитано: имя `useDebouncedValue` общее, а задержка привязана к `SEARCH_DEBOUNCE_MS`; это ровно та форма, которую просил F1, и других вызывающих кроме двух поисков нет.

```json
{"manifest_digest":"d35cc15d9c9af0ce63fe981ade35e79aac95c131692bcfd60013956fc4d8f64c","findings":[],"files":[{"i":".claude/agent-memory/asd-reviewer-combined/feedback_frontend-wave-probes.md","s":"checked"},{"i":"web/src/app/authFlow.test.tsx","s":"checked"},{"i":"web/src/app/editDialogs.test.tsx","s":"checked"},{"i":"web/src/app/useDebouncedValue.ts","s":"checked"},{"i":"web/src/features/org-structure/OrgStructurePage.test.tsx","s":"checked"},{"i":"web/src/test/fakeApi.ts","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"},{"i":"Overall quality","s":"reviewed"}]}
```

## Verdict
APPROVE: 0 findings

## Next action
Замечаний нет, review-fix wave-3 закрыт; можно переходить к завершающему шагу impl-review (полный прогон набора при APPROVE/latched у всех рецензентов волны).

## Escalations
Нет.
