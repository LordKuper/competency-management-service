---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Microsoft.EntityFrameworkCore.Design @ 10.0.12

Область: design-time пакет EF Core, без которого `dotnet ef` не работает с проектом. Инструмент CLI — `dotnet-ef-10.0.12.md`; рантайм EF, миграции и их применение — `efcore-10.0.12.md`; провайдер — `npgsql-entityframeworkcore-postgresql-10.0.3.md`.

## Canonical source
- Официальная документация (EF tools, .NET CLI; страница 2026-08-11): https://learn.microsoft.com/en-us/ef/core/cli/dotnet — «Before you can use the tools on a specific project, you'll need to add the `Microsoft.EntityFrameworkCore.Design` package to it»
- Breaking changes EF Core 10 (страница обновлена 2026-07-01): https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes ; EF Core 9: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes
- NuGet (nuspec 10.0.12): https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.design/10.0.12/microsoft.entityframeworkcore.design.nuspec — лицензия MIT, `developmentDependency: true`, target `net10.0`, описание «Shared design-time components for Entity Framework Core tools»; репозиторий https://github.com/dotnet/dotnet (commit `95017c711e6afc1085133d440e42b4bd78155701`)
- Зависимости 10.0.12 (net10.0; `exclude: Build` либо `Build, Analyzers`): Microsoft.EntityFrameworkCore.Relational 10.0.12, Humanizer.Core 2.14.1, Microsoft.CodeAnalysis.CSharp 5.0.0, Microsoft.CodeAnalysis.CSharp.Workspaces 5.0.0, Microsoft.CodeAnalysis.Workspaces.MSBuild 5.0.0, Microsoft.Build.Framework 18.0.2, Mono.TextTemplating 3.0.0, Newtonsoft.Json 13.0.4, Microsoft.Extensions.DependencyModel / Caching.Memory / Configuration.Abstractions / Logging 10.0.12
- Индекс версий: https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.design/index.json — последняя стабильная 10.0.12 (10.0.13+ нет); 11.0.0-preview.1…7, 11.0.0-rc.1 — не использовать
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — EF Core 10 (LTS), патч 10.0.12 новее среза знаний; в 10 инструменты стали использовать задачу MSBuild `ResolvePackageAssets` (следствие — требование `--framework` для multi-target проектов), и на этом пакете держится весь design-time пайплайн миграций.

## API surface used in project
- Подключение в **проекте запуска миграций** (startup project; им может быть проект backend с `Microsoft.NET.Sdk.Web`): `dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.12`; пакет — dev-зависимость (`PrivateAssets=all`; `dotnet add package` проставляет это по признаку `developmentDependency` — проверить в csproj).
- Версия **строго равна** версии EF Core и `dotnet-ef` (10.0.12): одновременное обновление всех трёх — через спринт.
- Пользователь пакета — `dotnet ef` (инструмент строит и запускает startup-проект): `migrations add`, `migrations script --idempotent`, `migrations bundle`, `migrations has-pending-model-changes`; сам пакет прикладного кода не требует.
- Если приложение нельзя запускать при design-time (старт зависит от БД/секретов) — `IDesignTimeDbContextFactory<TContext>` в проекте (упомянута на странице EF tools; по странице EF «Design-time DbContext creation» при наличии фабрики инструменты используют её вместо других способов создания контекста — эта страница в данной проверке не перечитывалась).

## Version-specific notes
- EF10 работает только на .NET 10; Design 10.0.12 зависит от `Microsoft.EntityFrameworkCore.Relational` 10.0.12 — версии `Microsoft.EntityFrameworkCore`, `.Relational`, `.Design` и провайдера Npgsql 10.0.3 (требует EF ≥ 10.0.4) должны разрешаться согласованно (проверять в NuGet lock-файле).
- Ссылка на пакет добавляет в граф зависимостей design-time библиотеки (Roslyn 5.0.0, Humanizer, Mono.TextTemplating, Newtonsoft.Json 13.0.4, Microsoft.Build.Framework 18.0.2). Для PII/HR-контура с минимумом зависимостей (supply chain) это учитывается при ревью lock-файла и SBOM: лицензии транзитивных пакетов (Roslyn, Humanizer.Core, Mono.TextTemplating, Newtonsoft.Json, Microsoft.Build.Framework) по их nuspec в этой проверке не сверялись — сверить на соответствие допустимому списку стека (MIT, Apache-2.0, BSD, ISC, PostgreSQL, MS-PL) до включения в lock-файл; каждая новая транзитивная ссылка попадает в сканирование и SBOM.
- Опции CLI, помеченные «Added in EF Core 11» (`dotnet-ef.json`, `--connection` для `database drop`/`migrations remove`, `--add`), в 10.0.12 отсутствуют.

## Deprecations and breaking changes from prior version (EF Core 9 → 10)
Changelog по странице breaking changes EF Core 10 (2026-07-01), только то, что касается инструментов/design-time:
- (Medium) Инструменты EF требуют `--framework` для проектов с `<TargetFrameworks>` (инструменты стали опираться на `ResolvePackageAssets`, недоступную для multi-target проектов). Проект backend — single-target `net10.0`, требование не затрагивает; при появлении multi-target проекта — передавать `--framework net10.0`.
- Остальные пункты EF10 (параметризованные коллекции, имена параметров SQL, `ExecuteUpdateAsync`, complex types, `IRelationalCommandDiagnosticsLogger`) к design-time пакету не относятся (см. `efcore-10.0.12.md`); прочие пункты страницы — SQL Server и Microsoft.Data.Sqlite — вне стека.
- Из EF Core 9 (действуют): `has-pending-model-changes` как проверка в CI; блокировка миграций и исключение при pending model changes.

## Project conventions
- Пакет подключён только к проекту, который является startup-проектом `dotnet ef`; в другие проекты (домен, тесты) не добавляется.
- Вместе с `dotnet-ef` 10.0.12 (локальный инструмент в `.config/dotnet-tools.json`) — версии синхронны; обновление — только через спринт.
- Design-time зависимости не должны требоваться для работы приложения в образе `app` (образ без SDK и без `dotnet ef`, миграции — bundle/Job, см. `dotnet-ef-10.0.12.md`).
- Миграции генерируются на сборочном хосте вне контура (с интернетом); нужные пакеты восстанавливаются по NuGet lock-файлам.

## Known issues and workarounds
- Ошибка `Could not load file or assembly 'Microsoft.EntityFrameworkCore.Design'` (issue EF9 с SDK 9.0.200 и `.deps.json`; обход `<Publish>true</Publish>`): по заметке EF9 исправлена в EF10 — подтвердить при первом запуске `dotnet ef` на SDK 10.0.401 (из `efcore-10.0.12.md`; не проверено).
- Попадание Design и его design-time зависимостей в publish-выход: `PrivateAssets=all` ограничивает только транзитивность на потребителей проекта, а не публикацию; по умолчанию runtime-assets пакета включаются в выход (шаблонный `IncludeAssets` содержит `runtime`). Нужно ли исключать их из образа `app` (`ExcludeAssets=runtime` при publish либо отдельный проект миграций) — проверить на spike; **не верифицировано по первичному источнику**.
- Старт приложения при design-time выполняется целиком (как и при генерации OpenAPI): код, ходящий в БД, — под проверкой окружения или `IDesignTimeDbContextFactory`.
