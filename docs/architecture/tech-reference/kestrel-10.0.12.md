# Kestrel @ 10.0.12 (в составе ASP.NET Core)

Область: веб-сервер Kestrel — привязка, протоколы, лимиты, TLS, поведение в контейнере и за прокси. Minimal API, валидация, health checks, статические файлы, `UseForwardedHeaders`, Data Protection — `aspnetcore-10.0.12.md`; образ — `dotnet-aspnet-image-10.0.12.md`.

## Canonical source
- Official docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-10.0 (страница обновлена 2026-09-03)
- Прокси / балансировщики: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
- ASP.NET Core 10: https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0 ; breaking changes: https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview
- Релиз 10.0.12 (2026-09-08): https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md
- Last verified: 2026-10-05

## API surface used in project
- Kestrel — сервер ASP.NET Core по умолчанию; входит в shared framework `Microsoft.AspNetCore.App` 10.0.12 (ставится образом `aspnet`), отдельного пакета нет. Обслуживает API `/api/v1` и статику SPA в одном процессе.
- Привязка: вне контейнера по умолчанию `http://localhost:5000` (и `https://localhost:5001` при наличии dev-сертификата); переопределение — `ASPNETCORE_URLS`, `--urls`, ключ `urls`, `UseUrls`, `KestrelServerOptions.Listen` / `ListenUnixSocket`. В официальных образах задан `ASPNETCORE_HTTP_PORTS=8080` (комментарий Dockerfile: «Configure web servers to bind to port 8080 when present») — сервер слушает 8080; порт >1024 доступен non-root.
- Протоколы: HTTP/1.1, HTTP/2, HTTP/3; по умолчанию у endpoint `HttpProtocols.Http1AndHttp2`; HTTP/2 по TLS требует выбора в ALPN, иначе соединение остаётся HTTP/1.1.
- TLS: `ListenOptions.UseHttps(...)` (сертификат из файла/хранилища/объекта) или секция конфигурации `Kestrel:Endpoints`; без сертификата по умолчанию HTTPS-endpoint невозможен (`UseHttps()` без default-сертификата бросает исключение). Где терминируется TLS — Kestrel или Ingress — открытый вопрос Q8.
- Лимиты: `serverOptions.Limits` — `MaxRequestBodySize` (**по умолчанию 30 000 000 байт ≈ 28,6 МБ**), `MaxConcurrentConnections`, `MinRequestBodyDataRate` / `MinResponseDataRate`, `KeepAliveTimeout`, `RequestHeadersTimeout`; для отдельного запроса — `IHttpMaxRequestBodySizeFeature` (до начала чтения тела; после — исключение, смотреть `IsReadOnly`). example:
  `builder.WebHost.ConfigureKestrel(o => { o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; /* значение — пример */ });`
- `ConfigureEndpointDefaults` применяется только к endpoint'ам, созданным после вызова; повторный вызов заменяет предыдущий.

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — .NET 10 (мажор) известен, патч 10.0.12 и часть изменений хостинга новее; breaking changes и release notes ASP.NET Core 10 прочитаны.
- 10.0.12 — security-релиз (6 CVE, `dotnet-aspnet-image-10.0.12.md`); LTS до 2028-11-14.
- .NET 10: пулы памяти Kestrel (а также IIS, HTTP.sys) автоматически освобождают блоки при простое/малой нагрузке (настройка не нужна); метрики `Microsoft.AspNetCore.MemoryPool`; в DI доступен `IMemoryPoolFactory<byte>`; имена `*.localhost` трактуются как loopback.
- Kestrel допускается как с reverse proxy, так и без него; при работе за прокси требуется настройка forwarded headers (см. `aspnetcore-10.0.12.md`). `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` включает middleware с настройками «для облаков» и **не** ограничивает источники `KnownProxies` (предупреждение Microsoft).

## Deprecations and breaking changes from prior version
- `WebHostBuilder`, `IWebHost`, `WebHost` — obsolete (`ASPDEPR004` / `ASPDEPR008`), заменитель — `HostBuilder` / `WebApplicationBuilder`; опции Kestrel задаются через `builder.WebHost.ConfigureKestrel(...)` (в список obsolete не входит).
- `IPNetwork` и `ForwardedHeadersOptions.KnownNetworks` — obsolete (`ASPDEPR005`) → `System.Net.IPNetwork` и `KnownIPNetworks`.
- Cookie-аутентификация больше не перенаправляет на логин для известных API-эндпойнтов (401/403), исключения от `IExceptionHandler` не пишутся в диагностику, если `TryHandleAsync` вернул `true` — подробности `aspnetcore-10.0.12.md`.
- .NET runtime по умолчанию не ставит обработчик SIGTERM; для ASP.NET-хоста действий не требуется (`UseConsoleLifetime` регистрирует сигналы) — graceful shutdown Kestrel при остановке пода работает.

## Project conventions
- Единственный веб-сервер проекта; порт 8080 берётся из образа — `ASPNETCORE_URLS` без необходимости не задавать. Процесс-супервизор и сторонний reverse proxy внутри контейнера не нужны (stack.html).
- Лимит размера загрузки вложений задавать явно под требования design (ограничения типов и размера — открытый пункт design, Q7), глобально через `Limits.MaxRequestBodySize` или точечно на эндпойнте загрузки; значение по умолчанию (≈28,6 МБ) считать ориентиром, не решением.
- TLS: вариант A — терминация на Ingress (Kestrel слушает plain HTTP 8080, нужны forwarded headers с явным доверием к сети Ingress); вариант B — Kestrel с сертификатом из Kubernetes Secret (`UseHttps` / `Kestrel:Endpoints`). Выбор — Q8/design.
- Остановка пода: согласовать `terminationGracePeriodSeconds` и время остановки хоста приложения (design).

## Known issues and workarounds
- Хостинг в Docker: `System.IO.IOException … the configured user limit (128) on the number of inotify instances has been reached` → `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`.
- Лимит `MaxRequestBodySize` нельзя менять, когда приложение уже начало читать тело запроса (исключение).
- При использовании `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` в кластере без ограничения доверенных прокси возможна подмена заголовков `X-Forwarded-*` — предпочитать явную настройку `KnownProxies` / `KnownIPNetworks`.
