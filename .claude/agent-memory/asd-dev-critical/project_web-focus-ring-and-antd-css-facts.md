---
name: web-focus-ring-and-antd-css-facts
description: Overriding antd focus and shimmer styles from the stylesheet - specificity vs !important and Biome, custom theme tokens as CSS variables, rc-dropdown keyboard model, focus loss on an element swap, keyboard checks in headless Chrome without a backend
metadata:
  type: project
---

Facts that decided the focus-ring work in `web/`, checked in headless Chrome, not derivable from the code:

- **antd's own focus rules beat any plain global rule**: Button is `.ant-btn:not(:disabled):focus-visible` (0,3,0, offset 1px), Menu items and Dropdown items (0,4,0), fields draw a glow and set `outline: 0`, the Skeleton shimmer is `.ant-skeleton-active .ant-skeleton-*` (0,3,0). A `:focus-visible` rule overrides them only with `!important`; Biome (`--error-on-warnings`) flags `!important` (`lint/complexity/noImportantStyles`), so wrap the block in `/* biome-ignore-start lint/complexity/noImportantStyles: <reason> */` ... `/* biome-ignore-end lint/complexity/noImportantStyles: <reason> */` (both comments need `: reason`).
- **A field's focused element is its inner `<input>`**: put the ring on the wrapper with `.ant-input-affix-wrapper:has(input:focus-visible)` and `.ant-select:has(input:focus-visible)` (TreeSelect is an `.ant-select`) and set `outline: none !important` on the inner input.
- **Every key of the theme `token` becomes a `--ant-<kebab-name>` CSS variable** (cssinjs `transformToken`; numbers get `px`), custom keys included, so a token antd has no counterpart for (`colorAccent`, `focusRingOffset`, `motionEaseIn`) is published by spreading a plain object into `token` (an explicit unknown key fails the `Partial<AliasToken>` type check; antd 6.6.5 has no `motionEaseIn`). The variables are defined on the `.ant-app` root and antd roots, not on `body`/`:root`.
- **rc-dropdown keyboard model**: with `trigger=["click"]` and the default `autoFocus: false`, Enter/Space on the trigger opens the menu with focus kept on the trigger; Tab moves into the menu (first item active), arrows move, Enter chooses, Esc closes and returns focus to the trigger. The arrows do nothing while focus is still on the trigger.
- **Swapping the root element type of a component remounts it and drops focus to `body`** (a lazy plain `<button>` replaced by `<Dropdown><button/></Dropdown>`). Biome `a11y/noAutofocus` flags `autoFocus` on a DOM element; use a stable module-level ref callback (`ref={isNow ? focusWhenMounted : undefined}`).
- **A confirm or dialog opened from a menu item returns focus to `body` when it closes** (antd restores focus to the menu item, which is gone); not fixed, only seen.
- **Skeleton shimmer ignores the `motion: false` token**; it needs a stylesheet rule under `@media (prefers-reduced-motion: reduce)`. `getComputedStyle(el).animationName` of every `.ant-skeleton-active *` shows it (`ant-skeleton-loading` vs `none`).
- **Keyboard check with no backend**: Node 24's global `WebSocket` talks CDP directly (no `ws` package); `Fetch.enable` on `<base>/api/*` plus `Fetch.fulfillRequest` serves mock JSON; `Input.dispatchKeyEvent` (`rawKeyDown`, `char` with `text` for Enter/Space, `keyUp`) gives real Tab/Enter/arrow/Esc, and `:focus-visible` matches after such keys; `Emulation.setEmulatedMedia` with `prefers-reduced-motion` must be set before the navigation because the theme reads the media query at module load. Wait ~450 ms before reading outlines (antd transitions them).

**Why:** each cost a failed run or a wrong first fix while making keyboard focus match the design system.
**How to apply:** any change to global focus, ring or motion CSS, antd Dropdown/Menu keyboard behaviour, or a browser check of `web/` without the API (see also [[web-cdp-real-backend-smoke-facts]], [[web-org-card-hierarchy-facts]]).
