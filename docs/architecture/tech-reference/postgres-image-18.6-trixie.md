# postgres (официальный Docker-образ) @ 18.6-trixie

## Canonical source
- Official docs (README образа): https://github.com/docker-library/docs/blob/master/postgres/README.md ; Docker Hub: https://hub.docker.com/_/postgres
- Dockerfile и entrypoint: https://github.com/docker-library/postgres/blob/master/18/trixie/Dockerfile , https://github.com/docker-library/postgres/blob/master/18/trixie/docker-entrypoint.sh
- Смена PGDATA/VOLUME в 18: https://github.com/docker-library/postgres/issues/1370
- Docker Hub API (тег и платформы): https://hub.docker.com/v2/repositories/library/postgres/tags/18.6-trixie — тег пересобран 2026-09-24, linux/amd64 присутствует
- Last verified: 2026-10-05

## API surface used in project
- Образ `postgres:18.6-trixie` (теги `18.6`, `18`, `latest`, `18.6-trixie`, `18-trixie`, `trixie` — один и тот же образ, README); в манифестах закрепляется по digest (`@sha256:…`); образ не пересобирается и не дорабатывается.
- Содержимое: `debian:trixie-slim` (Debian 13), PostgreSQL `18.6-1.pgdg13+2` из репозитория PGDG, `gosu 1.19`, `postgresql-18-jit` (если доступен), `libnss-wrapper`, локаль `en_US.UTF-8` (`ENV LANG en_US.utf8`); пользователь `postgres` (uid/gid 999, домашний каталог `/var/lib/postgresql`); `EXPOSE 5432`; `ENTRYPOINT docker-entrypoint.sh`, `CMD ["postgres"]`; `STOPSIGNAL SIGINT`; `listen_addresses = '*'` в `postgresql.conf.sample`.
- Данные: `ENV PGDATA /var/lib/postgresql/18/docker`, `VOLUME /var/lib/postgresql`.
- Переменные окружения: `POSTGRES_PASSWORD` (обязателен, непустой, иначе ошибка при инициализации пустого каталога — кроме `POSTGRES_HOST_AUTH_METHOD=trust`), `POSTGRES_USER` (создаётся суперпользователем), `POSTGRES_DB`, `POSTGRES_INITDB_ARGS`, `POSTGRES_HOST_AUTH_METHOD`, `PGDATA`; суффикс `_FILE` для секретов (например `POSTGRES_PASSWORD_FILE`).
- Параметры сервера: всё, что передано как аргументы, уходит в `postgres` (`-c name=value`); example (Kubernetes `args` заменяет `CMD` целиком — первым писать `postgres`): `args: ["postgres", "-c", "shared_buffers=256MB"]`.
- Скрипты `/docker-entrypoint-initdb.d` выполняются от `postgres`, в порядке имён и **только при пустом каталоге данных**.
- Kubernetes (топология стека: workload `db`, одна реплика, PVC): том монтируется в `/var/lib/postgresql`; example: `volumeMounts: [{ name: pgdata, mountPath: /var/lib/postgresql }]`. Пароль — из Secret (`POSTGRES_PASSWORD_FILE` на смонтированный Secret либо `POSTGRES_PASSWORD` из `secretKeyRef`). Пробы готовности/живости — вместо Docker healthcheck (в образе `HEALTHCHECK` нет); `pg_isready` в образе доступен (его использует wait-стратегия Testcontainers).

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — раскладка данных образа изменилась в 18 относительно ≤17 (PGDATA, VOLUME); для проекта критично при монтировании PVC. README и исходники Dockerfile/entrypoint прочитаны.
- README: «PGDATA стал версионно-зависимым в PostgreSQL 18 и выше», «объявленный VOLUME изменён на `/var/lib/postgresql`» — чтобы при обновлении между мажорами работал быстрый `pg_upgrade --link`.
- Следствие (вывод из ENV, не из README): данные кластера лежат в подкаталоге `18/docker`, а не в корне тома, поэтому служебный `lost+found` в корне PVC не конфликтует с `initdb`.
- Entrypoint (при запуске от root): создаёт `PGDATA` (`chmod 00700`), делает `chown postgres`, затем перезапускает себя через `gosu postgres`. При запуске **не от root** (`runAsUser: 999`) `chown` пропускается — каталог тома должен быть доступен на запись пользователю 999 (`fsGroup`/владелец тома — решение design).
- `initdb` и «arbitrary --user»: пользователь должен существовать в `/etc/passwd` (README) — для Kubernetes использовать uid 999 (`postgres`).
- `/dev/shm` в контейнере по умолчанию 64 МБ; при исчерпании — `No space left on device` (README: `--shm-size`). В Kubernetes типовой приём — `emptyDir` с `medium: Memory`, смонтированный в `/dev/shm` (приём Kubernetes, в README образа не описан).
- Локаль: `LANG=en_US.utf8`; других локалей в образе нет (в Dockerfile сгенерирована только `en_US.UTF-8`). README для Debian-вариантов предлагает собственный Dockerfile с `localedef`; для ICU — `POSTGRES_INITDB_ARGS` вида `--locale-provider=icu --icu-locale=de-DE` (пример README приведён для Alpine-вариантов). Русская collation при неизменённом образе — открытый вопрос design (см. `postgresql-18.6.md`).
- Тег `18.6-trixie` пересобирается при обновлении базового слоя, не меняя версию PostgreSQL — поэтому закрепление по digest и ежемесячный цикл пересборки (stack.html).

## Deprecations and breaking changes from prior version
- ≤17 → 18: `PGDATA` `/var/lib/postgresql/data` → `/var/lib/postgresql/18/docker`; `VOLUME` `/var/lib/postgresql/data` → `/var/lib/postgresql`. Миграция: монтировать том в `/var/lib/postgresql` (не в `/var/lib/postgresql/data`).
- Entrypoint 18 обнаруживает `PG_VERSION` в прежнем расположении (`/var/lib/postgresql/data`) и останавливается с сообщением о том, что это «usually the result of upgrading the Docker image without upgrading the underlying database using pg_upgrade», и рекомендует монтировать в `/var/lib/postgresql`.
- Issue docker-library/postgres#1370 (открыт на момент проверки): монтирование тома в `/var/lib/postgresql/data` на образах 18 ломает инструменты/чарты с прежним путём; симптом, который там описан, — ошибка монтирования (`error mounting … to rootfs at /var/lib/postgresql/data … no such file or directory`) в Docker.
- Варианты 18.6: `trixie` (Debian 13, выбран проектом), `bookworm`, `alpine3.24`, `alpine3.23` (README). Базовые ОС образов проекта различаются: `db` — Debian 13, `app` — Ubuntu 24.04 (для .NET 10 Debian-образы не выпускаются, `dotnet-aspnet-image-10.0.12.md`).

## Project conventions
- Workload `db` — одна реплика, PVC; `app` подключается по настраиваемой строке подключения (путь к внешней БД позже). Тип ресурса (StatefulSet/Deployment), Service, `fsGroup` — в design.
- PVC монтируется **только** в `/var/lib/postgresql`; `PGDATA` не переопределять. Монтирование в `/var/lib/postgresql/data` на образе 18 недопустимо: `PGDATA` остаётся `/var/lib/postgresql/18/docker`, и данные оказываются вне тома (следствие из ENV; на Docker — ошибка из issue #1370).
- Образ собирается/забирается на сборочном хосте вне контура (с интернетом), в контур доставляется готовым (registry либо tar-импорт на узлы, Q11); целевая платформа linux/amd64 (Q10); перед переносом — сканирование и SBOM; версии меняются только через спринт.
- Образ тестов Testcontainers — тот же тег (`testcontainers-postgresql-4.15.0.md`).
- Секреты — только из Kubernetes Secrets, не в образе и не в репозитории (stack.html).

## Known issues and workarounds
- `STOPSIGNAL SIGINT` (быстрое выключение PostgreSQL): образ-`STOPSIGNAL` учитывают containerd и CRI-O (Kubernetes blog, v1.33); при runtime без поддержки Kubernetes пошлёт `SIGTERM` (smart shutdown — ждёт отключения клиентов) → возможен `SIGKILL` по `terminationGracePeriodSeconds` и crash recovery. Проверить при развёртывании; для явного сигнала в Kubernetes есть `lifecycle.stopSignal` (feature `ContainerStopSignals`, v1.33+).
- Запуск не от root без права записи в том (`fsGroup`) → `initdb` не создаст `PGDATA`; запуск от root — entrypoint сам делает `chown` и сбрасывает привилегии.
- Порядок запуска `app`/`db` Kubernetes не гарантирует: `app` ждёт готовности БД (повторы/проба) — детали в design.
- Образ не содержит дополнительных расширений кроме поставляемых пакетом PostgreSQL; расширения Debian-вариантов — установкой пакетов в собственном образе (README), что противоречит «образ без доработок» — см. `postgresql-18.6.md` про `pg_trgm`.
