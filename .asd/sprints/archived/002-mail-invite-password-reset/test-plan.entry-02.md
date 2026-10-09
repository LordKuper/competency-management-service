# Test plan — sprint 002-mail-invite-password-reset, entry 2 (rotated)

Повествовательные строки записи 2, ротация по `artifact-layout.md` "Test plan". Не редактируется.

## Risk → check decisions

Запись 2 (delta с 1cdc829): суперсединг строки записи 1 про `deploy/dev/docker-compose.yml`, `launchSettings.json`.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `src/Competency.Api/appsettings.Development.json` (SMTP по умолчанию → Mailpit `127.0.0.1:11025`, `None`; `App:PublicBaseUrl`), удаление `Smtp__*`/`App__PublicBaseUrl` из `launchSettings.json` (AC-17) | пользовательские секреты не перекрывают dev-умолчания; dev-хост не стартует без `Smtp__*` | — | none | декларативный JSON без ветвлений; приоритет пользовательских секретов над `appsettings.Development.json` в Development — поведение фреймворка, не проекта. Тестовый хост работает в Production с явными переменными окружения (запись 1), файл на него не влияет; проверка Development-запуска потребовала бы нового хоста в другой среде (новая инфраструктура теста, §17). Старт без значений уже покрывает `PlatformTests.Ac1_StartWithAMissingOrInvalidMailOrLinkSetting_…`; файл проходит `build` (копируется в вывод) |
| `deploy/dev/docker-compose.yml` (`MP_SMTP_DISABLE_RDNS: "true"` у Mailpit), `deploy/dev/README.md` (раздел user-secrets) (AC-17) | письмо из dev-стека ждёт ~10 с приветствия SMTP | — | none | декларативный ключ образа и проза; тот же ключ уже проверен в тестовом Mailpit (`MailCatcher`: без него сессия ждёт ~10 с, запись 1); запуск dev-стека — ручная проверка (строка AC-3), стек пользователя не трогался |

Проверка остатков (`artifact-layout.md` "Agent memory"): удалённые термины дельты — `Smtp__*` и `App__PublicBaseUrl` в `launchSettings.json`; в `src`, `tests`, `deploy`, `.claude/agent-memory/**` утверждений об их наличии в профиле запуска нет (упоминания `Smtp__*` — настройки тестового хоста и `appsettings.Development.json`). Дефекты памяти записи 1 (`D-1`, `D-2`) исправлены в 1798700.
