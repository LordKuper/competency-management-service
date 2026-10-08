# Сборка и развёртывание

Образы собираются вне контура, на машине с интернетом, и переносятся в контур готовыми: запущенный сервис ничего не скачивает. Манифесты — plain YAML (без Kustomize и Helm). Ingress и TLS манифесты не задают.

| Файл | Назначение |
|---|---|
| `Dockerfile`, `.dockerignore` (корень репозитория) | образ `app`: SPA (с шрифтами PT) и API, непривилегированный пользователь `app` (UID 1654), порт 8080 |
| `deploy/k8s/configmap.yaml` | несекретная конфигурация `app` и `db` |
| `deploy/k8s/db.yaml` | Service и StatefulSet `db` на официальном образе `postgres:18.6-trixie` (не пересобирается) |
| `deploy/k8s/app.yaml` | Service, PVC ключей DataProtection и Deployment `app` |
| `deploy/secret.template.yaml` | шаблон Secret с плейсхолдерами; лежит выше `deploy/k8s`, чтобы `kubectl apply -f deploy/k8s/` не создал Secret с плейсхолдерами |

## 1. Сборка образов (вне контура)

Нужен Docker с BuildKit. Образ платформы `linux/amd64` получается на любом хосте.

1. Определить digest базовых образов (строка `Digest:` в выводе):

   ```
   docker buildx imagetools inspect node:24.21.0-trixie-slim
   docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0.401
   docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0.12-noble
   docker buildx imagetools inspect postgres:18.6-trixie
   ```

2. Собрать `app` из корня репозитория, подставив digest вместо `<DIGEST>`:

   ```
   docker build \
     --build-arg NODE_IMAGE=node:24.21.0-trixie-slim@sha256:<DIGEST> \
     --build-arg SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:<DIGEST> \
     --build-arg RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.12-noble@sha256:<DIGEST> \
     -t competency-app:<VERSION> .
   ```

   Без `--build-arg` берутся теги — только для локальных проверок. Версии SDK и runtime меняются парой (`10.0.401` ↔ `10.0.12`), цикл патчей .NET — ежемесячно.

3. Просканировать `competency-app` и `postgres` на уязвимости, сформировать SBOM (инструменты стеком не заданы) и приложить результаты к поставке.

## 2. Перенос в контур

- Есть внутренний registry: `docker push`, узлы забирают образы сами.
- Нет registry: `docker save -o competency-images.tar competency-app:<VERSION> postgres:18.6-trixie`, перенос архива, импорт на каждом узле, где может запуститься под (для containerd — `ctr -n k8s.io images import competency-images.tar`). `imagePullPolicy: IfNotPresent` — pull не требуется.

Digest в манифестах: `postgres:18.6-trixie@sha256:<DIGEST>` в `db.yaml` и `competency-app:<VERSION>@sha256:<DIGEST>` в `app.yaml`. Digest у собранного локально образа появляется после `docker push`; при tar-доставке соответствие digest после импорта не проверено — сверить на узле (`crictl images --digests`), а если digest недоступен, оставить ссылку по тегу без `@sha256:<DIGEST>`.

## 3. Применение

Реальный Secret создаёт оператор; значения в репозиторий не попадают.

```
kubectl create namespace competency
cp deploy/secret.template.yaml <путь вне репозитория>/competency-secret.yaml
# заполнить плейсхолдеры, затем:
kubectl -n competency apply -f <путь вне репозитория>/competency-secret.yaml
# заполнить плейсхолдеры в deploy/k8s/configmap.yaml (почта и публичный адрес), затем:
kubectl -n competency apply -f deploy/k8s/
kubectl -n competency rollout status statefulset/db
kubectl -n competency rollout status deployment/app
```

Поля Secret `competency-secret`:

- `POSTGRES_PASSWORD` — пароль пользователя БД; `ConnectionStrings__Default` — строка подключения `app` с тем же паролем (без `;` и кавычек в пароле); `Database` и `Username` совпадают с `db-config`; `GSS Encryption Mode=Disable` из шаблона оставить (причина — в комментарии шаблона). Пароль применяется при первой инициализации тома: позже Secret его не меняет.
- `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` — первый глобальный администратор: его e-mail и пароль задаются при развёртывании так же, как пароль БД, и в репозиторий не попадают. E-mail одновременно служит именем для входа (отдельного имени пользователя нет), должен быть корректным адресом не длиннее 254 символов. Пароль подчиняется политике Identity: не короче 10 символов, цифра, строчная и заглавная буква, неалфавитно-цифровой символ.

- `Smtp__UserName`, `Smtp__Password` — учётная запись SMTP relay. Оба ключа задаются вместе; если relay принимает почту без аутентификации, оба удаляются из Secret (`optional: true`).

Почта и публичный адрес в `app-config` (`deploy/k8s/configmap.yaml`):

- `App__PublicBaseUrl` — адрес, по которому пользователи открывают приложение (например, `https://competency.corp.example`); с него начинаются все ссылки в письмах, заголовок `Host` запроса на них не влияет.
- `Smtp__Host`, `Smtp__Port` — корпоративный SMTP relay; `Smtp__From` — адрес отправителя, можно с именем: `Имя <адрес>`.
- `Smtp__SecureSocketOptions` — защита соединения: `StartTls` (порт 587, значение по умолчанию), `SslOnConnect` (порт 465), `StartTlsWhenAvailable`, `Auto`, `None`. `None` и `StartTlsWhenAvailable` допускают отправку без шифрования — только для перехватчика почты в разработке.
- `Smtp__Timeout` — предел одной отправки (по умолчанию `00:00:15`): письма по действиям администратора уходят сразу после сохранения, и его запрос ждёт отправку не дольше этого времени.
- Необязательные ключи (значения по умолчанию заданы в образе): `AccountLinks__InvitationLifetime` — срок ссылки-приглашения (`7.00:00:00`); `AccountLinks__PasswordResetLifetime` — срок ссылки сброса пароля (`01:00:00`); `AccountLinks__PasswordResetInterval` — не чаще какого интервала запрос «Не помню пароль» выдаёт учётной записи новую ссылку (`00:05:00`, более частые запросы молча пропускаются); `RateLimiting__PasswordReset__PermitLimit` и `RateLimiting__PasswordReset__WindowSeconds` — лимит запросов с одного адреса (`10` за `60` с), общий для «Не помню пароль», сброса пароля по ссылке и принятия приглашения.

Доверие внутреннему CA (шаг развёртывания): если сертификат relay выпущен внутренним CA, образ ему не доверяет, и отправка по `StartTls`/`SslOnConnect` завершается ошибкой TLS (`SslHandshakeException` в журнале). Смонтировать в под файл PEM, в котором системные корневые сертификаты дополнены сертификатом внутреннего CA (например, из ConfigMap), и указать путь к нему в переменной `SSL_CERT_FILE` контейнера `app`. Отключать проверку сертификата нельзя. После развёртывания проверить отправку: например, отправить приглашение на свой адрес.

Доступ без Ingress, для проверки: `kubectl -n competency port-forward svc/app 8080:8080` и `curl http://localhost:8080/healthz/ready`.

## 4. Поведение при старте

- `app` применяет схему сам (`MigrateAsync`) учётной записью из `ConnectionStrings__Default`; ей нужны DDL-права, расширение `pg_trgm` создаёт миграция. Порт открывается после миграций, поэтому `startupProbe` ждёт их; liveness — `/healthz/live`, readiness — `/healthz/ready` (проверяет БД).
- До миграций `app` проверяет настройки почты и публичного адреса: не задан или неверен `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__Timeout`, `App__PublicBaseUrl` (не абсолютный http(s) URL) или задан только один из `Smtp__UserName`/`Smtp__Password` — старт завершается ошибкой, в которой названы неверные ключи (логин и пароль SMTP в журнал не попадают). Доступность relay при старте не проверяется: отказ отправки виден только при отправке письма. Существующее развёртывание при обновлении до этой версии не стартует, пока эти ключи не заполнены в `app-config`.
- Kubernetes не гарантирует порядок запуска: пока `db` не готова, процесс `app` завершается и перезапускается — на первом развёртывании возможны рестарты.
- Если активного глобального администратора нет, `app` создаёт его из `Bootstrap__AdminEmail` и `Bootstrap__AdminPassword`; существующего не перезаписывает. Нет администратора и нет одного из ключей — старт завершается ошибкой, в которой назван недостающий ключ (пароль в журнал не попадает). После первого входа ключи `Bootstrap__*` можно убрать из Secret (`optional: true`).
- Обновление существующего развёртывания: миграция `EmailOnUserAccount` добавляет учётным записям обязательный уникальный e-mail и заполняет его для уже существующих записей значением `<имя пользователя>@local.invalid` (имя пользователя для входа становится тем же адресом). Войти нужно этим адресом и прежним паролем, затем заменить его на настоящий в разделе «Пользователи». Ключ Secret `Bootstrap__AdminUserName` больше не читается и его можно удалить; `Bootstrap__AdminEmail` для существующего администратора не используется. Миграция безвозвратно удаляет табельный номер и e-mail сотрудников (e-mail сотрудника теперь берётся из его учётной записи); журнал `audit` старые значения сохраняет.
- Переход на контракт API 2.0.0 (`info.version`, путь `/api/v1` прежний): администратор больше не задаёт пароль другим учётным записям. Метод `POST /api/v1/users/{id}/reset-password` удалён — вместо него `POST /api/v1/users/{id}/send-password-reset` отправляет пользователю ссылку для сброса пароля (пароль не меняется); поле `password` в `POST /api/v1/users` больше не читается — новая учётная запись создаётся приглашённой и получает письмо-приглашение. Пароли существующих учётных записей сохраняются. Интеграции, которые задавали пароль через API, нужно перевести на эти письма. Учётные записи с адресами `@local.invalid` (после миграции `EmailOnUserAccount`) писем не получат: сначала замените им адрес на настоящий в разделе «Пользователи».
- ФИО сотрудника: миграция `EmployeeNameInThreeFields` делит прежнее ФИО по пробелам на фамилию (первое слово), имя (второе) и отчество (остальные слова). Запись из одного слова получает имя `-`, пустая — `-` в фамилии и имени: такие записи нужно исправить вручную в разделе «Оргструктура». Часть длиннее 100 символов останавливает миграцию без изменений в БД — сначала сократите такое ФИО. Старые значения в журнале `audit` не меняются.
- Ключи DataProtection лежат на PVC `app-keys` (путь `DataProtection__KeysPath`); пока том жив, cookie-сессии переживают рестарт. `strategy: Recreate`, одна реплика: том `ReadWriteOnce`, а миграции не должны идти из двух подов.
- Данные PostgreSQL — на PVC `data-db-0`, смонтированном строго в `/var/lib/postgresql`; каталог данных не переопределяется.

## 5. Проверка манифестов (Manual verification)

`kubectl apply --dry-run=client -f deploy/k8s/` и `kubectl apply --dry-run=client -f deploy/secret.template.yaml` — команде нужен доступ к API-серверу (схема и discovery), без кластера она не выполняется. Результат: все объекты `created (dry run)`, ошибок нет.

## Открытые точки

TLS, резервное копирование и доставка образов — открытые вопросы, их ведёт `docs/architecture/stack.html`.

- TLS: Ingress не задан. `Authentication__Cookie__SecurePolicy` в `app-config` равен `SameAsRequest`; когда TLS заканчивается перед приложением, поставить `Always`. Заголовки `X-Forwarded-*` приложение не разбирает: за прокси лимиты входа и сброса пароля (`RateLimiting:Login`, `RateLimiting:PasswordReset`) считаются по адресу прокси, а не клиента.
- Резервного копирования нет, манифестов бэкапа нет; PVC `data-db-0` и `app-keys` — единственные копии данных.
- Доставка образов (registry или tar) не выбрана.
- `app` подключается учётной записью `POSTGRES_USER` — это суперпользователь кластера; отдельной роли с правами только на свою БД нет. Триггеры неизменяемости журнала включены как `ENABLE ALWAYS`, поэтому срабатывают и при `SET session_replication_role = replica`, но от DDL (`DROP TRIGGER`, `DISABLE TRIGGER`) не защищают (решение спринта). Расширение `pg_trgm` помечено как trusted, и роль-владелец БД без прав суперпользователя создаёт его сама (проверено).
- `requests`/`limits`, StorageClass и NetworkPolicy не заданы — их определяет кластер; ёмкости PVC (10Gi у `db`, 1Gi у `app-keys`) — стартовые значения.
- Остановка `db`: образ задаёт `STOPSIGNAL SIGINT`; проверить, что runtime кластера его учитывает. Иначе PostgreSQL получит SIGTERM, будет ждать клиентов, и по истечении `terminationGracePeriodSeconds: 120` под получит SIGKILL с последующим crash recovery.
- Обновление: пересборка, сканирование, перенос, `kubectl apply` с новыми digest. Из-за `Recreate` `app` недоступен на время смены пода.
