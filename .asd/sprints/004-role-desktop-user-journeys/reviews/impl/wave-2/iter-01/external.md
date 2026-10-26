[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: wave-2/iter-01
- **Severity floor (this iter)**: low
- **Unreviewed files**: нет (codex отработал по всему files[]; ограничение: сам codex сообщил, что тесты и браузер не запускал, интеграционное поведение не проверено)

## Kept findings

Источник: codex (gpt-6.1-sol), id из ответа codex. Маппинг: major → high, minor → medium.

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | web/src/features/desk/OnboardEmployeePage.tsx:207 | codex C1 (major). После создания сотрудника и возврата на первый шаг ответ 412 не обновляет outcome.employee. Повторные сохранения отправляют прежний If-Match и снова отклоняются. | После 412 перечитать сотрудника и обновить базу формы и outcome; проверить повторное сохранение с новой версией. |
| 2 | high | web/src/features/desk/OrgCleanupPage.tsx:68 | codex C2 (major). Выбор другой находки или фильтра размонтирует форму смены должности либо привязки сотрудника без подтверждения. Несохранённые значения теряются. | Регистрировать правки форм через useLeaveGuard и проводить смену находки и фильтра через confirmLeave. |
| 3 | medium | web/src/features/desk/OnboardEmployeePage.tsx:324 | codex C3 (minor). Шаг создания учётной записи передаёт только isUnsaved. Во время POST линейка шагов доступна; после ухода завершившийся запрос может переключить пользователя на итог и отбросить новые правки первого шага. | Передавать состояние сохранения из CreateAccountForm и блокировать смену шага до завершения запроса. |
| 4 | medium | web/src/features/desk/BuilderCheckStep.tsx:37 | codex C4 (minor). Сохранение перехода к отправке не передаётся в onEditsChange. Пока запрос выполняется, можно открыть предыдущий шаг и начать правки; поздний onSaved переключит на отправку без подтверждения потери правок. | Передавать goOn.isPending в состояние мастера; аналогично зарегистрировать выполняющийся переход на шаге отправки. |
| 5 | medium | web/src/features/desk/BuilderSubmitStep.tsx:85 | codex C5 (minor). Ошибка первого запроса полноты или предпросмотра публикации оставляет только Skeleton. Ни ошибки, ни повтора; кнопка «Назад» тоже исчезает. | Обработать ошибки обоих запросов через LoadFailure с повтором до ветки загрузки. |
| 6 | medium | web/src/features/desk/TrackPositionsPage.tsx:59 | codex C6 (minor). Загружается только первая страница треков, поиск Select фильтрует её локально. Редактируемый трек со следующей страницы нельзя найти и выбрать; фильтрация canEdit дополнительно сокращает первую страницу. | Добавить серверный поиск треков или загрузку следующих страниц. |
| 7 | medium | web/src/features/desk/BuilderCompetenciesStep.tsx:272 | codex C7 (minor). После снятия сохранённой компетенции создание блока с ней разрешено формой, но подключение отклоняется: held включает компетенции прежней сохранённой версии. Блок остаётся неподключённым. | Проверять пересечения по текущим chosen и составам оставленных блоков; добавить проверку замены отдельной компетенции блоком. |
| 8 | medium | web/src/features/desk/LinkButton.tsx:18 | codex C8 (minor). Безусловный preventDefault перехватывает Ctrl/Cmd+click и Shift+click: вместо открытия в другой вкладке или окне меняется текущая страница. | Перехватывать только обычный левый щелчок без клавиш-модификаторов. |
| 9 | medium | web/src/features/desk/scenario.css:26 | codex C9 (minor). background: transparent у .queue-row перекрывает фон .queue-row-selected при одинаковой специфичности; выбранная строка очереди не выделяется. | Разместить правило выбранной строки после базового либо использовать селектор .queue-row.queue-row-selected. |

## Dropped findings (counts only)

- Below severity floor (iter 1, floor low): 0
- Nitpick, by category: none: 0

## Verdict
CONCERNS: 9

## Next action
Orchestrator: route 9 findings (2 high, 7 medium) to review-fix; в следующую итерацию включить изменённые файлы. Стейлмейта нет (итерация 1).
