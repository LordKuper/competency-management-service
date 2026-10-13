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
- Sprint 004 decision: the header counter badge is composed from existing tokens (`accent` fill, `surface-inverse` text 9.59:1, `rounded-full`, `body-strong`, `control-height-sm`), no component token, no `design-md-delta.yaml`. Contrast of `accent` on the `primary` active nav item is 3.73:1 (not in DESIGN.md's contrast table).

**Why:** DESIGN.md already says `accent` may be a badge fill and `rounded-full` is for count badges; the logo plate in `app-shell.html` is the precedent for a composite element without its own token.
**How to apply:** at the sprint 004 design-promote add the 3.73:1 row to DESIGN.md Colors > Contrast and a horizontal-Steps note to the Steps row; reuse this approach for later composite elements before asking for a new token.
