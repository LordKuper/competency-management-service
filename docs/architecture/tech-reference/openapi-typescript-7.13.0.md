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
- Last verified: 2026-10-05 (dist-tag `latest = 7.13.0`; `next = 7.0.0-rc.1`; новее 7.13.0 релизов не найдено)

## API surface used in project
- CLI: `npx openapi-typescript ./openapi.json -o ./src/api/schema.d.ts` — генерация типов `paths`/`components` из JSON-документа OpenAPI, который .NET 10 строит при сборке backend (`Microsoft.AspNetCore.OpenApi` + `Microsoft.Extensions.ApiDescription.Server`, OpenAPI 3.1; YAML при сборке .NET 10 не поддерживается, см. `microsoft-extensions-apidescription-server-10.0.12.md`). Поддерживаются OpenAPI 3.0 и 3.1; путь и имя файла — по настройке сборки backend (пример имени — иллюстрация).
- `--check` — проверка актуальности сгенерированного файла в CI (по CLI-документации: «verify types are current»; код выхода при расхождении проверить в spike).
- Флаги, влияющие на результат (применение — решение design/реализации): `--export-type`, `--immutable`, `--enum`/`--enum-values`, `--root-types`, `--path-params-as-types`, `--alphabetize`, `--exclude-deprecated`, `--default-non-nullable` (**по умолчанию `true`**: объекты со значением `default` считаются non-nullable, кроме параметров), `--read-write-markers` (7.13: `$Read<T>`/`$Write<T>`).
- Выход — `.d.ts`, на который ссылается `createClient<paths>` (openapi-fetch). Решение, коммитить ли сгенерированный файл или генерировать в сборке, в стеке не зафиксировано.

## Version-specific notes
- 7.x: TypeScript — **peer-зависимость** и используется как рантайм-библиотека (Node API возвращает AST `ts.Node[]`, генерация через `ts.factory`). Поэтому генератор чувствителен к версии компилятора.
- 7.13.0: `--read-write-markers`; 7.12.0: сохранение регистра root-типов, исправление `enumValues` для `oneOf/anyOf`; 7.11.0: условные TS-enum; 7.10.0: хук `transformProperty`, `patternProperties`.
- Ключевое наблюдение: peer `^5.x` **не** включает 6.0.2 → без обхода `npm install` завершается ошибкой ERESOLVE. Фактическая работоспособность с 6.0.2 подтверждена только сообщениями сообщества (PR #2774: обходы `npm overrides`/`--legacy-peer-deps`, проблема — именно ограничение peer, а не рантайм); нами не проверялась.
- Поддержка TypeScript 6 в upstream не выпущена на 2026-10-05: PR #2774 «add TypeScript 6 support» (peer `^5.x || ^6.x`) открыт с 2026-04-15; PR #2872 «Support TypeScript 6 and 7 without a runtime compiler dependency» (мажорный релиз: `openapiTS()` возвращает `Promise<string>`, TypeScript не peer; тесты на 5.9.3/6.0.3/7.0.2) открыт, ждёт ревью.
- С TypeScript 7.0.2 генератор не работает (`Cannot read properties of undefined (reading 'createKeywordTypeNode')`, issue #2841).

## Deprecations and breaking changes from prior version
6.x → 7.0.0 (для справки): TypeScript стал peer-зависимостью и должен ставиться рядом; Node API возвращает AST TypeScript вместо строк; флаги авторизации удалены (теперь `redocly.yaml`); `--immutable-types` → `--immutable`, `--support-array-length` → `--array-length`. OpenAPI 2.x — только в 5.x и ранее. 7.12 → 7.13: breaking не заявлено.

## Project conventions
- Генерация только из JSON-документа OpenAPI, построенного при сборке backend; DTO руками не пишутся; ручная правка сгенерированного файла запрещена.
- Установка `typescript` — только точной версией: `npm i -D -E typescript@6.0.2` (npm `latest` = 7.0.2, с которым генератор не работает — issue #2841); openapi-typescript — тоже точной версией `7.13.0`.
- `tsconfig`: `moduleResolution: bundler`, `noUncheckedIndexedAccess: true` (рекомендации документации).
- Запасной вариант по stack.html — `@hey-api/openapi-ts 0.99.0` (не принят; документ не создаётся до принятия).

## Known issues and workarounds
- **Peer-конфликт `typescript ^5.x` vs 6.0.2 (известный риск в stack.html rev. 7; spike в реализации; манифест 7.13.0 повторно сверён 2026-10-05: peer `typescript ^5.x`, без `peerDependenciesMeta`).** Варианты обхода: (1) точечный `overrides` в корневом `package.json` для пакета `openapi-typescript` (npm: overrides читаются только из корня; допускается ссылка `"$typescript"` на спецификацию прямой зависимости); обход не воздействует на остальные peer-проверки — предпочтительный; (2) `legacy-peer-deps=true` в `.npmrc` — отключает peer-контроль для всего дерева (react-router, antd, TanStack Query), не рекомендуется документацией npm; тот же флаг обязан быть и у `npm ci`. Принятие обхода — решение design (spike по stack.html).
- Критерии spike: сгенерировать типы из реального JSON-документа OpenAPI (результат сборки backend) на 6.0.2, прогнать `tsc --noEmit` по результату, убедиться в стабильности `--check`; не включать `--read-write-markers` до исправления (в TS 6 `Date extends object` стало `true`, что затрагивает `Readable<T>`/`Writable<T>` по описанию PR #2774).
- Корректность разбора схемы .NET 10 (OpenAPI 3.1: `type: [..., "null"]`, перечисления, `oneOf`) на реальных данных не проверялась.
- Запасной путь при провале spike — `@hey-api/openapi-ts` (версия до 1.0, закрепление точной версии) либо ожидание релиза PR #2872.
