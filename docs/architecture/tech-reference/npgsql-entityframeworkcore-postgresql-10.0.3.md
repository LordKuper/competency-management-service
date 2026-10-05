---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Npgsql.EntityFrameworkCore.PostgreSQL @ 10.0.3

Область: провайдер EF Core для PostgreSQL; транзитивно драйвер ADO.NET `Npgsql` 10.0.3.

## Canonical source
- Документация провайдера: https://www.npgsql.org/efcore/
- Release notes провайдера 10.0: https://www.npgsql.org/efcore/release-notes/10.0.html
- Release notes драйвера Npgsql 10.0: https://www.npgsql.org/doc/release-notes/10.0.html
- GitHub releases (API): npgsql/efcore.pg — v10.0.3 (2026-09-14: исправления отображения JSON null и определения GIN-индексов на представлениях), v10.0.2 (2026-05-27), v10.0.0 (2025-11-22); npgsql/npgsql — v10.0.3 (2026-05-27), v10.0.2 (2026-03-12), v10.0.1 (2025-12-19), v10.0.0 (2025-11-22)
- NuGet (nuspec 10.0.3): https://api.nuget.org/v3-flatcontainer/npgsql.entityframeworkcore.postgresql/10.0.3/npgsql.entityframeworkcore.postgresql.nuspec — лицензия PostgreSQL; `net10.0`; зависимости: `Microsoft.EntityFrameworkCore` и `.Relational` `[10.0.4, 11.0.0)`, `Npgsql` 10.0.3; 10.0.3 — последняя стабильная 10.x (далее только 11.0.0 preview/rc)
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — патч 10.0.3 (2026-09) новее среза знаний; в 10.x несколько поведенческих изменений (DateOnly/TimeOnly, трансляция `Contains` по массивам, TLS root CA).

## API surface used in project
- Подключение: `UseNpgsql(connectionString)` либо с явным `NpgsqlDataSource`: `var ds = new NpgsqlDataSourceBuilder(cs).Build(); services.AddDbContextPool<AppDbContext>(o => o.UseNpgsql(ds));`. Провайдер сам создаёт `NpgsqlDataSource`; не менять конфигурацию data source внутри `ConfigureDataSource()` по запросам — для вариативной конфигурации передавать внешний `NpgsqlDataSource`. `UseNpgsql(cs, o => o.SetPostgresVersion(...))` задаёт версию PG для генерации SQL.
- FTS (`russian`): `HasGeneratedTsVectorColumn(p => p.SearchVector, "russian", p => new { p.Name, p.Description })` (stored generated `tsvector`), `HasIndex(p => p.SearchVector).HasMethod("GIN")`; запросы — `EF.Functions.ToTsVector("russian", …)`, `PlainToTsQuery`, `WebSearchToTsQuery`, `.Matches(query)` (`@@`), ранжирование `tsvector.Rank(tsquery)` / `RankCoverDensity`.
- Триграммы (`pg_trgm`): `HasPostgresExtension("pg_trgm")`; `EF.Functions.TrigramsSimilarity` (`similarity`), `TrigramsWordSimilarity` (`word_similarity`), `TrigramsAreSimilar` (`%`) — класс `NpgsqlTrigramsDbFunctionsExtensions`; индекс GIN/GiST с operator class (`HasMethod("gin")` + `HasOperators("gin_trgm_ops")` — сверить сигнатуру в API reference при реализации). `ILike`, `Contains`, `StartsWith` в сочетании с trgm-индексом.
- JSON: `ComplexProperty(…).ToJson()` → `jsonb`; частичные обновления через `ExecuteUpdateAsync`.
- Идентификаторы: `Guid.CreateVersion7()` транслируется в `uuidv7()` на PG 18.
- Сырой SQL очереди (`FOR UPDATE SKIP LOCKED`) — через `FromSql`/`ExecuteSql` (встроенной поддержки блокирующих подсказок в источниках не обнаружено).
- PostgreSQL 18: `HasComputedColumnSql(...)` без `stored: true` создаёт VIRTUAL-колонку; для материализации указывать `stored: true` явно.

## Version-specific notes
- Требует EF Core ≥ 10.0.4 (в проекте 10.0.12) и .NET 10; драйвер Npgsql 10 не поддерживает .NET 6.
- Время: `timestamp with time zone` ↔ `DateTime` только с `Kind=Utc` (иначе исключение); `timestamp without time zone` — `Unspecified`/`Local`; `DateTimeOffset` — только со смещением 0; `date`/`time` по умолчанию → `DateOnly`/`TimeOnly` (возврат — `LegacyDateAndTimeResolverFactory`); переключатель `Npgsql.EnableLegacyTimestampBehavior` относится к 6.0+, статус в 10 не подтверждён.
- TLS: при заданном корневом сертификате цепочка валидируется только им (изменение 10.0); `PostgresException.BatchCommand` по умолчанию `null`; ошибки разрешения имени оборачиваются в `NpgsqlException`; трассировка и метрики приведены к конвенциям OpenTelemetry (`db.client.operation.duration`); `RequireAuth` в строке подключения ограничивает методы аутентификации; поддержка `PGAPPNAME`.
- UUIDv7 (`uuidv7()`) и виртуальные generated-колонки провайдер использует только при таргете PG 18.

## Deprecations and breaking changes from prior version (9 → 10)
- `arrayColumn.Contains(element)` теперь транслируется в `element = ANY(arrayColumn)` вместо `@> ARRAY[element]`; для прежнего плана — GIN-индекс `HasIndex(i => i.ArrayColumn).HasMethod("gin")`.
- `cidr` ↔ `IPNetwork` вместо `NpgsqlCidr` (`EF.Functions.Network()`/`Merge()` возвращают `IPNetwork`).
- Драйвер: `date`/`time` → `DateOnly`/`TimeOnly`; `NpgsqlCopyTextReader` вместо `TextWriter`; `DataTypeName` приоритетнее `NpgsqlDbType`; `EnableJsonTypes()` для mutable JSON в `NpgsqlSlimDataSourceBuilder`; смена имён трассировок/метрик; `Timeout` COPY — `InfiniteTimeSpan` = бесконечно.

## Project conventions
- Все даты/время — UTC, `DateTime` с `Kind=Utc` на границе записи; `DateTimeOffset` только с offset 0.
- Расширения PostgreSQL и FTS-колонки/индексы — только через модель EF и миграции; конфигурация `russian`.
- Конфигурация подключения — настраиваемая строка (внешняя БД позже), секрет из Kubernetes Secret; значения параметров SQL в логи не попадают.
- Версию PG для генерации SQL фиксировать явно (18), если провайдер не определит её сам.

## Known issues and workarounds
- v10.0.3 исправляет: отображение JSON null и определение GIN-индексов на представлениях (актуально при scaffold/reverse-engineering; проект работает code-first).
- Поведение FTS/`pg_trgm` на кириллице и последствия смены провайдера коллации в PG 18 — см. `sql-postgresql-18.6.md`; в документации провайдера триграммы в разделе FTS не описаны (функции — в разделе translations).
- Npgsql рекомендует NodaTime для работы со временем; в стек не входит (без новых зависимостей), используется BCL-типы с соблюдением правил выше.
