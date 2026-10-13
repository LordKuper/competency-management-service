---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), commands.yaml (commands); decisions fold per sprint-lifecycle.md "Design-promote phase" fold rule into whichever persistent doc's owns matches
---

# @tanstack/react-query @ 5.104.1

## Canonical source
- Official docs: https://tanstack.com/query/latest/docs/framework/react/overview
- Миграция v4→v5: https://tanstack.com/query/latest/docs/framework/react/guides/migrating-to-v5
- Релизы: https://github.com/TanStack/query/releases
- npm: https://www.npmjs.com/package/@tanstack/react-query (манифест 5.104.1: peer `react ^18 || ^19`, dependency `@tanstack/query-core 5.104.1` (точная), без `engines`)
- `MutationCacheConfig`: https://tanstack.com/query/v5/docs/framework/react/reference/interfaces/MutationCacheConfig (страница `…/docs/reference/MutationCache` 2026-10-13 отвечала HTTP 500; порядок вызова — по `packages/query-core/src/mutation.ts`, ветка `main`, а не тег 5.104.1)
- Гайды: https://tanstack.com/query/v5/docs/framework/react/guides/window-focus-refetching, https://tanstack.com/query/v5/docs/framework/react/guides/query-invalidation, https://tanstack.com/query/v5/docs/framework/react/guides/mutations; образец глобальной инвалидации — https://tkdodo.eu/automatic-query-invalidation-after-mutations (блог мейнтейнера)
- Last verified: 2026-10-05 (dist-tag `latest = 5.104.1`); 2026-10-13 — `MutationCache`, `refetchOnWindowFocus`, инвалидация

## API surface used in project
- `QueryClient`, `QueryClientProvider` — один клиент на приложение.
- `useQuery({ queryKey, queryFn, ... })`, `useMutation({ mutationFn, onSuccess, ... })`, `useQueryClient`, `queryClient.invalidateQueries({ queryKey })`.
- Хелперы `queryOptions()` / `mutationOptions()` — переиспользуемые типизированные конфигурации поверх вызовов openapi-fetch; `skipToken` — типобезопасное отключение запроса.
- Инвалидация после мутации: `onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['…'] }) }` — возврат Promise держит `isPending` до обновления данных.
- `new QueryClient({ mutationCache: new MutationCache({ onSuccess }) })` — глобальный колбэк успешной мутации (счётчик рабочего стола). Сигнатура `onSuccess(data, variables, onMutateResult, mutation, context)`; `onError`, `onSettled`, `onMutate` — та же форма.
- `refetchOnWindowFocus` (`useQuery`, `defaultOptions.queries`), `focusManager` — перечитывание при возврате на вкладку; `focusManager` уже используется в тестах (`OrgStructurePage.test.tsx`).
- Не используется: SSR/hydration, persisters, `@tanstack/react-query-devtools` (отдельный пакет, в stack.html не входит — не добавлять без Complication Approval).

## Version-specific notes
- Требования: React 18+, TypeScript ≥5.6 (политика DefinitelyTyped, 2 года); типовые изменения выходят в патчах — версию держать точной (так и закреплено в stack.html).
- Умолчания: `staleTime: 0`, `gcTime` 5 мин, `retry: 3` с экспоненциальной паузой, refetch при монтировании/фокусе окна/восстановлении сети; structural sharing включён.
- Колбэки `MutationCache` вызываются для каждой мутации кэша, кто бы её ни запустил, и не зависят от компонента; колбэки `mutate()`, наоборот, не срабатывают, если компонент размонтирован до конца мутации. Порядок на успехе: `MutationCache.onSuccess`, затем `onSuccess` мутации; то же у `onSettled`. Каждый вызов ожидается (`await`): вернувший Promise колбэк держит мутацию в `pending`, пока Promise не завершится.
- `refetchOnWindowFocus` по умолчанию `true` и перечитывает только устаревшие (`stale`) запросы; при умолчании `staleTime: 0` — любой. Обработчик по умолчанию слушает `visibilitychange`, а не `focus`; в Chrome DevTools режим «Emulate a focused page» подавляет это событие.
- `invalidateQueries({ queryKey })` совпадает по префиксу (`['desk']` задевает `['desk', …]`; `exact: true` — только точный ключ), помечает запросы устаревшими и сразу перечитывает только отрисованные; остальные перечитаются, когда их снова начнут использовать.
- Ошибка по умолчанию типизируется как `Error`; изменить глобально можно через `declare module '@tanstack/react-query' { interface Register { defaultError: … } }`.
- На 2026-10-05 для React-адаптера актуальна ветка 5.x (dist-tag `latest`); ветка 6.x на странице релизов упоминается для отдельных адаптеров — на React не распространять.

## Deprecations and breaking changes from prior version
v4 → v5 (проект на v4-идиомах не пишется):
- только сигнатура-объект: `useQuery({ queryKey, queryFn })`;
- `status: 'loading'` → `'pending'`, `isLoading` → `isPending`; новый `isLoading` = `isPending && isFetching`;
- `cacheTime` → `gcTime`;
- `onSuccess`/`onError`/`onSettled` у **запросов** удалены (у мутаций остались);
- `keepPreviousData` → `placeholderData` (функция-идентичность `keepPreviousData`);
- `useSuspenseQuery`/`useSuspenseInfiniteQuery`/`useSuspenseQueries` стали стабильными.

## Project conventions
- Запросы строятся на openapi-fetch: `queryFn` вызывает `client.GET(...)`; при `error` — бросает типизированную ошибку (иначе TanStack Query посчитает запрос успешным, т.к. openapi-fetch на 4xx/5xx не бросает).
- ETag/`If-Match` (stack.html): ETag приходит заголовком ответа, поэтому `queryFn` должен возвращать его вместе с `data`; мутации передают `If-Match`; при конфликте версии (412/409) — `invalidateQueries` и сообщение пользователю. Контракт заголовков — в документе OpenAPI (JSON, строится при сборке backend).
- Глобальная обработка ошибок (в том числе 401) — через `QueryCache`/`MutationCache` `onError`, а не колбэки `useQuery`.
- Для 4xx (401/403/409/412) повторы по умолчанию (`retry: 3`) бессмысленны — задать функцию `retry` (рекомендация; решение design).
- Ключи запросов — по ресурсам/фабрике ключей, единый для инвалидации; `queryClient` создаётся один раз вне компонентов (`web/src/api/queryClient.ts`).
- Счётчик и стол (`docs/architecture/desk.md`) — ключи с префиксом `['desk']`:
  - `MutationCache.onSuccess` в `queryClient.ts` сбрасывает `['desk']` после любой успешной мутации. Promise инвалидации колбэк не возвращает: иначе каждая мутация оставалась бы `pending`, пока сервер пересчитывает стол;
  - запросы `['desk']` оставляют `staleTime: 0` и не выключают `refetchOnWindowFocus`: часть существующих запросов выключает его (`careerApi.ts`, `LIST_REREAD` в `orgStructureApi.ts`), и это нельзя переносить на стол;
  - `refetchInterval` не используется.

## Known issues and workarounds
- Не выявлено. Типы могут уточняться между патчами — обновление версии только через спринт с повторной проверкой `tsc --noEmit`.
