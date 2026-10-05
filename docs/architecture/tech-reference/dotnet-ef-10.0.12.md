---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# dotnet-ef (EF Core CLI tools) @ 10.0.12

## Canonical source
- Official docs: https://learn.microsoft.com/en-us/ef/core/cli/dotnet ; применение миграций: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
- NuGet: https://www.nuget.org/packages/dotnet-ef/10.0.12 (nuspec: MIT, `packageTypes: DotnetTool`, без dependencies; commit репозитория `95017c711e6afc1085133d440e42b4bd78155701`)
- Last verified: 2026-10-05 (в реестре есть 10.0.12 и предрелизы 11.0.0-preview.*/rc.1 — не использовать)

## API surface used in project
- Установка как **локальный инструмент** (манифест `.config/dotnet-tools.json` в репозитории): `dotnet tool install dotnet-ef --version 10.0.12`, затем `dotnet tool restore` в стадии .NET SDK.
- Проект запуска (startup project) должен ссылаться на `Microsoft.EntityFrameworkCore.Design` — версия должна совпадать с EF Core (10.0.12); пакет входит в стек (stack.html rev. 7, раздел «Backend»), отдельный документ — `microsoft-entityframeworkcore-design-10.0.12.md`.
- Команды: `dotnet ef migrations add <Name>`; `migrations script --idempotent -o <file>`; `migrations bundle [--self-contained -r linux-x64] -o <file>`; `migrations has-pending-model-changes`; `database update` (только разработка/тесты); общие опции `-p/--project`, `-s/--startup-project`, `-c/--context`, `--no-build`, `--configuration`; аргументы приложению — после `--` (например `-- --environment Production`).

## Version-specific notes
- Инструмент выполняет код приложения при проектировании (строит `DbContext`), среда по умолчанию — `Development`, если не заданы `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`; при генерации артефактов и запуске bundle среду задавать явно (иначе возможна подгрузка dev user-secrets).
- Начиная с EF Core 9: `Migrate()/MigrateAsync()` и bundle берут блокировку БД на время миграции; при изменениях модели без миграции `Migrate()` бросает исключение (`PendingModelChangesWarning`) — в CI запускать `has-pending-model-changes`.
- Опции, помеченные в документации «Added in EF Core 11» (`dotnet-ef.json`, `--connection` для `database drop`/`migrations remove`, `database update --add`, `migrations remove --offline`), **в 10.0.12 отсутствуют** — не использовать.
- Рекомендации Microsoft по применению: для автоматизированного развёртывания — migration bundle (исполняется без SDK, EF tools и исходников; `--self-contained` убирает и зависимость от .NET runtime); SQL-скрипт — когда SQL нужно проверять/согласовывать; `dotnet ef database update` — для разработки (требует SDK и исходники). Не ставить SDK и не запускать `dotnet ef` в образе приложения; не выполнять миграции из entrypoint каждой реплики; запускать bundle одноразовым job после готовности БД; идентичность развёртывания (права на схему) отдельна от идентичности приложения.

## Deprecations and breaking changes from prior version
- EF Core 9 → 10: изменений CLI, влияющих на проект, не выявлено в проверенных страницах. С EF Core 9 действуют блокировка миграций и исключение при pending model changes (см. выше); транзакция вокруг `MigrateAsync()` не поддерживается.

## Project conventions
- Версия инструмента = версия EF Core 10.0.12; обновление — только через спринт.
- Образ `app` — `aspnet:10.0.12-noble` без SDK, поэтому «применение» миграций средствами `dotnet ef` внутри `app` невозможно; способ доставки схемы (bundle как одноразовый Kubernetes Job, SQL-скрипт либо `MigrateAsync()` при старте) в stack.html не определён — решение design (EF Core 9+ защищает `MigrateAsync()` блокировкой, но Microsoft всё равно предпочитает отдельный шаг).
- Миграции — в репозитории, ревью SQL обязательно (`migrations script`) для данных PII/HR; значения параметров SQL в логи не попадают (stack.html).
- В CI: `has-pending-model-changes` и сборка bundle в стадии .NET SDK образа.

## Known issues and workarounds
- Bundle не позволяет заранее посмотреть SQL и список миграций (issue dotnet/efcore#25872) — для обзора использовать `migrations script`.
- Если контекст читает `appsettings.json`, файлы настроек должны лежать рядом с bundle; секреты не встраивать, строку подключения передавать `--connection` из Kubernetes Secret.
