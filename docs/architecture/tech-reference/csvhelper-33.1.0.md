---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# CsvHelper @ 33.1.0

Область: экспорт CSV (запись). Чтение CSV (импорт пользователей и т.п.) — только если решит design.

## Canonical source
- Документация: https://joshclose.github.io/CsvHelper/ (Getting started: https://joshclose.github.io/CsvHelper/getting-started/; Class maps: https://joshclose.github.io/CsvHelper/examples/configuration/class-maps/)
- Change log: https://joshclose.github.io/CsvHelper/change-log
- Исходники конфигурации (ветка master): https://github.com/JoshClose/CsvHelper/blob/master/src/CsvHelper/Configuration/CsvConfiguration.cs, `InjectionOptions.cs`
- NuGet (nuspec 33.1.0): https://api.nuget.org/v3-flatcontainer/csvhelper/33.1.0/csvhelper.nuspec — лицензия `MS-PL OR Apache-2.0`; цели .NET Framework 4.6.2/4.7/4.8, .NET Standard 2.0/2.1, `net8.0`, `net9.0`; на `net8.0`/`net9.0` зависимостей нет (на net10.0 берётся актив `net9.0`); 33.1.0 — последняя версия (33.0.0, 33.0.1, 33.1.0)
- Last verified: 2026-10-05 (дата релиза 33.1.0 в изученных источниках не указана)
- Оценка риска знаний (Phase 5): **LOW** — линия 33.x (2025) в пределах знаний; changelog 30.x–33.1 сверен.

## API surface used in project
- Запись: `using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" });` затем `csv.WriteRecords(records)` / `await csv.WriteRecordsAsync(asyncEnumerable)`.
- Маппинг колонок — `ClassMap<T>`: `Map(m => m.Id).Index(0).Name("id")`; регистрация `csv.Context.RegisterClassMap<FooMap>()` (заголовки и порядок — на стороне проекта, не по именам свойств).
- Конфигурация `CsvConfiguration` (значения по умолчанию по исходникам master): `HasHeaderRecord = true`, `NewLine = "\r\n"`, `Encoding = Encoding.UTF8`, `Delimiter` зависит от культуры (разделитель списка), `MaxFieldSize`.
- Защита от CSV/formula injection: `InjectionOptions` — `None` (по умолчанию), `Escape`, `Strip`, `Exception`; `InjectionCharacters` по умолчанию `= @ + - \t \r`, `InjectionEscapeCharacter` — `'`.
- Асинхронный писатель поддерживает `Dispose`/`DisposeAsync` (добавлено в 33.1.0).

## Version-specific notes
- 33.1.0: убрана поддержка net6.0 и net7.0, добавлена net9.0; оптимизации (`IsDefined` вместо `GetCustomAttributes`, `CacheKey` для ObjectCreator); исправления `Dispose/DisposeAsync` и `UseDefaultOnConversionFailure` при `null`-значении по умолчанию.
- 33.0.0/33.0.1: nullable reference types во всей библиотеке, обновление `Microsoft.Bcl.AsyncInterfaces` до 8.0.0, усиление null-аннотаций.
- Лицензия двойная (`MS-PL OR Apache-2.0`) — обе допустимы по `stack.html`; регистрации ключей нет.

## Deprecations and breaking changes from prior version (32.x → 33.x; ключевые изменения 30.x–32.x)
- 33.0.0: включение nullable может потребовать правок в зависимом коде (breaking по источникам).
- 32.0.0: изменены сигнатуры `RecordWriter.CreateWriteDelegate`/фабрик (принимают `Type`), `RecordManager`/`RecordCreator` — методы `GetDelegate`; `RecordWriter.Create` → `GetWriteDelegate` (затрагивает только кастомные расширения).
- 31.0.0: исходный `TypeConverter` переименован в `NotSupportedTypeConverter`, новый generic `TypeConverter<T>` занял имя.
- 30.0.0: писатель получает `IWriterConfiguration`; новые свойства конфигурации `LeaveOpen`, `MaxFieldSize`, `InjectionOptions` (по change log; по стороннему источнику булево `SanitizeForInjection` было заменено перечислением `InjectionOptions` ещё в 29.x).

## Project conventions
- Всегда явная культура: `CultureInfo.InvariantCulture` и явный `Delimiter` (в русской Excel-локали списочный разделитель — `;`; поведение Excel источниками этого прохода не подтверждено — проверить на выгрузке); для открытия в Excel с кириллицей — UTF-8 с BOM (`new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)` в `StreamWriter`) — рекомендация, проверить в Excel.
- Включать `InjectionOptions.Escape` (или `Exception`) для всех пользовательских строк (ФИО, комментарии), раз значение по умолчанию — `None`; escape добавляет `'` к значениям, начинающимся с `= @ + - \t \r` (см. «Known issues»).
- Колонки задаются `ClassMap` (стабильный контракт выгрузки), без рефлексии по свойствам DTO.
- Писать в поток ответа/`MemoryStream`, большие выгрузки — потоково из `IAsyncEnumerable<T>` (`WriteRecordsAsync`).

## Known issues and workarounds
- Известных блокирующих проблем для записи в изученных источниках нет.
- Список `InjectionCharacters` по умолчанию включает `-`: значения, начинающиеся с `-` (в т.ч. отрицательные числа в строковом виде), могут экранироваться при `InjectionOptions.Escape` — проверить тестом на выгрузке; при необходимости настроить `InjectionCharacters`/форматирование чисел.
