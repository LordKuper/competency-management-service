# Test plan — sprint 005-org-structure-excel-import, entry 2

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `PositionSelect`: эффект сброса учитывает, под каким подразделением выбрана должность (77cce66, D-1) | Простая смена подразделения перестала бы очищать должность, которой в новом подразделении нет | component: `EmployeeForm.test.tsx` «drops the old position once another unit is chosen …», «drops a position added in one unit when the form goes back to another» | keep (новых нет) | Тесты уже есть и проходят на исправленном коде. Мутация «очистка отключена» (`isChosenUnderOtherUnit` → `false`): `npx vitest run src/features/org-structure/EmployeeForm.test.tsx` → exit 1, оба теста; runs: 1; мутация возвращена |
| То же: смена подразделения и должности одним действием («Сделать основным») | Должность стирается до того, как форма сообщила и новое подразделение | component: `EmployeeAssignments.test.tsx` «swaps a secondary assignment …» | keep | Регрессия D-1 с доказательством fail-first на входе 1 (exit 1 на прежнем коде); на коде 77cce66 проходит |
| То же: должность, заданная при первой отрисовке и отсутствующая в перечне подразделения, больше не очищается (принятый побочный эффект) | Устаревшая должность попадёт в сохранение | не автоматизируется | none | Принятое разработчиком поведение: тест закрепил бы побочный эффект, а не требование. Правильность решает сервер (отказ при сохранении); для текущей должности сотрудника поведение закреплено `current` («keeps listing the position the employee holds …», `PositionSelect.test.tsx`), смена подразделения очищает — тесты выше |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | Нечего удалять: изменён один компонент, дублей и устаревших проверок нет | yes |

## Added tests

| Test | Regression proof |
|---|---|
| — | Новых тестов нет (см. решения выше) |
