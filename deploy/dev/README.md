# Локальная разработка в Visual Studio

Профиль запуска `Everything` по F5 поднимает всё для работы над приложением, без самописных скриптов:

| Что | Чем запускается | Адрес |
|---|---|---|
| PostgreSQL 18.6 | compose-проект `deploy/dev/docker-compose.dcproj`, сервис `db` | `127.0.0.1:15432` |
| Перехватчик почты Mailpit 1.31.4 | тот же compose-проект, сервис `mailpit` | SMTP `127.0.0.1:11025`, письма — http://localhost:18025 |
| API | `src/Competency.Api` под отладчиком, профиль `Competency.Api` из `Properties/launchSettings.json` | http://localhost:5000 |
| Vite и браузер | `web/web.esproj` (`npm run dev`), конфигурация `localhost (Chrome)` из `web/.vscode/launch.json` | http://localhost:5173 |

Профили лежат в `Competency.slnLaunch` (корень репозитория):

- `Everything` — БД, API под отладчиком, Vite; Chrome открывается на http://localhost:5173, запросы `/api` Vite проксирует на :5000. Войти: e-mail и пароль администратора из `Bootstrap__AdminEmail` и `Bootstrap__AdminPassword` в `launchSettings.json`.
- `API` — БД и API под отладчиком, без Vite и браузера. БД входит в оба профиля: API применяет миграции при старте и без БД не работает.

Перехватчик почты поднимается в обоих профилях вместе с БД: Visual Studio запускает compose-проект командой `docker compose … up -d` без списка сервисов, то есть все сервисы `docker-compose.yml` (`DockerServiceName=db` выбирает только сервис для действия запуска).

## Требования

1. Visual Studio 2026 или Visual Studio 2022 17.14+ (решение в формате `.slnx`; многопроектные профили запуска — с 17.11). В Visual Studio Installer → «Изменить» → рабочая нагрузка «ASP.NET и разработка веб-приложений», в ней включить:
   - «Средства разработки контейнеров» (Container development tools) — нужны для `.dcproj`;
   - «Поддержка JavaScript и TypeScript» — нужна для `.esproj`.

   Отдельная нагрузка «Node.js» не требуется.
2. Docker Desktop запущен, режим Linux-контейнеров.
3. Node.js 24.x и npm в `PATH` (`engines` в `web/package.json`). Один раз выполнить `npm ci` в каталоге `web`: установка зависимостей из MSBuild отключена.
4. Порты 5000, 5173, 15432, 11025 и 18025 свободны (последние два — Mailpit, см. «Почта»). Занят 15432 (например, БД, поднятой вручную) — остановить её или задать другой порт: переменная среды `DEV_DB_PORT` до запуска Visual Studio и тот же порт в `ConnectionStrings__Default` профиля `Competency.Api`. Windows резервирует диапазоны портов (`netsh int ipv4 show excludedportrange protocol=tcp`), поэтому 5432 здесь не используется.

## Как включить профиль запуска

1. Открыть `Competency.slnx`.
2. В списке рядом с кнопкой запуска выбрать `Everything` (или `API`), нажать F5.

Edge вместо Chrome: в `Competency.slnLaunch` заменить `DebugTarget` проекта `web\web.esproj` на `localhost (Edge)`.

Если профилей в списке нет, включить Tools → Options → All Settings → Preview Features → «Enable Multi-Project Launch Profiles» (в Visual Studio 2022: Environment → Preview Features) и открыть решение заново. Профиль можно проверить и изменить через контекстное меню решения → «Configure Startup Projects…».

## Порядок запуска и ожидание БД

Visual Studio запускает проекты в порядке профиля (БД, API, Vite) и не ждёт готовности PostgreSQL. В среде `Development`, которую задаёт профиль, API до минуты повторяет подключение к БД, затем применяет миграции и создаёт первого администратора. В других средах ожидания нет: при недоступной БД процесс завершается сразу, как описано в `deploy/README.md`. Первый запуск на пустом томе дольше: PostgreSQL инициализирует кластер.

## Значения только для локальной разработки

JSON не допускает комментариев, поэтому оговорка здесь: пароли в этих файлах — тестовые, годятся только для рабочей станции. Порт БД слушает один `127.0.0.1`. `launchSettings.json` в образ и в `dotnet publish` не попадает.

| Где | Что задано |
|---|---|
| `deploy/dev/docker-compose.yml` | пользователь, пароль и имя БД (`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`) |
| `src/Competency.Api/Properties/launchSettings.json` | `ConnectionStrings__Default` с тем же паролем (`GSS Encryption Mode=Disable` — причина в `deploy/secret.template.yaml`); `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` |
| `src/Competency.Api/appsettings.Development.json` | `Smtp:*` — перехватчик без шифрования и аутентификации; `App:PublicBaseUrl` — адрес Vite |

## Почта

Вся почта приложения уходит в Mailpit, реальные письма не отправляются. Письма видны в веб-интерфейсе http://localhost:18025 и хранятся, пока работает контейнер: тома у Mailpit нет, остановка удаляет письма. Ссылки в письмах ведут на Vite (http://localhost:5173, `App:PublicBaseUrl`) и в профиле `API` без Vite не открываются.

Порты 11025 и 18025 меняются переменными среды до запуска Visual Studio: `DEV_MAIL_SMTP_PORT` (тот же порт — в `Smtp:Port` файла `appsettings.Development.json`) и `DEV_MAIL_UI_PORT`. Веб-интерфейс отвечает только на имена `localhost` и `127.0.0.1` (`MP_ALLOWED_HOSTS`, защита от DNS rebinding).

## Своя SMTP-песочница

Чтобы смотреть письма не в Mailpit, а в собственной песочнице (пример — Mailtrap Sandbox), значения `Smtp:*` из `appsettings.Development.json` переопределяются User Secrets: они лежат вне репозитория (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`) и в `Development` читаются после `appsettings.Development.json`. Логин и пароль задаются вместе.

```
dotnet user-secrets set "Smtp:Host" "sandbox.smtp.mailtrap.io" --project src/Competency.Api
dotnet user-secrets set "Smtp:Port" "587" --project src/Competency.Api
dotnet user-secrets set "Smtp:SecureSocketOptions" "StartTls" --project src/Competency.Api
dotnet user-secrets set "Smtp:UserName" "<логин из Mailtrap>" --project src/Competency.Api
dotnet user-secrets set "Smtp:Password" "<пароль из Mailtrap>" --project src/Competency.Api
```

В Visual Studio то же делает «Manage User Secrets» в контекстном меню проекта `Competency.Api`: открывается `secrets.json`, ключи — `{ "Smtp": { "Host": "…", … } }`.

Вернуться к Mailpit: `dotnet user-secrets clear --project src/Competency.Api` (удаляет все секреты проекта) или `dotnet user-secrets remove "Smtp:Host" --project src/Competency.Api` и так для каждого ключа `Smtp:*`. Секреты в репозиторий не попадают.

## База, созданная до входа по e-mail

Как войти в такую базу — в «Поведение при старте» `deploy/README.md` (обновление существующего развёртывания). Локально проще начать с чистой БД: `docker compose -f deploy/dev/docker-compose.yml down -v`.

## Остановка БД

Остановка отладки (Shift+F5) завершает API и Vite; контейнеры БД и Mailpit могут остаться запущенными. Остановить:

- в Visual Studio: View → Other Windows → Containers, контейнеры `db` и `mailpit` проекта `competency-dev` → Stop;
- из консоли: `docker compose -f deploy/dev/docker-compose.yml down` — контейнеры удаляются, данные БД остаются в томе `competency-dev-pgdata`, письма Mailpit пропадают; `down -v` удаляет и данные (следующий запуск начнёт с чистой БД).

## Независимость от Visual Studio

`dotnet build` и `dotnet test` не требуют Visual Studio и Node.js: `.dcproj` для CLI — пустой проект, `.esproj` собирается без `npm install` и `npm run build`. Проверенные команды: `dotnet build Competency.slnx`, `dotnet build tests/Competency.Tests`, `dotnet test --project tests/Competency.Tests`. Docker-образ собирается из `src/Competency.Api` и `web` и этих проектов не использует.
