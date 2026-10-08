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
| 2 | | delta с записи 1: `git diff 1cdc829...HEAD` без `.asd`, `docs`, `.claude`, `.codex`, `.agents` (4 файла: `appsettings.Development.json`, `launchSettings.json`, `deploy/dev/docker-compose.yml`, `deploy/dev/README.md`; AC-17) плюс правки памяти D-1/D-2 (1798700) |

Предохранитель impacted set (`sprint-lifecycle.md` "Impacted test set"): поверхность затрагивает общую инфраструктуру (`PlatformModule.cs`, все `packages.lock.json`, `Program.cs`, `appsettings.json`), поэтому набор — полный (`dotnet test` + `npm test`), без выборки по ссылкам и AC. Предстратегический прогон существующих тестов: backend 116 тестов, 116 красных (хост API не стартует без `Smtp__*`/`App__PublicBaseUrl`, `dotnet test --solution Competency.slnx -c Release`, exit 2); web 60 из 60 зелёных (`npm --prefix web test`, exit 0). Сборка `Debug` занята процессом `Competency.Api` пользователя (запущен из Visual Studio, не останавливался), поэтому backend гоняется в `-c Release`.

## Risk → check decisions

Строки записи 1 — в `test-plan.entry-01.md`. Запись 2 (delta с 1cdc829): строки ниже; суперсединг строки записи 1 про `deploy/dev/docker-compose.yml`, `launchSettings.json`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `src/Competency.Api/appsettings.Development.json` (SMTP по умолчанию → Mailpit `127.0.0.1:11025`, `None`; `App:PublicBaseUrl`), удаление `Smtp__*`/`App__PublicBaseUrl` из `launchSettings.json` (AC-17) | пользовательские секреты не перекрывают dev-умолчания; dev-хост не стартует без `Smtp__*` | — | none | декларативный JSON без ветвлений; приоритет пользовательских секретов над `appsettings.Development.json` в Development — поведение фреймворка, не проекта. Тестовый хост работает в Production с явными переменными окружения (запись 1), файл на него не влияет; проверка Development-запуска потребовала бы нового хоста в другой среде (новая инфраструктура теста, §17). Старт без значений уже покрывает `PlatformTests.Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_…`; файл проходит `build` (копируется в вывод) |
| `deploy/dev/docker-compose.yml` (`MP_SMTP_DISABLE_RDNS: "true"` у Mailpit), `deploy/dev/README.md` (раздел user-secrets) (AC-17) | письмо из dev-стека ждёт ~10 с приветствия SMTP | — | none | декларативный ключ образа и проза; тот же ключ уже проверен в тестовом Mailpit (`MailCatcher`: без него сессия ждёт ~10 с, запись 1); запуск dev-стека — ручная проверка (строка AC-3 ниже), стек пользователя не трогался |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённые термины дельты — `Smtp__*` и `App__PublicBaseUrl` в `launchSettings.json`; в `src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений об их наличии в профиле запуска нет (упоминания `Smtp__*` — настройки тестового хоста и `appsettings.Development.json`). Дефекты памяти записи 1 (`D-1`, `D-2`) исправлены в 1798700.

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
- Scope: full (запись 2; предохранитель: дельта — конфигурация `src/Competency.Api/` и `appsettings`, класс общей инфраструктуры, поэтому выборка не применялась; повтор полного прогона обязателен и для повторного входа). Предстратегический прогон = этот же: дельта не менялась между ним и итогом
- Result: pass — backend 157 passed / 0 failed / 0 skipped (exit 0), web 11 файлов, 82 passed / 0 failed (exit 0)
- Lint / build: pass — `npm --prefix web run lint` exit 0, `npm --prefix web run check:api` exit 0, `dotnet build Competency.slnx --tl:off -c Release` 0 предупреждений 0 ошибок, `npm --prefix web run build` exit 0
- HEAD: 4700f9990d7dfdb6b11f980d472d247b87a8c68d

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
