---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/testing.md (verdict)
---

# Test plan — sprint 002-mail-invite-password-reset

## Entry log

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 1cdc829 | вся поверхность изменений: `git diff main...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (75 файлов, ветка на 342baa6) |
| 2 | 906d167 | delta с записи 1: `git diff 1cdc829...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (4 файла: `appsettings.Development.json`, `launchSettings.json`, `deploy/dev/docker-compose.yml`, `deploy/dev/README.md`; AC-17) плюс правки памяти D-1/D-2 (1798700) |
| 3 | 2d661f6 | delta с записи 2: `git diff 906d167...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (5 файлов: `AccountMail.cs`, `AuthCard.tsx`, `passwordPolicy.ts`, `LinkPasswordPage.tsx`, `UserForm.tsx`; AC-18) |
| 4 | 6a13874 | delta с записи 3: `git diff 2d661f6...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (3 файла: `AuthCard.tsx`, `appsettings.json`, `deploy/README.md`; AC-18, AC-4 срок приглашения — неделя) |
| 5 | cb8ee2a | delta с записи 4: `git diff 6a13874...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (2 файла: `LoginPage.tsx`, `productName.ts`; AC-18, подзаголовок экрана входа убран) |
| 6 | 772dde2 | delta с записи 5: `git diff cb8ee2a...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (7 файлов: `AuthEndpoints.cs`, `PasswordPolicyResponse.cs`, `openapi.json`, `schema.d.ts`, `passwordPolicy.ts`, `LinkPasswordPage.tsx`, `ChangePasswordModal.tsx`; AC-19, минимальная длина пароля из API) |
| 7 | 352584d | delta с записи 6: `git diff 772dde2...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (6 файлов: `PlatformModule.cs`, `SmtpOptions.cs`, `MailSender.cs`, `appsettings.json`, `deploy/README.md`, `deploy/dev/README.md`; AC-1, review-fix wave-1/iter-01: external #1, #2, combined F1–F3) |

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 7: дельта — `PlatformModule.cs` (регистрация сквозных служб и проверка настроек почты при старте каждого хоста) и `appsettings.json` (общий файл настроек образа). Это общий модуль и файл настроек всего приложения, поэтому предохранитель срабатывает: набор — полный, оба проекта (backend `-c Release` и web). Предстратегический прогон на HEAD 2a188ac: backend 160 из 160, web 86 из 86. Сборка `Debug` занята процессом пользователя, backend в `-c Release`. Строк тестера review-fix в этом раунде нет (коммиты cf381d1, 2e616dc, 0e43e68 — код и документация, `tests/` не тронут), удалений для переноса нет.

## Risk → check decisions

Строки записей 1–6 — в `test-plan.entry-01.md` … `test-plan.entry-06.md`. Запись 7 (delta с 772dde2):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AddMail`: `Smtp:SecureSocketOptions` проверяется `Enum.IsDefined` (AC-1, external #1) | значение `99` проходит привязку и старт, MailKit трактует неопределённое значение как соединение без TLS: ссылки с токенами и учётные данные SMTP уходят открытым текстом | интеграционный на реальном хосте: строка `Smtp__SecureSocketOptions=99` теории `Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_…` (старт отказывает, в ошибке `'Smtp:SecureSocketOptions'`) | add | механика «старт отказывает с именем ключа» в теории уже есть, новая — одна `InlineData`; юнит на валидатор повторил бы реализацию (§17) |
| `AddMail`: `Smtp:Timeout` не больше `49.17:02:47.294`, предела `CancelAfter` (AC-1, external #2) | значение выше предела проходит проверку на положительность, `CancelAfter` бросает `ArgumentOutOfRangeException` в `MailSender` до `try`: отправка заканчивается исключением вместо `false`; константу можно сдвинуть на 1 мс в любую сторону | интеграционный: строки теории `50.00:00:00` и граничная `49.17:02:47.295` (старт отказывает); `Ac1_TheLongestSmtpTimeoutTheStartAccepts_StillSendsTheMail` — хост с `49.17:02:47.294`, приглашение, письмо в Mailpit | add | граница и есть значение (литерал важен, §17). Отправка на максимуме доказывает, что `CancelAfter` принимает ровно то, что пропускает старт; независимо проверено в скрипте: 4294967294 мс принято, 4294967295 мс — `ArgumentOutOfRangeException` |
| `AddMail`: `Smtp:Host` проверяется `Uri.CheckHostName` (AC-1, combined F2) | плейсхолдер `<SMTP_HOST>` из `configmap.yaml` проходит старт, каждое письмо уходит в отказ вместо ошибки старта с именем ключа | интеграционный: строка `Smtp__Host=<SMTP_HOST>` той же теории; прежняя строка с пустым значением остаётся | add | литерал плейсхолдера — сам вход (§17) |
| `SmtpOptions.CheckCertificateRevocation`, `appsettings.json` `Smtp:CheckCertificateRevocation` (AC-1, combined F1; решение пользователя 2026-10-08: по умолчанию `true`) | умолчание станет `false` и проверка отзыва сертификата relay тихо отключится в каждом развёртывании | компонентный: `Ac1_ShippedSmtpSettings_CheckCertificateRevocation_UntilAnOperatorSwitchesItOff` привязывает поставляемый `appsettings.json` к `SmtpOptions` и требует `true` | add | заявленное требование, проверяется значение, а не формулировка; без TLS-сервера это единственный наблюдаемый уровень умолчания |
| `MailSender.cs`: передача `CheckCertificateRevocation` в `SmtpClient`, ветвь `false` (AC-1, combined F1) | настройка не доходит до клиента MailKit | — | none | эффект виден только в TLS-рукопожатии с сертификатом, цепочка которого доверена хосту, при недоступных CRL/OCSP. Mailpit тестов работает с `None`; TLS-сервер с доверенным тестовым CA требует правки хранилища доверия хоста или новой тестовой инфраструктуры (Complication Approval), не строится. Потолок: снятая передача настройки (умолчание MailKit тоже `true`) останется зелёной; отправку после развёртывания оператор проверяет по `deploy/README.md`, шаг «Доверие внутреннему CA» |
| `deploy/README.md`, `deploy/dev/README.md` (combined F1, F3) | инструкция расходится с кодом: неверное имя ключа, неверный способ сменить порт | — | none | инструкции оператору и разработчику, ни один тест репозитория не читает `deploy/**` (поиск по `tests` пуст). Описанное поведение — отказ старта с именем `Smtp__SecureSocketOptions`, `Smtp__Timeout`, `Smtp__Host` — закреплено строками выше. Сверка имени ключа `Smtp__CheckCertificateRevocation` с свойством потребовала бы первой в репозитории проверки «документ против кода» (новый класс тестов и зависимость от раскладки репозитория), цена выше вреда опечатки в документе |
| `LastAdministratorTests.Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne`, сценарий с приглашённым администратором (AC-7, review-fix wave-2/iter-01: external #1) | тест гонки двух снятий последних двух администраторов лишь стартовал оба запроса вместе (общий `TaskCompletionSource`, 4 повтора): перекрытие транзакций не гарантировано, последовательный прогон проходит те же проверки, поэтому снятая блокировка или лишний «учитываемый» администратор оставались незамеченными | интеграционный детерминированный: внешняя транзакция держит блокировку активных администраторов (`RowLock.HoldActiveAdministratorsAsync`, тот же `SELECT … FOR UPDATE`, что `ActiveAdministrators.LockAsync`); первое снятие ждёт на ней, второе встаёт следом (оба подтверждены по `pg_stat_activity`, снятие не должно ответить, пока блокировка держится); после снятия блокировки первое отвечает 200, второе — 409 с «последн…» (его сеанс был действителен при старте, поэтому это отказ по правилу, а не 401), активным остаётся ровно первый из администраторов. Приглашённый администратор остаётся в сценарии и не считается | change | заменяет прежнюю проверку: детерминизм задаёт ожидание на блокировке, повторы не нужны (раунд один, три строки теории). Потолок: блокировка администраторов и блокировка дерева `pg_advisory_xact_lock` каждая по отдельности сериализует эти пути (снятие любой одной тест не краснит, эквивалентные мутации); краснеет снятие обеих вместе, то есть пропажа защиты целиком, и снятие признака «зарегистрирован» |

Проверка остатков: проверка `Smtp:Host` «не задан» заменена на «не имя хоста и не адрес»; в `tests`, `deploy`, `src`, `web/src`, `.claude/agent-memory/**` утверждений о прежнем тексте ошибки и о правке `Smtp:Port` в `appsettings.Development.json` нет (поиск пуст).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| `PlatformTests.Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_FailsNamingTheSetting`: четыре новые строки (`Smtp__Host=<SMTP_HOST>`, `Smtp__SecureSocketOptions=99`, `Smtp__Timeout=50.00:00:00`, `Smtp__Timeout=49.17:02:47.295`) | fail-first на коде до исправления (`git archive cf381d1~1 src` во внешней копии + текущие `tests/`, строка с `CheckCertificateRevocation` из копии убрана, свойства там нет): `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj -c Release --filter-class "*PlatformTests"` → exit 2, упали 4 из 24 — ровно четыре новые строки; первая упавшая проверка каждой: «Expected failure to be System.InvalidOperationException because a host that cannot send its mail or build its links must refuse to start, but found <null>.»; остальные 20, включая пять прежних строк теории, прошли; runs: 1. Граница: mutation (`MaxSmtpTimeout = TimeSpan.FromMilliseconds(uint.MaxValue)` вместо `uint.MaxValue - 1`, на 1 мс выше): `--filter-method "*Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_FailsNamingTheSetting*"` → exit 2, упала 1 из 9 — строка `Smtp__Timeout` / `49.17:02:47.295`, та же проверка `BeOfType`; runs: 1; во внешней копии, репозиторий не менялся |
| `PlatformTests.Ac1_TheLongestSmtpTimeoutTheStartAccepts_StillSendsTheMail` | до исправления зелёный (верхней границы не было, `CancelAfter` принимает максимум): сторона «принимается» закреплена мутацией (`MaxSmtpTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 2)`, на 1 мс ниже): `--filter-method "*Ac1_TheLongestSmtpTimeoutTheStartAccepts_StillSendsTheMail*"` → exit 2, упал этот тест: `The API host exited with code -532462766 before it was ready … OptionsValidationException: 'Smtp:Timeout' is not a positive time span of at most 49.17:02:47.2930000` (первое падение — старт хоста, а не `Should()`); runs: 1; во внешней копии |
| `PlatformTests.Ac1_ShippedSmtpSettings_CheckCertificateRevocation_UntilAnOperatorSwitchesItOff` | на коде до исправления не компилируется (свойства нет), поэтому имя упавшего теста даёт только мутация. mutation 1 (`"CheckCertificateRevocation": true` → `false` в поставляемом `appsettings.json`): `--filter-method "*Ac1_ShippedSmtpSettings_CheckCertificateRevocation_UntilAnOperatorSwitchesItOff*"` → exit 2, упал этот тест: «Expected boolean to be True because only an operator who cannot reach the revocation endpoints turns the check off, but found False.»; mutation 2 (ключ удалён из `appsettings.json` и `= true` убран из `SmtpOptions`): та же команда → exit 2, то же сообщение; runs: 2; во внешней копии |
| `LastAdministratorTests.Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne` (переписан: три строки теории по одному раунду, `RowLock.HoldActiveAdministratorsAsync`) | fail-first по мутациям во внешней копии `rv2-mut` (`git archive HEAD` + текущие `tests/`), `dotnet test --project tests/Competency.Tests/Competency.Tests.csproj -c Release --filter-method "*Ac7_TwoConcurrentRemovalsOfTheOnlyTwoAdministrators_LeaveExactlyOne*"`; репозиторий не менялся. M1: из `ActiveAdministrators.LockAsync` убран `FOR UPDATE` → exit 0, 3 из 3 зелёные (эквивалентная мутация: блокировку дерева `pg_advisory_xact_lock` держит каждое снятие, а запись строки жертвы ждёт блокировку теста). M1c: `pg_advisory_xact_lock` заменён на `SELECT {LockKey}` в `OrgTree.BeginExclusiveAsync` при восстановленном `FOR UPDATE` → exit 0, 3 из 3 зелёные (эквивалентная мутация: блокировка администраторов сериализует сама). M1b: сняты обе блокировки → exit 2, строки гоняли по одной (после первой красной строки остальные падают на общем состоянии фикстуры «active … is empty», читать первую): `block/block`, `dismiss/dismiss`, `dismiss/block` — у каждой первая упавшая проверка «Expected results[1].Status to be HttpStatusCode.Conflict {value: 409} because the removal that came second finds the first one done and no other active administrator, the invited one not counting: …, but found HttpStatusCode.OK {value: 200}.» M2: из `ActiveAdministrators.HasOtherAsync` убрано `&& user.PasswordHash != null` (приглашённый администратор начинает считаться) → exit 2, три строки по одной, то же первое падение, что у M1b; runs: 1 на каждую мутацию и строку |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx -c Release && npm --prefix web test` (`test` из `commands.yaml`; `-c Release`, вывод `Debug` может быть занят Visual Studio пользователя)
- Scope: запись 7 — предохранитель сработал (`PlatformModule.cs`, `appsettings.json`), набор полный: оба проекта целиком; измерено дерево HEAD плюс тесты записи 7 до их коммита (шесть новых тестов и строк: backend 160 → 166)
- Result: pass — backend 166 passed / 0 failed / 0 skipped (exit 0), web 11 файлов, 86 passed / 0 failed (exit 0)
- Lint / build: pass — `npm --prefix web run lint` exit 0, `npm --prefix web run check:api` exit 0, `dotnet build Competency.slnx --tl:off -c Release` без предупреждений и ошибок, `npm --prefix web run build` exit 0
- HEAD: 2a188ac18b9b8976a6710d1b3799ae560ce13901

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/rules/sprint-lifecycle.md` "Impl-test phase"). One table only: append rows, never a second `## Defects` section — the check fails on one.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | `.claude/agent-memory/asd-dev-critical/project_users-modal-menu-facts.md` | строка 11 описывает удалённый `ResetPasswordModal`: «**Reset-password target is looked up by id** from the live list data (`dialog.kind === "resetPassword"` + `data.items.find`) … `ResetPasswordModal` lost its `open` prop» | leftover-term check (`artifact-layout.md` "Agent memory"): `ResetPasswordModal` | fixed | 1798700 |
| D-2 | 1 | `.claude/agent-memory/asd-dev-critical/project_password-reset-link-facts.md` | строка 9 неверна после этого прохода: «The test `ApiHost` only raises `RateLimiting__Login__PermitLimit`, so suites hitting these three endpoints from one address need `RateLimiting__PasswordReset__PermitLimit` raised too» — `ApiHost` теперь задаёт оба лимита | leftover-term check (`artifact-layout.md` "Agent memory"): устаревшее утверждение о тестовом хосте | fixed | 1798700 |

## Manual verification (optional)

Только там, где автоматизация невозможна: внешний вид и адаптивность новых экранов (Playwright и e2e в проекте не используются) и живой dev-стек.

| AC | Steps | Expected observation |
|---|---|---|
| AC-11 | Запустить приложение (профиль F5 «Everything» или `npm --prefix web run dev` с API). Открыть `/login`, `/forgot-password`, `/accept-invitation#token=x`, `/reset-password#token=x` при ширине окна ~1280 px и ~820 px (планшет) | экраны в стиле экрана входа (карточка по центру, логотип, заголовок), не выходят за границы окна и читаются на обоих размерах; после открытия ссылки с токеном адресная строка без `#token=…` |

result: pass — экраны отображаются нормально на обоих размерах; замечание: убрать подсказку «Необязательно: учётная запись может быть привязана…» в форме пользователя (поправка AC-18, Task 8)

| AC-3 | В dev-стеке (профиль F5 «Everything») создать пользователя в «Пользователях» и открыть веб-интерфейс Mailpit (`http://127.0.0.1:18025`) | в Mailpit есть письмо-приглашение на русском со ссылкой на `http://localhost:5173/accept-invitation#token=…`; ссылка открывает экран «Задание пароля» |

result: pass — письмо пришло, ссылка рабочая; замечания к текстам и экрану входа (поправка AC-18, Task 8)

| AC-18 | Запустить приложение (профиль F5 «Everything»). Открыть `/login`, `/forgot-password`, `/accept-invitation#token=x` при ширине ~1280 px и ~820 px; в «Пользователях» открыть форму создания пользователя; создать пользователя и открыть письмо-приглашение в Mailpit (`http://127.0.0.1:18025`) | на карточке рядом с логотипом показано название «Калибр», вёрстка не ломается на обоих размерах; в форме пользователя нет подсказки под полем «Сотрудник»; подсказка пароля и вводный текст на экране задания пароля короче; в письме нет фразы «Если вы не ждали этого письма, просто удалите его.» |

result: fail — название «Калибр» мелкое, должно быть крупнее, под стать логотипу; срок ссылки-приглашения — неделя (поправка AC-18 и срока, Task 9)

| AC-18 (повтор) | Запустить приложение (профиль F5 «Everything»). Открыть `/login`, `/forgot-password`, `/accept-invitation#token=x` при ширине ~1280 px и ~820 px | название «Калибр» крупнее прежнего (1.5× заголовка), соразмерно логотипу 80 px; вёрстка карточки не ломается и не выходит за границы окна на обоих размерах |

result: pass — название крупное, соразмерно логотипу; дополнительно: убрать подзаголовок «Компетенции и карьерный рост» (Task 10)

| AC-19 | Запустить приложение (профиль F5 «Everything»). Открыть `/accept-invitation#token=x` («Задание пароля»), `/reset-password#token=x` («Новый пароль»), затем после входа открыть окно смены пароля. Сверить число с `Identity:Password:RequiredLength` (по умолчанию 10; при желании запустить API с `Identity__Password__RequiredLength=14`) | подсказка под полем пароля на всех трёх: «Не короче N символов; заглавные и строчные буквы, цифры и специальные символы.», N совпадает с настройкой |

result: pass — подсказка называет точную длину на всех трёх экранах
