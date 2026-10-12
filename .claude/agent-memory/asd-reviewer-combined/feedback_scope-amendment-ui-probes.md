---
name: scope-amendment-ui-probes
description: probes for a small web/ scope-amendment delta (row-actions menu, empty-state texts, theme tokens) that found the real low findings in about 25 turns
metadata:
  type: feedback
---

On a small UI amendment delta (a new shared row-actions component, `locale.emptyText`, theme tokens):
- Compare every JSDoc example of a label or aria-label prop with the plan's approved text and the actual caller; an example
  copied from an older sibling (`UserListPage` «Действия с пользователем …») contradicts the approved format.
- A new `<Component>.test.tsx` that also tests whole tables or tabs: Grep the helper names (`serveRole`, `const page = (`)
  across `web/src`; copied helpers plus a sibling suite with the same describe theme (`catalogueTables.test.tsx`) is a low
  Best practices finding (move the blocks, keep the component file for the component).
- Check the "other lists" clause of the task: Grep `type="link"` and `<Space size={0}` over `web/src` and count links per row.
- Verify theme token names in `web/node_modules/antd/es/<component>/style/index.d.ts`.
- A shared component with one caller that separates a design-doc layout rule from rights logic and is tested on its own is
  keep-as-is, not "abstraction with no second use case".
- Re-review of a test-move fix: diff the moved fixture against the target file's base helper (ids, version, pageSize behind
  `If-Match` and routes) and Grep the moved helper names for local shadows in older tests. A shadow is low only, so it drops
  under a medium floor; such a delta takes about 10 turns.

**Why:** these were the only defects in such a delta; the rest was clean.
**How to apply:** right after the diff read; see also [[frontend-wave-probes]].
