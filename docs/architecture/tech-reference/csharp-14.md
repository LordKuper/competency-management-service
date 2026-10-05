---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# C# @ 14 (.NET SDK 10.0.401, платформа .NET 10 LTS)

## Canonical source
- Язык: What's new in C# 14 — https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14 (страница обновлена 2026-09-03)
- Выбор версии языка: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version (2026-01-16)
- Breaking changes .NET 10: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0 (2026-07-30)
- Политика поддержки: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core — .NET 10 LTS, GA 2025-11-11, актуальный патч 10.0.12 (2026-09-08), конец поддержки 2028-11-14
- Метаданные релиза: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json — релиз 10.0.12 → SDK 10.0.401 (вторая feature band — 10.0.112)
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — C# 14/.NET 10 вышли в 2025-11, патчи 10.0.12 и SDK 10.0.401 новее среза знаний; изменения сверены с первичными источниками выше.

## API surface used in project
- `LangVersion` не задаётся: для `net10.0` по умолчанию C# 14. Значение `latest` не использовать (документация: зависит от машины, ломает воспроизводимость сборки).
- Записи (`record`) и `required`-члены как DTO минимальных API; правила валидации — атрибуты DataAnnotations (`[Required]`, `[Range]`) под `AddValidation()` (см. `aspnetcore-10.0.12.md`).
- `field` (C# 14): свойства без явного поля, нормализация ввода в сеттере. Пример: `public string Email { get; set => field = value?.Trim().ToLowerInvariant() ?? string.Empty; }`.
- Extension members (C# 14): `extension<T>(IEnumerable<T> source) { public bool IsEmpty => !source.Any(); }` — свойства-расширения и статические расширения; для явного маппинга DTO ↔ сущность допустимы обычные методы расширения (AutoMapper/Mapperly по умолчанию не используются).
- Null-conditional assignment (C# 14): `customer?.Order = GetCurrentOrder();` (правая часть не вычисляется при `null`; `++`/`--` не допускаются).
- Асинхронность: `CancellationToken` сквозь обработчики и `BackgroundService`; `IAsyncEnumerable<T>` для потоковой выгрузки (XLSX/CSV/PDF).
- Nullable reference types включены глобально (`Directory.Build.props`).

## Version-specific notes
- `field` — контекстное ключевое слово: член с именем `field` внутри типа с field-backed свойствами нужно писать как `@field`/`this.field`.
- C# 14 first-class spans: `array.Contains(x)` в expression tree привязывается к `MemoryExtensions.Contains`, а не к `Enumerable.Contains` (breaking change .NET 10, «C# 14 overload resolution with span parameters»). Затрагивает `Expression.Compile(preferInterpretation: true)`; в запросах EF Core 10 с массивами обработка добавлена в EF Core (dotnet/efcore PR #37183 — по результатам веб-поиска, версия включения не подтверждена) → проверить spike'ом на 10.0.12 запросы вида `ids.Contains(x.Id)`; обход — приводить к `IEnumerable<T>`/`List<T>`.
- Runtime .NET 10: `BackgroundService.ExecuteAsync` целиком выполняется на фоновом потоке и не блокирует старт других сервисов (раньше синхронная часть до первого `await` блокировала). Код, который должен выполниться до старта остальных сервисов — в конструктор/`StartAsync`/`IHostedLifecycleService`.
- Runtime .NET 10 не ставит обработчик SIGTERM по умолчанию; для ASP.NET Core и Generic Host (`UseConsoleLifetime`) действий не требуется — graceful shutdown под Kubernetes сохраняется.
- SDK 10: `NuGetAuditMode` по умолчанию `all` (аудит транзитивных пакетов); при `TreatWarningsAsErrors` добавить `<WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>`, чтобы уязвимость в транзитивной зависимости не ломала restore без решения. Аудит ходит в реестр — работает на сборочном хосте (с интернетом), не в контуре.
- SDK 10: `dotnet new sln` создаёт `.slnx` по умолчанию; `PackageReference` без `Version` — ошибка (NU1015); `dotnet tool install --local` создаёт manifest по умолчанию.
- `global.json`: поле `version` требует полный номер (`10.0.401`, без масок); при использовании NuGet lock-файлов документация рекомендует `rollForward: disable` (ссылки на dotnet/aspnetcore#65061, dotnet/sdk#48795); секция `test.runner` (MTP) доступна с .NET 10 SDK.
- Платформа: .NET 10 LTS до 2028-11-14 — соответствует `stack.html`.

## Deprecations and breaking changes from prior version (C# 13 / .NET 9)
- C# 14: `field`-коллизии (см. выше); перегрузки со `Span<T>` применимы к массивам → смена привязки в expression tree.
- API obsoletions .NET 10 («API obsoletions», source incompatible), `System.Linq.AsyncEnumerable` включён в core libraries (source incompatible), `System.Text.Json` проверяет конфликты имён свойств (DTO с одинаковыми JSON-именами теперь падает), `BufferedStream.WriteByte` без неявного flush, null-значения сохраняются в конфигурации — полный список: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0.
- Контейнеры: образы .NET 10 по умолчанию на Ubuntu (в `stack.html` — `noble`, соответствует).

## Project conventions
- Единый язык для API, воркера, миграций и тестов; решение — `.slnx`; версии NuGet-пакетов — точные, restore по lock-файлам (`RestorePackagesWithLockFile`, в CI `RestoreLockedMode`).
- `Directory.Build.props`: `Nullable=enable`, `ImplicitUsings`, `TreatWarningsAsErrors=true` + `WarningsNotAsErrors` для NU190x; `LangVersion` не переопределять.
- Без Mediator/AutoMapper: прямые обработчики и явный маппинг (принцип «встроенное вместо сторонних»).
- Код и идентификаторы — английские; сообщения пользователю — русские (`language.docs`).

## Known issues and workarounds
- Примеры в Learn по Identity/OpenAPI местами используют устаревшие API (`WithOpenApi`, `AddDefaultIdentity`) — см. соответствующие tech-reference.
- Структурных проблем самого C# 14 на дату проверки не обнаружено; сверять `compiler breaking changes` при обновлении SDK (страница списка compiler breaking changes по прямому URL на момент проверки вернула 404 — не подтверждено).
