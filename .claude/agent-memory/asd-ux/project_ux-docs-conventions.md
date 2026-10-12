---
name: project-ux-docs-conventions
description: Conventions and traps for persistent docs/ux/*.html in this repo (a11y scope, mockup style, tokens, items to re-verify in code)
metadata:
  type: project
---

- `docs/ux/accessibility.html` declares keyboard behaviour, ARIA and screen readers OUT of scope by customer decision (2026-10-05). Any focus/keyboard rule is a scope extension: return proposed text in Flagged choices, do not edit (asd-ux must not write a11y rules). User decision 2026-10-12: do not extend accessibility.html; focus and keyboard-drag rules live only in the screen UX docs, with no links to accessibility.html.
- Persistent ux docs wrap content in `<div class="ds">` with `--ds-*` token variables (hex only at that root, primitive layer); mockups reference `var(--ds-*)` only. org-structure.html uses inline styles plus a scoped `o-*` style block; career-framework.html uses a scoped class set. No sprint/task ids; no AC numbers for new subsystems.
- Re-verify in code before documenting, as these were slated for a code fix: (a) whether `web/src/app/theme.ts` sets `Menu.darkPopupBg` (antd default `#001529` is off-palette; intended is `surface-inverse`) for the "Администрирование" submenu; (b) how many inline row-action links career/positions tables show (DESIGN.md allows at most two visible, the rest in a Dropdown); (c) empty-state text of the scales table and the nested tracks table (should not be antd's default "Нет данных").

**Why:** found during a design-promote; they recur on any later UX promote.
**How to apply:** check before writing a11y text, new mockups, or reviewing shell/menu visuals; then update `career-framework.html` if the code changed.
