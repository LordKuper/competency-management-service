---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# Biome (`@biomejs/biome`) @ 2.5.15

## Canonical source
- Official docs: https://biomejs.dev/ ; установка: https://biomejs.dev/guides/getting-started/ ; конфигурация: https://biomejs.dev/reference/configuration/
- Релиз 2.5 (2026-06-05): https://biomejs.dev/blog/biome-v2-5/ ; миграция v1→v2: https://biomejs.dev/guides/upgrade-to-biome-v2/ ; релизы: https://github.com/biomejs/biome/releases
- npm: https://www.npmjs.com/package/@biomejs/biome (манифест 2.5.15: `engines.node >=14.21.3`, без dependencies; `bin: biome`; optionalDependencies — платформенные пакеты `@biomejs/cli-{linux-x64, linux-x64-musl, linux-arm64, linux-arm64-musl, win32-x64, win32-arm64, darwin-x64, darwin-arm64}@2.5.15`)
- Last verified: 2026-10-05 (dist-tag `latest = 2.5.15`)

## API surface used in project
Lint + форматирование (+ organize imports через `assist`) frontend одним нативным бинарником; TypeScript compiler API не используется.
- Установка: `npm i -D -E @biomejs/biome` (точная версия; `-E` подчёркнут в документации).
- Команды: `biome ci` (CI-вариант `check`: формат + lint + imports, без записи), `biome check --write` (локально), `biome format --write`, `biome lint --write` (безопасные автоисправления).
- Конфиг `biome.json`: `$schema` на `https://biomejs.dev/schemas/2.5.15/schema.json` (версия схемы = версия пакета), `files.includes`, `formatter` (`indentStyle`, `indentWidth`, `lineWidth`), `vcs` (`useIgnoreFile`), `linter`, `assist`.
- Платформа: бинарник выбирается по `os`/`cpu`/`libc`; `@biomejs/cli-linux-x64` (glibc, без install-скриптов — по манифесту) нужен в образе `node:24.21.0-trixie-slim`.

## Version-specific notes
- 2.5 (2026-06-05): более 500 lint-правил, 73 nursery-правила переведены в стабильные группы, форматтер-опция `delimiterSpacing`, режим `--watch` для `lint`/`format`/`check`, reporter `concise`, команда `biome upgrade` (для автономной установки — при установке через npm не нужна).
- Правила могут быть type-aware (собственный вывод типов Biome, без TypeScript API) — поэтому закрепление TypeScript 6.0.x на Biome не влияет (stack.html).
- Новые правила появляются в патч-релизах (2.5.9–2.5.15 добавляли nursery-правила и правки производительности) — при повторных запусках `biome ci` на новом патче возможны новые предупреждения; версия закреплена точно.

## Deprecations and breaking changes from prior version
- 2.4 → 2.5: `linter.rules.recommended` объявлен deprecated, вместо него — `"preset": "recommended"`; автоматическая миграция — `biome migrate --write`. Точное размещение ключа сверять по JSON-схеме 2.5.15 (в документации по конфигурации пример схемы ещё на 2.3.11).
- 1.x → 2.0 (для справки; проект новый): `files.ignore`/`files.include` заменены единым `files.includes` (пути относительно файла конфигурации, `*` не пересекает `/`, только `**`); удалена опция `all`; правила имеют индивидуальную severity по умолчанию; перепис organize imports (Node-модули больше не приоритизируются); удалена поддержка `rome.json`.

## Project conventions
- Biome — единственный инструмент lint+format frontend (без ESLint/Prettier).
- Типы проверяет `tsc --noEmit`, не Biome.
- `biome ci` — обязательный шаг перед сборкой; `biome.json` в репозитории, `$schema` обновляется вместе с версией пакета.
- Лок-файл npm обязан содержать `@biomejs/cli-linux-x64` (и платформу разработки) — см. `npm-11.19.0.md`.

## Known issues and workarounds
- Не выявлено. Поддержка синтаксиса, добавленного в TypeScript 6.0 (например, `#/` subpath imports), не проверялась — проверить при реализации на реальном коде.
