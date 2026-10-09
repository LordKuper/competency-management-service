[REVIEW-impl-combined]: APPROVE

# Review — combined

- **Phase**: impl-review
- **Iteration**: wave-2/iter-03 (порог high)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

Находок нет.

## Coverage (internal reviewers only)

```json
{"manifest_digest":"2ca8dbab1f5a11da946b5e09769c06c0e9675b807b9c6b620319bed89640e74e","findings":[],"files":[{"i":".claude/agent-memory/asd-reviewer-combined/feedback_wave-review-probes.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"pass"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"pass"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Algorithmic complexity [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Regression detection [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Hot path identification [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"SSoT","s":"pass"},{"i":"Template adherence","s":"n/a","p":"no templated artefact in scope"},{"i":"HTML shell wrapping","s":"n/a","p":"no HTML file in scope"},{"i":"Provenance","s":"n/a","p":"no HTML file in scope"},{"i":"Traceability","s":"n/a","p":"no HTML file in scope"},{"i":"Persistent actuality (impl-review)","s":"pass"},{"i":"In-code doc comments (impl-review, `code-style.md` §7)","s":"pass"},{"i":"Stub-resolution verification (impl-review)","s":"pass"},{"i":"Framework mode (`self_hosting: enabled`, impl-review only)","s":"n/a","p":"self_hosting not enabled"},{"i":"Documentation economy","s":"pass"},{"i":"Custom rules consistency","s":"pass"},{"i":"Overall quality","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"n/a","p":"no UI surface in scope"},{"i":"Over-engineering checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Structure / cohesion checklist [design-review, impl-review] — critical, undroppable","s":"reviewed"},{"i":"Complexity-vs-value tradeoff [design-review, impl-review]","s":"reviewed"},{"i":"Perf budget compliance [impl-review]","s":"n/a","p":"no budgets defined"},{"i":"Perf anti-patterns [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Algorithmic complexity [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Regression detection [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Hot path identification [impl-review]","s":"n/a","p":"no perf budgets section and no executable file in scope"},{"i":"Overall quality","s":"reviewed"}]}
```

Заметки по проверке (без находок):
- Combined F1 из iter-02 закрыт. Строка 12 в `.claude/agent-memory/asd-tester-critical/project_competency-test-harness.md` теперь различает два вида гонок, как и предлагало исправление. Старт за gate через `TaskCompletionSource` допускается только для гонки, которая не охраняет инвариант на несколько строк, и итог должен быть ровно {200, 409}. Гонка такого инварианта (последний администратор, одна учётная запись на сотрудника, счётчик неверных паролей, каскад увольнения) строится на удерживаемой блокировке или паузе в триггере с ожиданием через `pg_stat_activity`, и у проигравшего один известный статус. Допуск {200, 401} убран для фиксированного порядка. Строка согласуется со строками 18, 20, 34 и 40 (проигравший в гонке последнего администратора получает 409, а не 401) и с `custom-coding-rules.md` «Multi-row invariant locking».
- Ссылка на `OrgTreeTests` актуальна. В `tests/Competency.Tests/OrgTreeTests.cs:52,74` есть gate `TaskCompletionSource(RunContinuationsAsynchronously)` и раунды. Строка 58 проверяет ровно {OK, Conflict}. Тест на три перемещения (строки 84-85) проверяет 2×200 и 1×409, то есть набор тот же.
- Ниже порога, не находка: в строке 34 гонка сброса пароля против смены своего пароля названа настоящей гонкой («exactly one winner, not which»). Это не противоречит строке 12: смена сохраняется вне блокировки, поэтому порядок не фиксирован по итогу, а запрет из строки 12 касается только фиксированного порядка.
- Новый пункт в `.claude/agent-memory/asd-reviewer-combined/feedback_wave-review-probes.md:23-24` и обновлённое `description` точны: пункт описывает приём, который нашёл F1 в iter-02. Устаревших имён нет. Строка индекса `MEMORY.md` новый пункт не называет, но она не входит в scope и укладывается в лимит, так что это не дефект.
- Ручная проверка: единственная строка с fail по-прежнему `test-plan.md:86` (AC-18). Разбор из iter-01 и iter-02 в силе: срок приглашения закрыт Task 9, frontend относится к wave 3. В `test-plan.entry-07.md:30,33` слово «fail» стоит в колонке доказательства регрессии, это не ручные строки.
- Исполняемых файлов в диффе нет. Сборку и тесты я не перезапускал. Зелёная проверка завершения review-fix (build 0/0, lint 0) взята из `decisions-log.md:128`.

## Verdict
APPROVE

## Next action
Combined в wave-2 одобряет (external APPROVE уже зафиксирован в iter-02). Дальше идёт маршрутизация по ростеру wave 2. Эскалаций нет.

## Escalations
Нет.
