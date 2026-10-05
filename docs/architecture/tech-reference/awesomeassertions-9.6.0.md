# AwesomeAssertions @ 9.6.0

## Canonical source
- Official docs: https://awesomeassertions.org/ (introduction: https://awesomeassertions.org/introduction)
- Repository / README / releases: https://github.com/AwesomeAssertions/AwesomeAssertions , https://github.com/AwesomeAssertions/AwesomeAssertions/releases (9.6.0 — 2026-08-20)
- Пакет: https://www.nuget.org/packages/AwesomeAssertions/9.6.0 (Apache-2.0; nuspec: https://api.nuget.org/v3-flatcontainer/awesomeassertions/9.6.0/awesomeassertions.nuspec)
- Last verified: 2026-10-05

## API surface used in project
- Пакет `AwesomeAssertions`, пространство имён `using AwesomeAssertions;`.
- Fluent-assertions через `Should()`: example: `actual.Should().StartWith("AB").And.EndWith("HI").And.Contain("EF");`, `numbers.Should().OnlyContain(n => n > 0);`, `action.Should().Throw<RuleViolationException>();`.
- Заявленная совместимость тест-фреймворков: MSTest2/3/4, xUnit2, **xUnit3**, NUnit3/4, MSpec, TUnit — xunit.v3 поддерживается.
- API унаследован от FluentAssertions 7.x (README: сообщество-форк кодовой базы ≤7.x под Apache-2.0); детали конкретных методов сверять с документацией AwesomeAssertions, а не FluentAssertions ≥8.

## Version-specific notes
- Риск знаний (Phase 5): **MEDIUM** — линия 9.x, релизы 9.3–9.6 новее известного автору; release notes прочитаны (GitHub Releases API).
- Целевые платформы 9.6.0 (nuspec): net47, netstandard2.0, netstandard2.1, net6.0, net8.0; единственная зависимость — System.Threading.Tasks.Extensions 4.5.4 (только net47 / netstandard2.0), на net8.0+ зависимостей нет.
- 9.6.0 (2026-08-20): возможность добавлять reportables в `AssertionScope`; опция `ExcludingObsoleteMembers` для структурного сравнения; исправлены дублирование правил сопоставления и регистр в сообщениях.
- 9.5.0 (2026-07-19): `[Not]BeDecoratedWith` для `ParameterInfo`; `AssertionChain.ForCondition` с ленивым условием; `IntersectWith` возвращает `AndWhichConstraint`.
- 9.4.0 (2026-02-18): сборка под .NET 10; настраиваемый максимум элементов при сообщениях о коллекциях; атрибуты `[return: NotNull]` у `Should()`.
- 9.3.0 (2025-10-29): поддержка MSTest V4; исключение по имени члена; улучшена визуализация табуляций в diff строк.

## Deprecations and breaking changes from prior version
- 9.0.0: проект переименован FluentAssertions → AwesomeAssertions, сборка под net8.0 (GitHub release). Для 9.1–9.6 breaking changes в release notes не заявлено.
- Миграция с FluentAssertions: дроп-ин цель — AwesomeAssertions 7.0.0-совместимый API (README); FluentAssertions ≥8 (коммерческая лицензия Xceed) не используется (stack.html).

## Project conventions
- Только AwesomeAssertions; пакеты FluentAssertions в решение не добавлять (лицензионные риски, stack.html).
- Один стиль assertions в тестах: `Should()` из AwesomeAssertions; встроенный `Assert` из xunit.v3 с ним не смешивать без необходимости (сообщения об ошибках и стиль).
- Версия закреплена точно; обновление — через спринт.

## Known issues and workarounds
- Известных проблем для 9.6.0 в просмотренных release notes не обнаружено; при странном поведении сверять с GitHub Issues проекта.
