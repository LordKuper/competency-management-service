[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-3/iter-02
- **Severity floor (this iter)**: medium
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | web/src/features/career/MatrixEditor.tsx:156 | codex F3. Поля редактора остаются доступны во время сохранения. Если отправить порог 60 и ввести 70, то после ответа редактор пересоздаётся и значение 70 теряется без предупреждения. Codex воспроизвёл это в jsdom. | Блокировать изменение содержимого, пока `save.isPending`. Проверить сценарий с задержанным ответом. |
| 2 | high | web/src/features/career/TrackModal.tsx:54 | codex F1. Новый трек создаётся с `version=0`. Условие `saved.version &gt; (base?.version ?? 0)` не выбирает `saved`, поэтому `current` остаётся без трека. Если после этого сохранение уровня отклонено, повторное сохранение снова шлёт POST трека и получает конфликт имени. Codex воспроизвёл это в jsdom. | Выбирать `saved` при отсутствии `base`, независимо от версии. Добавить проверку повторного сохранения при `version=0`. |
| 3 | high | web/src/features/career/MatrixEditor.tsx:172 | codex F2. `trackOrder` ставит текущий порядок справочника перед `entries.levels` из снимка. После перестановки уровней трека опубликованная или архивная матрица показывается в новом порядке, вопреки AC-17. | Для Published и Archived брать порядок `entries.levels` из снимка. Добавить проверку после перестановки справочника. |

Codex оценил F1 и F2 как major, а F3 как critical. Номера строк F1 и F3 сверены по коду (TrackModal.tsx:54, MatrixEditor.tsx:172 и :156). Код F3 в этой точке совпадает с описанием.

## Dropped findings (counts only)

- Below severity floor (iter 2, floor medium): 0
- Nitpick, by category: none

Статус предыдущих 9 находок (P1–P9): codex пометил все как resolved. Carry-over нет, стопор не наступил.

## Verdict
FAIL: 3

## Next action
Исправить F3 (блокировка правок во время сохранения), F1 (выбор `current` при `version=0`) и F2 (порядок уровней из снимка для Published и Archived). Затем запустить iter 3 с порогом high+.

Ограничения прогона: backend-сборку и штатные Vitest-наборы codex не запускал, потому что read-only sandbox это блокирует. Три дефекта он воспроизвёл отдельными jsdom-пробами. Проверка типов TypeScript прошла.

## User decision

FAIL 1–3 приняты к исправлению без изменения понятий и контрактов (решение пользователя, 2026-10-12): 1 — блокировать правку содержимого на время сохранения; 2 — после создания трека повторное сохранение не создаёт трек заново; 3 — опубликованная и архивная матрица показывает порядок уровней из снимка.
