---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Microsoft.AspNetCore.Identity.EntityFrameworkCore @ 10.0.12

Область: хранилище ASP.NET Core Identity на EF Core (PostgreSQL через Npgsql) для локальной аутентификации с cookie-сессией. Базовый пакет `Microsoft.AspNetCore.Identity` входит в shared framework; Razor-UI (`Microsoft.AspNetCore.Identity.UI`) не используется.

## Canonical source
- Identity в ASP.NET Core (страница 2026-09-18): https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0
- Identity API для SPA (2026-03-23): https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0
- Passkeys / WebAuthn (2026-09-01): https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0
- Breaking change «Cookie login redirects disabled for known API endpoints» (2026-09-16): https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/cookie-authentication-api-endpoints
- NuGet (nuspec 10.0.12): https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.identity.entityframeworkcore/10.0.12/microsoft.aspnetcore.identity.entityframeworkcore.nuspec — лицензия MIT; зависимости (net10.0): `Microsoft.Extensions.Identity.Stores` 10.0.12, `Microsoft.EntityFrameworkCore.Relational` 10.0.12
- API reference: `PasswordHasherOptions`, `IdentitySchemaVersions`, `IEmailSender<TUser>` — https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasheroptions?view=aspnetcore-10.0
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — Identity в .NET 10 получил passkeys и схему `Version3`; примеры в документации ориентированы на Razor-UI (`AddDefaultIdentity`), не на состав стека проекта.

## API surface used in project
- EF-хранилище: `IdentityDbContext<TUser, TRole, TKey>` (или базовый `DbContext` с моделью Identity), `AddEntityFrameworkStores<TContext>()`; таблицы Identity — в той же БД PostgreSQL.
- Менеджеры: `UserManager<TUser>` (создание пользователя, пароль, блокировки, токены сброса), `SignInManager<TUser>`: `PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure: true)`, `SignOutAsync()`.
- Способ подключения (решение design, оба варианта доступны без Identity.UI): (а) `AddIdentityCore<TUser>()` + `AddRoles<TRole>()` + `AddEntityFrameworkStores<TContext>()` + `AddSignInManager()` и cookie-схема `AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`; (б) `AddIdentityApiEndpoints<TUser>().AddEntityFrameworkStores<TContext>()` + `app.MapIdentityApi<TUser>()` — эндпойнты `POST /register`, `/login`, `/refresh`, `/confirmEmail`, `/resendConfirmationEmail`, `/forgotPassword`, `/resetPassword`, `/manage/2fa`, `GET/POST /manage/info`; по умолчанию активны и cookie, и собственные bearer-токены (cookie выдаётся при `useCookies=true`; токены — не JWT, «для простых сценариев»).
- Опции: `IdentityOptions.Password` (`RequiredLength`, `RequireDigit`, `RequiredUniqueChars`, …), `Lockout` (`DefaultLockoutTimeSpan`, `MaxFailedAccessAttempts`, `AllowedForNewUsers`; значения в примерах документации — 5 минут и 5 попыток), `SignIn.RequireConfirmedAccount`, `Stores.SchemaVersion`, `Stores.MaxLengthForKeys`.
- `PasswordHasher`: формат V3 (`CompatibilityMode` по умолчанию «ASP.NET Identity version 3»), PBKDF2, `PasswordHasherOptions.IterationCount` по умолчанию 100 000 (по `stack.html` — HMAC-SHA512).
- E-mail: `IEmailSender<TUser>` (пространство имён `Microsoft.AspNetCore.Identity`, сборка в shared framework; методы `SendConfirmationLinkAsync`, `SendPasswordResetLinkAsync`, `SendPasswordResetCodeAsync`) — реализация поверх MailKit (см. `mailkit-4.18.1.md`). **Не** `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender`, который живёт в пакете Identity.UI и встречается в примерах Learn.
- Выход «везде»: security stamp, периодическая ревалидация cookie — `SecurityStampValidatorOptions.ValidationInterval` (компромисс между мгновенной инвалидацией и нагрузкой на БД).
- Метрики: Meter `Microsoft.AspNetCore.Identity` (создание/изменение пользователей, проверки пароля, токены, попытки входа) — .NET 10; используются только при включённом OpenTelemetry.

## Version-specific notes
- .NET 10: passkeys (WebAuthn/FIDO2) в Identity. `IdentitySchemaVersions` содержит `Default`, `Version1`, `Version2`, `Version3` (assembly `Microsoft.Extensions.Identity.Core`). Таблица `AspNetUserPasskeys` относится к схеме `Version3` (подтверждено issue dotnet/aspnetcore#64548). Опт-ин для существующих приложений — `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3` и миграция (по вторичному источнику, перепроверить). Какая версия схемы — по умолчанию в 10.0.12 и попадёт ли `AspNetUserPasskeys` в первую миграцию проекта — в первичных источниках не подтверждено → проверить spike'ом, сгенерировав `InitialCreate`. Passkeys в стек не входят (2FA — открытый вопрос design).
- Cookie-схема: неаутентифицированные/неавторизованные запросы к «известным API-эндпойнтам» дают 401/403 без redirect (подробности и переключатели — `aspnetcore-10.0.12.md`); ответы содержат `Location`.
- EF Core 9+: опции Identity, влияющие на модель (`Stores.SchemaVersion`, `Stores.MaxLengthForKeys`), должны одинаково применяться при design-time (запускать EF tools со startup-проектом приложения или реализовать `IDesignTimeDbContextFactory<TContext>`), иначе `Migrate`/`database update` падает с `PendingModelChangesWarning`.
- Базовый пакет `Microsoft.AspNetCore.Identity` — в shared framework; пакет EF-хранилища — отдельный (в стеке). Зависимости по nuspec: только `Microsoft.Extensions.Identity.Stores` и `Microsoft.EntityFrameworkCore.Relational`.

## Deprecations and breaking changes from prior version (ASP.NET Core 9 → 10)
- Новая схема Identity `Version3` (passkeys) — потенциальная причина «неожиданного» изменения модели при апгрейде: проверять `dotnet ef migrations has-pending-model-changes`.
- Изменение cookie-редиректов для API-эндпойнтов (behavioral) — см. выше.
- Для Blazor Web App с `BlazorDisableThrowNavigationException` — изменения `IdentityRedirectManager`; к проекту (React SPA) не относится.
- Прочих несовместимостей, специфичных для пакета EF-хранилища Identity, в breaking changes ASP.NET Core 10 не обнаружено.

## Project conventions
- Локальная аутентификация, корпоративный IdP не используется (решение пользователя, окончательное); OIDC/SAML/Kerberos не подключаются; сторонних auth-библиотек нет.
- Экран входа — в React SPA; Razor-страницы Identity и пакет Identity.UI не используются: не копировать код с `AddDefaultIdentity`/`IEmailSender` из примеров Learn.
- `lockoutOnFailure: true` всегда; пароли хранятся только как хэши `PasswordHasher`, в логи не попадают.
- Серверная проверка прав/scope — единственный источник истины; антифорджери/SameSite для cookie-сессии и ключи Data Protection (том/Secret) — design.
- Политика паролей, начальный администратор, сброс пароля по e-mail (через SMTP relay, Q8), 2FA, способ заведения пользователей — вопросы design (`stack.html`).
- `LockoutEnd` (`DateTimeOffset?`) должен записываться со смещением 0 — требование Npgsql для `timestamptz` (см. `npgsql-entityframeworkcore-postgresql-10.0.3.md`); проверить миграцией/тестом блокировки.

## Known issues and workarounds
- Примеры Learn: `AddDefaultIdentity` (требует Identity.UI), `IEmailSender` из Identity.UI, `.WithOpenApi()` (устарел, `ASPDEPR002`) — заменять на `AddIdentityCore`/`IEmailSender<TUser>`/`AddOpenApiOperationTransformer`.
- `AspNetUserPasskeys`: известная проблема размера первичного ключа (1024 байта; открыто, milestone .NET 12 Planning, issue #64548 на 2025-11-27) — сообщение касается SQL Server (кластерный индекс 900 байт); для PostgreSQL проверить миграцией, если таблица будет создаваться.
- Встроенные Identity API endpoints и bearer-токены не предназначены для полноценного токен-сервера — при cookie-сессии использовать `useCookies=true` и не опираться на токены.
