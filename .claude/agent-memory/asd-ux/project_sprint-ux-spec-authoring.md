---
name: project-sprint-ux-spec-authoring
description: How sprint ux-spec drafts are authored in this repo (class set to reuse, link depth, shell head, no python) and the counter-badge token decision of sprint 004
metadata:
  type: project
---

- Sprint ux-spec (`.asd/sprints/<id>/design/ux-spec.html`) = `t_html-shell.html` head copied from `docs/ux/app-shell.html` + one `<div class="ds">` with a scoped `<style>` (the compact class set of `docs/ux/career-framework.html`: `.pg .row .stack .cols .btn .lnk .tag .card .t .inp .alert .scrim .modal .chip .mx .dot`). Reuse it, add only what is missing; raw px/hex only in the `.ds{--ds-*}` root.
- Links from `design/` to repo docs are four levels up (`../../../../docs/ux/...`, `../../../../web/public/brand/logo-64.png`), not three.
- Shell has no python in this environment (the `python3` stub fails); use `node -e` for tag-balance and contrast checks.
- Existing letters are plain text (`MailSender` sends `TextFormat.Plain`): draft letters as plain text with a bare URL, not HTML layout.
- Sprint 004 decision: the header counter badge is composed from existing tokens (`accent` fill, `surface-inverse` text 9.59:1, `rounded-full`, `body-strong`, `control-height-sm`), no component token. Design-review iter 1 (2026-10-13): DESIGN.md doc notes (contrast row, horizontal Steps) need a carrier, so `design-md-delta.yaml` exists with documentation-only `update` entries (values unchanged); promote reads it. Contrast of `accent` on the `primary` active nav item is 3.73:1 (not in DESIGN.md's contrast table).

- Delta format (iter 2, 2026-10-13): `new_value` is always the real current token value (full map for composite tokens), never "unchanged"; `usage` = inline token comment; exact DESIGN.md table/row text for doc-only edits goes in a `doc_patch` list, `reason` = why. Publishing-action list lives only in PRD AC-34.4 / ADR-4 `publishingActions`: UX notes reference it, never re-list. Informational (`isReference`) desk groups get their own truncation text (no "выполните действия"). Route table home = `docs/architecture/desk.md`, no `href` to adr.html from ux-spec.

- Iter 3→4 (2026-10-13): 412 has two kinds in the spec: by `If-Match` (record changed → reread, "Общие состояния" 3) and with `publication` address (cascade changed/appeared → confirm window with fresh list + `alert-warning` «Перечень изменился — подтвердите заново», state 3б). AC-34.11: field error «Выбрана черновая запись — сначала утвердите её» on the reference field in record edit windows (mock in m-publish-confirm). Do not renumber m-states items (other notes reference "макет 5"/"состояние 5"): add `3б`-style items.

**Why:** DESIGN.md already says `accent` may be a badge fill and `rounded-full` is for count badges; the logo plate in `app-shell.html` is the precedent for a composite element without its own token.
**How to apply:** never write "стоит дополнить DESIGN.md" in a spec without a delta entry (promote patches DESIGN.md only from the delta). Mockup example data must obey the PRD exclusivity rules (one object = one desk item; version in work suppresses published-matrix findings; the queue shows only entries the user may approve). At the sprint 004 design-promote add the 3.73:1 row to DESIGN.md Colors > Contrast and a horizontal-Steps note to the Steps row; reuse this approach for later composite elements before asking for a new token.
