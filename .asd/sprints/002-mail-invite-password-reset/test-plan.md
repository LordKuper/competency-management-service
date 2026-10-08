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

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"), запись 6: дельта — `AuthEndpoints.cs` и `PasswordPolicyResponse.cs` (модуль `UserManagement`), `openapi/openapi.json` и `web/src/api/schema.d.ts` (производные артефакты контракта, добавочно), `passwordPolicy.ts`, `LinkPasswordPage.tsx`, `ChangePasswordModal.tsx` (каталог `features/auth`). Сборочной, CI- или общей инфраструктуры нет: `AuthEndpoints.cs` — файл одного модуля, а не общий модуль. Предохранитель не срабатывает. Набор по ссылкам: `ContractTests` (читает `openapi.json`), `linkScreens.test.tsx` (рендерит оба экрана с хуком), новый `PasswordPolicyTests`; `ChangePasswordModal` тестами не охвачен. Предстратегический прогон: backend 157 из 157, web 82 из 82 (запрос политики без обработчика получает 599, экраны остаются на запасной подсказке). Сборка `Debug` занята процессом пользователя, backend в `-c Release`.

## Risk → check decisions

Строки записей 1–5 — в `test-plan.entry-01.md` … `test-plan.entry-05.md`. Запись 6 (delta с cb8ee2a):

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `GET /api/v1/auth/password-policy`, `PasswordPolicyResponse { minLength }` из `IdentityOptions.Password.RequiredLength` (AC-19) | число зашито, а не берётся из настройки развёртывания; переопределение `Identity__Password__RequiredLength` не доходит до экрана | интеграционный на реальном хосте (`PasswordPolicyTests`: хост с `Identity__Password__RequiredLength=17`; общий хост отдаёт 10) | add | ровно то, ради чего AC-19: значение следует конфигурации. Путь через реальный Kestrel и привязку опций; юнит на `IOptions` повторил бы реализацию (§17) |
| `.AllowAnonymous()` на новом методе (AC-19) | метод закрыт сессией, экраны ссылки (без сессии) получают 401 | те же интеграционные тесты: `Anonymous()` клиент без входа получает 200 | add | отдельный тест не нужен: оба случая выше идут анонимным клиентом |
| `openapi/openapi.json`: метод присутствует, без 401/429, схема с `minLength` (AC-19) | метод пропал из контракта или получил 401/429 (клиент уводит анонимный экран на вход) | контрактный, по образцу `ContractTests.Ac5_AnonymousLinkMethods_…` | add | `check:api` ловит лишь расхождение файла с кодом, не свойства метода |
| `usePasswordHint()` в `LinkPasswordPage` и `ChangePasswordModal` (AC-19) | подсказка не показывает число сервера; при сбое запроса подсказки нет или экран падает; анонимный экран зовёт `/auth/me` | компонентный (`linkScreens.test.tsx`, оба экрана ссылки): ответ 17 → «Не короче 17 символов;», запрос один, `/auth/me` ноль; ответ 500 → запасной текст без числа, `/auth/me` ноль | add | видимое поведение. `ChangePasswordModal` берёт тот же хук и отдельного теста не получает (число на модальном окне — в `Manual verification`) |
| `schema.d.ts` (сгенерирован) | расхождение с контрактом | `check:api` | keep | существующая проверка |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалены константа `PASSWORD_HINT` и утверждение «подсказка не называет длину»; в `src`, `web/src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений о них нет (поиск пуст, кроме нового комментария в `passwordPolicy.ts` о запасном тексте).

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

| Test | Regression proof |
|---|---|
| `PasswordPolicyTests.Ac19_PasswordPolicy_ReportsTheLengthThatDeploymentConfigured_WithoutASession`, `…_ReportsTheShippedDefault_WhenNothingIsOverridden` | mutation (эндпоинт отдаёт зашитое `10` вместо `RequiredLength`): `dotnet test --solution Competency.slnx -c Release --filter-class "*PasswordPolicyTests"` → exit 2, упал тест настроенной длины 17 (тест умолчания 10 прошёл, как и ожидалось); runs: 1; мутация откатана |
| `ContractTests.Ac19_PasswordPolicy_IsPublished_AsAnAnonymousGet_WithoutA401Or429_AndWithTheMinimumLength` | n/a: закрепляет уже верный контракт нового метода, без мутации; зелёный на `openapi.json` HEAD |
| `linkScreens.test.tsx`: «password hint on the … screen (AC-19)», 2 теста × 2 экрана | mutation (хук подставляет «Не короче 8» вместо `data.minLength`): `npx vitest run src/features/auth/linkScreens.test.tsx` → exit 1, упали 2 из 16 — оба «names the minimum length…» (по экрану); тест запасного текста мутацию не затрагивает; runs: 1; мутация откатана |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Competency.slnx -c Release && npm --prefix web test` (`test` из `commands.yaml`; `-c Release`, вывод `Debug` может быть занят Visual Studio пользователя)
- Scope: запись 6 — предохранитель не сработал, набор по ссылкам (`ContractTests`, `PasswordPolicyTests`, `linkScreens.test.tsx`); для дешёвой уверенности прогнаны оба проекта целиком
- Result: pass — backend 160 passed / 0 failed / 0 skipped (exit 0), web 11 файлов, 86 passed / 0 failed (exit 0)
- Lint / build: pass — `npm --prefix web run lint` exit 0, `npm --prefix web run check:api` exit 0, `dotnet build Competency.slnx --tl:off -c Release` без предупреждений и ошибок, `npm --prefix web run build` exit 0
- HEAD: 13afa160414d429663c342197cb1a669235431c4

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
