---
name: web-org-card-hierarchy-facts
description: Org-structure card hierarchy (Task 12) - antd render cost for thousands of cards, connector CSS offsets, headless-Chrome CDP mock harness pitfalls, antd menu icon names in tests
metadata:
  type: project
---

Facts that cost a round while building the card hierarchy in `web/src/features/org-structure`, not derivable from the code:

- **antd is the cost, not React.** 2000 employee cards built from antd `Card`/`Avatar`/`Flex`/`Typography` re-expanded in ~950 ms (script 636 ms, prod build, headless Chrome); the same card as plain markup styled by `var(--ant-*)` took ~330 ms (script 90 ms). Units are few, so they stay antd; employee cards are plain divs. `Skeleton`-style `Card loading` is fine for the single loading row.
- A memoized card only skips work if every callback it gets is stable: `useUnitActivation` had to return a `useCallback` (`mutateAsync` and `App.useApp()` `modal`/`message` are stable); otherwise one toggle re-rendered 300 cards (75 ms script vs 12 ms).
- Connector pseudo-elements are positioned from the card's padding box: with a 1px border, a `::before` on the card needs `- var(--ant-line-width)` on left/top/width/height or it draws a second line 1px beside the `li::before` line.
- antd icons carry `role="img" aria-label="edit"`, so a menu item reads "edit Править"; testing-library needs `name: /Править/`, and a toggle button given a visible icon needs an explicit `aria-label` to keep the unit name clean.
- jsdom: creating a unit through the card flow (modal, POST, tree refetch, address change) exceeds testing-library's default 1000 ms `findBy` timeout; give it `{ timeout: 4000 }`. First vitest run on this box took ~3 min (import 70 %).
- CDP mock harness (headless Chrome, `Fetch.enable` on `<base>/api/*`, `Fetch.fulfillRequest` with base64 body): launch Chrome from node with `taskkill /PID <pid> /F /T` (single slash; `//PID` is only for Git Bash) or the process never exits; run long scripts with a watchdog `setTimeout(process.exit)` and redirect output to a file (a `| tail` pipe shows nothing until exit); `Page.captureScreenshot` `clip` is in document coordinates, so add `window.scrollY`.
- The user's VS-launched Vite on 5173 disappeared during the run (not killed by me: only my own PIDs 5900/5901 and scratch Chrome trees were stopped); do not assume it is up when verifying.

**Why:** each was found by measuring or by a failed scratch run.

**How to apply:** any later list-heavy `web/` UI, or a headless-Chrome check of the app (see also [[web-ui-test-and-form-gotchas]], [[scratch-process-control]]).
