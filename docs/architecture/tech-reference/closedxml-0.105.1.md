---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# ClosedXML @ 0.105.1

Область: экспорт XLSX (MIT) на сервере; кириллические шрифты — в образе `app`.

## Canonical source
- Документация: https://docs.closedxml.io/en/latest/ (разделы Graphic Engine, Tips & Tricks → Missing font, Bulk insert data, Migrations)
- Релизы (GitHub API): https://api.github.com/repos/ClosedXML/ClosedXML/releases — 0.105.1 (2026-07-25: «bound SixLabors.Fonts to version range [1.0.0,3.0.0)»), 0.105.0 (2025-05-14), 0.104.2 (2024-11-15, обновление DocumentFormat.OpenXml/RBush из-за уязвимостей), 0.104.1 (2024-09-30)
- NuGet (nuspec 0.105.1): https://api.nuget.org/v3-flatcontainer/closedxml/0.105.1/closedxml.nuspec — лицензия MIT; `netstandard2.0`/`netstandard2.1`; зависимости: `ClosedXML.Parser` 2.0.0, `DocumentFormat.OpenXml` `[3.1.1, 4.0.0)`, `ExcelNumberFormat` 1.1.0, `RBush.Signed` 4.0.0, `SixLabors.Fonts` `[1.0.0, 3.0.0)` (+ `Microsoft.Bcl.HashCode`, `System.Buffers`, `System.Memory` на netstandard2.0). Стабильных версий ≥ 1.0 и prerelease новее 0.105.1 в реестре нет.
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — библиотека 0.x с нестабильным API; 0.105.1 (2026-07) новее среза знаний; документация упоминает уже миграцию 0.105 → 0.106.

## API surface used in project
- Книга: `using var wb = new XLWorkbook(); var ws = wb.AddWorksheet("Лист1");` значения/стили через `ws.Cell(r, c)`; сохранение в поток — `wb.SaveAs(stream)` (в документации упомянут `SaveAs`; потоковая перегрузка — сверить по API reference).
- Пакетная вставка: `InsertData` (коллекции `IEnumerable<T>`) и `InsertTable` (`DataTable`/коллекции) — см. «Bulk insert data».
- Ширина колонок: `ws.Column(n).AdjustToContents()` — требует метрик шрифта (graphic engine); альтернативно `IXLColumn.Width` (в множителях максимальной ширины цифры), `IXLRow.Height` (в пунктах).
- Graphic engine (шрифты): `DefaultGraphicEngine` (пространство имён `ClosedXML.Graphics`), статический `Instance` (запасной шрифт «Microsoft Sans Serif», системные шрифты через SixLabors.Fonts); конструктор `new DefaultGraphicEngine("Имя шрифта")`; фабрики `DefaultGraphicEngine.CreateWithFontsAndSystemFonts(Stream fallbackFont, params Stream[] fonts)` и `CreateOnlyWithFonts(Stream fallbackFont, params Stream[] fonts)` (без системных шрифтов); подключение — на книгу `new XLWorkbook(new LoadOptions { GraphicEngine = … })` или глобально `LoadOptions.DefaultGraphicEngine = …`.
- Тексты ячеек: тип Text — до 32 767 символов (лимит Excel); значения Blank/Logical/Number/Text/Error.

## Version-specific notes
- Порядок поиска шрифта в `DefaultGraphicEngine` (0.105.x): шрифт из книги → именованный запасной → встроенный `CarlitoBare` (метрическое подобие Calibri, только метрики глифов, без контуров). На Linux при отсутствии шрифта используется встроенный запасной, поэтому `AdjustToContents` не должен падать; покрытие кириллицы в встроенных метриках в источниках не указано.
- Зависимости: `SixLabors.Fonts` `[1.0.0, 3.0.0)` — NuGet выбирает нижнюю границу (1.0.0, Apache-2.0); версии 2.x существуют (2.0.0–2.1.3) и в диапазон входят; в `stack.html` требуется следить, чтобы в lock-файле остался 1.x (прямых ссылок на `SixLabors.*` не добавлять — Six Labors Split License).
- Аудит NuGet (.NET 10 SDK по умолчанию `all`) проверяет и транзитивные зависимости (`DocumentFormat.OpenXml`, `SixLabors.Fonts`, …).
- Память: модель книги целиком в памяти (в release notes 0.104.1 — оптимизации хранения значений и оценки по времени загрузки 250 тыс. строк) → при больших выгрузках учитывать лимиты памяти пода; потоковая запись большого объёма в изученных источниках не описана.

## Deprecations and breaking changes from prior version (0.104 → 0.105)
- Hyperlinks: `IXLRangeBase.Hyperlinks` перенесён в `IXLWorksheet.Hyperlinks`; `IXLHyperlinks.Add()` и `TryDelete()` удалены; `Delete()` возвращает `bool`.
- Формулы: в `FormulaA1`/`FormulaR1C1` значения обрезаются и убирается ведущий `=`; для функций новее 2013 префикс `_xlfn` добавляется автоматически (`CONCAT` → `_xlfn.CONCAT`); `CHAR()` трактует значения как Win1252; `DOLLAR()` использует культуру книги.
- 0.105.0: переписана инфраструктура ~180 функций, сортировка диапазона обновляет ссылки в формулах.
- 0.104.1 (для справки): XLParser заменён на ClosedParser, OpenXML SDK 3.0, перестроены pivot-таблицы, пересмотрен AutoFilter.

## Project conventions
- Только экспорт (запись) XLSX (по `stack.html`); значения записываются как данные, формулы для экспорта не предполагаются.
- Кириллические шрифты поставляются в образе `app` (OFL/Apache); для расчёта ширины колонок при отсутствии системных шрифтов явно задавать graphic engine с шрифтом проекта (`CreateWithFontsAndSystemFonts(fontStream)`), либо выставлять ширины колонок вручную; шрифт книги по умолчанию — тот, что поставляется в образе (не Calibri/Arial).
- Пользовательские строки, начинающиеся с `=`, `+`, `-`, `@`, при экспорте не должны превращаться в формулы (защита от formula injection): записывать как текст (явный тип/`SetValue`) — поведение присваивания `Cell.Value` для строк с `=` в доступных источниках не описано → закрыть тестом.
- Прямых ссылок на `SixLabors.*` не добавлять; `lock`-файл проверять на `SixLabors.Fonts` 1.x.
- Выгрузки ограничивать по объёму (лимиты строк/памяти) — решает design.

## Known issues and workarounds
- Отсутствие системного шрифта на Linux: решение документации — явный fallback через `LoadOptions.DefaultGraphicEngine = new DefaultGraphicEngine("DejaVu Sans")` (имя шрифта из `SixLabors.Fonts.SystemFonts.Collection.Families`) или встроенный Carlito (`CreateWithFontsAndSystemFonts(stream)`); «Create adapter» (`IXLGraphicEngine`) — крайний вариант.
- Несовпадение имён: в документации класс указан как `ClosedXML.Graphic.DefaultGraphicEngine`, в исходниках 0.105.1 — пространство имён `ClosedXML.Graphics`; ориентироваться на исходники.
- Документация упоминает версию 0.106 (миграция 0.105 → 0.106) — в реестре NuGet на дату проверки её нет; обновление только через спринт.
