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
| 8 | bffabb7 | delta с записи 7: `git diff 352584d...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (5 файлов: `AuthEndpoints.cs` — только doc-комментарий; тесты `LastAdministratorTests.cs`, `RowLock.cs`, `PasswordPolicyTests.cs`, `AccountLinkTests.cs`; AC-7, review-fix wave-2/iter-01: external #1, детерминированный тест гонки последних администраторов) |
| 9 | 274b926 | delta с записи 8: `git diff bffabb7...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` — ПУСТО (единственная правка review-fix wave-2/iter-02 — e260b3d, память агента `.claude/agent-memory/asd-tester-critical/project_competency-test-harness.md`) |
| 10 | c372e1b | delta с записи 9: `git diff 274b926...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (3 файла web: `useSendUserMail.ts`, `UserListPage.tsx`, `passwordPolicy.ts`; review-fix wave-3/iter-01: F1 защита и индикатор повторной отправки письма, F2 склонение «символа»/«символов», external #1 текст предупреждения) |
| 11 | 759792b | delta с записи 10: `git diff c372e1b...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (2 файла web: `useSendUserMail.ts` — только JSDoc; `userMail.test.tsx` — комментарий перенесён в JSDoc хелпера `isClosing`; review-fix wave-3/iter-02) |


## Risk → check decisions

Строки записей 1–10 — в `test-plan.entry-01.md` … `test-plan.entry-10.md`. Запись 11 (delta с c372e1b, review-fix wave-3/iter-02):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `useSendUserMail.ts`: JSDoc перенесён; `userMail.test.tsx`: комментарий перенесён в JSDoc хелпера `isClosing` | поведение не менялось (только комментарии/документация) | существующие тесты `userMail.test.tsx` | none | дельта — комментарии без изменения кода и ассертов; новое поведение отсутствует, новые тесты не нужны (§17) |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

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

- Command: `dotnet test --solution Competency.slnx -c Release && npm --prefix web test`; `npm --prefix web run lint`; `dotnet build Competency.slnx -c Release --tl:off && npm --prefix web run build`
- Scope: terminal full-suite (impl-review wave-3/iter-03), unscoped, whole tree
- Result: pass — backend 166 из 166 (exit 0, 0 сбоев, 0 пропущено); web 11 файлов / 89 из 89 (exit 0)
- Lint / build: pass — `lint` exit 0; backend build -c Release exit 0 (0 предупреждений, 0 ошибок); web build exit 0
- HEAD: aef943d2303ee03218f810d516a6838ee70eb009

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
