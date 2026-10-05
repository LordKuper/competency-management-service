---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Microsoft.Extensions.ApiDescription.Server @ 10.0.12

Область: MSBuild-пакет генерации документа OpenAPI при `dotnet build`. Регистрация и содержимое документа (`AddOpenApi`, трансформеры, `Microsoft.OpenApi`) — `microsoft-aspnetcore-openapi-10.0.12.md`; потребитель результата (TS-типы) — `openapi-typescript-7.13.0.md`.

## Canonical source
- Документация, раздел про генерацию при сборке (страница 2026-09-04): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
- Release notes ASP.NET Core 10 (страница 2026-08-31): https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0
- NuGet (nuspec 10.0.12): https://api.nuget.org/v3-flatcontainer/microsoft.extensions.apidescription.server/10.0.12/microsoft.extensions.apidescription.server.nuspec — лицензия MIT, `developmentDependency: true`, `minClientVersion 2.8`, группы зависимостей не заявлены, описание «MSBuild tasks and targets for build-time Swagger and OpenApi document generation»; репозиторий https://github.com/dotnet/dotnet (commit `95017c711e6afc1085133d440e42b4bd78155701`, тот же, что у `dotnet-ef` и `Microsoft.EntityFrameworkCore.Design` 10.0.12)
- Индекс версий: https://api.nuget.org/v3-flatcontainer/microsoft.extensions.apidescription.server/index.json — последняя стабильная 10.0.12 (10.0.6–10.0.12 присутствуют; 10.0.13+ нет); 11.0.0-preview.1…7 и 11.0.0-rc.1 — не использовать
- MSBuild-свойства и цели (ветка release/10.0): https://raw.githubusercontent.com/dotnet/aspnetcore/release/10.0/src/Tools/Extensions.ApiDescription.Server/src/build/Microsoft.Extensions.ApiDescription.Server.props , `…/Microsoft.Extensions.ApiDescription.Server.targets`
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — механизм (инструмент `dotnet-getdocument`, MSBuild-свойства) существует с более ранних версий, но патч 10.0.12 новее среза знаний, а вокруг него в .NET 10 изменилось всё остальное: OpenAPI 3.1 по умолчанию, `Microsoft.OpenApi` 2.x, формат результата при сборке — только JSON (YAML при сборке не поддерживается). Ошибочное предположение «`openapi.yaml` при сборке» уже было в стеке до проверки.

## API surface used in project
- Подключение: `<PackageReference Include="Microsoft.Extensions.ApiDescription.Server" Version="10.0.12"><PrivateAssets>all</PrivateAssets></PackageReference>` в проекте backend (dev-зависимость; версия строго равна версии платформы 10.0.12). Вместе с `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` обязателен — без него в приложении нечего генерировать).
- После установки документ генерируется на `dotnet build` без доп. шагов (`OpenApiGenerateDocumentsOnBuild` по умолчанию `true`, пока `OpenApiGenerateDocuments` = `true`); результат — **только JSON**. YAML на этапе сборки .NET 10 не поддерживается (документация: «Generating OpenAPI documents in YAML format at build time isn't supported but planned for a future preview»); YAML — лишь runtime-маршрут `app.MapOpenApi("/openapi/{documentName}.yaml")`.
- MSBuild-свойства (по `.props` ветки release/10.0 и документации):
  - `OpenApiGenerateDocumentsOnBuild` — генерировать после каждой сборки (по умолчанию `true`);
  - `OpenApiDocumentsDirectory` — каталог результата; по умолчанию `$(BaseIntermediateOutputPath)` (то есть `obj/`; в документации — «app's output directory», пример `cat obj/{ProjectName}.json`), путь трактуется относительно папки проекта, либо абсолютный; `.` — каталог проекта;
  - `OpenApiGenerateDocumentsOptions` — аргументы инструмента: `--file-name <имя без расширения>`, `--document-name <имя>` (иначе генерируются все зарегистрированные документы), `--openapi-version OpenApi3_1` (из release notes).
- Имя файла: `{ProjectName}.json` для документа `v1`; при нескольких документах и имени не `v1` — `{ProjectName}_{DocumentName}.json`. Пример свойств (иллюстрация, итоговое имя файла проверить после первой сборки): `<OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/openapi</OpenApiDocumentsDirectory>`, `<OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>`.
- Механизм: инструмент (`dotnet-getdocument` → `GetDocument.Insider`) загружает собранную сборку и запускает entry point приложения с заглушкой `IServer`; выполняется весь код старта (DI, конфигурация), потому что информацию в документе нельзя получить статическим анализом.

## Version-specific notes
- Генерация запускается из `dotnet build` одного проекта (single-target `net10.0`); для multi-target проектов по `.targets` генерация на сборке не включается (пересказ `.targets`, дословно не сверялось) — проект backend single-target.
- `OpenApiGenerationEnvironment` (выбор окружения при генерации) — **только .NET 11+**; в 10.0.12 его нет. Окружение при сборке задаётся только переменными `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` процесса сборки; значение по умолчанию не проверялось — не рассчитывать на `appsettings.Development.json` и не делать генерацию зависимой от окружения.
- Terminal Logger скрывает сообщения шага `GetDocument`: `dotnet build -tlp:v=d` или `dotnet build --tl:off`.
- Пакет не зависит от NuGet-пакетов (в nuspec группы зависимостей отсутствуют), поэтому в deps/lock транзитивных элементов нет.
- README пакета в репозитории минимален (ссылается на партнёрские пакеты NSwag/Swashbuckle); достоверные сведения — документация ASP.NET Core и `.props`/`.targets`.

## Deprecations and breaking changes from prior version (.NET 9 → 10)
Changelog по release notes ASP.NET Core 10 (страница 2026-08-31), только то, что затрагивает генерацию при сборке:
- OpenAPI 3.1 — версия документа по умолчанию; nullable выражается массивом в `type` (`["string","null"]`), а не `nullable: true`; потребителям (openapi-typescript 7.13 поддерживает 3.0/3.1) — проверить на реальных данных. Сменить версию на сборке: `--openapi-version OpenApi3_0`/`OpenApi3_1`.
- `Microsoft.OpenApi` 2.0: трансформеры переписываются под интерфейсы схемы, `JsonNode` вместо `OpenApiAny`, `Nullable` удалён (см. `microsoft-aspnetcore-openapi-10.0.12.md`).
- YAML: в .NET 10 — только runtime-эндпойнт; на сборке не поддерживается (в release notes — «added in a future preview»).
- Генерация при сборке — обычный `dotnet build`: нужно, чтобы **код старта не требовал БД** (см. ниже).
- `Microsoft.Extensions.ApiDescription.Client` и `dotnet openapi` устарели (генерация клиентов — сторонние генераторы; в проекте — openapi-typescript).

## Project conventions
- Контракт API — файл JSON, сгенерированный при сборке backend; TS-типы SPA генерируются из него (`openapi-typescript`), YAML в проекте не нужен. Все упоминания «`openapi.yaml`» в проекте читать как «JSON-документ OpenAPI».
- Старт приложения не должен трогать БД/конфигурацию, которых нет на сборочном хосте: условие вида `if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider") { /* регистрация DbContext, строки подключения, миграции, фоновые сервисы */ }` (документация, раздел «Customize runtime behavior during build-time document generation»; применимо к 10.0 и 11.0). Сборка не должна требовать доступа к PostgreSQL; сборочный образ SDK не имеет БД.
- Размещение результата (в репозитории рядом с проектом или только в `obj/`) и порядок стадий multi-stage сборки (SPA ждёт JSON от backend) — решение design (см. раздел «Ограничения и открытые вопросы» `stack.html`).
- Версия пакета ведётся вместе с платформой (10.0.12); обновление — только через спринт.

## Known issues and workarounds
- Если регистрация сервисов старта падает без БД/секретов — генерация падает ошибкой сборки: закрывать проверкой entry assembly (выше) либо поставлять безопасные значения по умолчанию для сборки.
- Несколько документов с именем не `v1` — к имени файла добавляется `_{DocumentName}`; фиксировать `--file-name`/`--document-name`, чтобы путь для кодогенерации был стабилен.
- Свойство `OpenApiGenerateDocumentsOnBuild` в тексте страницы документации не описано — подтверждено по `.props` пакета (ветка release/10.0).
- Не проверялось на реальном проекте: полное отсутствие побочных эффектов при запуске entry point (фоновые сервисы, Identity/DataProtection, DbContext-регистрации), порядок хука относительно `Build` в `.targets` (пересказ), поведение при `PublishAot`/trimming.
