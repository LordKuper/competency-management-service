---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# SQL (диалект PostgreSQL) @ 18.6

Область: SQL-диалект и функции PostgreSQL 18, которыми пользуется проект (FTS, `pg_trgm`, очереди `SKIP LOCKED`). Образ/эксплуатация `db` (workload, PVC, probes) — в `postgresql-18.6.md` и `postgres-image-18.6-trixie.md`.

## Canonical source
- Документация PG 18: https://www.postgresql.org/docs/18/
- Release notes 18.6 (выпуск 2026-08-13): https://www.postgresql.org/docs/release/18.6/
- Release notes 18 (миграция с 17): https://www.postgresql.org/docs/18/release-18.html
- Версионирование и EOL: https://www.postgresql.org/support/versioning/ — 18.6 текущий минор; 18 GA 2025-09-25, конец поддержки 2030-11-14
- Официальный Docker-образ: https://github.com/docker-library/docs/blob/master/postgres/README.md
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — мажор 18 вышел 2025-09, минор 18.6 (2026-08) новее среза знаний; в PG 18 есть несовместимости, затрагивающие FTS и `pg_trgm` (см. ниже).

## API surface used in project
- Полнотекстовый поиск, конфигурация `russian` (`pg_catalog.russian`, Snowball; присутствует в стандартной поставке — пример `\dF russian` в документации psql):
  `to_tsvector('russian', name || ' ' || description)`, `websearch_to_tsquery('russian', :q)` / `plainto_tsquery`, оператор `@@`, `ts_rank`; индекс GIN по `tsvector`. Из EF Core — через Npgsql (`HasGeneratedTsVectorColumn`, `Matches`), см. `npgsql-entityframeworkcore-postgresql-10.0.3.md`.
- `pg_trgm`: `CREATE EXTENSION pg_trgm;` операторы/функции `%`, `similarity()`, `word_similarity()`; индексы `USING GIN (col gin_trgm_ops)` (поддерживают `LIKE`/`ILIKE`/`~`/`~*`) и `gist_trgm_ops` (быстрее для `ORDER BY ... <->  LIMIT`); пороги по умолчанию: `pg_trgm.similarity_threshold` 0.3, `word_similarity_threshold` 0.6, `strict_word_similarity_threshold` 0.5; сравнение регистронезависимое в стандартной сборке.
- Очереди/outbox: `SELECT … FROM job WHERE status = 'pending' ORDER BY id LIMIT :n FOR UPDATE SKIP LOCKED` в той же транзакции, что и фиксация результата. Синтаксис: `FOR lock_strength [OF …] [NOWAIT | SKIP LOCKED]`.
- Доступные в PG 18 (используются только при явном решении design): `uuidv7()` (+ алиас `uuidv4()`); `RETURNING OLD/NEW` для INSERT/UPDATE/DELETE/MERGE; виртуальные generated-колонки (`GENERATED ALWAYS AS (...) VIRTUAL`, теперь значение по умолчанию); skip scan для многоколоночных B-tree.
- Сырой SQL из приложения — только параметризованный (`FromSql`/`ExecuteSql` с интерполяцией EF Core), без конкатенации значений.

## Version-specific notes
- FTS и `pg_trgm` (PG 18, формулировка release notes): «Change full text search to use the default collation provider of the cluster to read configuration files and dictionaries, rather than always using libc». Для кластеров с не-libc провайдером по умолчанию (ICU, builtin) поведение некоторых FTS-функций и `pg_trgm` может измениться; после `pg_upgrade` таких кластеров рекомендуется переиндексировать индексы FTS и `pg_trgm`. Официальный образ по умолчанию инициализируется с локалью `en_US.utf8`; русская сортировка/ICU (`POSTGRES_INITDB_ARGS="--locale-provider=icu --icu-locale=ru-RU"` показан в README для Alpine; для образа Debian доступность ICU-локалей проверять spike'ом в `pg_collation`) — открытый пункт design (`stack.html`).
- 18.6 (2026-08-13) — минор с исправлениями безопасности (в числе затронутых компонентов: ограничение длины `tsvector`/`tsquery`, `contrib/pg_trgm` GiST, `to_char()`, regexp-функции; полный список CVE — в release notes). Требуемые действия после обновления с любого 18.x по release notes: проверить `reltuples` у GIN-индексов (запрос в release notes), при использовании `btree_gist`/`ltree` — возможна переиндексация; при обновлении с версий < 18.2 — дополнительные шаги (раздел E.4 release notes).
- Docker-образ PG 18: `PGDATA=/var/lib/postgresql/18/docker`, объявленный `VOLUME` — `/var/lib/postgresql` (изменено относительно ≤17); PVC монтировать в `/var/lib/postgresql`. Для `--user` с произвольным UID `initdb` требует записи пользователя в `/etc/passwd` (варианты: `nss_wrapper`, bind-mount `/etc/passwd`, предварительный `chown` тома). Секреты: суффикс `_FILE` (`POSTGRES_PASSWORD_FILE`).
- Минорные релизы выходят не реже раза в 3 месяца; сообщество рекомендует всегда работать на текущем миноре (обновление без dump/restore) — синхронизировано с регламентом патчей в `stack.html`.
- Виртуальные generated-колонки (PG 18, по умолчанию `VIRTUAL`): выражение только из встроенных иммутабельных функций и типов, без пользовательских типов/функций, без ссылок на другие generated-колонки, без default/identity. Для tsvector под GIN Npgsql создаёт STORED-колонку (документация Npgsql).
- `SKIP LOCKED`: даёт «несогласованный» срез данных — подходит для очередей, не для общих запросов; блокировка строк, но `ROW SHARE` на таблицу берётся как обычно; `ORDER BY` + блокирующее предложение на `READ COMMITTED` может вернуть строки не по порядку (обход из документации — подзапрос с `FOR UPDATE`, но он блокирует все строки).

## Deprecations and breaking changes from prior version (PG 17 → 18)
- `initdb` включает контрольные суммы данных по умолчанию (`--no-data-checksums` отключает; `pg_upgrade` требует совпадения настроек).
- FTS/`pg_trgm` читают конфигурацию и словари через провайдер коллации кластера по умолчанию (см. выше).
- MD5-пароли объявлены устаревшими (предупреждение при `CREATE/ALTER ROLE`; `md5_password_warnings`) — использовать SCRAM.
- `VACUUM`/`ANALYZE` по умолчанию обрабатывают потомков наследования (`ONLY` — прежнее поведение).
- `COPY FROM` в CSV больше не трактует `\.` как конец данных (`\.` должна стоять отдельной строкой); старые клиенты psql могут давать проблемы с `\copy`.
- Запрещены unlogged-партиционированные таблицы; AFTER-триггеры выполняются от роли, поставившей событие в очередь; убраны неработающие привилегии на rules; `pg_backend_memory_contexts`: убрана колонка `parent`, `level` — с единицы; приоритет сокращений часовых поясов сессии над `timezone_abbreviations`.
- Generated-колонки по умолчанию виртуальные (раньше всегда stored).

## Project conventions
- SQL — через EF Core; сырой SQL точечно (FTS-запросы, claim-запрос очереди `SKIP LOCKED`), всегда параметризованный; значения параметров и PII не логируются.
- Схема и расширения (`pg_trgm`) создаются миграциями EF Core (`HasPostgresExtension`), не ручным DDL; применяет их само приложение при старте (см. `efcore-10.0.12.md`).
- Очередь/outbox в той же БД: claim и результат — в одной транзакции; обработчики идемпотентны; параметры lease/повторов — design.
- Время хранить в `timestamptz` (UTC); кластер — UTF-8; локаль/collation для русской сортировки фиксируется при `initdb` (design, открытый пункт `stack.html`).
- Целевая версия функций — PG 18; приложение не должно требовать функций новее 18.x.

## Known issues and workarounds
- Поведение FTS/`pg_trgm` для кириллицы (регистр, `ё`/`е`, морфология `russian`) в документации PG не расписано — проверить на тестовых данных проекта до фиксации индексов; после смены локали/провайдера коллации — `REINDEX` FTS/trgm-индексов.
- PVC монтируется **только** в `/var/lib/postgresql` (на образах PG 18 `VOLUME` изменён, `PGDATA=/var/lib/postgresql/18/docker`, README docker-library). При монтировании в иной путь (в т.ч. прежний `/var/lib/postgresql/data` или `/data`) кластер создаётся вне PVC — в записываемом слое контейнера — и теряется при пересоздании pod'а (следствие из ENV; ошибка монтирования из docker-library/postgres#1370 — симптом Docker, не режим отказа Kubernetes).
