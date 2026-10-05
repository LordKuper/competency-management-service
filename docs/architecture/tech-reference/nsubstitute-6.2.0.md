# NSubstitute @ 6.2.0

## Canonical source
- Official docs: https://nsubstitute.github.io/ (Getting started: https://nsubstitute.github.io/help/getting-started/)
- Release notes: https://github.com/nsubstitute/NSubstitute/releases (v6.2.0 — 2026-08-11)
- Пакет: https://www.nuget.org/packages/NSubstitute/6.2.0 (BSD-3-Clause; nuspec: https://api.nuget.org/v3-flatcontainer/nsubstitute/6.2.0/nsubstitute.nuspec)
- Last verified: 2026-10-05

## API surface used in project
- Создание подмены: `Substitute.For<IService>()` — example: `var svc = Substitute.For<IService>();`
- Возвращаемые значения: `svc.Add(1, 2).Returns(3);`, последовательность `svc.Mode.Returns("HEX", "DEC", "BIN");`, вычисляемое `svc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(x => (int)x[0] + (int)x[1]);`
- Проверка вызовов: `svc.Received().Add(1, 2);`, `svc.DidNotReceive().Add(5, 7);`
- Сопоставление аргументов: `Arg.Any<T>()`, `Arg.Is<T>(x => x < 0)`; события: `svc.PoweringUp += Raise.Event();`
- Подменять можно интерфейсы и члены классов, переопределяемые из тестовой сборки (virtual).

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — линия 6.x; changelog 6.0.0–6.2.0 прочитан (GitHub Releases API, даты — `published_at`).
- Целевые платформы пакета: net8.0 и netstandard2.0; зависимость Castle.Core 5.1.1 (для netstandard2.0 ещё System.Threading.Tasks.Extensions 4.5.4).
- 6.2.0 (2026-08-11): исправлено сопоставление generic-вызовов (больше не используется присваиваемость возвращаемого типа; исправлено сравнение generic-методов); поддержка C# 13 `params`-коллекций в сопоставлении аргументов; публикация NuGet через trusted publishing.
- 6.1.0 (2026-08-08): новый матчер сравнения объектов по ссылке; **аннотации nullability, введённые в 6.0, отменены** — публичный API снова без nullability.

## Deprecations and breaking changes from prior version
- 6.0.0 (2026-07-12) относительно 5.x: целевые платформы сведены к .NET 8 / .NET Standard 2.0; удалён устаревший (legacy obsolete) API; `CompatArg` помечен obsolete (поддержка до C# 7.0); nullability на публичном API для .NET 8+ (отменено в 6.1.0 — при миграции с 6.0.x ожидать исчезновения предупреждений nullability).
- 6.1.0 → 6.2.0: breaking changes не заявлено.

## Project conventions
- Подменяются границы приложения (свои интерфейсы), не EF Core / `DbContext`: доступ к данным проверяется интеграционными тестами на реальном PostgreSQL (stack.html, `testcontainers-postgresql-4.15.0.md`).
- Moq не используется (SponsorLink — stack.html, «Рассмотрено / не принято»).
- Подмены настраиваются до вызова тестируемого кода, `Received()` проверяется после его завершения (thread-safety guideline NSubstitute).

## Known issues and workarounds
- Не-виртуальные и `internal virtual` члены классов не подменяются — вызывается реальный код; предпочитать интерфейсы. NSubstitute.Analyzers рекомендуются документацией, но в стек не входят — добавление только через Complication Approval.
- Конфигурация и `Received()` во время параллельного использования подмены из других потоков даёт гонки: сначала настройка, затем прогон кода, затем проверка.
