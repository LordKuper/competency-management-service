[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-3/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | web/src/features/career/CatalogModal.tsx:88 | codex finding 1 (major). Кнопка «Отмена» закрывает изменённую форму сразу, без подтверждения потери правок. Codex воспроизвёл это на NameModal. | Вызывать onCancel через confirmLeave единого leave guard. Добавить тест отмены изменённой формы. |
| 2 | high | web/src/features/career/NameModal.tsx:78 | codex finding 2 (major). Форма остаётся редактируемой во время сохранения; дополнительный ввод теряется после успеха. Та же проблема в остальных формах справочников. | Блокировать поля и изменение списков, пока isSubmitting. Закрепить проверкой с отложенным ответом. |
| 3 | medium | web/src/features/org-structure/UnitHrModal.tsx:225 | codex finding 3 (minor). Ошибка назначения HR без errors проглатывается; при 500 окно остаётся открытым без алерта. | Показывать ошибки без полевых сообщений через describeApiError. Добавить тест на ответ 500. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 3

## Next action
Route the 3 findings to review-fix.

resolved: 1 — user decision 2026-10-22 (explicit «Отмена» closes the dialog at once; docs/architecture/stack.html «Несохранённые правки в SPA», decisions-log impl assessment), 2026-10-26
