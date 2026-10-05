---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# ASP.NET Core (Minimal API, Kestrel) @ 10.0.12

Область: shared framework `Microsoft.AspNetCore.App` (SDK `Microsoft.NET.Sdk.Web`) — Minimal API, Kestrel, встроенная валидация, health checks, hosting. OpenAPI, Identity и EF Core — отдельные tech-reference (`microsoft-aspnetcore-openapi-10.0.12.md`, `microsoft-aspnetcore-identity-entityframeworkcore-10.0.12.md`, `efcore-10.0.12.md`); Kestrel (лимиты, порты) — также в `kestrel-10.0.12.md`; образы — `dotnet-aspnet-image-10.0.12.md`, `dotnet-sdk-image-10.0.401.md`.

## Canonical source
- What's new in ASP.NET Core 10 (страница 2026-08-31): https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0
- Breaking changes ASP.NET Core 10: https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview
- Миграция 9 → 10 (2026-04-22): https://learn.microsoft.com/en-us/aspnet/core/migration/90-to-100
- Релиз 10.0.12 (2026-09-08, LTS до 2028-11-14): https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json (в релизе — исправления безопасности по списку CVE в release-metadata)
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — мажор 10 (2025-11) с несколькими поведенческими изменениями (cookie-редиректы, OpenAPI 3.1, валидация), патч 10.0.12 новее среза знаний.

## API surface used in project
- Minimal API: `WebApplication.CreateBuilder`, `MapGroup("/api/v1")`, `TypedResults`, endpoint filters; DI/Options, `BackgroundService` (воркер outbox), `IHostedService`.
- Встроенная валидация: `builder.Services.AddValidation();` — DataAnnotations на параметрах и типах обработчиков; `.DisableValidation()` на эндпойнте; `[ValidatableType]`, `[SkipValidation]`; ошибки → 400 (кастомизация через `IProblemDetailsService`). Генератор находит валидируемые типы только в сборке, где вызван `AddValidation()` (при многосборочной раскладке вызывать в каждой сборке с эндпойнтами).
- Cookie-аутентификация (хранилище/менеджеры — в Identity-doc): для «известных API-эндпойнтов» неаутентифицированный запрос → 401, неавторизованный → 403 (без redirect), см. «Breaking changes». Для эндпойнтов, не распознанных автоматически: `.DisableCookieRedirect()`; обратное — `.AllowCookieRedirect()` / `[AllowCookieRedirect]`.
- Статика SPA: `app.UseDefaultFiles(); app.UseStaticFiles(); app.MapFallbackToFile("index.html");`. `MapStaticAssets()` обслуживает только ассеты из манифеста, формируемого при build/publish (файлы вне `wwwroot` на этапе сборки, добавленные позже, в манифест не попадают); для отдачи файлов вне манифеста нужен `UseStaticFiles`, `MapStaticAssets` сам default-документы не отдаёт. SPA, копируемый в образ отдельной стадией Dockerfile после `dotnet publish`, в манифест не попадёт → `UseStaticFiles`.
- Health checks: `AddHealthChecks()`; `MapHealthChecks("/healthz/live", new() { Predicate = _ => false })` и `/healthz/ready` с `Predicate = c => c.Tags.Contains("ready")` — под readiness/liveness/startup-пробы Kubernetes; результаты по умолчанию `Unhealthy → 503`.
- Логи: JSON в stdout (`builder.Logging.AddJsonConsole()`); SQL-параметры и PII не логируются.
- Proxy/Ingress: `UseForwardedHeaders` (`ForwardedHeaders = XForwardedFor | XForwardedProto`), доверенные сети — `KnownProxies` / `KnownIPNetworks`; по умолчанию доверен только loopback. Переменная `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` включает middleware без ограничения по `KnownProxies` (настройка для облаков).
- Data Protection (ключи cookie-сессии/antiforgery): `AddDataProtection().SetApplicationName("…").PersistKeysToFileSystem(new DirectoryInfo("…"))`; срок жизни ключа по умолчанию 90 дней (`SetDefaultKeyLifetime`); в контейнере ключи нужно хранить на томе, переживающем контейнер. `PersistKeysToDbContext<T>` требует отдельного пакета (`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`) — вне стека, только через Complication Approval.
- Kestrel: `builder.WebHost.ConfigureKestrel(o => o.Limits…)`; для эндпойнтов загрузки вложений явно задавать `MaxRequestBodySize` (глобально, `[RequestSizeLimit]` или `IHttpMaxRequestBodySizeFeature`); порт 8080 в контейнере (`ASPNETCORE_HTTP_PORTS`, по `stack.html`).
- Обработка ошибок: `AddProblemDetails()`, `UseExceptionHandler`, `IExceptionHandler`; в .NET 10 диагностика не пишется, если `TryHandleAsync` вернул `true` (`ExceptionHandlerOptions.SuppressDiagnosticsCallback`).

## Version-specific notes
- 10.0.12 — патч .NET 10 LTS от 2026-09-08; SDK для сборки 10.0.401; образ `aspnet:10.0.12-noble` — см. `dotnet-aspnet-image-10.0.12.md`.
- Минимальные API на .NET 10: SSE (`TypedResults.ServerSentEvents`); пустая строка из `[FromForm]` для nullable-значимых типов даёт `null` вместо ошибки разбора.
- JSON: тела запросов читаются через `PipeReader` автоматически; кастомные `JsonConverter` должны учитывать `Utf8JsonReader.HasValueSequence`; временный обход — `AppContext`-переключатель `Microsoft.AspNetCore.UseStreamBasedJsonParsing=true`.
- Валидация: API перенесены в пространство имён и NuGet-пакет `Microsoft.Extensions.Validation` (10.0.x существует в NuGet); публичные API и поведение не менялись, код менять не требуется; низкоуровневые resolver-API помечены экспериментальными, `AddValidation()` и встроенный фильтр — стабильны. Нужна ли явная `PackageReference` в `Microsoft.NET.Sdk.Web`-проекте — в источниках не указано (проверить spike'ом).
- Метрики: аутентификация и Identity публикуют метрики (Meter `Microsoft.AspNetCore.Identity`) — полезно только при корпоративном OTLP-коллекторе (в стеке условный пункт).
- Память: автоматическое освобождение простаивающих блоков пула (Kestrel), интерфейс `IMemoryPoolFactory<T>`.

## Deprecations and breaking changes from prior version (ASP.NET Core 9 → 10)
- Cookie login redirects отключены для известных API-эндпойнтов (behavioral): признак — `IDisableCookieRedirectMetadata` (автоматически у `[ApiController]`, Minimal API, читающих/пишущих JSON, `TypedResults`, SignalR); XHR и раньше получали 401/403. Ответы 401/403 по-прежнему содержат заголовок `Location` с URI входа/отказа. Откат: приложение целиком — AppContext-переключатель `Microsoft.AspNetCore.Authentication.Cookies.IgnoreRedirectMetadata=true` (через `RuntimeHostConfigurationOption`), точечно — `AllowCookieRedirect`, либо переопределение `OnRedirectToLogin`/`OnRedirectToAccessDenied`.
- `WithOpenApi` устарел (`ASPDEPR002`) → `AddOpenApiOperationTransformer`.
- `IPNetwork` и `ForwardedHeadersOptions.KnownNetworks` устарели (`ASPDEPR005`) → `System.Net.IPNetwork`, `KnownIPNetworks`.
- `IActionContextAccessor`/`ActionContextAccessor` устарели; `IncludeOpenAPIAnalyzers` и MVC API-анализаторы устарели; `Microsoft.Extensions.ApiDescription.Client` устарел (генерация клиентов — инструментами самих генераторов); Razor runtime compilation устарела; `WebHostBuilder`, `IWebHost`, `WebHost` устарели (использовать `WebApplication`/Generic Host).
- Исключения: диагностика подавляется, если `IExceptionHandler.TryHandleAsync` вернул `true` (behavioral).
- Уровня .NET (затрагивают проект): `BackgroundService.ExecuteAsync` — целиком на фоновом потоке; SIGTERM-хендлер runtime не ставится по умолчанию (host регистрирует свой); `System.Text.Json` проверяет конфликты имён свойств.

## Project conventions
- REST под `/api/v1`; ошибки — `ProblemDetails`; оптимистичная блокировка — `ETag`/`If-Match` (конфликт → 412/409, решает design); сервер — единственный источник истины по правам, scope и валидации.
- Без MediatR/AutoMapper; прямые обработчики эндпойнтов, явный маппинг; DI/Options/`BackgroundService`/health checks — только встроенные средства.
- Секреты — только из Kubernetes Secrets (env или смонтированные файлы), конфигурация — из ConfigMap; не в образе и не в репозитории.
- Воркер outbox — `BackgroundService` с перехватом исключений внутри цикла (необработанное исключение останавливает хост) и уважением `CancellationToken` при остановке пода.
- Код Program.cs, выполняемый до `builder.Build()`, должен быть безопасен при запуске генератором OpenAPI на этапе сборки (см. `microsoft-aspnetcore-openapi-10.0.12.md`).

## Known issues and workarounds
- `AddDbContextCheck<T>()` находится не во встроенных health checks, а в пакете `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`; в стеке (stack.html rev. 7) пакет **не принят** — проверка БД реализуется собственным `IHealthCheck` с `Database.CanConnectAsync` (регистрация через `AddHealthChecks().AddCheck<…>("db", tags: ["ready"])`). Встроенное в shared framework — только `AddHealthChecks()`/`MapHealthChecks()`/`IHealthCheck`.
- Health checks: документация предупреждает о подмене заголовка `Host`; защищать `MapHealthChecks` через `RequireHost("*:порт")`/`RequireAuthorization()` (для kubelet-проб — отдельный порт/путь без авторизации, решает design).
- Документация противоречива по имени признака «известного API-эндпойнта»: release notes (2026-08-31) называют `IApiEndpointMetadata`, страница breaking change (2026-09-16) — `IDisableCookieRedirectMetadata` (+ парный `IAllowCookieRedirectMetadata`). За основу взята страница breaking change как более новая; имя проверить по API reference при реализации.
