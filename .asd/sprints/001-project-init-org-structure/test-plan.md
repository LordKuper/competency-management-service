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
| 5 |  | delta since entry 4 (08d21ccc83fa5cacc398b0fb94e75540dd18b50a) |

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

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx && npm --prefix web test` (`test` из `commands.yaml`, без изменений)
- Scope: full — impacted set по поиску ссылок (`change-password`, `auth/login`, `SignInAsync`, `LockedOut`) занимает 10 файлов тестов, почти весь backend, поэтому запущен весь `test`; дельта правит один файл модуля UserManagement, общей инфраструктуры не затрагивает (предохранитель «Impacted test set» не срабатывает); HEAD ffa46ea
- Result: pass — backend (xunit.v3, Microsoft Testing Platform): итог 97, успешно 97, сбой 0, пропущено 0, 19,8 с, exit 0; web (vitest): 5 файлов, 23 теста пройдено, 0 сбоев, 2,7 с, exit 0; e2e: не выполнялся (Playwright исключён решением пользователя). Pre-strategy на HEAD 6250db9: backend 97/97. Состав: 97 прежних, новых строк нет (утверждения добавлены в существующий тест)
- Lint / build: pass — `npm --prefix web run lint` (eslint + typecheck) exit 0; `dotnet build Competency.slnx --tl:off` 0 предупреждений, 0 ошибок, exit 0; `npm --prefix web run build` exit 0
- HEAD: ffa46ea — прогон на дереве, включающем тест этого entry (коммит ffa46ea); commit с `test-plan.md` код не меняет

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

Дефектов продукта в entry 1, 2, 3, 4 и 5 не найдено: новые тесты прошли на HEAD без правок production-кода. Красные строки — дефекты тестов, исправленные внутри entry: entry 2 `Ac5_ServerErrorLog_…` (запись журнала доходила до вывода позже ответа, `ApiHost.LogsAfterAsync`); entry 4 `ApiResponse` печатал `Id` и скрывал причину упавшего утверждения (`PrintMembers`). Находка без дефекта: утверждение внешнего ревью wave-2/iter-01 о гонке привязки с увольнением не воспроизводится на коде до исправления (см. строку `bind` в `Risk → check decisions`).

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
