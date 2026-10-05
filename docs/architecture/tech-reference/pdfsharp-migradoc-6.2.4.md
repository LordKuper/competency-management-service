---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# PDFsharp-MigraDoc @ 6.2.4

Область: экспорт PDF (PDFsharp + MigraDoc, MIT) в контейнере Linux; кириллические шрифты — в образе `app`. Пакет `PDFsharp-MigraDoc` — кроссплатформенная («core») сборка без GDI+/WPF; пакеты `…-GDI`/`…-WPF` (Windows) не используются.

## Canonical source
- Документация: https://docs.pdfsharp.net/ (Font resolving: https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html; Upgrade to PDFsharp 6: https://docs.pdfsharp.net/General/Overview/Upgrade-to-PDFsharp-6.html; History: https://docs.pdfsharp.net/General/History.html)
- Репозиторий и лицензия (MIT, empira Software GmbH): https://github.com/empira/PDFsharp
- NuGet (nuspec 6.2.4): https://api.nuget.org/v3-flatcontainer/pdfsharp-migradoc/6.2.4/pdfsharp-migradoc.nuspec — лицензия MIT; `net8.0`, `net9.0`, `net10.0`, `netstandard2.0`; зависимости: `PDFsharp` 6.2.4, `Microsoft.Extensions.Logging.Abstractions` 8.0.3, `System.Security.Cryptography.Pkcs` 8.0.1. Линия 6.x: 6.2.0–6.2.4 (6.2.4 — последняя стабильная), в реестре есть только `7.0.0-preview-1`.
- Last verified: 2026-10-05
- Оценка риска знаний (Phase 5): **MEDIUM** — 6.2.4 новее среза знаний; документация скудная (разделы про Unicode/кириллицу и пример `IFontResolver` не найдены); на Linux обязателен собственный резолвер шрифтов.

## API surface used in project
- Шрифты (обязательно на Linux): реализовать `PdfSharp.Fonts.IFontResolver` — `FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)` (сопоставление семейства/начертания с именем лица) и `byte[]? GetFont(string faceName)` (байты шрифта; шрифт — файл из образа `app` или embedded resource). Регистрация **один раз при старте до создания первого `XFont`**: `GlobalFontSettings.FontResolver = new ProjectFontResolver();` (дополнительно `GlobalFontSettings.FallbackFontResolver`).
- `FontResolverInfo` содержит имя лица и признаки имитации bold/italic; для кириллицы в наборе нужны реальные Regular/Bold(/Italic) файлы шрифтов с кириллическими глифами.
- MigraDoc: объектная модель `MigraDoc.DocumentObjectModel.Document` (Sections, Paragraph, Table, Styles) и рендер — `MigraDoc.Rendering.PdfDocumentRenderer` → `PdfDocument.Save(Stream)`. Иллюстративно: `var r = new PdfDocumentRenderer { Document = doc }; r.RenderDocument(); r.PdfDocument.Save(stream, false);` (сверить сигнатуры по API reference 6.2.4).
- PDFsharp напрямую (при необходимости): `PdfDocument`, `PdfPage`, `XGraphics`, `XFont`, `XFontStyleEx`.

## Version-specific notes
- Core-сборка (Linux/macOS): поддерживаемые форматы изображений — JPG, PNG, BMP (PNG читает встроенная public-domain библиотека BigGustave); «cannot create paths from fonts» (по Upgrade-to-6).
- 6.2.0: цифровые подписи PDF, многоцветные глифы (emoji), PDF/A (с оговорками), восстановлено чтение UTF-16LE; .NET Framework 4.6.2; добавлена цель .NET 8; MD5 заменён на MD5Managed (FIPS).
- 6.2.3: поддержка .NET 9/10; больше не компилируется против .NET 6 (пакеты NuGet остаются пригодными для .NET 6). 6.2.4: вместо полного пакета логирования используется `Microsoft.Extensions.Logging.Abstractions` (GitHub #325).
- Для разработки на Windows есть `GlobalFontSettings.UseWindowsFontsUnderWindows`/`UseWindowsFontsUnderWsl2` (читают системные Arial, Times New Roman, Courier New, Verdana, Lucida Console, Symbol) — документация: только для разработки, на production нужен собственный резолвер. `GlobalFontSettings.ResetFontManagement()` сбрасывает кеши в тестах (обходит правило «резолвер назначается один раз»).
- Unicode/кириллица: по неофициальным источникам (форум PDFsharp) кириллица работает при использовании шрифта с нужными глифами; в первичной документации 6.x настройки кодировки (`PdfFontEncoding`/`XPdfFontOptions`) не найдены — **проверить spike'ом** на реальных русских строках (включая «ё», длинные ФИО, таблицы с переносами).

## Deprecations and breaking changes from prior version (до 6.0 → 6.0; 6.0 → 6.2)
- 6.0: на Linux без `IFontResolver` шрифты не загружаются; `FailsafeFontResolver` (пример) всегда подставляет шрифт Segoe — годится только для тестов.
- 6.0: `XFontStyle` переименован в `XFontStyleEx`; неявные преобразования `XUnit` устарели (`XUnitPt`/`Point`); свойства `XGraphics` `_MUH_`/`_MFEH_` удалены; MigraDoc: `DefaultPageSetup` бросает исключение при изменении — править `PageSetup` секций или присваивать клон; миграция с PdfSharpCore — заменить пространства имён `PdfSharpCore` на `PdfSharp`.
- 6.2.0: `CoreBuildFontResolver` удалён → `UseWindowsFontsUnderWindows`/`UseWindowsFontsUnderWsl2`.
- 7.0.0-preview-1 (в реестре): таргеты .NET 9/10, требуется пересборка при переходе с 6.x — в проект не берётся.

## Project conventions
- Шрифты для PDF — файлы в образе `app` (лицензии OFL/Apache), единый `IFontResolver` проекта регистрируется однократно при старте приложения до первого рендера.
- Документы строятся на сервере из данных БД; PII не логируется; рендер — в поток.
- Только MIT-библиотеки: QuestPDF отклонён; новых зависимостей для PDF не добавлять.
- Объём/сложность отчётов ограничивать (память пода) — решает design.

## Known issues and workarounds
- Без `IFontResolver` на Linux шрифты не загружаются (Upgrade-to-6) — проверять регистрацию резолвера и наличие шрифтовых файлов в образе на старте приложения (fail-fast).
- Доступные примеры `IFontResolver` в документации отсутствуют — собственный резолвер писать по двум методам интерфейса; `FailsafeFontResolver` не использовать в production.
- Поддержка PDF/A заявлена «с ограничениями» — при требовании PDF/A проверять отдельно.
