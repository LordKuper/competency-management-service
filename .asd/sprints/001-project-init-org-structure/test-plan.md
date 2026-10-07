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
| 2 | 133c58c37107b0d4528f9fee2ee765d1e88e5e01 | delta since entry 1 (82776210245b389c80e76088fc1cffa74a0f0579) |
| 3 | 3ae1630d0671e0ae021b7308610c13fb5b367f8d | delta since entry 2 (133c58c37107b0d4528f9fee2ee765d1e88e5e01) |
| 4 | 08d21ccc83fa5cacc398b0fb94e75540dd18b50a | delta since entry 3 (3ae1630d0671e0ae021b7308610c13fb5b367f8d) |
| 5 | a460cdd91cb096209e06ba868f90574e1b8633e7 | delta since entry 4 (08d21ccc83fa5cacc398b0fb94e75540dd18b50a) |
| 6 | 7366455d6e0d2f2001ee1808e5dcae3d5c36021d | delta since entry 5 (a460cdd91cb096209e06ba868f90574e1b8633e7) |

## Risk → check decisions

Entry 6 — дельта `a460cdd91cb096209e06ba868f90574e1b8633e7...HEAD` без `.asd/**`, `docs/**` и `.claude/**`: review-fix wave-2/iter-03, dev 69b73b0 (единственный файл `src/Competency.UserManagement/AuthEndpoints.cs`; тестов не менял и строк в план не добавлял, поэтому review-fix removal rows для переноса нет). Строки entry 5 перенесены в `test-plan.entry-05.md`; заменяющих строк нет: фикс не меняет поведения, под которое написаны строки entry 5 (событие `Auth.LockedOut` на смене пароля и на входе, последовательные попытки). Pre-strategy run (`commands.yaml` `test`, backend, чистая копия `git archive HEAD`, 00db355, без тестов этого entry): 97/97, exit 0 — регрессий от фикса нет. Окружение прежнее: реальный процесс API и один контейнер PostgreSQL на прогон, Playwright исключён решением пользователя, новых зависимостей нет.

Гонка детерминирована: отдельное соединение Npgsql без пула держит `SELECT id FROM users WHERE id = X FOR UPDATE` в открытой транзакции, N неверных паролей отправляются одновременно, тест без пауз ждёт, пока N бэкендов базы хоста окажутся в `pg_stat_activity.wait_event_type = 'Lock'`, и фиксирует транзакцию. Ожидание достигается и до исправления (запросы тогда стоят на `UPDATE` той же строки), поэтому тесты краснеют на утверждениях, а не на таймауте. Сценарии идут на хосте класса (`SignInRaceHost`, в его базе работают только эти тесты), по одному, поэтому чужих ожиданий блокировок в базе нет.

Обратный патч и мутации выполнялись в копиях `git archive HEAD` вне репозитория (production-код рабочего дерева не менялся); `core.autocrlf=true` даёт в копии CRLF, якоря мутаций с CRLF. Команда: `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-class <класс>`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AuthEndpoints.VerifyPasswordAsync` → `CountAttemptAsync`: проверка пароля и счёт попыток в короткой транзакции под `SELECT … FOR UPDATE` строки учётной записи с перечитыванием (69b73b0; внешнее #1 wave-2/iter-03; AC-6, AC-15) | одновременные неверные пароли одной учётной записи считаются по устаревшей копии: проигравшее сохранение падает на токене конкурентности, его результат игнорируется, попытка теряется (предел блокировки обходится); два запроса на пределе оба сообщают о начале блокировки, и `Auth.LockedOut` пишется дважды; ответ начавшего блокировку отличается от остальных и выдаёт состояние учётной записи; те же риски на смене пароля (общий помощник) | integration (реальный PostgreSQL, процесс API, удерживаемая блокировка строки) | add | Новый класс `SignInRaceTests`, обе theory по поверхности (вход, смена пароля). `Ac6_WrongPasswordsAtTheSameMoment_AreAllCounted`: счёт 0, три одновременных неверных пароля → `access_failed_count == 3`. `Ac6_TwoWrongPasswordsAtTheLimitAtTheSameMoment_…`: счёт 4 (SQL), два одновременных → оба 401 (смена пароля — 400) с одинаковыми телами за вычетом `traceId`; по журналу запросов вход пишет `Auth.LockedOut` один раз и `Auth.LoginFailed` два, смена пароля — `Auth.LockedOut` один (проигравший не пишет ничего); верный пароль после гонки отклонён (учётная запись заблокирована); по `action=Auth.LockedOut&entityId=` ровно одно событие, читается после всех шагов. Смена пароля — своя строка theory, а не довод «помощник общий»: у неё свой журнал и свой ответ. `MaxFailedAccessAttempts` = 5 (`AttemptsBeforeLockout`); Identity сбрасывает счёт в 0 при начале блокировки, поэтому на пределе счёт не утверждается, а блокировка читается по отказу верного пароля. |
| `AuthEndpoints.EnsureSaved` после `AccessFailedAsync` и `ResetAccessFailedCountAsync` (69b73b0) | счёт, который не удалось сохранить, принимается за учтённый: неверный пароль получает обычный отказ и подбор не ограничен; верный пароль входит, не сбросив счёт | integration | add | `SessionTests.Ac6_AttemptWhoseCountCannotBeSaved_FailsTheRequestInsteadOfBeingTakenForCounted`. Неудачный `IdentityResult` достижим без внедрения сбоя: `UserManager.UpdateAsync` валидирует учётную запись и отказывает при пустом `user_name` (SQL `UPDATE users SET access_failed_count = 2, user_name = ''`; вход ищет по e-mail, поэтому запись находится). Неверный пароль → 500 (до исправления 401), верный → 500 (до исправления 200). Отказ по токену конкурентности, который раньше игнорировался, под блокировкой строки недостижим, поэтому гонкой эта ветка не проверяется; состояние учётной записи искусственное, другого способа получить неудачный результат нет. |
| `Infrastructure/Waiting.cs`; `DismissalRaceTests` (приватный `WaitUntilAsync` вынесен в общую инфраструктуру для второго потребителя) | — | — | none | Поведения нет: перенос вспомогательной функции без смены логики. Предохранитель «Impacted test set» (общая инфраструктура тестов) сработал, поэтому запущен весь backend: `DismissalRaceTests` 3/3 в составе 102/102. |

## Removed tests

Удалений нет: ни один тест не потерял риск в дельте. Приватный `WaitUntilAsync` — вспомогательная функция, не тест.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Новые тесты: 5 прогонов в 3 методах (две theory по две строки и один fact); backend 97 → 102.

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/SignInRaceTests.cs`: `Ac6_TwoWrongPasswordsAtTheLimitAtTheSameMoment_AreRefusedAlike_AndStartTheLockoutOnce` (строки `SignIn`, `ChangePassword`) | pre-fix → красный: копия `git archive HEAD`, в которой `AuthEndpoints.cs` заменён на `git show 69b73b0~1:src/Competency.UserManagement/AuthEndpoints.cs`, с тестами entry 6, `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-class Competency.Tests.SignInRaceTests` → exit 2, красный 4 из 4; первые утверждения: `(surface: SignIn)` «Expected journaled to be equal to {"Auth.LockedOut", "Auth.LoginFailed", "Auth.LoginFailed"}, but {"Auth.LockedOut", "Auth.LockedOut", "Auth.LoginFailed", "Auth.LoginFailed"} contains 1 item(s) too many.», `(surface: ChangePassword)` «Expected journaled to be equal to {"Auth.LockedOut"}, but {"Auth.LockedOut", "Auth.LockedOut"} contains 1 item(s) too many.»; runs: 1. mutation M5 (в `CountAttemptAsync` заблокированная учётная запись возвращает `LockoutStarted`, а не `LockedOut`) → exit 2, красный 2 из 4, те же два утверждения; runs: 1. mutation M6a (в `DenyAsync` отказ, начавший блокировку, получает другой `title`) → exit 2, красный 1 из 4, `(surface: SignIn)`: «Expected responses.Select(WithoutTraceId).Distinct() to contain a single item because both attempts are refused with the same response, whichever of them started the lockout, but found {…»; runs: 1. mutation M6b (в `ChangePasswordAsync` отказ, начавший блокировку, получает другую ошибку) → exit 2, красный 1 из 4, `(surface: ChangePassword)`, то же утверждение; runs: 1. mutation M3 (убран `SELECT … FOR UPDATE`) и mutation M4 (убрано `ReloadAsync`) → exit 2, красный 4 из 4 каждая; в строках на пределе первым срабатывает статус, а не журнал: `(surface: SignIn)` «Expected responses.Select(response => response.Status)[0] to equal HttpStatusCode.Unauthorized {value: 401} by value, but found HttpStatusCode.InternalServerError {value: 500}.», `(surface: ChangePassword)` то же для `[1]` и 400 (проигравшее сохранение теперь падает в 500 из-за `EnsureSaved`, а не молча теряется); runs: 2. Без мутаций: 5 прогонов класса подряд, 4/4 каждый. |
| `tests/Competency.Tests/SignInRaceTests.cs`: `Ac6_WrongPasswordsAtTheSameMoment_AreAllCounted` (строки `SignIn`, `ChangePassword`) | pre-fix → красный: тот же прогон, обе строки: «Expected (host.ScalarAsync<int>($"SELECT access_failed_count FROM users WHERE id = '{account.Id}'")) to be 3 because no attempt is lost, but found 1.»; runs: 1 (общий с первой строкой). mutation M3 и mutation M4 → exit 2, обе строки красные с тем же утверждением, «found 1»; runs: 2 (общие с первой строкой). |
| `tests/Competency.Tests/SessionTests.cs`: `Ac6_AttemptWhoseCountCannotBeSaved_FailsTheRequestInsteadOfBeingTakenForCounted` | pre-fix → красный: копия с `AuthEndpoints.cs` из `69b73b0~1`, `--filter-class Competency.Tests.SessionTests` → exit 2, красный 1 из 10: «Expected wrong.Status to be HttpStatusCode.InternalServerError {value: 500} because a wrong password that cannot be counted is not answered as a counted one, but found HttpStatusCode.Unauthorized {value: 401}.»; runs: 1. mutation M1 (убран `EnsureSaved` у `AccessFailedAsync`) → exit 2, красный 1 из 10, то же утверждение, 401; runs: 1. mutation M2 (убран `EnsureSaved` у `ResetAccessFailedCountAsync`) → exit 2, красный 1 из 10: «Expected right.Status to be HttpStatusCode.InternalServerError {value: 500} because a right password that cannot clear the count is not answered as a success, but found HttpStatusCode.OK {value: 200}.»; runs: 1. |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx && npm --prefix web test` (`test` из `commands.yaml`, без изменений)
- Scope: full — impacted set по поиску ссылок (`VerifyPasswordAsync`, `auth/login`, `change-password`, `LockedOut`, `WaitUntilAsync`) занимает почти весь backend, а тесты entry правят общую инфраструктуру (`Infrastructure/Waiting.cs`, `DismissalRaceTests.cs`): предохранитель «Impacted test set» срабатывает, поэтому запущен весь `test`
- Result: pass — backend (xunit.v3, Microsoft Testing Platform): итог 102, успешно 102, сбой 0, пропущено 0, 18,9 с, exit 0; web (vitest): 5 файлов, 23 теста пройдено, 0 сбоев, 2,9 с, exit 0; e2e: не выполнялся (Playwright исключён решением пользователя). Pre-strategy на чистой копии HEAD 00db355 (без тестов entry 6): backend 97/97. Состав: 97 прежних и 5 новых строк (`SignInRaceTests` 4, `SessionTests` 1)
- Lint / build: pass — `npm --prefix web run lint` (eslint + typecheck) exit 0; `dotnet build Competency.slnx --tl:off` 0 предупреждений, 0 ошибок, exit 0; `npm --prefix web run build` exit 0
- HEAD: 8807eed — прогон на рабочем дереве, равном дереву коммита 8807eed (файлы тестов закоммичены без правок после прогона); коммит с `test-plan.md` код не меняет

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

Дефектов продукта в entry 1, 2, 3, 4, 5 и 6 не найдено: новые тесты прошли на HEAD без правок production-кода. Красные строки — дефекты тестов, исправленные внутри entry: entry 2 `Ac5_ServerErrorLog_…` (запись журнала доходила до вывода позже ответа, `ApiHost.LogsAfterAsync`); entry 4 `ApiResponse` печатал `Id` и скрывал причину упавшего утверждения (`PrintMembers`). Находка без дефекта: утверждение внешнего ревью wave-2/iter-01 о гонке привязки с увольнением не воспроизводится на коде до исправления (см. строку `bind` в `Risk → check decisions`).

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
