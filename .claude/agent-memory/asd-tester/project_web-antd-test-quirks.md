---
name: web-antd-test-quirks
description: antd traps in the web vitest suite - duplicate modal title ids, empty-state text, closed modals staying in the DOM
metadata:
  type: project
---

In `web` component tests with antd:
- Every modal gets the same title id (`test-id`), so `findByRole("dialog", { name })` finds the wrong dialog or none; find the title text (`.ant-modal-confirm-title`) and `closest('[role="dialog"]')`.
- A closed modal stays in the DOM (jsdom, no animation end), so a second modal of the same title is ambiguous; split cancel and confirm into separate tests.
- `Empty` puts "Нет данных" in the SVG `<title>` of its image even with a custom description; assert `.ant-empty-description` text, never `queryByText("Нет данных")` to be null.
- Dropdown menu items are in the DOM only after the trigger is clicked; they carry `role="menuitem"`.

**Why:** each cost a failed run while writing the row-actions tests.
**How to apply:** any test of a confirm dialog, an empty table or a «…» menu in `web`.
