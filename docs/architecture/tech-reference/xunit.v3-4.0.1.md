# xunit.v3 @ 4.0.1

## Canonical source
- Official docs: https://xunit.net/docs/getting-started/v3/getting-started
- Microsoft Testing Platform (xunit.v3): https://xunit.net/docs/getting-started/v3/microsoft-testing-platform ; обзор MTP: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro
- Release notes: https://xunit.net/releases/v3/4.0.1 (2026-09-12), https://xunit.net/releases/v3/4.0.0 (2026-08-14)
- Пакет: https://www.nuget.org/packages/xunit.v3/4.0.1 (Apache-2.0; nuspec: https://api.nuget.org/v3-flatcontainer/xunit.v3/4.0.1/xunit.v3.nuspec)
- Last verified: 2026-10-05

## API surface used in project
- Пакет `xunit.v3` 4.0.1 → `xunit.v3.mtp-v2 [4.0.1]` → `xunit.v3.core.mtp-v2 [4.0.1]`, `xunit.v3.assert [4.0.1]`, `xunit.analyzers 2.1.0` (nuspec; группы net8.0 и net472, net10.0 подходит по совместимости TFM).
- Тест-проект — исполняемый: `<OutputType>Exe</OutputType>`; запускается напрямую (`dotnet run`) или через `dotnet test`.
- `dotnet test` в режиме Microsoft Testing Platform (MTP) на .NET 10 SDK включается в `global.json` (не свойством проекта); example: `{ "test": { "runner": "Microsoft.Testing.Platform" } }`.
- Фильтры в режиме MTP передаются напрямую: `dotnet test --filter-class ClassName` (xunit docs).
- `[Fact]` / `[Theory]`; общий контекст: `IClassFixture<T>`; `[CollectionDefinition]` + `ICollectionFixture<T>` + `[Collection("name")]`; `[assembly: AssemblyFixture(typeof(T))]` (создаётся один раз до первого теста сборки, освобождается после последнего); async-setup — `IAsyncLifetime` (`ValueTask InitializeAsync()`; `DisposeAsync` — из `IAsyncDisposable`).
- Параллелизм: по умолчанию по коллекциям (один класс = одна коллекция); в 4.0: `[assembly: Parallelization(Mode = ParallelMode.None)]` / `ParallelMode.All`, точечный опт-аут `[Fact(DisableParallelism = true)]`, `[TestClass(DisableParallelism = true)]`.

## Version-specific notes
- Риск знаний (Phase 5): **HIGH** — мажор 4 новее известной автору линии 3.x; changelog 4.0.0 / 4.0.1 прочитан по первичным источникам (ссылки выше).
- 4.0.1 (2026-09-12): Microsoft Testing Platform обновлена до 2.4.0; исправлена сериализация `DateTime` / `DateTimeOffset` (раньше значения нормализовались в UTC — терялась информация о часовом поясе и ложно определялись дубликаты; важно для `[Theory]` с датами); константа `XUNIT_GENERATED_DISABLE_WARNINGS` отключает предупреждения компилятора в генерируемом коде; новый отчёт Markdown и расширенный CTRF-отчёт у раннеров.
- 4.0.0 (2026-08-14): поддержка Native AOT; полностью параллельный режим; orderers классов и методов (порядок: коллекция → класс → метод → кейс); `ITestMetadata.TestLabel`; уведомления жизненного цикла фикстур; generic-варианты атрибутов (.NET 8+). Поддержка MTP v1 прекращена — только v2; поддержка Mono прекращена. Консольный раннер (dotnet tool) по release notes требует .NET 10 SDK+.
- Пакеты `Microsoft.NET.Test.Sdk` и `xunit.runner.visualstudio` описаны как нужные для VSTest-режима; для MTP достаточно `xunit.v3`.

## Deprecations and breaking changes from prior version
- 3.x → 4.0: `AssemblyRunnerOptions.ParallelizeTestCollections` obsolete → `ParallelMode`; свойства `CollectionBehavior` (`DisableTestParallelization`, `MaxParallelThreads`) obsolete → атрибут `Parallelization`; `TransformFactory` obsolete → методы `RegisteredRunnerConfig`; изменены сигнатуры части конструкторов раннеров/контекстов; удалены `CecilSourceInformationProvider`, прежний конструктор `ExecutionErrorTestCase`, пространство `Xunit.Runners`; реализации `ITestCaseOrderer` пересмотреть из-за нового порядка orderers.
- 4.0.1: `TestFrameworkExecutor.CreateDiscoverer` помечен obsolete (удаление в следующем мажоре).
- Из v2 (для справки): `async void` тесты падают сразу (нужен `Task`/`ValueTask`); `IAsyncLifetime` возвращает `ValueTask`; `ITestOutputHelper` в пространстве `Xunit`; пакеты `xunit` → `xunit.v3`, `xunit.assert` → `xunit.v3.assert`.

## Project conventions
- Тест-проекты — `OutputType Exe`, запуск через `dotnet test` в режиме MTP (stack.html); VSTest и MTP в одном решении не смешивать (Microsoft: такой сценарий не поддерживается).
- Assertions — AwesomeAssertions (`awesomeassertions-9.6.0.md`), моки — NSubstitute (`nsubstitute-6.2.0.md`); FluentAssertions ≥8 и Moq запрещены (stack.html, «Рассмотрено / не принято»).
- Интеграционные тесты с БД — общий контейнер PostgreSQL через fixture (`testcontainers-postgresql-4.15.0.md`); тесты, делящие контейнер/БД, либо объединяются в коллекцию, либо получают изоляцию (отдельная БД) — решение в тест-стратегии design.
- Версии закреплены точно; обновление — только через спринт.

## Known issues and workarounds
- MTP строже VSTest: по умолчанию может завершаться ошибкой, если ни один тест не выполнен (Microsoft: «can fail when no tests run») — проверять фильтры в CI.
- Если фикстура реализует и `IDisposable`, и `IAsyncDisposable`, вызывается только один из них (v3) — освобождение ресурсов класть в `DisposeAsync`.
