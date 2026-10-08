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
- Last verified: 2026-10-05 (источники); конвенции проекта сверены с кодом 2026-10-08 (спринт 002)
- Оценка риска знаний (Phase 5): **MEDIUM** — Identity в .NET 10 получил passkeys и схему `Version3`; примеры в документации ориентированы на Razor-UI (`AddDefaultIdentity`), не на состав стека проекта.

## API surface used in project
- EF-хранилище: `IdentityDbContext<TUser, TRole, TKey>` (или базовый `DbContext` с моделью Identity), `AddEntityFrameworkStores<TContext>()`; таблицы Identity — в той же БД PostgreSQL.
- Менеджеры: `UserManager<TUser>` (создание пользователя, пароль, блокировки, токены сброса), `SignInManager<TUser>`: `PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure: true)`, `SignOutAsync()`. В проекте — только `UserManager`: `CreateAsync(user)` без пароля (приглашённая запись), `FindByEmailAsync`, `FindByIdAsync`, `UpdateAsync`, `ValidatePasswordAsync`, `PasswordHasher.HashPassword`, `UpdateSecurityStampAsync`. Токен-методы (`GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`, `GenerateEmailConfirmationTokenAsync`) не используются: провайдеры токенов не зарегистрированы (`AddDefaultTokenProviders` не вызывается), и вызов этих методов завершится ошибкой.
- Способ подключения (в проекте выбран (а), без `AddRoles` и `SignInManager`: `AddIdentityCore<AppUser>()` + `AddUserStore<AppUserStore>()` и собственная cookie-схема `SessionAuthentication`; (б) не используется; оба варианта доступны без Identity.UI): (а) `AddIdentityCore<TUser>()` + `AddRoles<TRole>()` + `AddEntityFrameworkStores<TContext>()` + `AddSignInManager()` и cookie-схема `AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies()`; (б) `AddIdentityApiEndpoints<TUser>().AddEntityFrameworkStores<TContext>()` + `app.MapIdentityApi<TUser>()` — эндпойнты `POST /register`, `/login`, `/refresh`, `/confirmEmail`, `/resendConfirmationEmail`, `/forgotPassword`, `/resetPassword`, `/manage/2fa`, `GET/POST /manage/info`; по умолчанию активны и cookie, и собственные bearer-токены (cookie выдаётся при `useCookies=true`; токены — не JWT, «для простых сценариев»).
- Опции: `IdentityOptions.Password` (`RequiredLength`, `RequireDigit`, `RequiredUniqueChars`, …), `Lockout` (`DefaultLockoutTimeSpan`, `MaxFailedAccessAttempts`, `AllowedForNewUsers`; значения в примерах документации — 5 минут и 5 попыток), `SignIn.RequireConfirmedAccount`, `Stores.SchemaVersion`, `Stores.MaxLengthForKeys`.
- `PasswordHasher`: формат V3 (`CompatibilityMode` по умолчанию «ASP.NET Identity version 3»), PBKDF2, `PasswordHasherOptions.IterationCount` по умолчанию 100 000 (по `stack.html` — HMAC-SHA512).
- E-mail: `IEmailSender<TUser>` (пространство имён `Microsoft.AspNetCore.Identity`, сборка в shared framework; методы `SendConfirmationLinkAsync`, `SendPasswordResetLinkAsync`, `SendPasswordResetCodeAsync`) в проекте не используется: письма приглашения и сброса шлёт собственный `AccountMail` модуля через `MailSender` (MailKit, `mailkit-4.18.1.md`). Если понадобится — именно он, **не** `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender`, который живёт в пакете Identity.UI и встречается в примерах Learn.
- Выход «везде»: security stamp, периодическая ревалидация cookie — `SecurityStampValidatorOptions.ValidationInterval` (компромисс между мгновенной инвалидацией и нагрузкой на БД).
- Метрики: Meter `Microsoft.AspNetCore.Identity` (создание/изменение пользователей, проверки пароля, токены, попытки входа) — .NET 10; используются только при включённом OpenTelemetry.

## Version-specific notes
- .NET 10: passkeys (WebAuthn/FIDO2) в Identity. `IdentitySchemaVersions` содержит `Default`, `Version1`, `Version2`, `Version3` (assembly `Microsoft.Extensions.Identity.Core`). Таблица `AspNetUserPasskeys` относится к схеме `Version3` (подтверждено issue dotnet/aspnetcore#64548). Опт-ин для существующих приложений — `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3` и миграция (по вторичному источнику, перепроверить). Какая версия схемы — по умолчанию в 10.0.12 и попадёт ли `AspNetUserPasskeys` в первую миграцию проекта — в первичных источниках не подтверждено. В проекте вопрос снят: схема Identity по умолчанию не используется — `AppDbContext` обычный `DbContext`, а `AppUser` отображён одной таблицей `users` с нужными колонками (без таблиц ролей, claims, логинов, токенов и `AspNetUserPasskeys`). Passkeys и 2FA в проект не входят.
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
- Неверный пароль всегда считается в lockout; пароли хранятся только как хэши `PasswordHasher`, в логи не попадают. Учёт попыток идёт под `SELECT … FOR UPDATE` строки учётной записи с перечитыванием (`VerifyPasswordAsync`), иначе параллельные попытки теряются.
- `SecurityStampValidator` не используется: собственный `OnValidatePrincipal` на каждом запросе проверяет, что учётная запись есть, не заблокирована, stamp совпадает и привязанный сотрудник работает; всё, что должно оборвать сессию, меняет stamp (`AppUser.EndSessions()` либо `UpdateSecurityStampAsync`).
- Серверная проверка прав/scope — единственный источник истины; CSRF — `SameSite=Strict` + проверка `Origin`/`Sec-Fetch-Site`, antiforgery не используется; ключи Data Protection — каталог на PVC (`stack.html`).
- Политика паролей (минимум 10, остальное по умолчанию Identity; SPA читает длину из `GET /api/v1/auth/password-policy`), начальный администратор (`Bootstrap__AdminEmail`/`Bootstrap__AdminPassword`), блокировка 5 попыток на 15 минут, без 2FA и без заведения пользователей импортом — решено в спринте 001 (`stack.html`). Со спринта 002 пароль задаёт только сам пользователь: по ссылке-приглашению, по ссылке сброса или сменой своего пароля; администратор паролей не задаёт.
- Ссылки из писем — без провайдеров токенов Identity: `DataProtectorTokenProvider` без состояния и действует, пока не сменился security stamp, — отменить прежнюю ссылку без обрыва сессий нельзя, смена e-mail stamp не меняет, а DataProtection в проекте только для cookie (Q4 `stack.html`). Вместо него — собственный одноразовый токен с SHA-256 в `users`, сроком и привязкой к stamp (`user-management.md`).
- «Приглашён» = `PasswordHash == null`. Вход такой записи не доходит до проверки пароля (`CheckPasswordAsync` для записи без хэша вернул бы `false`, не хэшируя, и ответил бы быстрее): ответ тот же 401, время выравнивается хэшированием введённого пароля (`SpendHashingTime`). Задание пароля по ссылке — `PasswordHasher.HashPassword` + `ClearLockout` + новый stamp в одной записи строки под `FOR UPDATE`.
- `LockoutEnd` (`DateTimeOffset?`) должен записываться со смещением 0 — требование Npgsql для `timestamptz` (см. `npgsql-entityframeworkcore-postgresql-10.0.3.md`); покрыто интеграционными тестами блокировки.

## Known issues and workarounds
- Примеры Learn: `AddDefaultIdentity` (требует Identity.UI), `IEmailSender` из Identity.UI, `.WithOpenApi()` (устарел, `ASPDEPR002`) — заменять на `AddIdentityCore`/`IEmailSender<TUser>`/`AddOpenApiOperationTransformer`.
- `AspNetUserPasskeys`: известная проблема размера первичного ключа (1024 байта; открыто, milestone .NET 12 Planning, issue #64548 на 2025-11-27) — сообщение касается SQL Server (кластерный индекс 900 байт); для PostgreSQL проверить миграцией, если таблица будет создаваться.
- Встроенные Identity API endpoints и bearer-токены не предназначены для полноценного токен-сервера — при cookie-сессии использовать `useCookies=true` и не опираться на токены.
