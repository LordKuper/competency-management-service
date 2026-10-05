---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Entity Framework Core @ 10.0.12

Область: `Microsoft.EntityFrameworkCore` (+ `.Relational`), design-time `Microsoft.EntityFrameworkCore.Design`, инструмент `dotnet-ef` 10.0.12 (отдельно — `dotnet-ef-10.0.12.md`). Провайдер PostgreSQL — `npgsql-entityframeworkcore-postgresql-10.0.3.md`.

## Canonical source
- What's new in EF Core 10 (страница 2026-08-05): https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew — EF10 выпущен в 2025-11, LTS до 2028-11-10, требует .NET 10 SDK/runtime
- Breaking changes EF Core 10 (обновлена 2026-07-01): https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes
- Breaking changes EF Core 9 (обновлена 2026-08-03; действуют и в 10): https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes
- Применение миграций (2026-08-05): https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
- EF tools (.NET CLI, 2026-08-11): https://learn.microsoft.com/en-us/ef/core/cli/dotnet
- NuGet: `Microsoft.EntityFrameworkCore` 10.0.12 и `dotnet-ef` 10.0.12 присутствуют в реестре (последние 10.x); `Microsoft.EntityFrameworkCore.Design` 10.0.12 — лицензия MIT, `developmentDependency`, зависимости: Microsoft.EntityFrameworkCore.Relational 10.0.12, Humanizer.Core 2.14.1, Microsoft.CodeAnalysis.CSharp(.Workspaces) 5.0.0, Microsoft.CodeAnalysis.Workspaces.MSBuild 5.0.0, Microsoft.Build.Framework 18.0.2, Mono.TextTemplating 3.0.0, Newtonsoft.Json 13.0.4, Microsoft.Extensions.* 10.0.12
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — мажор 10 (LTS) с изменениями трансляции запросов (параметризованные коллекции, имена параметров) и миграций; патч 10.0.12 новее среза знаний.

## API surface used in project
- `DbContext`, `AddDbContext`/`AddDbContextPool`; логирование значений параметров выключено (`EnableSensitiveDataLogging` не включать); в EF10 inline-константы (`EF.Constant`) в логах редактируются (`?`).
- Аудит append-only: `SaveChangesInterceptor` (`SavingChangesAsync(DbContextEventData, InterceptionResult<int>, CancellationToken)`; изменения читаются из `ChangeTracker.Entries()`, строки аудита добавляются в тот же `SaveChanges`).
- Оптимистичная блокировка: явный `int Version` как concurrency token (`Property(e => e.Version).IsConcurrencyToken()`), увеличение версии — в прикладном коде/интерцепторе; конфликт → `DbUpdateConcurrencyException`; ETag/`If-Match` на уровне HTTP.
- Запросы: `AsNoTracking`, `Include`, `AsSplitQuery` (в EF10 порядок в split-запросах согласован), LINQ `LeftJoin`/`RightJoin` (.NET 10), `ExecuteUpdateAsync`/`ExecuteDeleteAsync`, именованные query filters `HasQueryFilter("Name", expr)` и `IgnoreQueryFilters(["Name"])`, complex types (`ComplexProperty`, `.ToJson()`, optional complex types, structs).
- `ExecuteUpdateAsync` принимает обычную (не expression) лямбду: `s => { s.SetProperty(b => b.Views, 8); if (cond) s.SetProperty(b => b.Name, "x"); }`; поддержка JSON-колонок complex types.
- Параметризованные коллекции (`ids.Contains(x.Id)`): по умолчанию — несколько скалярных параметров с паддингом; управление — `UseParameterizedCollectionMode(ParameterTranslationMode.…)` у провайдера и `EF.Constant/Parameter/MultipleParameters` на запрос.
- Сырой SQL: `FromSql`/`ExecuteSql` (интерполяция → параметры); `FromSqlRaw` с конкатенацией — предупреждение анализатора EF10.
- Миграции: `dotnet ef migrations add`, `migrations bundle` (`--self-contained --target-runtime linux-x64` опционально), `migrations script --idempotent`, `migrations has-pending-model-changes` (CI), runtime `Database.MigrateAsync()` (с блокировкой БД).
- Для `dotnet ef` в проекте приложения нужен пакет `Microsoft.EntityFrameworkCore.Design` 10.0.12 (PrivateAssets=all) — входит в стек (stack.html rev. 7), отдельный документ — `microsoft-entityframeworkcore-design-10.0.12.md`.

## Version-specific notes
- EF10 работает только на .NET 10; провайдер Npgsql 10.0.3 требует `Microsoft.EntityFrameworkCore` ≥ 10.0.4 (10.0.12 удовлетворяет).
- С EF Core 9: блокировка БД при `Migrate/MigrateAsync` (защита от параллельных миграций; SQL-скрипты ею не защищены); `Migrate` бросает исключение при pending model changes (`RelationalEventId.PendingModelChangesWarning`) и при внешней транзакции вокруг миграций (`MigrationsUserTransactionWarning`). Не вызывать `EnsureCreatedAsync()` перед `MigrateAsync()`.
- EF10 возвращает транзакцию на каждую миграцию (в EF9 все ожидающие миграции шли в одной транзакции).
- Для развёртывания документация рекомендует bundle как одноразовый Job после готовности БД: не ставить SDK/`dotnet ef` в образ приложения, не запускать миграции из entrypoint каждой реплики, не перезапускать контейнер миграций после успешного выхода; использовать отдельную учётную запись с правами на схему (у приложения — только чтение/запись данных). Bundle использует блокировку миграций и `UseSeeding`; читает `appsettings.json` из каталога запуска; окружение задавать явно (`ASPNETCORE_ENVIRONMENT=Production`), строку подключения передавать `--connection` из секрета.
- Для PostgreSQL EF10 использует `jsonb` через Npgsql для complex types `.ToJson()`.
- `ExecuteUpdateAsync` по JSON-колонкам требует complex types (не owned entities).

## Deprecations and breaking changes from prior version (EF Core 9 → 10)
- (Medium) EF tools требуют `--framework` для multi-target проектов.
- (Low) Параметризованные коллекции теперь по умолчанию в несколько параметров (раньше — JSON-массив) → возможны другие планы запросов; откат — `ParameterTranslationMode.Parameter`/`Constant`.
- (Low) Имена SQL-параметров упрощены (`@city` вместо `@__city_0`): снапшот-тесты на SQL и разбор `CommandText`/`ParameterName` в интерцепторах нужно обновить; после деплоя возможен всплеск перекомпиляции планов на сервере БД.
- (Low) `ExecuteUpdateAsync` принимает `Func<…>` вместо `Expression<Func<…>>` — код, строивший expression tree, перестаёт компилироваться.
- (Low) Имена колонок complex types уникализируются, вложенные — с полным путём (`Complex_NestedComplex_Property`); `IDiscriminatorPropertySetConvention` сменил сигнатуру; `IRelationalCommandDiagnosticsLogger` получил параметр `logCommandText`.
- (Low, не относится к PostgreSQL) инъекция `Application Name` в строку подключения SQL Server; `json` тип SQL Server по умолчанию на Azure SQL/compat 170; Microsoft.Data.Sqlite — изменения часовых поясов.
- Из EF9 (действуют): pending model changes → исключение; запрет внешней транзакции вокруг `Migrate`; `EF.Constant()`/`EF.Parameter()` не работают в compiled queries; ограничения `AsNoTrackingWithIdentityResolution` для JSON-коллекций.

## Project conventions
- Образ приложения — без `dotnet ef`/SDK; миграции доставляются bundle (собирается на сборочном хосте вне контура, версия инструмента `dotnet-ef` и `Design` = версии EF) и применяются одноразовым Kubernetes Job; идемпотентный SQL-скрипт — альтернатива при необходимости ревью DBA.
- CI: `dotnet ef migrations has-pending-model-changes`; при опциях Identity, меняющих модель, — `IDesignTimeDbContextFactory` или запуск tools со startup-проектом приложения.
- Маппинг DTO ↔ сущности — явный (без AutoMapper); сырой SQL — точечно и параметризованно; значения параметров и PII в логи не пишутся; `EnableDetailedErrors` в production выключен.
- Время — UTC (`timestamptz`); версионирование сущностей — явный `int Version` вместо `xmin`.
- Выбор между единым `DbContext` и контекстами по подсистемам — design (реестр подсистем).

## Known issues and workarounds
- `Microsoft.EntityFrameworkCore.Design` (в стеке с rev. 7; подробности — `microsoft-entityframeworkcore-design-10.0.12.md`): пакет нужен в проекте для работы `dotnet ef` (`dotnet add package Microsoft.EntityFrameworkCore.Design`, PrivateAssets=all); его зависимости (Roslyn 5.0.0, Humanizer, Mono.TextTemplating, Newtonsoft.Json) — только design-time. Проблема EF9/SDK 9.0.200 с `.deps.json` (`Could not load … Microsoft.EntityFrameworkCore.Design`; workaround `<Publish>true</Publish>`) по заметке EF9 должна быть исправлена в EF10 — подтвердить при первом запуске `dotnet ef` на SDK 10.0.401.
- Запросы с массивами под C# 14: `array.Contains(x)` может привязываться к `MemoryExtensions.Contains`; EF Core содержит обработку (PR dotnet/efcore#37183, по поиску) — проверить spike'ом на 10.0.12.
- Блокировка миграций зависит от провайдера (документация EF: механизм «varies significantly across database providers»); для Npgsql детали в источниках не получены — проверить документацию провайдера; SQL-скрипты блокировкой не защищены.
