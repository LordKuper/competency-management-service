[REVIEW-impl-correctness]: APPROVE

# Review — correctness

- **Phase**: impl-review
- **Iteration**: wave-3/iter-03

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|

## Coverage (internal reviewers only)

Область: 12 файлов манифеста, диф `197345bc8ac8378a.diff`; изменённые компоненты прочитаны целиком вместе с `web/src/app/useEditBase.ts` и `web/src/app/useFormSubmit.ts`.

- `CompetencyModal.tsx`: фокус на первое отклонённое поле перенесён в эффект и ставится только при `!isSubmitting`. `firstRejectedField` сбрасывает состояние при ошибке без полей, после фокуса поле очищается, так что повторный отказ по тому же полю снова ставит фокус. При 412 форма пересоздаётся по `key`, и состояние фокуса пропадает без вреда: в ответе 412 полей нет.
- `TrackModal.tsx`: эффект `setFieldValue("name", ...)` срабатывает только при смене `current`. Это бывает при `setSaved` во время сохранения (форма в это время заблокирована), при перечитывании в ветке 412 и при смене `base` после отказа (`useEditBase`). Фоновое перечитывание кэша `base` не меняет, поэтому введённое имя не затирается. Поведение совпадает с правилом custom-coding-rules «Mutating UI flows»: после 412 запись перечитывается, показывается пользователю, отправляется новая версия.
- `UnitHrModal.tsx`: `Form disabled={isSubmitting}` блокирует Select и кнопку «Закрепить», которая берёт блокировку из DisabledContext. Тест это подтверждает.
- Тесты держат ответ через `deferred()` и проверяют блокировку до ответа. Имя при перечитывании отличается, а отсутствие PUT проверяется. Утверждения соответствуют коду.
- Файлы памяти: записи о методе, без номеров Task и спринта, коду не противоречат.
- UI: сырых значений нет. Управление фокусом после отказа соответствует правилам доступности.
- Ниже порога (medium, в таблицу не внесено): кнопка «Снять закрепление» вне формы остаётся доступной, пока закрепление ещё сохраняется (`UnitHrModal.tsx:145`). Оркестратор это уже зафиксировал (decisions-log, 2026-10-26).

```json
{"manifest_digest":"30b7bdf423150d2de679febadafdbf2f0c999aabe81992a239f8c5d1ca0328fa","findings":[],"files":[{"i":".claude/agent-memory/asd-dev-critical/project_ef-left-join-count-and-visibility-facts.md","s":"checked"},{"i":".claude/agent-memory/asd-dev-critical/project_git-commit-only-option-order.md","s":"checked"},{"i":".claude/agent-memory/asd-dev-critical/project_web-focus-ring-and-antd-css-facts.md","s":"checked"},{"i":".claude/agent-memory/asd-reviewer-correctness/feedback_no-prior-iteration-reports.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/MEMORY.md","s":"checked"},{"i":".claude/agent-memory/asd-tester-critical/project_competency-test-harness.md","s":"checked"},{"i":"web/src/features/career/CompetencyModal.test.tsx","s":"checked"},{"i":"web/src/features/career/CompetencyModal.tsx","s":"checked"},{"i":"web/src/features/career/TrackModal.test.tsx","s":"checked"},{"i":"web/src/features/career/TrackModal.tsx","s":"checked"},{"i":"web/src/features/org-structure/UnitHrModal.test.tsx","s":"checked"},{"i":"web/src/features/org-structure/UnitHrModal.tsx","s":"checked"}],"rules":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"pass"},{"i":"Security [impl-review]","s":"pass"},{"i":"Contracts [impl-review]","s":"pass"},{"i":"Best practices [impl-review]","s":"pass"},{"i":"AC coverage trace [impl-review]","s":"pass"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"pass"},{"i":".asd/project/custom-common-rules.md","s":"pass"},{"i":".asd/project/custom-coding-rules.md","s":"pass"}],"sections":[{"i":"Draft correctness [design-review]","s":"n/a","p":"outside phase gate"},{"i":"Bugs [impl-review]","s":"reviewed"},{"i":"Security [impl-review]","s":"reviewed"},{"i":"Contracts [impl-review]","s":"reviewed"},{"i":"Best practices [impl-review]","s":"reviewed"},{"i":"AC coverage trace [impl-review]","s":"reviewed"},{"i":"UI conformance [design-review — `n/a: outside phase gate` without a ux-spec/design-system draft; impl-review — conditional on a UI surface in scope]","s":"reviewed"}]}
```

## Verdict
APPROVE

## Next action
Ревьюер correctness для wave-3 завершён (APPROVE-latch).
