---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Microsoft.AspNetCore.OpenApi @ 10.0.12

## Canonical source
- Документация (страница 2026-09-04): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
- Release notes ASP.NET Core 10 (2026-08-31): https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0
- NuGet (nuspec 10.0.12): https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.openapi/10.0.12/microsoft.aspnetcore.openapi.nuspec — лицензия MIT, `net10.0`, framework reference `Microsoft.AspNetCore.App`, зависимость `Microsoft.OpenApi [2.12.0, 3.0.0)`; на NuGet 10.0.12 — последняя стабильная 10.x
- Microsoft.OpenApi 2.12.0 (nuspec): MIT, зависимость `System.Text.Json 8.0.5`; на NuGet также 2.12.1, 2.12.2 и линия 3.x (3.0.0–3.10.2) — вне допустимого диапазона
- Build-time генерация: пакет `Microsoft.Extensions.ApiDescription.Server` 10.0.12 (входит в стек; отдельный документ — `microsoft-extensions-apidescription-server-10.0.12.md`); MSBuild-свойства — https://raw.githubusercontent.com/dotnet/aspnetcore/release/10.0/src/Tools/Extensions.ApiDescription.Server/src/build/Microsoft.Extensions.ApiDescription.Server.props
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **HIGH** — Microsoft.OpenApi 2.x (ломающая смена объектной модели), OpenAPI 3.1 по умолчанию, ограничения build-time генерации и ошибочное исходное допущение стека «YAML при сборке» (исправлено в stack.html rev. 7: на сборке — только JSON, генерацию выполняет отдельный пакет `Microsoft.Extensions.ApiDescription.Server`).

## API surface used in project
- Регистрация и эндпойнт: `builder.Services.AddOpenApi();` (документ `v1` по умолчанию; имя меняется `AddOpenApi("name")`), `app.MapOpenApi();` → `/openapi/v1.json`. YAML — **только** runtime-эндпойнт с суффиксом: `app.MapOpenApi("/openapi/{documentName}.yaml");`.
- Версия спецификации: по умолчанию OpenAPI 3.1 (JSON Schema 2020-12); выбор — `options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0` (`Microsoft.OpenApi`), на этапе сборки — `--openapi-version OpenApi3_1` в `OpenApiGenerateDocumentsOptions`.
- Трансформеры: `OpenApiOptions.AddDocumentTransformer / AddOperationTransformer / AddSchemaTransformer`; на эндпойнте — `.AddOpenApiOperationTransformer((operation, context, ct) => …)` (замена устаревшего `WithOpenApi`). Фильтр включения эндпойнтов — `OpenApiOptions.ShouldInclude`.
- Описания из XML-комментариев: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` (source generator подхватывает комментарии текущей и подключённых сборок); комментарии лямбд не захватываются — использовать именованные методы (по release notes).
- Build-time генерация (для контракта TS-типов): пакет `Microsoft.Extensions.ApiDescription.Server`; после установки документ генерируется на `dotnet build` автоматически (`OpenApiGenerateDocumentsOnBuild` по умолчанию `true`); свойства: `OpenApiDocumentsDirectory` (по умолчанию `$(BaseIntermediateOutputPath)`, т.е. `obj/`; относительно проекта), `OpenApiGenerateDocumentsOptions` (`--file-name`, `--document-name`, `--openapi-version`). Имя файла: `{ProjectName}.json` для документа `v1`, иначе `{ProjectName}_{DocumentName}.json`. Формат — **JSON** (YAML при сборке .NET 10 не поддерживается). Подробно — `microsoft-extensions-apidescription-server-10.0.12.md`.
- Механизм: tool `GetDocument.Insider` запускает entry point приложения с заглушечным `IServer`; выполняется весь startup-код (DI, конфигурация). Защита: `if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider") { /* БД, строки подключения, миграции */ }`.

## Version-specific notes
- Microsoft.OpenApi 2.0 (в составе .NET 10): `OpenApiAny` заменён на `JsonNode`; свойство `Nullable` удалено — допускающий null тип выражается флагом `JsonSchemaType.Null` в `OpenApiSchema.Type`; сущности типизированы интерфейсами (inlined/referenced). Пример миграции: `schema.Example = new JsonObject { … }` вместо `new OpenApiObject { … }`.
- Диапазон `[2.12.0, 3.0.0)`: прямую ссылку на `Microsoft.OpenApi` 3.x не добавлять — несовместима с пакетом 10.0.12.
- Интерактивный UI (Swagger UI/Scalar) пакетом не поставляется; в стеке Scalar — условный пункт (только при полностью офлайн-бандле).
- Совместим с Native AOT; неизвестные HTTP-методы (например, `QUERY`) исключаются из документа; числа и даты форматируются инвариантной культурой.
- Только .NET 11+: свойство `OpenApiGenerationEnvironment` (выбор окружения при генерации; по документации эквивалентно переменным `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`) и OpenAPI 3.2 по умолчанию; в 10.0.12 этого свойства нет.

## Deprecations and breaking changes from prior version (.NET 9 → 10)
- `WithOpenApi` устарел (`ASPDEPR002`) → `AddOpenApiOperationTransformer` (для Swashbuckle — `IOperationFilter`, для NSwag — `IOperationProcessor`; в проекте не используются).
- `Microsoft.Extensions.ApiDescription.Client` и `dotnet openapi`/`OpenApiReference` устарели: генерация клиентов — инструментами генераторов (в проекте — `openapi-typescript`).
- `IncludeOpenAPIAnalyzers` и MVC API-анализаторы устарели.
- Microsoft.OpenApi 1.x → 2.x: смена объектной модели (см. выше) — все документ-/операционные/схемные трансформеры писать под 2.x.
- OpenAPI 3.0 → 3.1 по умолчанию (JSON Schema 2020-12): представление nullable и `example` в документе меняется; потребители контракта (генератор TS-типов) должны поддерживать 3.1 (иначе — `OpenApiVersion = OpenApi3_0`).

## Project conventions
- Источник контракта — код: документ OpenAPI генерируется из Minimal API; DTO на frontend руками не пишутся (принцип «контракт из кода»).
- **Решение стека (stack.html rev. 7):** контракт — JSON-документ OpenAPI, генерируемый при сборке backend пакетом `Microsoft.Extensions.ApiDescription.Server` 10.0.12; TS-типы строятся из него (`openapi-typescript`). Основание: YAML при сборке в .NET 10 не поддерживается (документация 2026-09-04: «Generating OpenAPI documents in YAML format at build time isn't supported but planned for a future preview»; release notes: YAML — только для runtime-эндпойнта). YAML остаётся возможным лишь runtime-маршрутом `/openapi/{documentName}.yaml`, в контракт кодогенерации он не входит. Старт приложения при генерации не должен трогать БД (см. ниже и `microsoft-extensions-apidescription-server-10.0.12.md`). Порядок стадий сборки (SPA зависит от JSON backend) — решение design.
- Операции: `WithName/WithSummary/WithTags/Produces<…>` и операционные трансформеры; `WithOpenApi` не использовать.
- При включённой генерации на сборке: код старта, ходящий в БД/конфигурацию, закрывать проверкой entry assembly; сборка не должна требовать доступа к PostgreSQL.
- Документ отдаётся только при необходимости (Development/по решению design); интерактивный UI — только офлайн.

## Known issues and workarounds
- Terminal Logger скрывает сообщения шага `GetDocument` при `dotnet build` — смотреть через `dotnet build -tlp:v=d` или `--tl:off`.
- Если зарегистрировано несколько документов и имя не `v1`, к имени файла добавляется `_{DocumentName}`; выбрать один — `--document-name`.
- Примеры Microsoft Learn (в т.ч. Identity для SPA) продолжают использовать `.WithOpenApi()` — не копировать, получите предупреждение `ASPDEPR002`.
- Свойство `OpenApiGenerateDocumentsOnBuild` в тексте страницы документации не описано — подтверждено по `.props` пакета `Microsoft.Extensions.ApiDescription.Server` (ветка release/10.0).
