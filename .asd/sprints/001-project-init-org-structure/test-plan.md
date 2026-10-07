---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 001-project-init-org-structure

## Entry log

Appended each entry, never rewritten. `HEAD analysed` is the commit the strategy/prune passes
were scoped through; the next re-entry's delta is `git diff <this sha>...HEAD`.

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 82776210245b389c80e76088fc1cffa74a0f0579 | full change surface |
| 2 | | delta since entry 1 (82776210245b389c80e76088fc1cffa74a0f0579) |

## Risk → check decisions

Entry 2 — дельта `82776210245b389c80e76088fc1cffa74a0f0579...HEAD` без `.asd/**` и `docs/**`: review-fix wave-1/iter-01 (6311570, 4d8f49c, 668e2cc, 0c82ff8, правки комментариев, README, `Dockerfile` и `configmap.yaml`). Строки entry 1 перенесены в `test-plan.entry-01.md`; заменяющих строк нет: ни один фикс не менял поведение, под которое те строки написаны. Pre-strategy run (`commands.yaml` `test`, полный набор: дельта правит `Competency.Platform` и `Dockerfile`, предохранитель «Impacted test set»): backend 82/82 (exit 0), web 23/23 (exit 0) на HEAD 4d86b07 — регрессии от удаления claim `employee_id` и правок миграций нет. Окружение прежнее: реальный процесс API и один контейнер PostgreSQL 18.6 на прогон, Playwright исключён решением пользователя, новых зависимостей нет.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| Миграция `AuditTriggersEnableAlways` (AC-15) | триггеры неизменяемости журнала глушатся `SET LOCAL session_replication_role = replica` под учётной записью приложения (суперпользователь): UPDATE, DELETE и TRUNCATE `audit_events` проходят без DDL — принятый FAIL внешнего ревью wave-1/iter-01 | integration + прямой SQL | add | `PlatformTests.Ac15_Journal_RejectsUpdateDeleteAndTruncate_EvenWhenOrdinaryTriggersAreSilencedByTheReplicaRole`: транзакция, `SET LOCAL session_replication_role = replica`, затем один из трёх операторов; ожидается `23001`. Транзакция откатывается при освобождении соединения, поэтому при регрессии журнал общего хоста не страдает. Существующий `Ac15_Journal_RejectsUpdateDeleteAndTruncateInTheDatabase` (обычный режим) остаётся и без миграции зелёный: этот пробел замыкает только новый тест. |
| Миграция `UserRoleCheck`, `ck_users_role` в `UserManagementEntityConfiguration` (AC-7) | в `users.role` попадает значение вне enum (прямой SQL, ошибка кода); слишком узкое ограничение ломает старт на пустой БД | integration + прямой SQL | add | `PlatformTests.Ac7_UserRole_IsLimitedToTheKnownValuesInTheDatabaseItself`: `UPDATE users SET role = 'Auditor'` даёт `23514` с `ConstraintName = ck_users_role`, роль учётной записи через API не изменилась. Применение всех миграций на пустой БД и приём обеих допустимых ролей уже проверяют `Ac3_EmptyDatabase_…` и тесты с GlobalAdmin (слишком узкое ограничение рушит старт каждого хоста — доказательство ниже), новых строк для них нет. |
| Отказ `Up` миграции `UserRoleCheck`, пока в `users` есть неизвестная роль | деплой на БД с чужим значением | — | none | Утверждение — гарантия PostgreSQL для `ADD CONSTRAINT` (существующие строки проверяются, при отказе ничего не меняется), собственной логики в миграции нет: тест проверял бы PostgreSQL. Проверяемо (снять ограничение, вставить строку, стереть запись истории миграции, перезапустить хост), но не стоит строки: отказ старта оператор видит сразу. |
| `ProblemExceptionHandler` (AC-5) | запись 5xx без стека (причину в проде не найти) или со стеком у не-5xx; в запись попадает `Message` исключения (ПДн, значения БД) | integration (реальный хост, JSON-логи) | add | `PlatformTests.Ac5_ServerErrorLog_CarriesTheStackTraceButNotTheExceptionMessage_AndOtherFailuresCarryNoStack`: хост на отдельной БД; 412 от устаревшей версии и 500 от check-ограничения, добавленного прямым SQL (его имя — уникальный маркер и входит в `Message` исключения). Запись 500 содержит `State.StackTrace` с кадрами и не содержит маркер; запись 412 — `StackTrace` null. Записи ищутся по `State.Status`, а не по тексту сообщения. |
| Чтение журнала хоста в тестах (`ApiHost.Logs`) | запись доходит до вывода позже ответа на запрос, который её породил: тест, читающий журнал сразу после ответа, мигает | integration | add | Во время entry пойман дефект теста: `Ac5_ServerErrorLog_…` один раз красный («no such item was found» для записи 500) при том, что хост залогировал её. `ApiHost.LogsAfterAsync(fragment)` ждёт появления фрагмента с таймаутом 10 с (условное ожидание, как `WaitUntilReadyAsync`); им же читает журнал существующий `Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesOrSqlValues` (его проверки не менялись, прежнее чтение имело ту же гонку). |
| Claim `employee_id`, `ICurrentActor.EmployeeId`, `PlatformClaims.EmployeeId` (удалены, AC-8) | регрессия чтения привязки или входа | — | keep | Читателей не было (ревью F1, `grep` по `src` подтверждает: `employee_id` остался только столбцом БД). Claim лежит в зашифрованной cookie и снаружи не наблюдается; `employeeId` и ФИО в `/auth/me` читаются из БД и покрыты `UserBindingTests.Ac8_AccountOfAnyRole_CanBeBoundToAWorkingEmployee`, `SessionTests` и `PlatformTests.Ac3_EmptyDatabase_…` — зелёные в pre-strategy run и в suite gate. Тест «claim отсутствует» проверял бы реализацию, а не поведение. |
| Doc-комментарии (`AuditSaveChangesInterceptor`, `PlatformModule`, миграции `EmailOnUserAccount`, `EmployeeNameInThreeFields`), README, комментарии `Dockerfile` и `configmap.yaml` (AC-4, AC-16) | — | — | none | Поведения нет: только комментарии и текст. Статически их закрывают сборка с `TreatWarningsAsErrors` (0 предупреждений) и `custom.check-api` (exit 0). |

## Removed tests

Удалений нет: ни один тест не потерял риск в дельте. Дельта изменила только `Ac5_Logs_AreJsonAndCarryNoPasswordsEmailsNamesOrSqlValues` (способ чтения журнала, см. выше), проверки те же.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Мутации выполнялись в копии `git archive HEAD` вне репозитория (production-код рабочего дерева не менялся), тесты — как в commit entry 2. Запуск: `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-class Competency.Tests.PlatformTests` (в копии нет `web/node_modules`, поэтому не через `.slnx`). Имя красного теста и первая строка утверждения — из вывода раннера, в порядке его печати.

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/PlatformTests.cs`: `Ac15_Journal_RejectsUpdateDeleteAndTruncate_EvenWhenOrdinaryTriggersAreSilencedByTheReplicaRole` (3 строки теории) | mutation: удалены файлы миграции `AuditTriggersEnableAlways` (состояние до 668e2cc) → exit 2, красные 3 из 13, первым `…(statement: "UPDATE audit_events SET reason = 'edited'")`: «Expected a <Npgsql.PostgresException> to be thrown because the application account may switch to the replica role, which silences every trigger not enabled always, but no exception was thrown.» (то же у строк TRUNCATE и DELETE); старый `Ac15_Journal_RejectsUpdateDeleteAndTruncateInTheDatabase` на этой мутации зелёный; runs: 1. mutation: в `Up` обычным оставлен только `audit_events_reject_truncate` → exit 2, красна одна строка `(statement: "TRUNCATE audit_events")`, то же сообщение; runs: 1. mutation: обычным оставлен только `audit_events_reject_update_delete` → exit 2, красны строки UPDATE и DELETE; runs: 1. Каждая строка теории доказана отдельно. |
| `tests/Competency.Tests/PlatformTests.cs`: `Ac7_UserRole_IsLimitedToTheKnownValuesInTheDatabaseItself` | mutation: удалены файлы миграции `UserRoleCheck` (состояние до 0c82ff8) → exit 2, красный 1 из 13: «Expected a <Npgsql.PostgresException> to be thrown, but no exception was thrown.»; runs: 1. Контроль «слишком узко»: `role IN ('User')` в миграции → exit 2, красны 12 из 13, первым существующий `Ac3_EmptyDatabase_GetsSchemaExtensionsAndBootstrapAdministrator`: «System.InvalidOperationException : The API host exited with code -532462766 before it was ready.» (хост не создаёт bootstrap-администратора GlobalAdmin) — новая строка для применения миграций на пустой БД не нужна; runs: 1. |
| `tests/Competency.Tests/PlatformTests.cs`: `Ac5_ServerErrorLog_CarriesTheStackTraceButNotTheExceptionMessage_AndOtherFailuresCarryNoStack` | mutation: `ProblemExceptionHandler.cs` из `4d8f49c~1` (состояние до фикса) → exit 2, красный 1 из 13: «Expected stack[0] <null> to contain " at " because a server error is logged with the place it happened.»; runs: 1. mutation: стек пишется для любого статуса (`exception.StackTrace` без `isServerError ?`) → exit 2: «Expected clientError.State["StackTrace"] to be <null> because only a server error is logged with its stack trace, but found    at Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlModificationCommandBatch.ThrowAggregateUpdateConcurrencyExceptionAsync(…)»; runs: 1. mutation: для 5xx пишется `exception.ToString()` (стек плюс `Message`) → exit 2: «Did not expect serverError.Line "{"Timestamp":…» to contain "ck_unit_name_marker_<guid>" because the exception message names the violated constraint and is never logged.»; runs: 1. Reword-контроль: «Request failed with status {Status}» → «Unhandled failure, status {Status}» → exit 0, 13/13: тест не привязан к тексту сообщения; runs: 1. Тест на мигание: до `LogsAfterAsync` одна красная строка за 5 прогонов класса («Expected var serverError = records to contain a single item matching … status 500, but no such item was found»), после — 16 прогонов класса подряд (два параллельных цикла по 8) 13/13 без сбоев, и три полных прогона gate 87/87. |
| `tests/Competency.Tests/Infrastructure/ApiHost.cs` (`LogsAfterAsync`) | n/a — вспомогательный код, проверяется тестами выше; устраняет гонку «запись журнала позже ответа» (доказательство в строке выше). |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx && npm --prefix web test` (`test` из `commands.yaml`, без изменений)
- Scope: full — дельта правит модуль `Competency.Platform` (общий для всех модулей), `Dockerfile` и `deploy/**`, предохранитель «Impacted test set» переводит набор в полный; pre-strategy run до правки тестов: backend 82/82, web 23/23 на HEAD 4d86b07
- Result: pass — backend (xunit.v3, Microsoft Testing Platform): итог 87, успешно 87, сбой 0, пропущено 0, время тестов 15,5 с, exit 0 (82 прежних + 5 новых строк `PlatformTests`); web (vitest): 5 файлов, 23 теста пройдено, 0 сбоев, 2,56 с, exit 0; e2e: не выполнялся (Playwright исключён решением пользователя). Ещё два прогона backend на том же дереве: 87/87, 87/87 (16,8 с, 16,8 с)
- Lint / build: pass — `npm --prefix web run lint` exit 0; `dotnet build Competency.slnx --tl:off && npm --prefix web run build` exit 0, 0 предупреждений, 0 ошибок; `npm --prefix web run check:api` exit 0
- Wall-clock: `test` 22 с (backend 19 с, web 3 с), `lint` 4 с, `build` 6 с — 32 с вместе; контейнер PostgreSQL один на прогон, хосты API — по одному на группу тестов
- HEAD: ebd51a8957c878b2dc7a5e6f2be1844d60bb80c5 — прогон выполнен на дереве, уже включающем тесты этого entry; commit с `test-plan.md` код не меняет

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

Дефектов продукта в entry 1 и entry 2 не найдено: новые тесты прошли на HEAD без правок production-кода. Единственная красная строка entry 2 (`Ac5_ServerErrorLog_…`, запись журнала доходила до вывода позже ответа) — дефект теста, исправлен внутри entry (`ApiHost.LogsAfterAsync`).

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
