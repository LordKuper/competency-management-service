---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# openapi-typescript @ 7.13.0

## Canonical source
- Official docs: https://openapi-ts.dev/introduction ; CLI: https://openapi-ts.dev/cli
- Changelog: https://raw.githubusercontent.com/openapi-ts/openapi-typescript/main/packages/openapi-typescript/CHANGELOG.md ; релизы: https://github.com/openapi-ts/openapi-typescript/releases
- Статус поддержки TypeScript 6/7: PR https://github.com/openapi-ts/openapi-typescript/pull/2774 , https://github.com/openapi-ts/openapi-typescript/pull/2872 ; issue https://github.com/openapi-ts/openapi-typescript/issues/2841
- npm: https://www.npmjs.com/package/openapi-typescript (манифест 7.13.0: **peer `typescript: ^5.x`** (без peerDependenciesMeta); dependencies `@redocly/openapi-core ^1.34.6`, `parse-json ^8.3.0`, `ansi-colors ^4.1.3`, `change-case ^5.4.4`, `yargs-parser ^21.1.1`, `supports-color ^10.2.2`; `bin: openapi-typescript`; без `engines`)
- Last verified: 2026-10-05 (dist-tag `latest = 7.13.0`; `next = 7.0.0-rc.1`; новее 7.13.0 релизов не найдено); spike на TypeScript 6.0.2 выполнен 2026-10-05 (Task 3 sprint 001)

## API surface used in project
- CLI: `npx openapi-typescript ./openapi.json -o ./src/api/schema.d.ts` — генерация типов `paths`/`components` из JSON-документа OpenAPI, который .NET 10 строит при сборке backend (`Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server`, OpenAPI 3.1; YAML при сборке .NET 10 не поддерживается, см. `microsoft-extensions-apidescription-server-10.0.12.md`). Поддерживаются OpenAPI 3.0 и 3.1; путь и имя файла — по настройке сборки backend (пример имени — иллюстрация).
- `--check` — проверка актуальности сгенерированного файла в CI: генерирует вывод в памяти и сравнивает **побайтово** с существующим файлом; код выхода 0 — файл актуален, 1 — расхождение (сообщение «Generated types are not up-to-date!»; CRLF вместо LF тоже расхождение), 1 — файла нет (необработанное `ENOENT`). Проверено spike (см. «Результат spike»).
- Флаги, влияющие на результат (применение — решение design/реализации): `--export-type`, `--immutable`, `--enum`/`--enum-values`, `--root-types`, `--path-params-as-types`, `--alphabetize`, `--exclude-deprecated`, `--default-non-nullable` (**по умолчанию `true`**: объекты со значением `default` считаются non-nullable, кроме параметров), `--read-write-markers` (7.13: `$Read<T>`/`$Write<T>`).
- Выход — `.d.ts`, на который ссылается `createClient<paths>` (openapi-fetch). Решение, коммитить ли сгенерированный файл или генерировать в сборке, в стеке не зафиксировано.

## Version-specific notes
- 7.x: TypeScript — **peer-зависимость** и используется как рантайм-библиотека (Node API возвращает AST `ts.Node[]`, генерация через `ts.factory`). Поэтому генератор чувствителен к версии компилятора.
- 7.13.0: `--read-write-markers`; 7.12.0: сохранение регистра root-типов, исправление `enumValues` для `oneOf/anyOf`; 7.11.0: условные TS-enum; 7.10.0: хук `transformProperty`, `patternProperties`.
- Ключевое наблюдение: peer `^5.x` **не** включает 6.0.2 → без обхода `npm install` завершается ошибкой ERESOLVE (воспроизведено в spike). Работоспособность с 6.0.2 подтверждена spike (генерация, `tsc --noEmit` по результату, `--check`) при точечном `overrides`; проблема — именно ограничение peer, а не рантайм (согласуется с PR #2774).
- Поддержка TypeScript 6 в upstream не выпущена на 2026-10-05: PR #2774 «add TypeScript 6 support» (peer `^5.x || ^6.x`) открыт с 2026-04-15; PR #2872 «Support TypeScript 6 and 7 without a runtime compiler dependency» (мажорный релиз: `openapiTS()` возвращает `Promise<string>`, TypeScript не peer; тесты на 5.9.3/6.0.3/7.0.2) открыт, ждёт ревью.
- С TypeScript 7.0.2 генератор не работает (`Cannot read properties of undefined (reading 'createKeywordTypeNode')`, issue #2841).

## Deprecations and breaking changes from prior version
6.x → 7.0.0 (для справки): TypeScript стал peer-зависимостью и должен ставиться рядом; Node API возвращает AST TypeScript вместо строк; флаги авторизации удалены (теперь `redocly.yaml`); `--immutable-types` → `--immutable`, `--support-array-length` → `--array-length`. OpenAPI 2.x — только в 5.x и ранее. 7.12 → 7.13: breaking не заявлено.

## Project conventions
- Генерация только из JSON-документа OpenAPI, построенного при сборке backend; DTO руками не пишутся; ручная правка сгенерированного файла запрещена.
- Установка `typescript` — только точной версией: `npm i -D -E typescript@6.0.2` (npm `latest` = 7.0.2, с которым генератор не работает — issue #2841); openapi-typescript — тоже точной версией `7.13.0`.
- `tsconfig`: `moduleResolution: bundler`, `noUncheckedIndexedAccess: true` (рекомендации документации).
- Сгенерированный `web/src/api/schema.d.ts` коммитится; перегенерация — `npm run gen:api` (читает `../openapi/openapi.json`, флагов нет), актуальность — `npm run check:api` (`--check`). Файл исключён из Biome и не правится вручную.
- `web/.gitattributes` (`* text=auto eol=lf`) обязателен: при `core.autocrlf=true` (умолчание Git for Windows) без него checkout даёт CRLF, и `--check` ошибочно сообщает о расхождении.
- `skipLibCheck: true` в `tsconfig` нужен из-за типов транзитивных пакетов antd (`@rc-component/*` не проходят проверку объявлений на TS 6.0.2 `strict`), но он отключает и проверку самого `schema.d.ts`; поэтому скрипт `typecheck` отдельно прогоняет `tsc --noEmit --strict --ignoreConfig src/api/schema.d.ts` (TS 6 не допускает файлы в командной строке без `--ignoreConfig`).
- Запасной вариант по stack.html — `@hey-api/openapi-ts 0.99.0` (не принят; документ не создаётся до принятия).

## Known issues and workarounds
- **Peer-конфликт `typescript ^5.x` vs 6.0.2 — снят точечным `overrides` (spike Task 3 выполнен 2026-10-05, см. «Результат spike»; манифест 7.13.0 повторно сверён: peer `typescript ^5.x`, без `peerDependenciesMeta`).** Варианты обхода: (1) точечный `overrides` в `package.json` frontend (`web/package.json`) для пакета `openapi-typescript` (npm: overrides читаются только из корня пакета; ссылка `"$typescript"` указывает на спецификацию прямой зависимости) — принят; обход не воздействует на остальные peer-проверки; (2) `legacy-peer-deps=true` в `.npmrc` — отключает peer-контроль для всего дерева (react-router, antd, TanStack Query), не рекомендуется документацией npm; не используется.
- Критерии spike: сгенерировать типы из JSON-документа OpenAPI на 6.0.2, прогнать `tsc --noEmit` по результату, убедиться в поведении `--check`; не включать `--read-write-markers` до исправления (в TS 6 `Date extends object` стало `true`, что затрагивает `Readable<T>`/`Writable<T>` по описанию PR #2774) — флаг не включён.
- Корректность разбора схемы .NET 10 (OpenAPI 3.1: `type: [..., "null"]`, перечисления, `oneOf`): на момент spike реальный `openapi.json` содержит `paths: {}` (эндпойнтов ещё нет), поэтому конструкции .NET 10 проверены на синтетическом документе той же формы (см. «Результат spike»); повторить на реальном документе при регенерации в Task 4–6.
- Запасной путь при провале spike — `@hey-api/openapi-ts` (версия до 1.0, закрепление точной версии) либо ожидание релиза PR #2872. Spike пройден, запасной путь не задействован.

## Результат spike (Task 3, 2026-10-05)
Среда: Node 24.15.0, npm 11.16.0 (Windows); `typescript` 6.0.2 (`npm i -D -E typescript@6.0.2`), `openapi-typescript` 7.13.0, `.npmrc`: `ignore-scripts=true`, `save-exact=true`, `engine-strict=true`.
- **ERESOLVE воспроизведён** (чистый каталог, `npm i -D -E typescript@6.0.2 openapi-typescript@7.13.0`): `Could not resolve dependency: peer typescript@"^5.x" from openapi-typescript@7.13.0`, `Found: typescript@6.0.2`.
- **Обход** в `web/package.json`: `"overrides": { "openapi-typescript": { "typescript": "$typescript" } }`; `legacy-peer-deps` не используется. После него `npm install` и `npm ci` проходят без ERESOLVE, `npm ls --all` показывает единственную копию `typescript@6.0.2` (`deduped` у `openapi-typescript`), других отклонений от peer-ограничений нет.
- **Генерация из реального `openapi/openapi.json`** (OpenAPI 3.1.1, `paths: {}`): `openapi-typescript ../openapi/openapi.json -o src/api/schema.d.ts` — код выхода 0, результат повторяется побайтово; `tsc --noEmit --strict --ignoreConfig src/api/schema.d.ts` — код 0. Реальный документ пока пуст, поэтому он не проверяет разбор схем.
- **Генерация из синтетического документа формы .NET 10** (OpenAPI 3.1.1: `type: ["null","string"]`, `type: ["null","integer","string"]` с `pattern` у `ProblemDetails.status`, `oneOf: [{type: null}, {...}]`, `format: uuid/date/date-time/int32`, `enum`, `additionalProperties` с массивами у `errors`, заголовок ответа `ETag`, заголовочный параметр `If-Match`, ответы 204/400/401/404/412/428 `application/problem+json`): код выхода 0, вывод детерминирован (две генерации побайтово равны), `tsc` без `skipLibCheck` — 0; `createClient<paths>` (openapi-fetch 0.17.0) выводит типы: `null | string` для nullable, объединение `Role`, обязательный `params.header["If-Match"]`, `error` как объединение `ProblemDetails`. Синтетический документ — только проверка формы, в репозиторий не добавлялся.
- **`--check`**: актуальный файл — код 0; файл с лишней строкой — код 1 и `Generated types are not up-to-date!`; контракт изменён, файл не перегенерирован — код 1; файл с CRLF вместо LF — код 1; файл отсутствует — код 1 (необработанное `ENOENT`, не сообщение о расхождении).
- Флаги генерации не заданы; `--read-write-markers` не включён.
- Не покрыто: поведение на реальном документе с эндпойнтами — перепроверить `check:api`/`tsc` при первой регенерации (Task 4); `@hey-api/openapi-ts` не оценивался.
