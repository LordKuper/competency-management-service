[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-3/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | web/src/features/users/useSendUserMail.ts:65 | Повторная отправка доступна, пока предыдущий запрос не завершён: Codex воспроизвёл два параллельных POST для одной строки; индикатора загрузки нет; повтор уходит со старым If-Match и получает 412 одновременно с успешной отправкой первого письма. Хук возвращает `(user) => send.mutate(user)` без защиты от повторного вызова и без состояния pending для строки. (codex F1: minor) | Блокировать повторный запуск для строки и показывать загрузку до завершения запроса; тест с задержанным ответом. |

Проверки Codex: lint, typecheck и check:api прошли; Vitest не выполнен (EPERM в песочнице только для чтения) — не находка.

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none

## Verdict
CONCERNS: 1

## Next action
Создатель исправляет находку 1: защита от параллельной повторной отправки по строке, индикатор загрузки и тест на задержанный ответ; затем следующая итерация волны 3.
