# Сборка и развёртывание

Образы собираются вне контура, на машине с интернетом, и переносятся в контур готовыми: запущенный сервис ничего не скачивает. Манифесты — plain YAML (без Kustomize и Helm). Ingress и TLS манифесты не задают (открытый вопрос Q8).

| Файл | Назначение |
|---|---|
| `Dockerfile`, `.dockerignore` (корень репозитория) | образ `app`: SPA (с шрифтами PT) и API, непривилегированный пользователь `app` (UID 1654), порт 8080 |
| `deploy/k8s/configmap.yaml` | несекретная конфигурация `app` и `db` |
| `deploy/k8s/db.yaml` | Service и StatefulSet `db` на официальном образе `postgres:18.6-trixie` (не пересобирается) |
| `deploy/k8s/app.yaml` | Service, PVC ключей DataProtection и Deployment `app` |
| `deploy/secret.template.yaml` | шаблон Secret с плейсхолдерами; лежит выше `deploy/k8s`, чтобы `kubectl apply -f deploy/k8s/` не создал Secret с плейсхолдерами |

## 1. Сборка образов (вне контура)

Нужен Docker с BuildKit. Образ платформы `linux/amd64` получается на любом хосте (Q10).

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

- Есть внутренний registry (Q11): `docker push`, узлы забирают образы сами.
- Нет registry: `docker save -o competency-images.tar competency-app:<VERSION> postgres:18.6-trixie`, перенос архива, импорт на каждом узле, где может запуститься под (для containerd — `ctr -n k8s.io images import competency-images.tar`). `imagePullPolicy: IfNotPresent` — pull не требуется.

Digest в манифестах: `postgres:18.6-trixie@sha256:<DIGEST>` в `db.yaml` и `competency-app:<VERSION>@sha256:<DIGEST>` в `app.yaml`. Digest у собранного локально образа появляется после `docker push`; при tar-доставке соответствие digest после импорта не проверено (Q11) — сверить на узле (`crictl images --digests`), а если digest недоступен, оставить ссылку по тегу без `@sha256:<DIGEST>`.

## 3. Применение

Реальный Secret создаёт оператор; значения в репозиторий не попадают.

```
kubectl create namespace competency
cp deploy/secret.template.yaml <путь вне репозитория>/competency-secret.yaml
# заполнить плейсхолдеры, затем:
kubectl -n competency apply -f <путь вне репозитория>/competency-secret.yaml
kubectl -n competency apply -f deploy/k8s/
kubectl -n competency rollout status statefulset/db
kubectl -n competency rollout status deployment/app
```

Поля Secret `competency-secret`:

- `POSTGRES_PASSWORD` — пароль пользователя БД; `ConnectionStrings__Default` — строка подключения `app` с тем же паролем (без `;` и кавычек в пароле); `Database` и `Username` совпадают с `db-config`; `GSS Encryption Mode=Disable` из шаблона оставить (в образе нет библиотеки Kerberos, без ключа при каждом старте в stderr попадает строка `Cannot load library libgssapi_krb5.so.2`). Пароль применяется при первой инициализации тома: позже Secret его не меняет.
- `Bootstrap__AdminUserName`, `Bootstrap__AdminPassword` — первый глобальный администратор. Пароль подчиняется политике Identity: не короче 10 символов, цифра, строчная и заглавная буква, неалфавитно-цифровой символ.

Доступ без Ingress, для проверки: `kubectl -n competency port-forward svc/app 8080:8080` и `curl http://localhost:8080/healthz/ready`.

## 4. Поведение при старте

- `app` применяет схему сам (`MigrateAsync`) учётной записью из `ConnectionStrings__Default`; ей нужны DDL-права, расширение `pg_trgm` создаёт миграция. Порт открывается после миграций, поэтому `startupProbe` ждёт их; liveness — `/healthz/live`, readiness — `/healthz/ready` (проверяет БД).
- Kubernetes не гарантирует порядок запуска: пока `db` не готова, процесс `app` завершается и перезапускается — на первом развёртывании возможны рестарты.
- Если активного глобального администратора нет, `app` создаёт его из `Bootstrap__*`; существующего не перезаписывает. Нет администратора и нет секрета — старт завершается ошибкой. После первого входа ключи `Bootstrap__*` можно убрать из Secret (`optional: true`).
- Ключи DataProtection лежат на PVC `app-keys` (путь `DataProtection__KeysPath`); пока том жив, cookie-сессии переживают рестарт. `strategy: Recreate`, одна реплика: том `ReadWriteOnce`, а миграции не должны идти из двух подов.
- Данные PostgreSQL — на PVC `data-db-0`, смонтированном строго в `/var/lib/postgresql`; каталог данных не переопределяется.

## 5. Проверка манифестов (Manual verification)

`kubectl apply --dry-run=client -f deploy/k8s/` и `kubectl apply --dry-run=client -f deploy/secret.template.yaml` — команде нужен доступ к API-серверу (схема и discovery), без кластера она не выполняется. Результат: все объекты `created (dry run)`, ошибок нет.

## Открытые точки

- Q8, TLS: Ingress не задан. `Authentication__Cookie__SecurePolicy` в `app-config` равен `SameAsRequest`; когда TLS заканчивается перед приложением, поставить `Always`. Заголовки `X-Forwarded-*` приложение не разбирает: за прокси лимит входа (`RateLimiting:Login`) считается по адресу прокси, а не клиента.
- Q5: резервного копирования нет, манифестов бэкапа нет; PVC `data-db-0` и `app-keys` — единственные копии данных.
- Q11: доставка образов (registry или tar) не выбрана.
- `app` подключается учётной записью `POSTGRES_USER` — это суперпользователь кластера; отдельной роли с правами только на свою БД нет. Триггер неизменяемости журнала от DDL не защищает (решение спринта), а суперпользователь обходит его и без DDL (`SET session_replication_role = replica`, проверено на образе). Отдельная роль-владелец БД без прав суперпользователя закрыла бы этот обход: расширение `pg_trgm` помечено как trusted, и такая роль создаёт его сама (проверено); триггер при этом по-прежнему снимается через DDL.
- `requests`/`limits`, StorageClass и NetworkPolicy не заданы — их определяет кластер; ёмкости PVC (10Gi у `db`, 1Gi у `app-keys`) — стартовые значения.
- Остановка `db`: образ задаёт `STOPSIGNAL SIGINT`; проверить, что runtime кластера его учитывает. Иначе PostgreSQL получит SIGTERM, будет ждать клиентов, и по истечении `terminationGracePeriodSeconds: 120` под получит SIGKILL с последующим crash recovery.
- Обновление: пересборка, сканирование, перенос, `kubectl apply` с новыми digest. Из-за `Recreate` `app` недоступен на время смены пода.
