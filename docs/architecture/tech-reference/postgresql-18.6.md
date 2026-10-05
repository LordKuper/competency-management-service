# PostgreSQL (СУБД, сервер) @ 18.6

Область: сервер PostgreSQL 18.6 как компонент рантайма — версия и поддержка, релиз 18.6, свойства кластера (инициализация, локаль), эксплуатация в workload `db`. SQL-диалект и функции (FTS `russian`, `pg_trgm`, `SKIP LOCKED`, миграции EF Core) — `sql-postgresql-18.6.md`; образ и раскладка тома — `postgres-image-18.6-trixie.md`.

## Canonical source
- Official docs: https://www.postgresql.org/docs/18/ ; завершение работы сервера: https://www.postgresql.org/docs/18/server-shutdown.html ; `default_text_search_config`: https://www.postgresql.org/docs/18/runtime-config-client.html
- Release notes 18.6: https://www.postgresql.org/docs/release/18.6/ ; release notes 18: https://www.postgresql.org/docs/18/release-18.html
- Политика версий: https://www.postgresql.org/support/versioning/ (18: релиз 2025-09-25, EOL 2030-11-14, актуальная минорная — 18.6)
- Last verified: 2026-10-05

## API surface used in project
- Сервер — отдельная stateful-нагрузка `db` (одна реплика, PVC); `app` подключается по настраиваемой строке подключения (TCP 5432, клиент Npgsql); путь к внешней БД позже — сменой строки подключения.
- Параметры сервера — аргументами запуска (`-c name=value`, образ передаёт их в `postgres`); значения для проекта не определены (design); example Kubernetes: `args: ["postgres", "-c", "<param>=<value>"]`.
- Свойства кластера, фиксируемые при `initdb` (выполняется при пустом каталоге данных; передаются через `POSTGRES_INITDB_ARGS`): локаль и провайдер collation, контрольные суммы данных (в 18 включены по умолчанию, `--no-data-checksums` отключает).
- Расширение `pg_trgm` — contrib-модуль, «trusted» (ставится не суперпользователем с `CREATE` в БД); подключается миграциями.
- Проверка готовности: клиентская утилита `pg_isready` в образе; Testcontainers использует `pg_isready --host localhost --dbname <db> --username <user>` — тот же вызов годится для exec-проб (решение о пробах — design).
- Завершение работы (PostgreSQL docs): `SIGTERM` — smart shutdown (запрещает новые подключения, ждёт завершения всех сессий); `SIGINT` — fast shutdown (прерывает транзакции, затем останавливается); `SIGQUIT` — immediate (без штатного shutdown, при старте — recovery по WAL; допускается только аварийно).

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — мажор 18 известен; минорные 18.1–18.6 (включая пропущенную 18.5) прочитаны; changelog 18.6 и раздел миграции 18 проверены по первичным источникам.
- 18.6 (2026-08-13) — bugfix + security-релиз; **18.5 не выпускался** (post-wrap регрессия); обновление с любой 18.x без dump/restore. Исправлено 15 CVE (список в release notes); для проекта заметны: CVE-2026-14662 (документированные лимиты длины `tsvector`/`tsquery` теперь принудительны), CVE-2026-6471 (допустимые плагины логического декодирования — `output_plugin_libraries`, по умолчанию `pgoutput`, `test_decoding`; репликация в проекте не используется), правки `pg_trgm` (GiST picksplit), гонки в `SERIALIZABLE`, tzdata 2026c.
- После обновления с любой 18.x release notes требуют проверить `reltuples` таблиц с GIN-индексами (запрос в release notes); `btree_gist` (float/bit) и `ltree` (>~14 653 меток) — возможна переиндексация; при обновлении с версий <18.2 — читать migration notes 18.2. Для нового кластера не актуально, но важно при следующих минорных обновлениях.
- Поддержка: 18.x — до 2030-11-14; минорные релизы — не реже одного раза в три месяца (stack.html: цикл патчей PostgreSQL — не реже раза в квартал); сообщество рекомендует всегда работать на текущем миноре.
- Новое в 18 (по release notes): асинхронный ввод-вывод (AIO), skip scan для составных B-tree, `uuidv7()`, виртуальные generated columns (по умолчанию), `OLD`/`NEW` в `RETURNING`, OAuth-аутентификация.

## Deprecations and breaking changes from prior version
- 17 → 18 (для справки; кластер создаётся с нуля): `initdb` включает контрольные суммы по умолчанию; MD5-пароли deprecated (предупреждения `CREATE`/`ALTER ROLE`, `md5_password_warnings`); FTS и `pg_trgm` используют провайдер collation кластера по умолчанию, а не всегда libc (после апгрейда — REINDEX FTS-/`pg_trgm`-индексов); `VACUUM`/`ANALYZE` обрабатывают наследников по умолчанию (`ONLY` — прежнее поведение); `COPY FROM` CSV не считает `\.` концом данных; `AFTER`-триггеры выполняются от роли, активной при постановке события в очередь; удалены права на правила в `GRANT`/`REVOKE`; запрещены unlogged partitioned tables; `pg_backend_memory_contexts`: убрана колонка `parent`, `level` — с единицы; приоритет сокращений часовых поясов сессии над `timezone_abbreviations`.
- Подробности по SQL-функциям и миграциям 17 → 18 — `sql-postgresql-18.6.md`.

## Project conventions
- Реальный PostgreSQL 18.6 (образ `postgres:18.6-trixie`) — и в `db`, и в интеграционных тестах (`testcontainers-postgresql-4.15.0.md`).
- Одна реплика; масштабирование и HA не заданы (stack.html). Резервное копирование и RPO/RTO — открытый вопрос Q5 (design); способ не выбран.
- Очереди и outbox — в той же транзакционной БД, без брокера; значения параметров SQL и ПДн не логируются; шифрование данных в покое — на уровне инфраструктуры (stack.html).
- Обновление минорных версий — пересборка/подмена образа по регламенту stack.html (новый digest), версия меняется только через спринт.
- Завершение пода: образ задаёт `STOPSIGNAL SIGINT` (fast shutdown); `terminationGracePeriodSeconds` и сигнал согласовать в design (`postgres-image-18.6-trixie.md`).

## Known issues and workarounds
- Локаль кластера в образе — `en_US.utf8` (единственная сгенерированная). По docs `default_text_search_config` инициализируется под `lc_ctype` кластера «если конфигурация, соответствующая локали, определена» — для `en_US` ожидается `english`; проверить `SHOW default_text_search_config`; русский FTS — с явным `'russian'`.
- Русская collation без доработки образа — открытый вопрос design: проверить на spike наличие ICU-collation (`SELECT collname FROM pg_collation WHERE collname LIKE 'ru%'`) и применимость `--locale-provider=icu --icu-locale=ru-RU` к Debian-образу (пример README — для Alpine). Локаль меняется только пересозданием кластера.
- `pg_trgm` входит в пакет PostgreSQL на Debian (файл `pg_trgm.control` присутствует в Debian-пакете `postgresql-17`); для PGDG `postgresql-18` в образе прямой проверки не было — подтвердить `CREATE EXTENSION pg_trgm` первым интеграционным тестом.
- Принудительное завершение (`SIGKILL` по истечении grace period) обходит штатный shutdown и приводит к recovery по WAL при следующем старте (вывод из описания `SIGQUIT` в docs) — недопустимо как штатный режим остановки.
