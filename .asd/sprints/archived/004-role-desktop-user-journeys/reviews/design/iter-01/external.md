[REVIEW-design-external]: CONCERNS

# External Review Report

- **Phase**: design-review
- **Iteration**: 1
- **Severity floor (this iter)**: low
- **Unreviewed files**: none

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | .asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html:m-review | В тексте указано «шаг 1 ничего не публикует». ADR-4 при этом допускает публикацию при согласовании шага 1 с последующим автосогласованием HR. Этот путь остаётся без перечня и подтверждения публикации, что нарушает AC-34.7. (codex: major) | Определять публикующее действие по publishingActions сервера и показывать перечень и подтверждение также на шаге 1. |
| 2 | high | .asd/sprints/004-role-desktop-user-journeys/design/adr.html:adr-3-decision | DELETE запрещает снятие последней действующей должности, только если действуют и трек, и уровень. AC-25.2 требует запрета для уровня действующего трека независимо от статуса уровня. Черновой уровень такого трека выпадает из защиты. (codex: major) | Убрать дополнительное условие действующего статуса уровня и согласовать вычисление canUnlink с AC-25.2. |
| 3 | high | .asd/sprints/004-role-desktop-user-journeys/design/adr.html:adr-4-decision | В графе каскада уровни включены только через версию матрицы, а у трека указана лишь ссылка на направление. При самостоятельном утверждении трека его черновые уровни, публикация которых сохранена в Context, отсутствуют в замыкании, предпросмотре и cascadeTag. (codex: major) | Добавить связь «трек → его черновые уровни». Включить эти уровни в единый перечень, проверки, тег и исполнение при публикации трека. |
| 4 | medium | .asd/sprints/004-role-desktop-user-journeys/design/ux-spec.html:m-drafts | «Сохранить и утвердить» сохраняет изменённые поля и сразу утверждает запись. Правка категории, направления или шкалы может изменить каскад, и прежний cascadeTag устаревает из-за новой версии корня. Актуальный перечень перед утверждением не показывается. (codex: minor) | После сохранения получать новый предпросмотр, показывать актуальный каскад перед подтверждением и отправлять соответствующий cascadeTag. |
| 5 | medium | .asd/sprints/004-role-desktop-user-journeys/design/prd.html:AC-30.4 | Запрет «справочная находка не поглощает справочную» противоречит таблице 1. Справочная AC-27.3 поглощает справочную AC-5.3, и для подразделения HR без руководителя и матрицы требования задают разные результаты. (codex: minor) | Явно установить приоритет исключений таблицы 1 над общим правилом AC-30.4 и согласовать формулировки. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 5

## Next action
Creator исправляет findings 1–5 в prd.html, ux-spec.html и adr.html. Затем оркестратор запускает design-review iteration 2 с полным набором ревьюеров.
