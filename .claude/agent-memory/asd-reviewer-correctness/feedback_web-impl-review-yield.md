---
name: web-impl-review-yield
description: Product impl-review of the web SPA (React + antd) in this app - the defect shapes that paid off and how to map more findings than rubric rows in the ledger
metadata:
  type: feedback
---

In a web impl-review wave, these checks found real defects that the diff alone did not show:

- **Unsaved flags derived from `Form.useWatch`.** The watched field is `""` after mount (from `initialValues`). A comparison with `current?.name`, which is `undefined` for a new record, makes a fresh "new" dialog read as unsaved. Then the leave guard asks on ×/Esc and `beforeunload` turns on.
- **Disabled controls with no reason next to them.** DESIGN.md `components.input-disabled` says "reason shown as help text". Compare every newly disabled field with its sibling branch, which often has the `extra` text.
- **Removed concepts that survive.** After a scope change removes something (categories, "нет требований"), grep `web/src` and `.claude/agent-memory/**` for its identifiers. Look in test fixtures (context lines of a hunk), client param types, and new memory files that name code identifiers that no longer exist in `src/`.
- **Raw px in CSS `@media`.** A media query cannot take a token, but design-system §6 still applies. The codebase pattern is a JS breakpoint (`Grid.useBreakpoint()`, antd `Row`/`Col`).

**Ledger mapping:** each rule row carries one `f`. When there are more findings than rubric rows, hang the extra ones on the custom-rule rows they really fit:
- `custom-common-rules.md` for obsolete surface left behind;
- `custom-coding-rules.md` "UI check against DESIGN.md" for DESIGN.md component-table gaps.

**Why:** a cross-file shape like these passes lint and tests, so only a targeted grep finds it.

**How to apply:** run these after the diff pass, before writing the ledger. Related: [[review-method-no-shell]], [[check-persistent-ac-text-before-raising]].
