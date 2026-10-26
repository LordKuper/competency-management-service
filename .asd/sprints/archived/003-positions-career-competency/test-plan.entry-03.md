# Test plan — sprint 003-positions-career-competency — entry 3

Сегмент записи 3 (`Entry log`: дельта после записи 2), вынесен при входе 4 по `artifact-layout.md` «Test plan»: строки повествования перенесены без правок, `Entry log`, `Suite run`, `Defects` и `Manual verification` остались в `test-plan.md`. Сегмент не правится: изменившийся риск получает замещающую строку в `test-plan.md`.

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Дельта после записи 2 (`779cf49`: только doc-комментарии в `EmployeeEndpoints.cs`, `EmployeeRequest.cs`, `OrgUnitPositionEndpoints.cs`; `c52a603`: удалена неиспользуемая константа `BeforeCareer` в `tests/Competency.Tests/Infrastructure/MigrationDatabase.cs`) | поведение не менялось: комментарии не компилируются в логику, константа нигде не читалась (иначе сборка тестов упала бы) | none | none | Нет изменения наблюдаемого поведения, проверять нечего (`code-style.md` §17). Правка затрагивает общую тестовую инфраструктуру, поэтому набор взят целиком (предохранитель «Impacted test set»); сборка и все тесты зелёные |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| — | в дельте ни один тест не удалён | yes |

## Added tests

В дельте тесты не добавлены. Объём: backend 473 → 473, web 303 → 303.

| Test | Regression proof |
|---|---|
| — | — |
