# Test plan — sprint 001-project-init-org-structure — entry 5 (rotated segment)

Строки `Risk → check decisions`, `Removed tests` и `Added tests` entry 5 (`HEAD analysed` a460cdd91cb096209e06ba868f90574e1b8633e7, scope «delta since entry 4»), перенесённые сюда на strategy pass entry 6 (`artifact-layout.md` "Test plan" Rotation). Сегмент не правится: строка, чей риск изменился, заменяется строкой в `test-plan.md`. Review-fix строк тестировщика с прошлого entry нет: review-fix wave-2/iter-03 (69b73b0) изменил только production-код.

## Risk → check decisions

Entry 5 — дельта `08d21ccc83fa5cacc398b0fb94e75540dd18b50a...HEAD` без `.asd/**`, `docs/**` и `.claude/**`: review-fix wave-2/iter-02, dev 4d0d9eb (единственный файл `src/Competency.UserManagement/AuthEndpoints.cs`; тестов не менял и строк в план не добавлял, поэтому review-fix removal rows для переноса нет). Строки entry 4 перенесены в `test-plan.entry-04.md`; заменяющих строк нет: фикс добавляет событие журнала, поведение, под которое написаны строки entry 4 (блокировка, сброс счётчика, политика, атомарность записи), не меняется. Pre-strategy run (`commands.yaml` `test`, backend, полный набор): 97/97, exit 0, на HEAD 6250db9 — регрессий от фикса нет. Окружение прежнее: реальный процесс API и один контейнер PostgreSQL на прогон, Playwright исключён решением пользователя, новых зависимостей нет.

Мутации выполнялись в копии `git archive HEAD` вне репозитория (production-код рабочего дерева не менялся) с тестом этого entry; `core.autocrlf=true` даёт в копии CRLF, поэтому якоря мутаций с CRLF. Команда: `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj --filter-class <класс>`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AuthEndpoints.ChangePasswordAsync` → `AuditLockoutAsync`, общий с `DenyAsync` (4d0d9eb; внешнее #1 wave-2/iter-02; AC-15) | локаут, начатый неверным текущим паролем при смене пароля, не попадает в журнал (событие `Auth.LockedOut` писал только вход): владелец украденной сессии блокирует учётную запись, а след остаётся пустым; обратные ошибки — событие пишется дважды, не тем запросом или под чужими actor и role, и путь входа теряет событие после выноса в помощник | integration (реальный PostgreSQL, процесс API) | add | Расширен `SessionTests.Ac6_WrongCurrentPasswordInChangePassword_LocksTheAccountLikeWrongSignIns` вместо нового теста: хост, пять неверных текущих паролей и заблокированное состояние в нём уже есть. Утверждения: по журналу `action=Auth.LockedOut&entityId=<учётная запись>` ровно одно событие (читается после всех шагов, в том числе после отказа при верном текущем пароле в заблокированной учётной записи, поэтому лишняя запись на любом шаге краснит), его actor — сама учётная запись, role — `User`, и записано оно запросом, достигшим предела (журнал пятой попытки — только `Auth.LockedOut`: `Auth.LoginFailed` смена пароля не пишет, как и раньше). Помощник общий, так что actor и role проверены заодно и для входа: `AuditTests.Ac15_SignInEvents_…` их у `Auth.LockedOut` не читал. Путь входа: нового теста не нужно — пятая попытка входа в `AuditTests.Ac15_SignInEvents_AreJournaledWithTheAccountIdentityAndNothingTyped` ждёт `Auth.LockedOut` и `Auth.LoginFailed` (мутация M4 ниже). Проверка `user is not null` в `DenyAsync` нужна анализу null: `LockoutStarted` без учётной записи недостижим, поведения нет. |

## Removed tests

Удалений нет.

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Новых тестов нет: расширен существующий, число тестов прежнее (97).

| Test | Regression proof |
|---|---|
| `tests/Competency.Tests/SessionTests.cs`: `Ac6_WrongCurrentPasswordInChangePassword_LocksTheAccountLikeWrongSignIns` (расширен, ffa46ea) | pre-fix → красный: копия `git archive HEAD` с тестом entry 5 и обратным патчем 4d0d9eb (`git show 4d0d9eb -- src`, `git apply -R`), фильтр `Competency.Tests.SessionTests` → exit 2, красный 1 из 9: «Expected lockouts to contain a single item because the lockout that change-password started is journaled once, as at sign-in, but the collection is empty.»; runs: 1. mutation M1: вторая запись `AuditLockoutAsync` на смене пароля → exit 2, красный 1 из 9, то же утверждение: «… but found {{[id, …], [timestamp, …], [actor, …], [role, User], [action, Auth.LockedOut], …»; runs: 1. mutation M5: событие на смене пароля пишется при любом отказе, кроме блокировки (`denial != LoginDenial.LockedOut`) → exit 2, красный 1 из 9, то же утверждение, «… but found {{[id, …»; runs: 1. mutation M2: в помощнике `Actor: "unknown"` → exit 2, красный 1 из 9: «Expected lockouts[0]!["actor"]!.GetValue<string>() to be the same string, but they differ at index 0:»; runs: 1. mutation M3: в помощнике `Role: "anonymous"` → exit 2, красный 1 из 9: «Expected lockouts[0]!["role"]!.GetValue<string>() to be the same string, but they differ at index 0:»; runs: 1. mutation M6: смена пароля пишет событие на отказе в уже заблокированной учётной записи, а не на попытке, начавшей блокировку (`denial == LoginDenial.LockedOut`; событие одно, actor и role верны, поэтому счёт и идентичность проходят) → exit 2, красный 1 из 9: «Expected (admin.AuditOfRequestAsync(attempts[^1].RequestId!)).Actions() to be equal to {"Auth.LockedOut"} because the attempt that reached the limit wrote the event, but found empty collection.»; runs: 1. Путь входа, существующий тест без правок: mutation M4: из `DenyAsync` убран вызов `AuditLockoutAsync` → фильтр `Competency.Tests.AuditTests` exit 2, красный 1 из 8, `Ac15_SignInEvents_AreJournaledWithTheAccountIdentityAndNothingTyped`: «Expected (admin.AuditOfRequestAsync(attempts[4].RequestId!)).Actions() to be equal to {"Auth.LockedOut", "Auth.LoginFailed"}, but {"Auth.LoginFailed"} contains 1 item(s) less.»; runs: 1. Без мутаций: класс `SessionTests` 9/9, весь backend 97/97. |
