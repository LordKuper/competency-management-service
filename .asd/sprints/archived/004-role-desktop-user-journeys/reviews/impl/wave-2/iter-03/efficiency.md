[REVIEW-impl-efficiency]: APPROVE

# Review — efficiency

- **Phase**: impl-review
- **Iteration**: wave-2/iter-03

## Findings

| # | Severity | Location | Description | Suggested fix | Category |
|---|---|---|---|---|---|

Замечаний на уровне порога high и выше нет.

## Оценка двойного барьера в OnboardEmployeePage

Вопрос impl-test: запрос `enabled: outcome !== undefined && isRefused` (`OnboardEmployeePage.tsx:104-107`) и подстановка `if (isRefused && latest !== outcome)` (`:109-112`) — каждый достаточен сам по себе. Решение: keep-as-is, замечание не поднято.

- Подтверждено чтением кода: достижимых состояний, где барьеры расходятся, нет. Данные `reread` берутся только из этого запроса; он включён только пока `isRefused`; `isRefused` сбрасывается в `onSaved`, `onAnother` и при подстановке. Мутанты `O1` и `O7` равнозначны и зелёные, красит только удаление обоих (`O8`) — это записано в `test-plan.md`.
- Это не защита от невозможного по контракту входа и не абстракция: барьеры — две половины одного правила из документирующего комментария компонента («перечитывать только после отказа по версии» и «подставлять только внутри цикла, открытого отказом»). Первый ещё и убирает лишний GET на каждый возврат фокуса и сети (до правки запрос был включён всегда); второй связан со сбросом `isRefused`.
- Цена — по одному операнду `&&`, кода, состояния или ветки они не добавляют. Снятие любого из них ничего не упрощает и не ускоряет, а стоит полного круга review-fix, impl-test и impl-review. Пункт «defensive code for impossible-by-contract case» не сработал.
- Повторное использование `useEditBase` (`web/src/app/useEditBase.ts`) здесь не подходит без перекройки составного `Outcome` (сотрудник, должность, учётная запись); не поднято.

## Coverage (internal reviewers only)

Manifest digest `f01385e4f0e2e98659f466b7f5b471bf9a455834c0277838d34ae96c6dc25cca`; перечисленные в манифесте файлы прочитаны по `df0bd70eb49d498c.diff`, `OnboardEmployeePage.tsx`, `PositionSelect.tsx`, `TrackPositionsPage.tsx`, `useEditBase.ts`, `orgStructureApi.ts` — контекст. Perf budget compliance: n/a, в `custom-coding-rules.md` нет бюджетов (задержка, память, пропускная способность); остальные четыре раздела производительности применены, замеров в `test-plan.md` не требовалось: перф-рискованных изменений нет (запрос теперь включается реже, формы только блокируются на время сохранения).

```json
{"manifest_digest":"f01385e4f0e2e98659f466b7f5b471bf9a455834c0277838d34ae96c6dc25cca","findings":[],"files":[{"i":".claude/agent-memory/asd-tester-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"web/src/features/desk/BuilderCompetenciesStep.test.tsx","s":"checked"},{"i":"web/src/features/desk/BuilderCompetenciesStep.tsx","s":"checked"},{"i":"web/src/features/desk/OnboardEmployeePage.test.tsx","s":"checked"},{"i":"web/src/features/desk/OnboardEmployeePage.tsx","s":"checked"},{"i":"web/src/features/desk/TrackPositionsPage.tsx","s":"checked"},{"i":"web/src/features/org-structure/EmployeeForm.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/EmployeeForm.tsx","s":"checked"},{"i":"web/src/features/org-structure/PositionSelect.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/PositionSelect.tsx","s":"checked"},{"i":"web/src/features/users/UserForm.tsx","s":"checked"},{"i":"web/src/features/users/UserModal.test.tsx","s":"checked"}],"rules":[{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"pass"},{"i":"Algorithmic complexity [impl-review]","s":"pass"},{"i":"Regression detection [impl-review]","s":"pass"},{"i":"Hot path identification [impl-review]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"reviewed"},{"i":"Algorithmic complexity [impl-review]","s":"reviewed"},{"i":"Regression detection [impl-review]","s":"reviewed"},{"i":"Hot path identification [impl-review]","s":"reviewed"}]}
```

## Verdict
APPROVE

## Next action
Ревьюер закрыт (APPROVE-latch); правок не требуется.
