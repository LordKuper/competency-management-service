---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# openapi-fetch @ 0.17.0

## Canonical source
- Official docs: https://openapi-ts.dev/openapi-fetch/ ; API: https://openapi-ts.dev/openapi-fetch/api ; middleware: https://openapi-ts.dev/openapi-fetch/middleware-auth
- Changelog: https://raw.githubusercontent.com/openapi-ts/openapi-typescript/main/packages/openapi-fetch/CHANGELOG.md ; релизы: https://github.com/openapi-ts/openapi-typescript/releases
- npm: https://www.npmjs.com/package/openapi-fetch (манифест 0.17.0: единственная dependency `openapi-typescript-helpers ^0.1.0` (последняя 0.1.0), без peer/engines; ESM `./dist/index.mjs` + CJS)
- Last verified: 2026-10-05 (dist-tag `latest = 0.17.0`)

## API surface used in project
Типизированный тонкий клиент поверх нативного `fetch`; типы — из JSON-документа OpenAPI (собирается backend) через openapi-typescript (тип `paths`).
- `createClient<paths>({ baseUrl, headers?, fetch?, querySerializer?, bodySerializer?, pathSerializer? })`. `baseUrl` — относительный (same-origin); префикс `/api/v1` задаётся либо в путях документа OpenAPI, либо в `baseUrl` — не дублировать.
- Методы `client.GET/POST/PUT/PATCH/DELETE(path, { params: { path, query, header }, body, headers, signal, parseAs })` → `{ data, error, response }`: `data` — 2xx, `error` — 4xx/5xx, `response` — исходный `Response` (статус и заголовки, в т.ч. `ETag`).
- Middleware: `client.use({ onRequest({ request, schemaPath, options }), onResponse({ request, response, options }), onError({ error }) })`, `client.eject(mw)`; `onRequest` вызываются в порядке регистрации, `onResponse` — в обратном; `onError` — только сетевые ошибки (4xx/5xx обрабатывать в `onResponse`); тело ответа перед чтением клонировать (`response.clone()`).
- Не используется: `wrapAsPathBasedClient()` (Proxy, накладные расходы).

```ts
const client = createClient<paths>({ baseUrl: "/" });
const { data, error, response } = await client.GET("/…/{id}", { params: { path: { id } } });
const etag = response.headers.get("ETag"); // для If-Match в мутации
```

## Version-specific notes
- Ветка 0.x: между минорами бывают breaking changes — версия закреплена точно.
- 0.17.0: поддержка маркеров `$Read<T>`/`$Write<T>` (флаг `--read-write-markers` openapi-typescript 7.13) и исправление: `Content-Length: 0` не считается пустым ответом при `Transfer-Encoding: chunked`.
- 0.16.0: пользовательские сериализаторы путей (глобально и на запрос); 0.15.0: middleware на уровне запроса; 0.14.0: пакет пересобран unbuild, минифицированная сборка удалена.
- Документация требует `noUncheckedIndexedAccess: true` и `tsc --noEmit` в CI.

## Deprecations and breaking changes from prior version
Накопленные breaking changes линии (для справки):
- 0.13.0: ответ `204` или с `Content-Length: 0` даёт `data === undefined` (раньше пустой объект); middleware — объектный API (`onRequest/onResponse/onError`);
- 0.12.0: `Content-Type` больше не выставляется автоматически на запросах без тела;
- 0.14.0: убрана минифицированная сборка.
0.16.0 → 0.17.0: breaking не заявлено.

## Project conventions
- Клиент создаётся один раз (модуль `api`), общий для TanStack Query.
- Единая точка обработки 401/сетевых ошибок — middleware (`onResponse`/`onError`).
- Клиент **не бросает** на 4xx/5xx: `queryFn`/мутации обязаны проверять `error` и бросать типизированную ошибку.
- Типы ошибок берутся только из ответов, объявленных в документе OpenAPI (backend обязан декларировать 4xx/ProblemDetails).
- Cookie-сессия: fetch по умолчанию `credentials: 'same-origin'` — достаточно при том же origin (SPA и API на одном Kestrel); antiforgery-заголовок, если потребуется, — через middleware (design).
- ETag/`If-Match`: `If-Match` передаётся как `params.header` (если объявлен в OpenAPI) либо `headers`.

## Known issues and workarounds
- Версия `openapi-typescript-helpers` разрешается как `^0.1.0` (0.1.0). Сгенерированные типы и helpers зависят от поведения TypeScript: PR openapi-typescript#2774 описывает изменение в TS 6 (`Date extends object` теперь `true`), влияющее на `Readable<T>`/`Writable<T>` (актуально при `--read-write-markers`); PR открыт, исправление не выпущено — не включать `--read-write-markers` без spike на TypeScript 6.0.2.
