---
name: web-ui-test-and-form-gotchas
description: web/ UI facts from Task 7 - jsdom has no ResizeObserver, router singleton vs vi.resetModules, openapi-fetch captures fetch early, antd Form.Item injects id, biome ci diff trick
metadata:
  type: project
---

Facts that cost a round while building the SPA shell and users screens (sprint 001, Task 7), not derivable from the code:

- A throwaway Vitest run of the whole app needs `globalThis.ResizeObserver` stubbed (jsdom has none; antd Menu/Table throw inside `SingleObserver`, which React Router reports as the generic RouteErrorPage). `src/test/setup.ts` does not provide it.
- `vi.resetModules()` + dynamic import of the router gives the app a second copy of React while `@testing-library/react` keeps the first, so every test after the first fails with the error page. Import `router`/`Providers`/`queryClient` statically once, call `queryClient.clear()` and `await router.navigate(path)` per test.
- `openapi-fetch` binds `globalThis.fetch` when `createClient` runs (module load of `api/client.ts`), so a fetch stub must be installed in `vi.hoisted(...)`, with a mutable handler map behind it.
- antd `Form.Item` injects `id`, `value`, `onChange` into its single child. A custom control (here `EmployeePicker`) must forward `id` to the real input, otherwise the label has no associated control (testing-library: "no form control was found associated to that label").
- `npm run lint` cannot take arguments (they go to the typecheck step). For Biome format/import-order diffs run `npx biome ci --colors=off --error-on-warnings <paths>` inside `web/` and hand-apply the shown diff; its import sort puts `Avatar, theme as antdTheme, Button` (not by the alias).
- A Vite dev server started for a curl check: find the PID with `netstat -ano | grep :<port>` and stop it with `taskkill //PID <pid> //F //T`.

**Why:** each was found only by running the throwaway scenarios or the linter.

**How to apply:** any later task writing or fixing `web/` UI, or a throwaway scenario run against the real router (see also [[web-toolchain-windows-notes]]).
