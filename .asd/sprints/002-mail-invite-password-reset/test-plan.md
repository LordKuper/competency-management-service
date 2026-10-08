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

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 3: `AccountMail.cs` — файл модуля `Competency.UserManagement`, остальные — файлы фич `web/src/features/{auth,users}`; сборочной, CI- и общей инфраструктуры нет, предохранитель не срабатывает, набор — по ссылкам: `linkScreens.test.tsx`, `userMail.test.tsx` (web) и тесты, шлющие письма `AccountMail` (`AccountLinkTests`, `AccountLinkLimitTests`, `AccountLinkRaceTests`, `MailFailureTests`, `MailTransportTests`). Поиск по `tests` и `web/src`: ни один тест не ищет изменённые тексты (`не ждали`, `PASSWORD_HINT`, подсказку поля «Сотрудник»), `alt` логотипа или название продукта. Предстратегический прогон выполнен шире набора, дёшево: backend 157 из 157 зелёных (`dotnet test --solution Competency.slnx -c Release`, exit 0), web 82 из 82 зелёных (`npm --prefix web test`, exit 0). Сборка `Debug` занята процессом `Competency.Api` пользователя, поэтому backend в `-c Release`.

## Risk → check decisions

Строки записи 1 — в `test-plan.entry-01.md`, записи 2 — в `test-plan.entry-02.md`. Запись 3 (delta с 906d167):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `AccountMail.cs` (из письма-приглашения убрана фраза «Если вы не ждали этого письма…»), `LinkPasswordPage.tsx`, `passwordPolicy.ts` (`PASSWORD_HINT`), `UserForm.tsx` (убрана подсказка поля), `AuthCard.tsx` (название «Калибр» рядом с логотипом, `alt=""`) (AC-18) | правка текстов ломает существующую проверку; название продукта читается дважды (логотип с `alt` и текст) | — | none | чистая правка формулировок и вёрстки без ветвлений; существующие тесты не опираются на удалённые тексты (поиск выше, 157 + 82 зелёных без правок), поэтому корректировать нечего. Тест на отсутствие фразы или на текст подсказки закрепил бы буквальную прозу (`.claude/agent-memory/asd-dev-critical/project_tests-pin-literal-prose.md`), а не поведение (§17). Вид карточки (название рядом с логотипом) в jsdom не проверяется; пользовательский просмотр — новая строка `Manual verification` AC-18 |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённые термины дельты — фраза «Если вы не ждали этого письма…», подсказка поля «Сотрудник», `alt` логотипа «Калибр»; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о них нет.

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx -c Release && npm --prefix web test` (`test` из `commands.yaml`; `-c Release`, вывод `Debug` может быть занят Visual Studio пользователя)
- Scope: запись 3 — impacted set дельты (предохранитель не сработал, см. выше); фактически прогнан полный набор обоих проектов. Предстратегический прогон = этот же: дельта не менялась между ним и итогом
- Result: pass — backend 157 passed / 0 failed / 0 skipped (exit 0), web 11 файлов, 82 passed / 0 failed (exit 0)
- Lint / build: pass — `npm --prefix web run lint` exit 0, `npm --prefix web run check:api` exit 0, `dotnet build Competency.slnx --tl:off -c Release` 0 предупреждений 0 ошибок, `npm --prefix web run build` exit 0
- HEAD: e4a841662d62e68f0fd6cb07b0e717d8aa2ca868

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
