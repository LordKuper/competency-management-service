---
name: web-org-structure-ui-facts
description: antd Tree/TreeSelect/Table behaviours in jsdom and shared-worktree lint for throwaway tests, from the org-structure screens (sprint 001, Task 8)
metadata:
  type: project
---

Facts that cost a round while building `web/src/features/org-structure`, not derivable from the code:

- antd `Tree` with `height` (virtual) and `TreeSelect` render their nodes in jsdom with only the `ResizeObserver` stub (see [[web-ui-test-and-form-gotchas]]); `role="tree"` takes its name from the `aria-label` prop.
- An antd `Table` renders its header text twice in jsdom (a hidden measure row), so `getByText("<column title>")` throws "multiple elements"; use `getAllByText`.
- In a test, pressing Escape with a `TreeSelect` dropdown open inside a `Modal` made the later submit click a no-op (the Modal evidently closed with the dropdown); close the dropdown by clicking the modal title instead.
- The `openapi-fetch` client calls the stubbed `fetch` with one `Request`, so a stub reads `request.method`, `new URL(request.url)`, `request.headers.get("If-Match")` and `await request.clone().text()`.
- A rc-field-form `Form` remounted with `key` but the same `useForm` instance keeps the user's typed values (store beats `initialValues`); to reset a form to server data after a 412 refetch, key the component that owns `Form.useForm()`, as `UserCard` does, not just the `<Form>`.
- Biome lints any `*.test.tsx` under `web/`, so a throwaway scenario file in the shared worktree must be biome-clean or it fails a sibling's `npm run lint` while it exists.
- `modal.confirm({ onOk })` must resolve to void/boolean: `.then(ok, (e) => modal.error(...))` fails `tsc` because `modal.error(...)` returns a modal handle; wrap the handler body in braces.

**Why:** each was found by the throwaway Vitest run or `tsc`.

**How to apply:** any later `web/` task using Tree, TreeSelect, Table or Modal with forms, or writing throwaway scenarios in a shared worktree.
