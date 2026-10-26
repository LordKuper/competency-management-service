[REVIEW-design-external]: FAIL

# External Review Report

- **Phase**: design-review
- **Iteration**: 3
- **Severity floor (this iter)**: high
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | .asd/sprints/004-role-desktop-user-journeys/design/adr.html: ADR-4 «Совпадение показанного и выполненного» (codex: major) | `cascadeTag` учитывает только версии корня и сущностей каскада. `publishingActions` и ответственный HR в нём не учтены. Сценарий: после предпросмотра непубликующего approve шага 1 администратор назначает ответственным HR автора матрицы. Тогда согласование публикует версию автоматически, хотя перечень пользователь не подтверждал, а тег изменения маршрута не обнаружит. Нарушаются AC-34.5 и AC-34.7. | Включить результат маршрутизации и ответственного HR в снимок предпросмотра. SPA передаёт его при всех переходах версии, включая непубликующие. Если результат изменился под блокировками, вернуть 412 с повторным предпросмотром и подтверждением. |

## Dropped findings (counts only)

- Below severity floor (iter 3, floor high): 0 (codex reported none)
- Nitpick, by category: none

## Stalemate

Стагнации нет. Прошлые находки iter-02: adr.html:648 (high), design-md-delta.yaml:23 (medium) и adr.html:356 (medium). В текущем прогоне они не повторены. Новая находка относится к другому дефекту в ADR-4.

## Verdict
FAIL: 1

## Next action
Автор ADR должен расширить снимок предпросмотра в ADR-4 (маршрут и ответственный HR), согласовать с этим AC-34.5 и AC-34.7 в prd.html и ux-spec.html, затем запустить iter-04.
